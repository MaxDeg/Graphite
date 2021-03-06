﻿// Copyright (c) Allan Hardy. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using App.Metrics.Configuration;
using App.Metrics.Filtering;
using App.Metrics.Reporting;
using App.Metrics.Reporting.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace App.Metrics.Extensions.Reporting.Graphite.Facts.Extensions
{
    public class GraphiteReporterExtensionsTests
    {
        [Fact]
        public void can_add_graphite_provider_with_custom_settings()
        {
            var factory = SetupReportFactory();
            var settings = new GraphiteReporterSettings
                           {
                               HttpPolicy = new HttpPolicy
                                            {
                                                BackoffPeriod = TimeSpan.FromMinutes(1)
                                            }
                           };
            Action action = () => { factory.AddGraphite(settings); };

            action.ShouldNotThrow();
        }

        [Fact]
        public void can_add_graphite_provider_with_custom_settings_and_filter()
        {
            var factory = SetupReportFactory();

            var settings = new GraphiteReporterSettings
            {
                               HttpPolicy = new HttpPolicy
                                            {
                                                BackoffPeriod = TimeSpan.FromMinutes(1)
                                            }
                           };
            Action action = () => { factory.AddGraphite(settings, new DefaultMetricsFilter()); };

            action.ShouldNotThrow();
        }

        [Fact]
        public void can_add_graphite_provider_with_filter()
        {
            var factory = SetupReportFactory();

            Action action = () => { factory.AddGraphite(new Uri("net.tcp://localhost"), new DefaultMetricsFilter()); };

            action.ShouldNotThrow();
        }

        [Fact]
        public void can_add_graphite_provider_without_filter()
        {
            var factory = SetupReportFactory();

            Action action = () => { factory.AddGraphite(new Uri("net.tcp://localhost")); };

            action.ShouldNotThrow();
        }

        private static ReportFactory SetupReportFactory()
        {
            var metricsMock = new Mock<IMetrics>();
            var options = new AppMetricsOptions();
            return new ReportFactory(options, metricsMock.Object, new LoggerFactory());
        }
    }
}