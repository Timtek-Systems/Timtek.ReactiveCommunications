﻿// This file is part of the Timtek.ReactiveCommunications project
// 
// Copyright © 2023 Tigra Astronomy, all rights reserved.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so. The Software comes with no warranty of any kind.
// You make use of the Software entirely at your own risk and assume all liability arising from your use thereof.
// 
// File: DeviceTransaction.cs  Last modified: 2023-10-27@17:54 by Tim Long

using System;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;
using TA.Utils.Core;

namespace Timtek.ReactiveCommunications;

/// <summary>Defines the common implementation for all commands</summary>
public abstract class DeviceTransaction
{
    private const string successBeforeCompletionMessage =
        @"The Successful property is invalid until the transaction has fully completed.
The property has been queried whilst the transaction is still 'in flight'.
This is probably a bug in application code and you should report this message to your application developer.
This might be a sign that the custom transaction class hasn't called base.OnCompleted() before evaluating success or failure.";

    private const string failedBeforeCompletionMessage =
        @"The Failed property is invalid until the transaction has fully completed.
The property has been queried whilst the transaction is still 'in flight'.
This is probably a bug in application code and you should report this message to your application developer.
This might be a sign that the custom transaction class hasn't called base.OnCompleted() before evaluating success or failure.";

    /// <summary>Used to generate unique transaction IDs for each created transaction.</summary>
    private static int transactionCounter;

    private readonly ManualResetEvent completion;
    private readonly ManualResetEvent hot;
    protected string command;

    /// <summary>Initializes a new instance of the <see cref="DeviceTransaction" /> class.</summary>
    /// <param name="command">The command to be sent to the communications channel.</param>
    /// <remarks>
    ///     This abstract class cannot be instantiated directly and is intended to be inherited by your own
    ///     transaction classes.
    /// </remarks>
    protected DeviceTransaction(string command)
    {
        Contract.Requires(!string.IsNullOrEmpty(command));
        TransactionId = GenerateTransactionId();
        Response = Maybe<string>.Empty;
        ErrorMessage = Maybe<string>.Empty;
        this.command = command;
        Timeout = TimeSpan.FromSeconds(10); // Wait effectively forever by default
        HotTimeout = TimeSpan.FromSeconds(10);
        completion = new ManualResetEvent(false); // Not signalled by default
        hot = new ManualResetEvent(false);
        State = TransactionLifecycle.Created;
    }

    /// <summary>
    ///     Reset a transaction so that it can be re-used (useful for automatic re-try).
    ///     A new <see cref="TransactionId" /> will be generated;
    ///     The <see cref="ErrorMessage" /> will be cleared;
    ///     The <see cref="Response" /> will be cleared;
    ///     The <see cref="State" /> will be reset to <see cref="TransactionLifecycle.Created" />.
    ///     The <see cref="Command" /> and <see cref="Timeout" /> values will be preserved.
    ///     May be overridden in derived transaction classes.
    /// </summary>
    public virtual void Reset()
    {
        TransactionId = GenerateTransactionId();
        ErrorMessage = Maybe<string>.Empty;
        Response = Maybe<string>.Empty;
        State = TransactionLifecycle.Created;
    }

    /// <summary>
    ///     Gets the transaction identifier of the transaction. Used to match responses to commands where
    ///     the protocol supports multiple overlapping commands and for logging and statistics gathering.
    ///     Each command must have a unique transaction Id.
    /// </summary>
    /// <value>The transaction identifier.</value>
    public long TransactionId { get; private set; }

    /// <summary>
    ///     Gets the command string. The command must be in a format ready for sending to a
    ///     transport provider. In other words, commands are 'Layer 7', we are talking directly to the
    ///     command interpreter inside the target device. Can be overridden in derived classes to add any necessary
    ///     encapsulation processing (such as checksum computation).
    /// </summary>
    /// <value>The command string as expected by the command interpreter in the remote device.</value>
    /// <remarks>Declared virtual so that derived transactions can insert any encapsulation processing needed.</remarks>
    public virtual string Command => command;

