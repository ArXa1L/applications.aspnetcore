﻿#if NETCOREAPP3_1
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Vostok.Commons.Helpers.Extensions;
using Vostok.Logging.Abstractions;

// ReSharper disable MethodSupportsCancellation

namespace Vostok.Applications.AspNetCore.Helpers
{
    internal class GenericHostManager : IDisposable
    {
        private readonly IHost host;
        private readonly ILog log;
        private volatile IHostApplicationLifetime lifetime;
        private volatile IDisposable shutdownRegistration;

        public GenericHostManager(IHost host, ILog log)
        {
            this.host = host;
            this.log = log;
        }

        public IServiceProvider Services => host.Services;

        public async Task StartHostAsync(CancellationToken shutdownToken)
        {
            lifetime = (IHostApplicationLifetime)host.Services.GetService(typeof(IHostApplicationLifetime));
            var environment = (IHostEnvironment)host.Services.GetService(typeof(IHostEnvironment));

            shutdownRegistration = shutdownToken.Register(
                () => host
                    .StopAsync()
                    .ContinueWith(t => log.Error(t.Exception, "Failed to stop Host."), TaskContinuationOptions.OnlyOnFaulted));

            log.Info("Starting Host.");

            log.Info("Hosting environment: {HostingEnvironment}.", environment.EnvironmentName);

            await host.StartAsync(shutdownToken);

            await lifetime.ApplicationStarted.WaitAsync();

            log.Info("Host started.");
        }

        public async Task RunHostAsync()
        {
            await lifetime.ApplicationStopping.WaitAsync();

            log.Info("Stopping Host.");

            await lifetime.ApplicationStopped.WaitAsync();

            log.Info("Host stopped.");

            host.Dispose();
        }

        public void Dispose()
        {
            host?.Dispose();
            shutdownRegistration?.Dispose();
        }
    }
}
#endif