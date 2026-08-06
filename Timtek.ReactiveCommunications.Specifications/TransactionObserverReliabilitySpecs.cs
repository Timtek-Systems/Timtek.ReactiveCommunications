// This file is part of the Timtek.ReactiveCommunications project
// 
// Copyright © 2026 Timtek Systems Limited, all rights reserved.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so. The Software comes with no warranty of any kind. You make use of the Software entirely at your own risk and assume all liability arising from your use thereof.
// 
// File: TransactionObserverReliabilitySpecs.cs  Last modified: 2026-08-05 by Copilot

using System;
using Machine.Specifications;
using Timtek.ReactiveCommunications.Specifications.Contexts;

namespace Timtek.ReactiveCommunications.Specifications;

/// <summary>
///     Regression test: a channel <c>Send</c> failure on one transaction must not permanently poison
///     the pipeline for every transaction that follows.
///     <see cref="TransactionObserver.CommitTransaction" /> increments an "active transactions" guard
///     before calling <c>channel.Send</c>. Previously, the guard was only decremented after the
///     surrounding block completed normally, so if <c>Send</c> threw (e.g. a disconnected USB-serial
///     adapter) the guard was left permanently incremented and every subsequent transaction was
///     rejected with a spurious "Detected transaction overlap" exception, even though there was no
///     real overlap. The guard is now decremented in a <c>finally</c> block so it is always released.
/// </summary>
[Subject(typeof(TransactionObserver), "recovery after a Send failure")]
class when_a_channel_send_failure_occurs_during_one_transaction : with_transaction_overlap_recovery_context
{
    Establish context = () => Context = ContextBuilder
        .WithSendFailingOnFirstCallOnly()
        .Build();

    Because of = () =>
    {
        Context.ExceptionFromFirstTransaction =
            Catch.Exception(() => Context.Observer.OnNext(Context.FirstTransaction));
        Context.ExceptionFromSecondTransaction =
            Catch.Exception(() => Context.Observer.OnNext(Context.SecondTransaction));
    };

    It should_report_the_simulated_failure_for_the_first_transaction =
        () => Context.ExceptionFromFirstTransaction.ShouldNotBeNull();

    It should_not_report_a_false_transaction_overlap_for_the_next_transaction =
        () => Context.ExceptionFromSecondTransaction.ShouldBeNull();

    It should_allow_the_next_transaction_to_complete_successfully =
        () => Context.SecondTransaction.Successful.ShouldBeTrue();
}

/// <summary>
///     Regression test for a subscription leak: when a transaction times out before a matching
///     response arrives, <see cref="DeviceTransaction.ObserveResponse" />'s subscription to the shared
///     response sequence used to remain undisposed. The leaked subscription stayed attached and could
///     be "resurrected" by a later, unrelated response that was intended for a completely different
///     transaction, silently flipping the timed-out transaction's state from Failed back to Completed.
///     <see cref="TransactionObserver.CommitTransaction" /> now disposes of the subscription returned
///     by <see cref="DeviceTransaction.ObserveResponse" /> as soon as the transaction settles, so a
///     timed-out transaction's subscription can no longer be resurrected.
/// </summary>
[Subject(typeof(TransactionObserver), "stale subscriptions after a timeout")]
class when_an_unrelated_response_arrives_after_a_transaction_has_already_timed_out :
    with_stale_transaction_leak_context
{
    Establish context = () => Context = ContextBuilder
        .WithTimedOutTransactionTimeout(TimeSpan.FromMilliseconds(50))
        .Build();

    Because of = () =>
    {
        Context.Observer.OnNext(Context.TimedOutTransaction);
        Context.Observer.OnNext(Context.SubsequentTransaction);
    };

    It should_still_complete_the_subsequent_transaction_successfully =
        () => Context.SubsequentTransaction.Successful.ShouldBeTrue();

    It should_leave_the_timed_out_transaction_in_the_failed_state =
        () => Context.TimedOutTransaction.State.ShouldEqual(TransactionLifecycle.Failed);

    It should_not_let_the_timed_out_transaction_report_success =
        () => Context.TimedOutTransaction.Successful.ShouldBeFalse();
}
