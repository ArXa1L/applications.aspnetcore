﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Vostok.Commons.Helpers.Extensions;
using Vostok.Hosting.Abstractions;
using Vostok.Hosting.Abstractions.Requirements;
using Vostok.Hosting.AspNetCore.Builders;
using Vostok.Logging.Abstractions;

namespace Vostok.Hosting.AspNetCore
{
    /// <summary>
    /// <para><see cref="VostokAspNetCoreApplication{TStartup}"/> is the abstract class developers inherit from in order to create Vostok-compatible AspNetCore services.</para>
    /// <para>Implement <see cref="Setup"/> method to configure <see cref="IWebHostBuilder"/> and customize built-in Vostok middlewares (see <see cref="IVostokAspNetCoreApplicationBuilder"/>).</para>
    /// <para>Override <see cref="WarmupAsync"/> method to perform any additional initialization after the DI container gets built but before the app gets registered in service discovery.</para>
    /// </summary>
    [PublicAPI]
    [RequiresPort]
    public abstract class VostokAspNetCoreApplication<TStartup> : IVostokApplication, IDisposable
        where TStartup : class
    {
        private readonly List<IDisposable> disposables = new List<IDisposable>();

        private volatile IHostApplicationLifetime lifetime;
        private volatile IHost host;
        private volatile ILog log;

        public async Task InitializeAsync(IVostokHostingEnvironment environment)
        {
            var builder = new VostokAspNetCoreApplicationBuilder<TStartup>();

            Setup(builder, environment);

            disposables.Add(host = builder.Build(environment));

            await StartHostAsync(environment).ConfigureAwait(false);

            await WarmupAsync(environment, host.Services).ConfigureAwait(false);
        }

        public Task RunAsync(IVostokHostingEnvironment environment) =>
            RunHostAsync();

        /// <summary>
        /// Override this method to configure <see cref="IWebHostBuilder"/> and customize built-in Vostok middleware components.
        /// </summary>
        public virtual void Setup([NotNull] IVostokAspNetCoreApplicationBuilder builder, [NotNull] IVostokHostingEnvironment environment)
        {
        }

        /// <summary>
        /// Override this method to perform any initialization that needs to happen after DI container is built and host is started, but before registering in service discovery.
        /// </summary>
        public virtual Task WarmupAsync([NotNull] IVostokHostingEnvironment environment, [NotNull] IServiceProvider serviceProvider) =>
            Task.CompletedTask;

        public void Dispose()
            => disposables.ForEach(disposable => disposable?.Dispose());

        private async Task StartHostAsync(IVostokHostingEnvironment environment)
        {
            log = environment.Log.ForContext<VostokAspNetCoreApplication<TStartup>>();

            lifetime = (IHostApplicationLifetime)host.Services.GetService(typeof(IHostApplicationLifetime));

            disposables.Add(
                environment.ShutdownToken.Register(
                    () => host
                        .StopAsync()
                        .ContinueWith(t => log.Error(t.Exception, "Failed to stop Host."), TaskContinuationOptions.OnlyOnFaulted)));

            log.Info("Starting Host.");

            await host.StartAsync(environment.ShutdownToken).ConfigureAwait(false);

            await lifetime.ApplicationStarted.WaitAsync();
            log.Info("Host started.");
        }

        private async Task RunHostAsync()
        {
            await lifetime.ApplicationStopping.WaitAsync();
            log.Info("Stopping Host.");

            await lifetime.ApplicationStopped.WaitAsync();
            log.Info("Host stopped.");

            host.Dispose();
        }
    }
}
