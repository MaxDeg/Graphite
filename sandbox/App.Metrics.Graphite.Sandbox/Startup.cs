﻿using System;
using App.Metrics.Configuration;
using App.Metrics.Extensions.Reporting.Graphite;
using App.Metrics.Extensions.Reporting.Graphite.Client;
using App.Metrics.Filtering;
using App.Metrics.Graphite.Sandbox.JustForTesting;
using App.Metrics.Reporting.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace App.Metrics.Graphite.Sandbox
{
    public class Startup
    {
        private static readonly bool HaveAppRunSampleRequests = true;
        private static readonly bool RunSamplesWithClientId = true;

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder().SetBasePath(env.ContentRootPath).
                                                     AddJsonFile("appsettings.json", optional: false, reloadOnChange: true).
                                                     AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true).
                                                     AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IApplicationLifetime lifetime)
        {
            if (RunSamplesWithClientId && HaveAppRunSampleRequests)
            {
                app.Use(
                    (context, func) =>
                    {
                        RandomClientIdForTesting.SetTheFakeClaimsPrincipal(context);
                        return func();
                    });
            }

            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            // loggerFactory.AddDebug();

            app.UseMetrics();
            app.UseMetricsReporting(lifetime);

            app.UseMvc();

            if (HaveAppRunSampleRequests)
            {
                SampleRequests.Run(lifetime.ApplicationStopping);
            }
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTestStuff();
            services.AddLogging().AddRouting(options => { options.LowercaseUrls = true; });

            services.AddMvc(options => options.AddMetricsResourceFilter());

            var reportFilter = new DefaultMetricsFilter();
            reportFilter.WithHealthChecks(false);

            services.AddMetrics(Configuration.GetSection("AppMetrics")).
                     AddGraphiteMetricsTextSerialization().
                     AddGraphiteMetricsSerialization().
                     AddAsciiEnvironmentInfoSerialization().
                     AddAsciiHealthSerialization().
                     AddReporting(
                         factory =>
                         {
                             factory.AddGraphite(
                                 new GraphiteReporterSettings
                                 {
                                     HttpPolicy = new HttpPolicy
                                                  {
                                                      FailuresBeforeBackoff = 3,
                                                      BackoffPeriod = TimeSpan.FromSeconds(30),
                                                      Timeout = TimeSpan.FromSeconds(10)
                                                  },
                                     GraphiteSettings = new GraphiteSettings(new Uri("net.tcp://localhost:32771")),
                                     // GraphiteSettings = new GraphiteSettings(new Uri("net.udp://localhost:32771")),
                                     ReportInterval = TimeSpan.FromSeconds(5)
                                 });
                         }).
                     AddHealthChecks(
                         factory =>
                         {
                             factory.RegisterPingHealthCheck("google ping", "google.com", TimeSpan.FromSeconds(10));
                             factory.RegisterHttpGetHealthCheck("github", new Uri("https://github.com/"), TimeSpan.FromSeconds(10));
                         }).
                     AddMetricsMiddleware(Configuration.GetSection("AspNetMetrics"));
        }
    }
}