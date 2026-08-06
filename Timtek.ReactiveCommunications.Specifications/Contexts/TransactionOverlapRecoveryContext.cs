// This file is part of the Timtek.ReactiveCommunications project
// 
// Copyright © 2026 Timtek Systems Limited, all rights reserved.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so. The Software comes with no warranty of any kind. You make use of the Software entirely at your own risk and assume all liability arising from your use thereof.
// 
// File: TransactionOverlapRecoveryContext.cs  Last modified: 2026-08-05 by Copilot

using System;
using Timtek.ReactiveCommunications.Transactions;

namespace Timtek.ReactiveCommunications.Specifications.Contexts;

/// <summary>
///     Holds the inputs and outputs for specs that exercise how <see cref="TransactionObserver" />
///     recovers (or fails to recover) after a channel <c>Send</c> failure on a previous transaction.
/// </summary>
internal class TransactionOverlapRecoveryContext
{
    public ICommunicationChannel Channel { get; set; }
    public TransactionObserver Observer { get; set; }
    public NoReplyTransaction FirstTransaction { get; set; }
    public NoReplyTransaction SecondTransaction { get; set; }
    public Exception ExceptionFromFirstTransaction { get; set; }
    public Exception ExceptionFromSecondTransaction { get; set; }
}
