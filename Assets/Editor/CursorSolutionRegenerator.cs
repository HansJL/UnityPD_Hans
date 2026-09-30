using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using Unity.CodeEditor;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

/// <summary>
/// Keeps UnityPD_Hans.Cursor.sln in sync with Unity's generated csproj files.
/// Cursor's C# language server cannot load Unity's legacy project format reliably.
/// </summary>
[InitializeOnLoad]
public static class CursorSolutionRegenerator
{
    const string RuntimeGuid = "{D4E5F6A7-8B9C-4D1E-2F3A-4B5C6D7E8F90}";
    const string EditorGuid = "{E5F6A7B8-9C0D-5E2F-3A4B-5C6D7E8F9012}";
    const string MsBuildNamespace = "http://schemas.microsoft.com/developer/msbuild/2003";

    static CursorSolutionRegenerator()
    {
        EditorApplication.delayCall += () =>
        {
            EnsureUnityProjectFiles();
            RegenerateIfStale();
        };
        CompilationPipeline.compilationFinished += _ => EditorApplication.delayCall += () => RegenerateIfStale();
    }

    static void EnsureUnityProjectFiles()
    {
        var root = Directory.GetParent(Application.dataPath).FullName;
        if (!File.Exists(Path.Combine(root, "Assembly-CSharp.csproj")))
            CodeEditor.CurrentEditor.SyncAll();
    }

    [MenuItem("Tools/Cursor/Regenerate Solution")]
    public static void RegenerateFromMenu()
    {
        if (Regenerate(force: true))
            Debug.Log("Regenerated UnityPD_Hans.Cursor.sln for Cursor IDE.");
        else
            Debug.LogWarning("Could not regenerate Cursor solution. Ensure Unity has generated Assembly-CSharp.csproj.");
    }

    static void RegenerateIfStale()
    {
        Regenerate(force: false);
    }

    static bool Regenerate(bool force)
    {
        var root = Directory.GetParent(Application.dataPath).FullName;
        var unityRuntime = Path.Combine(root, "Assembly-CSharp.csproj");
        var unityEditor = Path.Combine(root, "Assembly-CSharp-Editor.csproj");
        var cursorRuntime = Path.Combine(root, "UnityPD_Hans.Cursor.csproj");

        if (!File.Exists(unityRuntime) || !File.Exists(unityEditor))
            return false;

        if (!force && File.Exists(cursorRuntime))
        {
            var unityTime = File.GetLastWriteTimeUtc(unityRuntime);
            var cursorTime = File.GetLastWriteTimeUtc(cursorRuntime);
            if (cursorTime >= unityTime)
                return false;
        }

        ConvertUnityCsprojToSdk(
            unityRuntime,
            Path.Combine(root, "UnityPD_Hans.Cursor.csproj"),
            @"Temp\bin\Cursor\Runtime\",
            new Dictionary<string, string>());

        ConvertUnityCsprojToSdk(
            unityEditor,
            Path.Combine(root, "UnityPD_Hans.Cursor.Editor.csproj"),
            @"Temp\bin\Cursor\Editor\",
            new Dictionary<string, string> { { "Assembly-CSharp.csproj", "UnityPD_Hans.Cursor.csproj" } });

        File.WriteAllText(Path.Combine(root, "UnityPD_Hans.Cursor.sln"), BuildSolutionText());
        return true;
    }

    static string BuildSolutionText()
    {
        return $@"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
Project(""{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}"") = ""UnityPD_Hans.Cursor"", ""UnityPD_Hans.Cursor.csproj"", ""{RuntimeGuid}""
EndProject
Project(""{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}"") = ""UnityPD_Hans.Cursor.Editor"", ""UnityPD_Hans.Cursor.Editor.csproj"", ""{EditorGuid}""
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{RuntimeGuid}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{RuntimeGuid}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{RuntimeGuid}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{RuntimeGuid}.Release|Any CPU.Build.0 = Release|Any CPU
		{EditorGuid}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{EditorGuid}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{EditorGuid}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{EditorGuid}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
EndGlobal
";
    }

