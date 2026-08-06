// This file is part of the Timtek.ReactiveCommunications project
// 
// Copyright © 2026 Timtek Systems Limited, all rights reserved.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so. The Software comes with no warranty of any kind. You make use of the Software entirely at your own risk and assume all liability arising from your use thereof.
// 
// File: StaleTransactionLeakContext.cs  Last modified: 2026-08-05 by Copilot

using System.Reactive.Subjects;
using Timtek.ReactiveCommunications.Transactions;

namespace Timtek.ReactiveCommunications.Specifications.Contexts;

/// <summary>
///     Holds the inputs and outputs for specs that exercise how <see cref="TransactionObserver" />
///     handles a transaction whose response subscription is still attached to the shared response
///     sequence after the transaction has already timed out.
/// </summary>
internal class StaleTransactionLeakContext
{
    public ICommunicationChannel Channel { get; set; }
    public TransactionObserver Observer { get; set; }
    public Subject<char> ReceivedCharacters { get; set; }
    public BooleanTransaction TimedOutTransaction { get; set; }
    public BooleanTransaction SubsequentTransaction { get; set; }
}
