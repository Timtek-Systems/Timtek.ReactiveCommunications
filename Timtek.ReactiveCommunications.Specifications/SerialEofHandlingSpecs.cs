using System;
using System.ComponentModel;
using System.IO;
using System.IO.Ports;
using System.Text;
using Machine.Specifications;
using Timtek.ReactiveCommunications.Specifications.Helpers;

namespace Timtek.ReactiveCommunications.Specifications;

/// <summary>
///     Minimal test double for ISerialPort that allows us to raise DataReceived/EOF.
///     Only implements the members needed by ToObservableCharacterSequence.
/// </summary>
class TestSerialPort : ISerialPort
{
    public event SerialErrorReceivedEventHandler ErrorReceived;
    public event SerialPinChangedEventHandler    PinChanged;
    public event SerialDataReceivedEventHandler  DataReceived;
    public event EventHandler                    Disposed;

    public Encoding Encoding { get; set; } = Encoding.ASCII;

    // For EOF tests we never read bytes, so this can stay zero.
    public int BytesToRead => 0;

    public void RaiseEof()
    {
        var args = MockHelpers.CreateSerialDataReceivedEventArgs(SerialData.Eof);
        DataReceived?.Invoke(this, args);
    }

    #region ISerialPort members not relevant to these EOF-only specs

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
    public bool       IsOpen                 => true;
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

    public void Close() { }
    public void DiscardInBuffer() { }
    public void DiscardOutBuffer() { }
    public void Open() { }

    public int Read(byte[] buffer, int offset, int count) => 0;
    public int ReadChar() => -1;
    public int Read(char[] buffer, int offset, int count) => 0;
    public int ReadByte() => -1;
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

    public override string ToString() => $"TestSerialPort({PortName})";

    #endregion
}

[Subject(typeof(ObservableExtensions), "EOF behaviour")]
class when_observing_characters_with_eof_treated_as_error_and_eof_is_raised
{
    static TestSerialPort    port;
    static IObservable<char> sequence;
    static IDisposable       subscription;

    static string    observedChars;
    static Exception observedError;
    static bool      completed;

    Establish context = () =>
    {
        port = new TestSerialPort();
        observedChars = string.Empty;
        observedError = null;
        completed = false;

        // New overload taking EofBehaviour
        sequence = port.ToObservableCharacterSequence(EofBehaviour.Error);
    };

    Because of = () =>
    {
        subscription = sequence.Subscribe(
            c => observedChars += c,
            ex => observedError = ex,
            () => completed = true);

        // Simulate EOF from the serial layer
        port.RaiseEof();

        subscription.Dispose();
    };

    It should_not_emit_any_characters =
        () => observedChars.ShouldBeEmpty();

    It should_signal_an_error_to_the_observer =
        () => observedError.ShouldNotBeNull();

    It should_not_call_on_completed =
        () => completed.ShouldBeFalse();

    It should_indicate_eof_in_the_error_message =
        () => observedError.Message.ToLowerInvariant().ShouldContain("eof");
}

[Subject(typeof(ObservableExtensions), "EOF behaviour")]
class when_observing_characters_with_eof_treated_as_completion_and_eof_is_raised
{
    static TestSerialPort    port;
    static IObservable<char> sequence;
    static IDisposable       subscription;

    static string    observedChars;
    static Exception observedError;
    static bool      completed;

    Establish context = () =>
    {
        port = new TestSerialPort();
        observedChars = string.Empty;
        observedError = null;
        completed = false;

        sequence = port.ToObservableCharacterSequence(EofBehaviour.Complete);
    };

    Because of = () =>
    {
        subscription = sequence.Subscribe(
            c => observedChars += c,
            ex => observedError = ex,
            () => completed = true);

        port.RaiseEof();

        subscription.Dispose();
    };

    It should_not_emit_any_characters =
        () => observedChars.ShouldBeEmpty();

    It should_not_signal_an_error =
        () => observedError.ShouldBeNull();

    It should_call_on_completed =
        () => completed.ShouldBeTrue();
}