// This file is part of the Timtek.ReactiveCommunications project
// 
// Copyright © 2026 Timtek Systems Limited, all rights reserved.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so. The Software comes with no warranty of any kind. You make use of the Software entirely at your own risk and assume all liability arising from your use thereof.
// 
// File: TransactionOverlapRecoveryContextBuilder.cs  Last modified: 2026-08-05 by Copilot

using System;
using FakeItEasy;
using Timtek.ReactiveCommunications.Transactions;

namespace Timtek.ReactiveCommunications.Specifications.Contexts;

/// <summary>
///     Builds a <see cref="TransactionOverlapRecoveryContext" /> around a fake
///     <see cref="ICommunicationChannel" /> whose <c>Send</c> method can be configured to throw on its
///     first invocation only, simulating a device write failure (e.g. a disconnected USB-serial
///     adapter) part-way through an otherwise normal transaction.
/// </summary>
internal class TransactionOverlapRecoveryContextBuilder
{
    private const string FirstCommand = "FIRST";
    private const string SecondCommand = "SECOND";
    private bool firstSendShouldThrow = true;

    internal TransactionOverlapRecoveryContextBuilder WithSendFailingOnFirstCallOnly()
    {
        firstSendShouldThrow = true;
        return this;
    }

    internal TransactionOverlapRecoveryContext Build()
    {
        var sendCallCount = 0;
        var channel = A.Fake<ICommunicationChannel>();
        A.CallTo(() => channel.IsOpen).Returns(true);
        A.CallTo(() => channel.Send(A<string>.Ignored)).Invokes(() =>
        {
            sendCallCount++;
            if (firstSendShouldThrow && sendCallCount == 1)
                throw new InvalidOperationException("Simulated write failure - device disconnected");
        });

        var observer = new TransactionObserver(channel);

        return new TransactionOverlapRecoveryContext
        {
            Channel = channel,
            Observer = observer,
            FirstTransaction = new NoReplyTransaction(FirstCommand),
            SecondTransaction = new NoReplyTransaction(SecondCommand)
        };
    }
}