    /// <summary>
    ///     Gets or sets the time that the command can be pending before it is considered to have failed.
    ///     This property should be set from the constructor of a custom derived class. The default value
    ///     is 10 seconds.
    /// </summary>
    /// <value>The timeout period.</value>
    public TimeSpan Timeout { get; set; }

    /// <summary>
    ///     Gets or sets the maximum time that can elapse between the transaction being committed and
    ///     becoming "hot" (the active transaction).
    /// </summary>
    /// <value>The hot timeout.</value>
    protected TimeSpan HotTimeout { get; set; }

    /// <summary>
    ///     Gets the response string exactly as observed from the response sequence. The response is
    ///     wrapped in a <see cref="Maybe{String}" /> to differentiate an empty response from no response
    ///     or a failed response. An empty Maybe indicates an error condition. Commands where no repsponse
    ///     is expected will return a Maybe containing <see cref="string.Empty" />, not an empty Maybe.
    /// </summary>
    /// <value>The response string.</value>
    public Maybe<string> Response { get; protected set; }

    /// <summary>
    ///     Gets an indication that the transaction has failed. This property should not be relied upon until the transaction
    ///     is known to have completed,
    ///     e.g. by calling transaction.<see cref="WaitForCompletionOrTimeout" />.
    /// </summary>
    /// <value><c>true</c> if failed; otherwise, <c>false</c>.</value>
    /// <seealso cref="OnError" />
    public bool Failed => State == TransactionLifecycle.Failed;

    /// <summary>
    ///     Gets the error message for a failed transaction. The response is wrapped in a
    ///     <see cref="Maybe{String}" /> and if there is no error, then there will be no value:
    ///     <c>ErrorMessage.Any() == false</c>
    /// </summary>
    /// <value>May contain an error message.</value>
    public Maybe<string> ErrorMessage { get; protected set; }

    /// <summary>Indicates which stage of the transaction lifecycle the transaction is in.</summary>
    public TransactionLifecycle State { get; protected set; }

    /// <summary>
    ///     Indicates whether a transaction has completed successfully. Note: this is subtly different from
    ///     <c>!Failed</c>. This property should not be relied upon until the transaction is known to have completed,
    ///     e.g. by calling transaction.<see cref="WaitForCompletionOrTimeout" />.
    /// </summary>
    /// <seealso cref="State" />
    public bool Successful => State == TransactionLifecycle.Completed;

    /// <summary>
    ///     Indicates whether the transaction is completed. A completed transaction could either have
    ///     succeeded or failed, so it is necessary to also check <see cref="Successful" /> or
    ///     <see cref="Failed" />, or to examine the <see cref="State" /> property to determine the final
    ///     disposition of the transaction.
    /// </summary>
    public bool Completed => State == TransactionLifecycle.Completed || State == TransactionLifecycle.Failed;

    internal void MakeHot()
    {
        State = TransactionLifecycle.InProgress;
        hot.Set();
    }

    internal bool WaitUntilHotOrTimeout()
    {
        var wasHot = hot.WaitOne(HotTimeout);
        if (!wasHot)
        {
            State = TransactionLifecycle.Failed;
            ErrorMessage = Maybe<string>.From("Transaction was never executed");
        }

        return wasHot;
    }

    [ContractInvariantMethod]
    private void ObjectInvariant()
    {
        Contract.Invariant(Response   != null);
        Contract.Invariant(completion != null);
        Contract.Invariant(hot        != null);
    }

    /// <summary>
    ///     Generates the transaction identifier by incrementing a shared counter using an atomic
    ///     operation. At a constant rate of 1,000 transactions per second, the counter will not wrap for
    ///     several million years. This effectively guarantees that every transaction across all threads
    ///     will have a unique ID, within any given session.
    /// </summary>
    /// <returns>System.Int64.</returns>
    private static long GenerateTransactionId()
    {
        var tid = Interlocked.Increment(ref transactionCounter);
        return tid;
    }

