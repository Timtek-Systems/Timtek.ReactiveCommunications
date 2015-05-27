﻿// This file is part of the TA.Ascom.ReactiveCommunications project
// 
// Copyright © 2015 Tigra Astronomy, all rights reserved.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so,. The Software comes with no warranty of any kind.
// You make use of the Software entirely at your own risk and assume all liability arising from your use thereof.
// 
// File: NoReplyTransaction.cs  Last modified: 2015-05-25@18:22 by Tim Long

using System;

namespace TA.Ascom.ReactiveCommunications.Transactions
    {
    public class NoReplyTransaction : DeviceTransaction
        {
        public NoReplyTransaction(string command) : base(command) {}

        public override void ObserveResponse(IObservable<char> source)
            {
            Response = new Maybe<string>(string.Empty);
            OnCompleted();
            }
        }
    }