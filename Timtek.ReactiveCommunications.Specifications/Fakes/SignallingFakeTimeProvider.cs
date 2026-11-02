using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Time.Testing;

namespace Timtek.ReactiveCommunications.Specifications.Fakes;

/// <summary>
///     A <see cref="TimeProvider" /> that wraps a <see cref="FakeTimeProvider" /> and lets a spec wait until code under
///     test has registered a timer. Advancing a fake clock before the timer exists silently loses the advance, so specs
///     must wait for registration instead of sleeping or racing.
/// </summary>
internal sealed class SignallingFakeTimeProvider : TimeProvider
{
    // Only a safety net so that a broken implementation fails a spec instead of hanging it.
    private static readonly TimeSpan RegistrationSafetyCap = TimeSpan.FromSeconds(10);
    private readonly FakeTimeProvider inner = new();
    private readonly List<TimeSpan> registeredDueTimes = [];

    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow() => inner.GetUtcNow();

    /// <inheritdoc />
    public override long GetTimestamp() => inner.GetTimestamp();

    /// <inheritdoc />
    public override long TimestampFrequency => inner.TimestampFrequency;

    /// <inheritdoc />
    public override TimeZoneInfo LocalTimeZone => inner.LocalTimeZone;

    /// <inheritdoc />
    public override ITimer CreateTimer(TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = inner.CreateTimer(callback, state, dueTime, period);
        lock (registeredDueTimes)
        {
            registeredDueTimes.Add(dueTime);
            Monitor.PulseAll(registeredDueTimes);
        }

        return timer;
    }

    /// <summary>Advances the fake clock, firing any timers that fall due.</summary>
    public void Advance(TimeSpan delta) => inner.Advance(delta);

    /// <summary>Blocks until at least <paramref name="count" /> timers with the given due time have been created.</summary>
    public void WaitUntilTimersCreated(TimeSpan dueTime, int count)
    {
        var deadline = DateTime.UtcNow + RegistrationSafetyCap;
        lock (registeredDueTimes)
        {
            while (registeredDueTimes.Count(d => d == dueTime) < count)
            {
                var remaining = deadline - DateTime.UtcNow;
                if (remaining <= TimeSpan.Zero || !Monitor.Wait(registeredDueTimes, remaining))
                    throw new TimeoutException($"Fewer than {count} timers with due time {dueTime} were created.");
            }
        }
    }
}
