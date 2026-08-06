// This file is part of the Timtek.ReactiveCommunications project
// 
// Copyright © 2026 Timtek Systems Limited, all rights reserved.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so. The Software comes with no warranty of any kind. You make use of the Software entirely at your own risk and assume all liability arising from your use thereof.
// 
// File: ChannelReceiveResilienceContextBuilder.cs  Last modified: 2026-08-06 by Copilot

using Timtek.ReactiveCommunications.Specifications.Fakes;

namespace Timtek.ReactiveCommunications.Specifications.Contexts;

/// <summary>
///     Builds a <see cref="ChannelReceiveResilienceContext" /> around a real <see cref="SerialCommunicationChannel" />
///     backed by a <see cref="QueuedDataTestSerialPort" />, so that the channel's actual receive pipeline
///     (including its single, long-lived <c>Publish()</c>'d subject) is exercised exactly as it would be
///     in production.
/// </summary>
internal class ChannelReceiveResilienceContextBuilder
{
    private EofBehaviour eofBehaviour = EofBehaviour.Error;

    internal ChannelReceiveResilienceContextBuilder WithEofBehaviour(EofBehaviour behaviour)
    {
        eofBehaviour = behaviour;
        return this;
    }

    internal ChannelReceiveResilienceContext Build()
    {
        var port = new QueuedDataTestSerialPort();
        var endpoint = new SerialDeviceEndpoint("COM1");
        var channel = new SerialCommunicationChannel(endpoint, eofBehaviour, port);

        return new ChannelReceiveResilienceContext
        {
            Channel = channel,
            Port = port
        };
    }
}
