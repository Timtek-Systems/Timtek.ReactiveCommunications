using System;
using System.Reactive.Subjects;
using Timtek.ReactiveCommunications.Specifications.Fakes;
using Timtek.ReactiveCommunications.Transactions;

namespace Timtek.ReactiveCommunications.Specifications.Contexts;

/// <summary>A boolean transaction whose hot timeout can be chosen, so that "never executed" can be specified.</summary>
internal sealed class TransactionWithChosenHotTimeout : BooleanTransaction
{
    public TransactionWithChosenHotTimeout(string command, TimeSpan hotTimeout) : base(command) =>
        HotTimeout = hotTimeout;
}

internal class TransactionTimeoutContext
{
    // Inputs
    public TimeSpan Timeout { get; set; }
    public TimeSpan HotTimeout { get; set; }
    public TransactionWithChosenHotTimeout Transaction { get; set; }
    public ITransactionProcessor Processor { get; set; }
    public TransactionObserver Observer { get; set; }
    public TransactionObserver SystemClockObserver { get; set; }
    public Subject<char> ReceivedCharacters { get; set; }
    public SignallingFakeTimeProvider Clock { get; set; }

    // Outputs
    public bool Result { get; set; }
    public Exception Exception { get; set; }
}
