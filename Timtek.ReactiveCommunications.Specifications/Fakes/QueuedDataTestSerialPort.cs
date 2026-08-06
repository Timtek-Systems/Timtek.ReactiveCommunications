// This file is part of the Timtek.ReactiveCommunications project
// 
// Copyright © 2026 Timtek Systems Limited, all rights reserved.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so. The Software comes with no warranty of any kind. You make use of the Software entirely at your own risk and assume all liability arising from your use thereof.
// 
// File: QueuedDataTestSerialPort.cs  Last modified: 2026-08-06 by Copilot

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Ports;
using System.Text;
using Timtek.ReactiveCommunications.Specifications.Helpers;

namespace Timtek.ReactiveCommunications.Specifications.Fakes;

/// <summary>
///     A minimal, hand-written test double for <see cref="ISerialPort" /> that can enqueue character data
///     to be delivered via a real <see cref="DataReceived" /> event (serviced through <see cref="BytesToRead" />
///     and <see cref="ReadByte" />, exactly as <see cref="ObservableExtensions.ToObservableCharacterSequence(ISerialPort, EofBehaviour)" />
///     expects), and that can also raise a simulated End-Of-File condition. Only the members needed to
///     exercise the receive pipeline are meaningfully implemented.
/// </summary>
internal class QueuedDataTestSerialPort : ISerialPort
{
    private readonly Queue<byte> pendingBytes = new();

    public event SerialErrorReceivedEventHandler ErrorReceived;
    public event SerialPinChangedEventHandler    PinChanged;
    public event SerialDataReceivedEventHandler  DataReceived;
    public event EventHandler                    Disposed;

    public Encoding Encoding { get; set; } = Encoding.ASCII;

    public int BytesToRead => pendingBytes.Count;

    // Mirrors real SerialPort semantics: closed until Open() is called. This matters because
    // SerialCommunicationChannel.Open() is idempotent and short-circuits with "if (IsOpen) return;"
    // before it ever connects the receive pipeline, so a fake that is always "open" would prevent
    // the channel from ever subscribing to the port's DataReceived event.
    public bool IsOpen { get; private set; }

    /// <summary>Enqueues the given text as bytes and raises a <see cref="SerialData.Chars" /> event.</summary>
    public void RaiseCharsReceived(string text)
    {
        foreach (var b in Encoding.GetBytes(text))
            pendingBytes.Enqueue(b);
        var args = MockHelpers.CreateSerialDataReceivedEventArgs(SerialData.Chars);
        DataReceived?.Invoke(this, args);
    }

    /// <summary>Raises a simulated End-Of-File condition, exactly as a flaky adapter might do.</summary>
    public void RaiseEof()
    {
        var args = MockHelpers.CreateSerialDataReceivedEventArgs(SerialData.Eof);
        DataReceived?.Invoke(this, args);
    }

    public int ReadByte() => pendingBytes.Count > 0 ? pendingBytes.Dequeue() : -1;

    #region ISerialPort members not relevant to these specs

    public Stream     BaseStream             => Stream.Null;
    public int        BaudRate               { get; set; }
    public bool       BreakState             { get; set; }
    public int        BytesToWrite           => 0;
    public bool       CDHolding              => false;
    public bool       CtsHolding             => false;
    public int        DataBits               { get; set; }
    public bool       DiscardNull            { get; set; }
    public bool       DsrHolding             => false;
    public bool       DtrEnable              { get; set; }
    public Handshake  Handshake              { get; set; }
    public string     NewLine                { get; set; } = "\n";
    public Parity     Parity                 { get; set; }
    public byte       ParityReplace          { get; set; }
    public string     PortName               { get; set; } = "TEST";
    public int        ReadBufferSize         { get; set; }
    public int        ReadTimeout            { get; set; }
    public int        ReceivedBytesThreshold { get; set; }
    public bool       RtsEnable              { get; set; }
    public StopBits   StopBits               { get; set; }
    public int        WriteBufferSize        { get; set; }
    public int        WriteTimeout           { get; set; }
    public ISite      Site                   { get; set; }
    public IContainer Container              => null;

    public void Close() { IsOpen = false; }
    public void DiscardInBuffer() { }
    public void DiscardOutBuffer() { }
    public void Open() { IsOpen = true; }

    public int Read(byte[] buffer, int offset, int count) => 0;
    public int ReadChar() => -1;
    public int Read(char[] buffer, int offset, int count) => 0;
    public string ReadExisting() => string.Empty;
    public string ReadLine() => string.Empty;
    public string ReadTo(string value) => string.Empty;

    public void Write(string     text) { }
    public void Write(char[]     buffer, int offset, int count) { }
    public void Write(byte[]     buffer, int offset, int count) { }
    public void WriteLine(string text) { }

    public void Dispose() => Disposed?.Invoke(this, EventArgs.Empty);
    public object GetLifetimeService() => null;
    public object InitializeLifetimeService() => null;

    public override string ToString() => $"QueuedDataTestSerialPort({PortName})";

    #endregion
}
