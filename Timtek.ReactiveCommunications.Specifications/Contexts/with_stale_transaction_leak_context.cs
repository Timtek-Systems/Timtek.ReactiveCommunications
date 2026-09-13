// This file is part of the Timtek.ReactiveCommunications project
// 
// Copyright © 2026 Timtek Systems Limited, all rights reserved.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so. The Software comes with no warranty of any kind. You make use of the Software entirely at your own risk and assume all liability arising from your use thereof.
// 
// File: with_stale_transaction_leak_context.cs  Last modified: 2026-08-05 by Copilot

using Machine.Specifications;

namespace Timtek.ReactiveCommunications.Specifications.Contexts;

internal class with_stale_transaction_leak_context
{
    protected static StaleTransactionLeakContext Context;
    protected static StaleTransactionLeakContextBuilder ContextBuilder;

    Establish context = () => ContextBuilder = new StaleTransactionLeakContextBuilder();

    Cleanup after = () =>
    {
        Context = null;
        ContextBuilder = null;
    };
}
