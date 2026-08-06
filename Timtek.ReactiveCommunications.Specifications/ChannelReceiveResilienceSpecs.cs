// This file is part of the Timtek.ReactiveCommunications project
// 
// Copyright © 2026 Timtek Systems Limited, all rights reserved.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so. The Software comes with no warranty of any kind. You make use of the Software entirely at your own risk and assume all liability arising from your use thereof.
// 
// File: ChannelReceiveResilienceSpecs.cs  Last modified: 2026-08-06 by Copilot

using System;
using Machine.Specifications;
using Timtek.ReactiveCommunications.Specifications.Contexts;

namespace Timtek.ReactiveCommunications.Specifications;

/// <summary>
///     Regression test for a reliability bug distinct from the transaction-level bugs fixed elsewhere:
///     a <see cref="SerialCommunicationChannel" /> builds a single, long-lived <c>Publish()</c>'d subject
///     over its entire lifetime, connected to the underlying port exactly once in <c>Open()</c>. A plain
///     Rx <c>Subject{T}</c> permanently terminates the first time it receives <c>OnError</c> or
///     <c>OnCompleted</c> - any observer that subscribes afterwards would previously have immediately
///     received that same terminal notification (or nothing) forever, even though the physical port and
///     <see cref="ICommunicationChannel.IsOpen" /> remain healthy. A single transient receive glitch
///     (e.g. a flaky USB-serial adapter reporting EOF) at any point during a long session would
///     therefore permanently kill every subsequent "subscribe, send, receive, dispose" cycle against
///     <see cref="ICommunicationChannel.ObservableReceivedCharacters" />, with no outward sign that
///     anything was wrong. The fix makes the receive pipeline resubscribe itself automatically and
///     indefinitely on error or completion, so a transient glitch is now recovered from transparently.
/// </summary>
[Subject(typeof(SerialCommunicationChannel), "resilience to a transient receive error")]
class when_a_transient_receive_error_occurs_on_a_long_lived_channel : with_channel_receive_resilience_context
{
    Establish context = () => Context = ContextBuilder
        .WithEofBehaviour(EofBehaviour.Error)
        .Build();

    Because of = () =>
    {
        Context.Channel.Open();

        // First cycle: subscribe, send, receive, dispose - exactly as application code would do per command.
        var firstCycleText = string.Empty;
        var firstSubscription = Context.Channel.ObservableReceivedCharacters.Subscribe(
            c => firstCycleText += c,
            ex => { });
        Context.Port.RaiseCharsReceived("OK#");
        firstSubscription.Dispose();
        Context.FirstCycleReceivedText = firstCycleText;

        // Simulate a transient receive glitch, e.g. a flaky USB-serial adapter reporting EOF.
        Context.Port.RaiseEof();

        Context.ChannelIsOpenAfterGlitch = Context.Channel.IsOpen;

        // Second cycle: a brand new subscription, exactly as application code would create per command.
        var secondCycleText = string.Empty;
        var secondSubscription = Context.Channel.ObservableReceivedCharacters.Subscribe(
            c => secondCycleText += c,
            ex => Context.SecondCycleError = ex);
        Context.Port.RaiseCharsReceived("OK#");
        secondSubscription.Dispose();
        Context.SecondCycleReceivedText = secondCycleText;
    };

    It should_receive_data_on_the_first_cycle_before_the_glitch =
        () => Context.FirstCycleReceivedText.ShouldEqual("OK#");

    It should_still_report_the_channel_as_open_after_the_glitch =
        () => Context.ChannelIsOpenAfterGlitch.ShouldBeTrue();

    It should_still_receive_data_on_a_new_subscription_after_the_glitch =
        () => Context.SecondCycleReceivedText.ShouldEqual("OK#");
}
