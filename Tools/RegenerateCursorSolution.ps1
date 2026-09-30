# Regenerates SDK-style UnityPD_Hans.Cursor.sln from Unity's generated csproj files.
# Unity's legacy csproj format crashes Cursor's C# language server (VS 18 MSBuild).
# Run after Unity regenerates project files, or rely on Assets/Editor/CursorSolutionRegenerator.cs.

$ErrorActionPreference = 'Stop'

function Convert-UnityCsprojToSdk {
    param(
        [string]$SourcePath,
        [string]$DestPath,
        [string]$OutputPath,
        [hashtable]$ProjectRefMap
    )

    [xml]$xml = Get-Content -Raw -LiteralPath $SourcePath
    $ns = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
    $ns.AddNamespace('msb', 'http://schemas.microsoft.com/developer/msbuild/2003')

    $lang = $xml.SelectSingleNode('//msb:LangVersion', $ns).InnerText
    $defines = $xml.SelectSingleNode('//msb:DefineConstants', $ns).InnerText
    $unsafeNode = $xml.SelectSingleNode('//msb:AllowUnsafeBlocks', $ns)
    $allowUnsafe = if ($unsafeNode) { $unsafeNode.InnerText } else { 'False' }
    $noWarnNode = $xml.SelectSingleNode('//msb:NoWarn', $ns)
    $noWarn = if ($noWarnNode) { $noWarnNode.InnerText } else { '0169' }

    $sb = New-Object System.Text.StringBuilder
    [void]$sb.AppendLine('<Project Sdk="Microsoft.NET.Sdk">')
    [void]$sb.AppendLine('  <PropertyGroup>')
    [void]$sb.AppendLine('    <TargetFramework>netstandard2.1</TargetFramework>')
    [void]$sb.AppendLine("    <LangVersion>$lang</LangVersion>")
    [void]$sb.AppendLine('    <EnableDefaultItems>false</EnableDefaultItems>')
    [void]$sb.AppendLine('    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>')
    [void]$sb.AppendLine('    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>')
    [void]$sb.AppendLine('    <GenerateTargetFrameworkAttribute>false</GenerateTargetFrameworkAttribute>')
    [void]$sb.AppendLine('    <DisableImplicitFrameworkReferences>true</DisableImplicitFrameworkReferences>')
    [void]$sb.AppendLine('    <GenerateDependencyFile>false</GenerateDependencyFile>')
    [void]$sb.AppendLine('    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>')
    [void]$sb.AppendLine('    <Deterministic>false</Deterministic>')
    [void]$sb.AppendLine("    <AllowUnsafeBlocks>$allowUnsafe</AllowUnsafeBlocks>")
    [void]$sb.AppendLine("    <NoWarn>$noWarn;CS8019</NoWarn>")
    [void]$sb.AppendLine("    <DefineConstants>$defines</DefineConstants>")
    [void]$sb.AppendLine("    <OutputPath>$OutputPath</OutputPath>")
    [void]$sb.AppendLine('  </PropertyGroup>')

    $compiles = $xml.SelectNodes('//msb:Compile[@Include]', $ns)
    if ($compiles.Count -gt 0) {
        [void]$sb.AppendLine('  <ItemGroup>')
        foreach ($c in $compiles) {
            $inc = $c.GetAttribute('Include')
            [void]$sb.AppendLine("    <Compile Include=`"$inc`" />")
        }
        [void]$sb.AppendLine('  </ItemGroup>')
    }

    $nones = $xml.SelectNodes('//msb:None[@Include]', $ns)
    if ($nones.Count -gt 0) {
        [void]$sb.AppendLine('  <ItemGroup>')
        foreach ($n in $nones) {
            $inc = $n.GetAttribute('Include')
            [void]$sb.AppendLine("    <None Include=`"$inc`" />")
        }
        [void]$sb.AppendLine('  </ItemGroup>')
    }

    $refs = $xml.SelectNodes('//msb:Reference[@Include]', $ns)
    if ($refs.Count -gt 0) {
        [void]$sb.AppendLine('  <ItemGroup>')
        foreach ($r in $refs) {
            $inc = $r.GetAttribute('Include')
            $hint = $r.SelectSingleNode('msb:HintPath', $ns)
            $priv = $r.SelectSingleNode('msb:Private', $ns)
            [void]$sb.AppendLine("    <Reference Include=`"$inc`">")
            if ($hint) { [void]$sb.AppendLine("      <HintPath>$($hint.InnerText)</HintPath>") }
            if ($priv) { [void]$sb.AppendLine("      <Private>$($priv.InnerText)</Private>") } else { [void]$sb.AppendLine('      <Private>False</Private>') }
            [void]$sb.AppendLine('    </Reference>')
        }
        [void]$sb.AppendLine('  </ItemGroup>')
    }

    $prefs = $xml.SelectNodes('//msb:ProjectReference[@Include]', $ns)
    if ($prefs.Count -gt 0) {
        [void]$sb.AppendLine('  <ItemGroup>')
        foreach ($p in $prefs) {
            $inc = $p.GetAttribute('Include')
            $nameNode = $p.SelectSingleNode('msb:Name', $ns)
            $name = if ($nameNode) { $nameNode.InnerText } else { [IO.Path]::GetFileNameWithoutExtension($inc) }
            if ($ProjectRefMap.ContainsKey($inc)) {
                $mapped = $ProjectRefMap[$inc]
                [void]$sb.AppendLine("    <ProjectReference Include=`"$mapped`" />")
            }
            else {
                $dll = "Library\ScriptAssemblies\$name.dll"
                [void]$sb.AppendLine("    <Reference Include=`"$name`">")
                [void]$sb.AppendLine("      <HintPath>$dll</HintPath>")
                [void]$sb.AppendLine('      <Private>False</Private>')
                [void]$sb.AppendLine('    </Reference>')
            }
        }
        [void]$sb.AppendLine('  </ItemGroup>')
    }

    [void]$sb.AppendLine('</Project>')
    [IO.File]::WriteAllText($DestPath, $sb.ToString())
    Write-Host "Wrote $DestPath ($($compiles.Count) compiles, $($refs.Count) refs)"
}

$root = if ($PSScriptRoot) { Split-Path $PSScriptRoot -Parent } else { (Get-Location).Path }

$unityRuntime = Join-Path $root 'Assembly-CSharp.csproj'
$unityEditor = Join-Path $root 'Assembly-CSharp-Editor.csproj'

if (-not (Test-Path -LiteralPath $unityRuntime)) {
    throw "Missing $unityRuntime. Open the project in Unity and regenerate project files first."
}
if (-not (Test-Path -LiteralPath $unityEditor)) {
    throw "Missing $unityEditor. Open the project in Unity and regenerate project files first."
}

Convert-UnityCsprojToSdk `
    -SourcePath $unityRuntime `
    -DestPath (Join-Path $root 'UnityPD_Hans.Cursor.csproj') `
    -OutputPath 'Temp\bin\Cursor\Runtime\' `
    -ProjectRefMap @{}

Convert-UnityCsprojToSdk `
    -SourcePath $unityEditor `
    -DestPath (Join-Path $root 'UnityPD_Hans.Cursor.Editor.csproj') `
    -OutputPath 'Temp\bin\Cursor\Editor\' `
    -ProjectRefMap @{ 'Assembly-CSharp.csproj' = 'UnityPD_Hans.Cursor.csproj' }

$guidRuntime = '{D4E5F6A7-8B9C-4D1E-2F3A-4B5C6D7E8F90}'
$guidEditor = '{E5F6A7B8-9C0D-5E2F-3A4B-5C6D7E8F9012}'
$sln = @"

Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "UnityPD_Hans.Cursor", "UnityPD_Hans.Cursor.csproj", "$guidRuntime"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "UnityPD_Hans.Cursor.Editor", "UnityPD_Hans.Cursor.Editor.csproj", "$guidEditor"
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		$guidRuntime.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		$guidRuntime.Debug|Any CPU.Build.0 = Debug|Any CPU
		$guidRuntime.Release|Any CPU.ActiveCfg = Release|Any CPU
		$guidRuntime.Release|Any CPU.Build.0 = Release|Any CPU
		$guidEditor.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		$guidEditor.Debug|Any CPU.Build.0 = Debug|Any CPU
		$guidEditor.Release|Any CPU.ActiveCfg = Release|Any CPU
		$guidEditor.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
EndGlobal
"@

[IO.File]::WriteAllText((Join-Path $root 'UnityPD_Hans.Cursor.sln'), $sln)
Write-Host 'Wrote UnityPD_Hans.Cursor.sln'