    static void ConvertUnityCsprojToSdk(
        string sourcePath,
        string destPath,
        string outputPath,
        Dictionary<string, string> projectRefMap)
    {
        var xml = new XmlDocument();
        xml.Load(sourcePath);

        var ns = new XmlNamespaceManager(xml.NameTable);
        ns.AddNamespace("msb", MsBuildNamespace);

        var lang = SelectInnerText(xml, "//msb:LangVersion", ns) ?? "9.0";
        var defines = SelectInnerText(xml, "//msb:DefineConstants", ns) ?? "";
        var allowUnsafe = SelectInnerText(xml, "//msb:AllowUnsafeBlocks", ns) ?? "False";
        var noWarn = SelectInnerText(xml, "//msb:NoWarn", ns) ?? "0169";

        var sb = new StringBuilder();
        sb.AppendLine("<Project Sdk=\"Microsoft.NET.Sdk\">");
        sb.AppendLine("  <PropertyGroup>");
        sb.AppendLine("    <TargetFramework>netstandard2.1</TargetFramework>");
        sb.AppendLine($"    <LangVersion>{lang}</LangVersion>");
        sb.AppendLine("    <EnableDefaultItems>false</EnableDefaultItems>");
        sb.AppendLine("    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>");
        sb.AppendLine("    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>");
        sb.AppendLine("    <GenerateTargetFrameworkAttribute>false</GenerateTargetFrameworkAttribute>");
        sb.AppendLine("    <DisableImplicitFrameworkReferences>true</DisableImplicitFrameworkReferences>");
        sb.AppendLine("    <GenerateDependencyFile>false</GenerateDependencyFile>");
        sb.AppendLine("    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>");
        sb.AppendLine("    <Deterministic>false</Deterministic>");
        sb.AppendLine($"    <AllowUnsafeBlocks>{allowUnsafe}</AllowUnsafeBlocks>");
        sb.AppendLine($"    <NoWarn>{noWarn};CS8019</NoWarn>");
        sb.AppendLine($"    <DefineConstants>{defines}</DefineConstants>");
        sb.AppendLine($"    <OutputPath>{outputPath}</OutputPath>");
        sb.AppendLine("  </PropertyGroup>");

        AppendIncludes(sb, xml.SelectNodes("//msb:Compile[@Include]", ns), "Compile");
        AppendIncludes(sb, xml.SelectNodes("//msb:None[@Include]", ns), "None");
        AppendReferences(sb, xml.SelectNodes("//msb:Reference[@Include]", ns), ns);
        AppendProjectReferences(sb, xml.SelectNodes("//msb:ProjectReference[@Include]", ns), ns, projectRefMap);

        sb.AppendLine("</Project>");
        File.WriteAllText(destPath, sb.ToString());
    }

    static string SelectInnerText(XmlDocument xml, string xpath, XmlNamespaceManager ns)
    {
        return xml.SelectSingleNode(xpath, ns)?.InnerText;
    }

    static void AppendIncludes(StringBuilder sb, XmlNodeList nodes, string elementName)
    {
        if (nodes == null || nodes.Count == 0)
            return;

        sb.AppendLine("  <ItemGroup>");
        foreach (XmlNode node in nodes)
        {
            var include = node.Attributes?["Include"]?.Value;
            if (!string.IsNullOrEmpty(include))
                sb.AppendLine($"    <{elementName} Include=\"{include}\" />");
        }
        sb.AppendLine("  </ItemGroup>");
    }

    static void AppendReferences(StringBuilder sb, XmlNodeList nodes, XmlNamespaceManager ns)
    {
        if (nodes == null || nodes.Count == 0)
            return;

        sb.AppendLine("  <ItemGroup>");
        foreach (XmlNode node in nodes)
        {
            var include = node.Attributes?["Include"]?.Value;
            if (string.IsNullOrEmpty(include))
                continue;

            var hintPath = node.SelectSingleNode("msb:HintPath", ns)?.InnerText;
            var isPrivate = node.SelectSingleNode("msb:Private", ns)?.InnerText ?? "False";

            sb.AppendLine($"    <Reference Include=\"{include}\">");
            if (!string.IsNullOrEmpty(hintPath))
                sb.AppendLine($"      <HintPath>{hintPath}</HintPath>");
            sb.AppendLine($"      <Private>{isPrivate}</Private>");
            sb.AppendLine("    </Reference>");
        }
        sb.AppendLine("  </ItemGroup>");
    }

    static void AppendProjectReferences(
        StringBuilder sb,
        XmlNodeList nodes,
        XmlNamespaceManager ns,
        Dictionary<string, string> projectRefMap)
    {
        if (nodes == null || nodes.Count == 0)
            return;

        sb.AppendLine("  <ItemGroup>");
        foreach (XmlNode node in nodes)
        {
            var include = node.Attributes?["Include"]?.Value;
            if (string.IsNullOrEmpty(include))
                continue;

            var name = node.SelectSingleNode("msb:Name", ns)?.InnerText
                ?? Path.GetFileNameWithoutExtension(include);

            if (projectRefMap.TryGetValue(include, out var mapped))
            {
                sb.AppendLine($"    <ProjectReference Include=\"{mapped}\" />");
                continue;
            }

            sb.AppendLine($"    <Reference Include=\"{name}\">");
            sb.AppendLine($"      <HintPath>Library\\ScriptAssemblies\\{name}.dll</HintPath>");
            sb.AppendLine("      <Private>False</Private>");
            sb.AppendLine("    </Reference>");
        }
        sb.AppendLine("  </ItemGroup>");
    }
}
