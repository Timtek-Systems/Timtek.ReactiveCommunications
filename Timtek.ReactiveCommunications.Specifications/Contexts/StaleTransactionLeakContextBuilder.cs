// This file is part of the Timtek.ReactiveCommunications project
// 
// Copyright © 2026 Timtek Systems Limited, all rights reserved.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so. The Software comes with no warranty of any kind. You make use of the Software entirely at your own risk and assume all liability arising from your use thereof.
// 
// File: StaleTransactionLeakContextBuilder.cs  Last modified: 2026-08-05 by Copilot

using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using FakeItEasy;
using Timtek.ReactiveCommunications.Transactions;

namespace Timtek.ReactiveCommunications.Specifications.Contexts;

/// <summary>
///     Builds a <see cref="StaleTransactionLeakContext" /> around a fake <see cref="ICommunicationChannel" />
///     backed by a hand-controlled <see cref="Subject{T}" /> of <see cref="char" />. The command
///     belonging to the first (timed-out) transaction is never answered by the fake channel, so that
///     transaction is left to time out with its response subscription still attached. The command
///     belonging to the second transaction triggers a simulated device reply, pushed synchronously
///     through the same shared subject.
/// </summary>
internal class StaleTransactionLeakContextBuilder
{
    private const string TimedOutCommand = "GET_TIMED_OUT_FLAG";
    private const string SubsequentCommand = "GET_SUBSEQUENT_FLAG";
    private TimeSpan timedOutTransactionTimeout = TimeSpan.FromMilliseconds(50);

    internal StaleTransactionLeakContextBuilder WithTimedOutTransactionTimeout(TimeSpan timeout)
    {
        timedOutTransactionTimeout = timeout;
        return this;
    }

    internal StaleTransactionLeakContext Build()
    {
        var receivedCharacters = new Subject<char>();
        var channel = A.Fake<ICommunicationChannel>();
        A.CallTo(() => channel.IsOpen).Returns(true);
        A.CallTo(() => channel.ObservableReceivedCharacters).Returns(receivedCharacters.AsObservable());
        A.CallTo(() => channel.Send(A<string>.Ignored)).Invokes((string command) =>
        {
            // Only the subsequent command ever receives a simulated device reply.
            // The command belonging to the timed-out transaction is deliberately never answered.
            if (command == SubsequentCommand)
            {
                receivedCharacters.OnNext('1');
                receivedCharacters.OnNext('#');
            }
        });

        var observer = new TransactionObserver(channel);

        return new StaleTransactionLeakContext
        {
            Channel = channel,
            Observer = observer,
            ReceivedCharacters = receivedCharacters,
            TimedOutTransaction = new BooleanTransaction(TimedOutCommand) { Timeout = timedOutTransactionTimeout },
            SubsequentTransaction = new BooleanTransaction(SubsequentCommand) { Timeout = TimeSpan.FromSeconds(2) }
        };
    }
}
