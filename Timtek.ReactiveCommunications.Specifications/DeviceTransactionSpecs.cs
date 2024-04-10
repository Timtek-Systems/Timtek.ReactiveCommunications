// This file is part of the Timtek.ReactiveCommunications project
// 
// Copyright © 2015-2020 Tigra Astronomy, all rights reserved.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so. The Software comes with no warranty of any kind.
// You make use of the Software entirely at your own risk and assume all liability arising from your use thereof.
// 
// File: DeviceTransactionSpecs.cs  Last modified: 2020-07-20@00:51 by Tim Long

using System;
using System.Linq;
using System.Threading.Tasks;
using FakeItEasy;
using Machine.Specifications;
using TA.Utils.Core;
using Timtek.ReactiveCommunications.Specifications.Behaviours;
using Timtek.ReactiveCommunications.Specifications.Contexts;
using Timtek.ReactiveCommunications.Transactions;

namespace Timtek.ReactiveCommunications.Specifications;

[Subject(typeof(DeviceTransaction))]
class when_a_device_transaction_completes : with_rxascom_context
{
    Behaves_like<successful_transaction> a_successful_transaction;

    Establish context = () => Context = RxAscomContextBuilder
        .WithOpenConnection("Fake")
        .WithFakeResponse("Test")
        .Build();

    Because of = () =>
    {
        Transaction = new NoReplyTransaction("Test");
        Processor.CommitTransaction(Transaction);
        Transaction.WaitForCompletionOrTimeout();
    };
}

[Subject(typeof(DeviceTransaction), "unique identity")]
class when_generating_lots_of_transactions_asynchronously
{
    const int NumberOfTransactions = 10000;
    static DeviceTransaction[] transactions = new DeviceTransaction[NumberOfTransactions];

    Because of = () =>
    {
        var pendingTasks = new Task[NumberOfTransactions];
        for (var i = 0; i < NumberOfTransactions; i++)
        {
            var index = i;
            pendingTasks[index] =
                Task.Run(() => transactions[index] = new NoReplyTransaction(index.ToString()));
        }

        Task.WaitAll(pendingTasks);
    };

    It should_generate_unique_transaction_ids = () =>
        transactions.Select(t => t.TransactionId).Distinct().Count().ShouldEqual(transactions.Length);
}

[Subject(typeof(DeviceTransaction), "lifecycle")]
class when_creating_a_new_transaction
{
    static DeviceTransaction transaction;
    Because of = () => transaction = new BooleanTransaction("Dummy");
    It should_have_created_state = () => transaction.State.ShouldEqual(TransactionLifecycle.Created);

    It should_include_created_in_tostring =
        () => transaction.ToString().ShouldContain(nameof(TransactionLifecycle.Created));
}

[Subject(typeof(DeviceTransaction), "lifecycle")]
class when_a_transaction_times_out : with_rxascom_context
{
    Behaves_like<failed_transaction> a_failed_transaction;

    Establish context = () => Context = RxAscomContextBuilder
        .WithOpenConnection("Fake")
        .Build();

    Because of = () =>
    {
        Transaction = new BooleanTransaction("Dummy") { Timeout = TimeSpan.FromMilliseconds(1) };
        Processor.CommitTransaction(Transaction);
        Transaction.WaitForCompletionOrTimeout();
    };

    It should_give_timeout_as_the_reason_for_failure = () =>
        Transaction.ErrorMessage.Single().ShouldContain("Timed out");

    It should_include_failed_in_tostring = () => Transaction.ToString().ShouldContain("Failed");
}

[Subject(typeof(DeviceTransaction), "lifecycle")]
class when_a_transaction_is_in_progress : with_rxascom_context
{
    Establish context = () => Context = RxAscomContextBuilder
        .WithOpenConnection("Fake")
        .Build();

    Because of = () =>
    {
        Transaction = new BooleanTransaction("Dummy") { Timeout = TimeSpan.FromDays(1) };
        Processor.CommitTransaction(Transaction);
        Transaction.WaitUntilHotOrTimeout();
    };

    /*
     * Note: transactions do not necessarily move to "In Progress" immediately.
     * They may remain "Pending" for some time, therefore we cannot perform a test for
     * "In Progress" here and can only perform negative tests.
     */
    It should_not_be_completed = () => Transaction.Completed.ShouldBeFalse();
    It should_not_be_failed = () => Transaction.Failed.ShouldBeFalse();
    It should_not_be_successful = () => Transaction.Successful.ShouldBeFalse();
}

class ResettableTransaction : DeviceTransaction
{
    public ResettableTransaction(string command) : base(command) { }

    public override void ObserveResponse(IObservable<char> source)
    {
        throw new NotImplementedException();
    }

    internal void CompleteWithError(string message)
    {
        // Fakes the entire transaction lifecycle ending in failure.
        ErrorMessage = Maybe<string>.From(message);
        MakeHot();
        OnCompleted();
    }
}

class when_resetting_a_transaction
{
    const string Command = "yoohoo";
    const char Terminator = '%';
    const char Initiator = '&';

    Establish context = () =>
    {
        Processor = A.Fake<ITransactionProcessor>();
        A.CallTo(() => Processor.CommitTransaction(A<DeviceTransaction>.Ignored))
            .Invokes(_ => UUT.CompleteWithError("Failed by unit test"));
        UUT = new ResettableTransaction(Command) { Timeout = ExpectedTimeout };
        OriginalId = UUT.TransactionId;
    };


    Because of = () =>
    {
        Processor.CommitTransaction(UUT);
        UUT.WaitForCompletionOrTimeout();
        UUT.Reset();
    };

    It should_not_have_an_error = () => UUT.ErrorMessage.ShouldBeEmpty();
    It should_have_a_new_identity = () => UUT.TransactionId.ShouldNotEqual(OriginalId);
    It should_not_have_a_response = () => UUT.Response.ShouldBeEmpty();
    It should_preserve_the_command = () => UUT.Command.ShouldEqual(Command);
    It should_preserve_the_timeout = () => UUT.Timeout.ShouldEqual(ExpectedTimeout);
    It should_be_in_created_state = () => UUT.State.ShouldEqual(TransactionLifecycle.Created);

    static ResettableTransaction UUT;
    static readonly TimeSpan ExpectedTimeout = TimeSpan.FromMilliseconds(1);
    static long OriginalId;
    static ITransactionProcessor Processor;
}