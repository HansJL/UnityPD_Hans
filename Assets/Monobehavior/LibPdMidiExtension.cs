using System;
using System.Reflection;
using UnityEngine;

public static class LibPdMidiExtensions
{
    // Diese Erweiterung macht das fehlende SendMidiCC auf der LibPdInstance nutzbar
    public static void SendMidiCC(this LibPdInstance instance, int channel, int controller, int value)
    {
        if (instance == null) return;

        // 1. Wir müssen sicherstellen, dass die Instanz aktiv gesetzt ist (SetInstanceIfNeeded)
        MethodInfo setInstanceMethod = typeof(LibPdInstance).GetMethod("SetInstanceIfNeeded", BindingFlags.Instance | BindingFlags.NonPublic);
        if (setInstanceMethod != null)
        {
            setInstanceMethod.Invoke(instance, null);
        }

        // 2. Wir rufen die private native C-Methode aus der LibPdInstance auf via Reflection
        MethodInfo nativeCcMethod = typeof(LibPdInstance).GetMethod("libpd_controlchange", BindingFlags.Static | BindingFlags.NonPublic);
        
        if (nativeCcMethod != null)
        {
            // libpd erwartet Kanäle von 0-15. Unity nutzt 1-16.
            // Damit [ctlin 71 1] in Pd reagiert, übergeben wir Kanal 0!
            int pdChannel = channel - 1; 
            
            nativeCcMethod.Invoke(null, new object[] { pdChannel, controller, value });
        }
        else
        {
            Debug.LogError("Die native Methode 'libpd_controlchange' wurde in LibPdInstance nicht gefunden!");
        }
    }
}