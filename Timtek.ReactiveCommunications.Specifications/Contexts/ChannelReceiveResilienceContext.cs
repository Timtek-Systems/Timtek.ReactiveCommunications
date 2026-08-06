// This file is part of the Timtek.ReactiveCommunications project
// 
// Copyright © 2026 Timtek Systems Limited, all rights reserved.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so. The Software comes with no warranty of any kind. You make use of the Software entirely at your own risk and assume all liability arising from your use thereof.
// 
// File: ChannelReceiveResilienceContext.cs  Last modified: 2026-08-06 by Copilot

using System;
using Timtek.ReactiveCommunications.Specifications.Fakes;

namespace Timtek.ReactiveCommunications.Specifications.Contexts;

/// <summary>
///     Holds the inputs and outputs for specs that exercise a long-lived <see cref="SerialCommunicationChannel" />
///     across multiple independent "subscribe, send, receive, dispose" cycles against
///     <see cref="ICommunicationChannel.ObservableReceivedCharacters" />, with a simulated transient
///     receive error occurring between cycles.
/// </summary>
internal class ChannelReceiveResilienceContext
{
    /// <summary>The channel under test.</summary>
    public SerialCommunicationChannel Channel { get; set; }

    /// <summary>The fake serial port backing the channel.</summary>
    public QueuedDataTestSerialPort Port { get; set; }

    /// <summary>Text captured by the first cycle's subscription, before the simulated glitch.</summary>
    public string FirstCycleReceivedText { get; set; } = string.Empty;

    /// <summary>Text captured by the second cycle's subscription, after the simulated glitch.</summary>
    public string SecondCycleReceivedText { get; set; } = string.Empty;

    /// <summary>
    ///     Any error immediately replayed to the second cycle's subscription, if the shared subject had
    ///     already permanently terminated by the time it subscribed.
    /// </summary>
    public Exception SecondCycleError { get; set; }

    /// <summary>Whether the channel reports itself open after the simulated glitch.</summary>
    public bool ChannelIsOpenAfterGlitch { get; set; }
}
