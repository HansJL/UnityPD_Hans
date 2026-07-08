using System;
using System.Collections.Generic;
using UnityEngine;
using RtMidi;

public class MidiInput : MonoBehaviour
{
    public event Action<int, int, int> onNoteOn;
    public event Action<int, int> onNoteOff;
    public event Action<int, int, int> onControlChange;

    [Header("Options")]
    [SerializeField] bool logMessages = true;

    MidiIn _probe;
    readonly List<(MidiIn dev, string name)> _ports = new();
    readonly byte[] _buffer = new byte[32];

    void Start()
    {
        _probe = MidiIn.Create();
        _probe.ErrorReceived = (t, m) => Debug.LogWarning($"[MIDI] {t}: {m}");
    }

    void Update()
    {
        if (_ports.Count != _probe.PortCount) { CloseAllPorts(); OpenPorts(); }
        foreach (var p in _ports) Poll(p.dev, p.name);
    }

    void OpenPorts()
    {
        for (var i = 0; i < _probe.PortCount; i++)
        {
            var name = _probe.GetPortName(i);
            if (name.StartsWith("RtMidi") || name.StartsWith("Midi Through"))
            { _ports.Add((null, name)); continue; }
            var dev = MidiIn.Create();
            var portName = name;
            dev.ErrorReceived = (t, m) => Debug.LogWarning($"[MIDI:{portName}] {t}: {m}");
            dev.OpenPort(i);
            _ports.Add((dev, name));
            if (logMessages) Debug.Log($"[MIDI] Opened: {name}");
        }
    }

    void Poll(MidiIn dev, string name)
    {
        if (dev == null) return;
        while (true)
        {
            var msg = dev.GetMessage(_buffer, out _);
            if (msg.Length == 0) return;
            Dispatch(msg, name);
        }
    }

    void Dispatch(ReadOnlySpan<byte> msg, string name)
    {
        var status = (byte)(msg[0] >> 4);
        var channel = msg[0] & 0x0f;
        int d1 = msg.Length > 1 ? msg[1] : 0;
        int d2 = msg.Length > 2 ? msg[2] : 0;
        switch (status)
        {
            case 0x9 when d2 > 0:
                if (logMessages) Debug.Log($"[MIDI:{name}] ch{channel} NoteOn {d1} v{d2}");
                onNoteOn?.Invoke(channel, d1, d2); break;
            case 0x8:
            case 0x9:
                if (logMessages) Debug.Log($"[MIDI:{name}] ch{channel} NoteOff {d1}");
                onNoteOff?.Invoke(channel, d1); break;
            case 0xb:
                if (logMessages) Debug.Log($"[MIDI:{name}] ch{channel} CC {d1}={d2}");
                onControlChange?.Invoke(channel, d1, d2); break;
        }
    }

    void CloseAllPorts()
    {
        foreach (var p in _ports) p.dev?.Dispose();
        _ports.Clear();
    }

    void OnDestroy() { CloseAllPorts(); _probe?.Dispose(); }
}
