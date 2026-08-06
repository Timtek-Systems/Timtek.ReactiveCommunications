// This file is part of the Timtek.ReactiveCommunications project
// 
// Copyright © 2026 Timtek Systems Limited, all rights reserved.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so. The Software comes with no warranty of any kind. You make use of the Software entirely at your own risk and assume all liability arising from your use thereof.
// 
// File: with_channel_receive_resilience_context.cs  Last modified: 2026-08-06 by Copilot

using Machine.Specifications;

namespace Timtek.ReactiveCommunications.Specifications.Contexts;

/// <summary>Base class owning the Establish/Cleanup lifecycle for <see cref="ChannelReceiveResilienceContext" /> specs.</summary>
internal class with_channel_receive_resilience_context
{
    protected static ChannelReceiveResilienceContext Context;
    protected static ChannelReceiveResilienceContextBuilder ContextBuilder;

    Establish context = () => ContextBuilder = new ChannelReceiveResilienceContextBuilder();

    Cleanup after = () =>
    {
        Context = null;
        ContextBuilder = null;
    };
}
