// This file is part of the Timtek.ReactiveCommunications project
// 
// Copyright © 2024 Timtek Systems Limited, all rights reserved.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so. The Software comes with no warranty of any kind. You make use of the Software entirely at your own risk and assume all liability arising from your use thereof.
// 
// File: TransactionProcessorSpecs.cs  Last modified: 2024-3-27@9:23 by tim.long

using System;
using FakeItEasy;
using Machine.Specifications;
using TA.Utils.Core.Diagnostics;

namespace Timtek.ReactiveCommunications.Specifications;

class when_attempting_to_create_a_duplicate_transaction_observer_subscription
{
    Establish context = () =>
    {
        observer = new TransactionObserver(A.Fake<ICommunicationChannel>());
        processor = new ReactiveTransactionProcessor(new ConsoleLoggerService());
        processor.SubscribeTransactionObserver(observer);
    };

    Because of = () => exception = Catch.Exception(() => processor.SubscribeTransactionObserver(observer));

    It should_prevent_the_duplicate_subscription = () => exception.ShouldBeOfExactType<InvalidOperationException>();

    static TransactionObserver observer;
    static ReactiveTransactionProcessor processor;
    static Exception exception;
}