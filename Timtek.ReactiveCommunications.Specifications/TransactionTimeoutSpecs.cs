using System;
using System.Linq;
using System.Threading;
using Machine.Specifications;
using Timtek.ReactiveCommunications.Specifications.Contexts;

namespace Timtek.ReactiveCommunications.Specifications;

// Characterisation specs: these pin the behaviour on the system clock, which must not change.
// They use short real timeouts, and only in scenarios where the device is silent, so a loaded machine cannot affect them.

[Subject(typeof(DeviceTransaction), "timeout on the system clock")]
class when_a_transaction_completes_before_its_timeout_on_the_system_clock : with_transaction_timeout_context
{
    Establish context = () =>
        Context = ContextBuilder.WithTimeout(TimeSpan.FromSeconds(30)).WithDeviceReplying().Build();
    Because of = () =>
    {
        Context.Processor.CommitTransaction(Context.Transaction);
        Context.Result = Context.Transaction.WaitForCompletionOrTimeout();
    };
    It should_report_completion = () => Context.Result.ShouldBeTrue();
    It should_be_successful = () => Context.Transaction.Successful.ShouldBeTrue();
    It should_have_no_error_message = () => Context.Transaction.ErrorMessage.Any().ShouldBeFalse();
}

[Subject(typeof(DeviceTransaction), "timeout on the system clock")]
class when_a_transaction_receives_no_reply_on_the_system_clock : with_transaction_timeout_context
{
    Establish context = () => Context = ContextBuilder.Build();
    Because of = () =>
    {
        Context.Processor.CommitTransaction(Context.Transaction);
        Context.Result = Context.Transaction.WaitForCompletionOrTimeout();
    };
    It should_report_a_timeout = () => Context.Result.ShouldBeFalse();
    It should_be_failed = () => Context.Transaction.Failed.ShouldBeTrue();
    It should_say_it_timed_out = () => Context.Transaction.ErrorMessage.Single().ShouldEqual("Timed out");
    It should_have_no_response = () => Context.Transaction.Response.Any().ShouldBeFalse();
}

[Subject(typeof(DeviceTransaction), "timeout on the system clock")]
class when_a_transaction_is_never_executed_on_the_system_clock : with_transaction_timeout_context
{
    Establish context = () => Context = ContextBuilder.Build();
    Because of = () => Context.Result = Context.Transaction.WaitForCompletionOrTimeout();
    It should_report_a_timeout = () => Context.Result.ShouldBeFalse();
    It should_be_failed = () => Context.Transaction.Failed.ShouldBeTrue();
    It should_say_it_was_never_executed = () =>
        Context.Transaction.ErrorMessage.Single().ShouldEqual("Transaction was never executed");
}

[Subject(typeof(DeviceTransaction), "timeout on a supplied TimeProvider")]
class when_a_transaction_receives_no_reply_and_the_clock_passes_its_timeout : with_transaction_timeout_context
{
    Establish context = () => Context = ContextBuilder
        .WithTimeout(TimeSpan.FromSeconds(30))
        .WithHotTimeout(TimeSpan.FromSeconds(20))
        .WithFakeClock()
        .Build();
    Because of = () =>
    {
        Context.Processor.CommitTransaction(Context.Transaction);
        var waiting = Context.Transaction.WaitForCompletionOrTimeoutAsync(CancellationToken.None);
        Context.Clock.WaitUntilTimersCreated(Context.Timeout, 2);
        Context.Clock.Advance(Context.Timeout);
        Context.Result = waiting.GetAwaiter().GetResult();
    };
    It should_report_a_timeout = () => Context.Result.ShouldBeFalse();
    It should_be_failed = () => Context.Transaction.Failed.ShouldBeTrue();
    It should_say_it_timed_out = () => Context.Transaction.ErrorMessage.Single().ShouldEqual("Timed out");
    It should_have_no_response = () => Context.Transaction.Response.Any().ShouldBeFalse();
}

[Subject(typeof(DeviceTransaction), "timeout on a supplied TimeProvider")]
class when_a_reply_arrives_just_before_the_clock_reaches_the_timeout : with_transaction_timeout_context
{
    Establish context = () => Context = ContextBuilder
        .WithTimeout(TimeSpan.FromSeconds(30))
        .WithHotTimeout(TimeSpan.FromSeconds(20))
        .WithFakeClock()
        .Build();
    Because of = () =>
    {
        Context.Processor.CommitTransaction(Context.Transaction);
        var waiting = Context.Transaction.WaitForCompletionOrTimeoutAsync(CancellationToken.None);
        Context.Clock.WaitUntilTimersCreated(Context.Timeout, 2);
        Context.Clock.Advance(Context.Timeout - TimeSpan.FromMilliseconds(1));
        Context.ReceivedCharacters.OnNext('1');
        Context.ReceivedCharacters.OnNext('#');
        Context.Result = waiting.GetAwaiter().GetResult();
    };
    It should_report_completion = () => Context.Result.ShouldBeTrue();
    It should_be_successful = () => Context.Transaction.Successful.ShouldBeTrue();
}

[Subject(typeof(DeviceTransaction), "timeout on a supplied TimeProvider")]
class when_a_transaction_completes_and_the_clock_then_passes_its_timeout : with_transaction_timeout_context
{
    Establish context = () => Context = ContextBuilder
        .WithTimeout(TimeSpan.FromSeconds(30))
        .WithHotTimeout(TimeSpan.FromSeconds(20))
        .WithDeviceReplying()
        .WithFakeClock()
        .Build();
    Because of = () =>
    {
        Context.Processor.CommitTransaction(Context.Transaction);
        Context.Result = Context.Transaction.WaitForCompletionOrTimeout();
        Context.Clock.Advance(Context.Timeout + Context.Timeout);
    };
    It should_have_reported_completion = () => Context.Result.ShouldBeTrue();
    It should_remain_successful = () => Context.Transaction.Successful.ShouldBeTrue();
    It should_have_no_error_message = () => Context.Transaction.ErrorMessage.Any().ShouldBeFalse();
}

[Subject(typeof(TransactionObserver), "TimeProvider consistency")]
class when_a_reset_transaction_is_retried_through_the_same_observer : with_transaction_timeout_context
{
    Establish context = () => Context = ContextBuilder
        .WithTimeout(TimeSpan.FromSeconds(30))
        .WithDeviceReplying()
        .WithFakeClock()
        .Build();
    Because of = () =>
    {
        Context.Observer.OnNext(Context.Transaction);
        Context.Transaction.Reset();
        Context.Exception = Catch.Exception(() => Context.Observer.OnNext(Context.Transaction));
    };
    It should_not_throw = () => Context.Exception.ShouldBeNull();
}

[Subject(typeof(TransactionObserver), "TimeProvider consistency")]
class when_a_transaction_is_committed_to_an_observer_with_a_different_time_provider : with_transaction_timeout_context
{
    Establish context = () => Context = ContextBuilder
        .WithTimeout(TimeSpan.FromSeconds(30))
        .WithDeviceReplying()
        .WithFakeClock()
        .Build();
    Because of = () =>
    {
        Context.Observer.OnNext(Context.Transaction);
        Context.Transaction.Reset();
        Context.Exception = Catch.Exception(() => Context.SystemClockObserver.OnNext(Context.Transaction));
    };
    It should_throw_an_invalid_operation_exception = () => Context.Exception.ShouldBeOfExactType<InvalidOperationException>();
    It should_say_the_time_provider_must_not_change = () => Context.Exception.Message.ShouldContain("TimeProvider");
}