    /// <summary>
    ///     Waits (blocks) until the transaction is completed or a timeout occurs. The timeout duration
    ///     comes from the <see cref="Timeout" /> property.
    /// </summary>
    /// <returns><c>true</c> if the transaction completed successfully, <c>false</c> otherwise.</returns>
    public bool WaitForCompletionOrTimeout()
    {
        var hot = WaitUntilHotOrTimeout();
        if (!hot)
            return false;
        var signalled = completion.WaitOne(Timeout);
        if (!signalled)
        {
            Response = Maybe<string>.Empty;
            State = TransactionLifecycle.Failed;
            ErrorMessage = Maybe<string>.From("Timed out");
        }

        return signalled;
    }

    /// <summary>Returns a task that completes when the transaction completes, fails or times out.</summary>
    /// <param name="cancellation">
    ///     A cancellation token that can cancel the task. Note that this does not cancel the transaction
    ///     (these cannot be cancelled once committed).
    /// </param>
    /// <returns>Task{bool} that indicates completion if true, or timeout if false.</returns>
    public Task<bool> WaitForCompletionOrTimeoutAsync(CancellationToken cancellation)
    {
        return Task.Run(WaitForCompletionOrTimeout, cancellation);
    }

    private void SignalCompletion()
    {
        completion.Set();
    }

    /// <summary>
    ///     Observes the character sequence from the communications channel until a satisfactory response
    ///     has been received. This default implementation should be overridden in derived types toi
    ///     produce the desired results.
    /// </summary>
    /// <param name="source">The source.</param>
    public abstract void ObserveResponse(IObservable<char> source);

    /// <summary>
    ///     Called when the response sequence produces a value. This sets the transaction's Response string
    ///     but the sequence must complete before the transaction is considered successful.
    /// </summary>
    /// <param name="value">The value produced.</param>
    /// <remarks>
    ///     If you override the OnNext method in your own derived class, then you should call
    ///     <c>base.OnNext</c> from your overridden method to set the <see cref="Response" /> property
    ///     correctly.
    /// </remarks>
    protected virtual void OnNext(string value)
    {
        Contract.Requires(value != null);
        Response = value.AsMaybe();
    }

    /// <summary>
    ///     Called when the response sequence produces an error, or by a custom transaction to indicate an
    ///     error condition. This indicates a failed transaction.
    /// </summary>
    /// <param name="except">The exception.</param>
    /// <remarks>
    ///     Overriding <c>OnError</c> is discouraged but it you do, you should call <c>base.OnError</c>
    ///     before returning from your override method, to ensure correct thread synchronization and that
    ///     the internal state remains consistent. <see cref="OnError" /> and <see cref="OnCompleted" />
    ///     are mutually exclusive, therefore do not call <c>base.OnCompleted</c> after calling this
    ///     method.
    /// </remarks>
    protected virtual void OnError(Exception except)
    {
        Contract.Requires(except      != null);
        Contract.Ensures(Response     != null);
        Contract.Ensures(ErrorMessage != null);
        Response = Maybe<string>.Empty;
        ErrorMessage = except.Message.AsMaybe();
        State = TransactionLifecycle.Failed;
        SignalCompletion();
    }

    /// <summary>Called when the response sequence completes. This indicates a successful transaction.</summary>
    /// <remarks>
    ///     This method is normally overridden in a derived type, where you can perform additional parsing
    ///     and type conversion of any received response. The recommended pattern is for the derived type
    ///     to add a <c>Value</c> property of an appropriate type, then in the OnCompleted method, set the
    ///     <c>Value</c> property before calling <c>base.OnCompleted</c>. You MUST call
    ///     <c>base.OnCompleted</c> before returning from your override method, to ensure correct thread
    ///     synchronization and internal state consistency.
    /// </remarks>
    protected virtual void OnCompleted()
    {
        State = TransactionLifecycle.Completed;
        SignalCompletion();
    }

    /// <summary>Returns a <see cref="System.String" /> that represents this instance.</summary>
    /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
    [Pure]
    public override string ToString()
    {
        Contract.Ensures(Contract.Result<string>() != null);
        var disposition = State == TransactionLifecycle.Failed ? $"Failed ({ErrorMessage})" : State.ToString();
        return
            $"TID={TransactionId} [{Command.ExpandAscii()}] [{Response.ToString().ExpandAscii()}] {Timeout} {disposition}";
    }
}