using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using FakeItEasy;
using Timtek.ReactiveCommunications.Specifications.Fakes;

namespace Timtek.ReactiveCommunications.Specifications.Contexts;

internal class TransactionTimeoutContextBuilder
{
    private const string Command = "GET_FLAG";
    private TimeSpan timeout = TimeSpan.FromMilliseconds(50);
    private TimeSpan hotTimeout = TimeSpan.FromMilliseconds(50);
    private bool deviceReplies;
    private bool observerUsesFakeClock;

    internal TransactionTimeoutContextBuilder WithTimeout(TimeSpan value)
    {
        timeout = value;
        return this;
    }

    internal TransactionTimeoutContextBuilder WithHotTimeout(TimeSpan value)
    {
        hotTimeout = value;
        return this;
    }

    internal TransactionTimeoutContextBuilder WithDeviceReplying()
    {
        deviceReplies = true;
        return this;
    }

    internal TransactionTimeoutContextBuilder WithFakeClock()
    {
        observerUsesFakeClock = true;
        return this;
    }

    internal TransactionTimeoutContext Build()
    {
        var receivedCharacters = new Subject<char>();
        var channel = A.Fake<ICommunicationChannel>();
        A.CallTo(() => channel.IsOpen).Returns(true);
        A.CallTo(() => channel.ObservableReceivedCharacters).Returns(receivedCharacters.AsObservable());
        A.CallTo(() => channel.Send(A<string>.Ignored)).Invokes(() =>
        {
            if (!deviceReplies)
                return;
            receivedCharacters.OnNext('1');
            receivedCharacters.OnNext('#');
        });

        var clock = new SignallingFakeTimeProvider();
        var observer = observerUsesFakeClock
            ? new TransactionObserver(channel, clock)
            : new TransactionObserver(channel);
        var processor = new ReactiveTransactionProcessor();
        processor.SubscribeTransactionObserver(observer);

        var systemClockObserver = new TransactionObserver(channel);

        var transaction = new TransactionWithChosenHotTimeout(Command, hotTimeout) { Timeout = timeout };

        return new TransactionTimeoutContext
        {
            Timeout = timeout,
            HotTimeout = hotTimeout,
            Transaction = transaction,
            Processor = processor,
            Observer = observer,
            SystemClockObserver = systemClockObserver,
            ReceivedCharacters = receivedCharacters,
            Clock = clock
        };
    }
}
