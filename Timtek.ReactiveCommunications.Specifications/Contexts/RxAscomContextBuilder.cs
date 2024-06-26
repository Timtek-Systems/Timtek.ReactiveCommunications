﻿// This file is part of the Timtek.ReactiveCommunications project
// 
// Copyright © 2015-2020 Tigra Astronomy, all rights reserved.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so. The Software comes with no warranty of any kind.
// You make use of the Software entirely at your own risk and assume all liability arising from your use thereof.
// 
// File: RxAscomContextBuilder.cs  Last modified: 2020-07-20@00:51 by Tim Long

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Timtek.ReactiveCommunications.Specifications.Fakes;
using TA.Utils.Logging.NLog;
using Timtek.ReactiveCommunications;

namespace Timtek.ReactiveCommunications.Specifications.Contexts
    {
    internal class RxAscomContextBuilder
        {
        readonly ChannelFactory channelFactory;
        readonly StringBuilder fakeResponseBuilder = new StringBuilder();

        bool channelShouldBeOpen;
        string connectionString = "Fake";
        IObservable<char> observableResponse;
        PropertyChangedEventHandler propertyChangedAction;
        List<Tuple<string, Action>> propertyChangeObservers = new List<Tuple<string, Action>>();

        public RxAscomContextBuilder()
            {
            var logService = new LoggingService();
            channelFactory = new ChannelFactory(logService);
            channelFactory.RegisterChannelType(
                p => p.StartsWith("Fake", StringComparison.InvariantCultureIgnoreCase),
                connection => new FakeEndpoint(),
                endpoint => new FakeCommunicationChannel(fakeResponseBuilder.ToString())
            );
            }

        public RxAscomContext Build()
            {
            // Build the communications channel
            var channel = channelFactory.FromConnectionString(connectionString);
            var processor = new ReactiveTransactionProcessor();
            var observer = new TransactionObserver(channel);
            processor.SubscribeTransactionObserver(observer);
            if (channelShouldBeOpen)
                channel.Open();

            // Assemble the device controller test context
            var context = new RxAscomContext
                {
                Channel = channel,
                Processor = processor,
                Observer = observer
                };
            return context;
            }

        public RxAscomContextBuilder WithOpenConnection(string connection)
            {
            connectionString = connection;
            channelShouldBeOpen = true;
            return this;
            }

        public RxAscomContextBuilder WithFakeResponse(string fakeResponse)
            {
            fakeResponseBuilder.Append(fakeResponse);
            return this;
            }

        public RxAscomContextBuilder WithClosedConnection(string connection)
            {
            connectionString = connection;
            channelShouldBeOpen = false;
            return this;
            }

        public RxAscomContextBuilder OnPropertyChanged(PropertyChangedEventHandler action)
            {
            propertyChangedAction = action;
            return this;
            }

        public RxAscomContextBuilder WithObservableResponse(IObservable<char> observableResponse)
            {
            this.observableResponse = observableResponse;
            return this;
            }
        }
    }