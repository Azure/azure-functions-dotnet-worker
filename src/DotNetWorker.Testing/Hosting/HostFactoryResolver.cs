// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// Adapted for Azure Functions from dotnet/runtime HostFactoryResolver:
// https://github.com/dotnet/runtime/blob/main/src/libraries/Microsoft.Extensions.HostFactoryResolver/src/HostFactoryResolver.cs

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;

namespace Microsoft.Azure.Functions.Worker.Testing.Hosting;

internal static class HostFactoryResolver
{
    internal static Func<string[], object>? ResolveHostFactory(
        Assembly assembly,
        TimeSpan waitTimeout,
        Action<object> configureHostBuilder,
        Action<Exception?> entryPointCompleted)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentNullException.ThrowIfNull(configureHostBuilder);
        ArgumentNullException.ThrowIfNull(entryPointCompleted);

        MethodInfo? entryPoint = assembly.EntryPoint;
        if (entryPoint is null)
        {
            return null;
        }

        return args => new HostingListener(
            args,
            entryPoint,
            assembly,
            waitTimeout,
            configureHostBuilder,
            entryPointCompleted).CreateHost();
    }

    private sealed class HostingListener :
        IObserver<DiagnosticListener>,
        IObserver<KeyValuePair<string, object?>>
    {
        private static readonly AsyncLocal<HostingListener?> CurrentListener = new();
        private readonly string[] _args;
        private readonly MethodInfo _entryPoint;
        private readonly Assembly _applicationAssembly;
        private readonly TimeSpan _waitTimeout;
        private readonly Action<object> _configure;
        private readonly Action<Exception?> _entryPointCompleted;
        private readonly TaskCompletionSource<object> _host = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly object _subscriptionLock = new();
        private readonly List<IDisposable> _hostingSubscriptions = new();
        private IDisposable? _applicationAssemblyScope;
        private bool _subscriptionsDisposed;

        internal HostingListener(
            string[] args,
            MethodInfo entryPoint,
            Assembly applicationAssembly,
            TimeSpan waitTimeout,
            Action<object> configure,
            Action<Exception?> entryPointCompleted)
        {
            _args = args;
            _entryPoint = entryPoint;
            _applicationAssembly = applicationAssembly;
            _waitTimeout = waitTimeout;
            _configure = configure;
            _entryPointCompleted = entryPointCompleted;
        }

        internal object CreateHost()
        {
            IDisposable allListenersSubscription = DiagnosticListener.AllListeners.Subscribe(this);
            var thread = new Thread(RunEntryPoint) { IsBackground = true };
            thread.Start();

            try
            {
                if (!_host.Task.Wait(_waitTimeout))
                {
                    throw new TimeoutException(
                        $"Timed out waiting {_waitTimeout} for entry point '{_applicationAssembly.FullName}' to build an IHost.");
                }
            }
            catch (AggregateException) when (_host.Task.IsCompleted)
            {
            }
            finally
            {
                allListenersSubscription.Dispose();
                IDisposable[] subscriptions;
                lock (_subscriptionLock)
                {
                    _subscriptionsDisposed = true;
                    subscriptions = _hostingSubscriptions.ToArray();
                    _hostingSubscriptions.Clear();
                }

                foreach (IDisposable subscription in subscriptions)
                {
                    subscription.Dispose();
                }
            }

            return _host.Task.GetAwaiter().GetResult();
        }

        public void OnCompleted()
        {
            // Completion belongs to one DiagnosticListener. CreateHost owns and
            // disposes every subscription so one host cannot unsubscribe another.
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(DiagnosticListener listener)
        {
            if (listener.Name == "Microsoft.Extensions.Hosting")
            {
                IDisposable subscription = listener.Subscribe(this);
                lock (_subscriptionLock)
                {
                    if (_subscriptionsDisposed)
                    {
                        subscription.Dispose();
                    }
                    else
                    {
                        _hostingSubscriptions.Add(subscription);
                    }
                }
            }
        }

        public void OnNext(KeyValuePair<string, object?> value)
        {
            if (CurrentListener.Value != this)
            {
                return;
            }

            if (value.Key == "HostBuilding")
            {
                _configure(value.Value ?? throw new InvalidOperationException("The HostBuilding diagnostic event contained no builder."));
            }
            else if (value.Key == "HostBuilt")
            {
                DisposeApplicationAssemblyScope();
                _host.TrySetResult(value.Value ?? throw new InvalidOperationException("The HostBuilt diagnostic event contained no host."));
            }
        }

        private void RunEntryPoint()
        {
            Exception? exception = null;
            try
            {
                CurrentListener.Value = this;
                _applicationAssemblyScope = WorkerApplicationAssemblyContext.Push(_applicationAssembly);

                object? result = _entryPoint.GetParameters().Length == 0
                    ? _entryPoint.Invoke(null, Array.Empty<object>())
                    : _entryPoint.Invoke(null, new object[] { _args });

                if (result is Task task)
                {
                    task.GetAwaiter().GetResult();
                }

                _host.TrySetException(new InvalidOperationException(
                    $"Entry point '{_applicationAssembly.FullName}' exited without building an IHost."));
            }
            catch (TargetInvocationException invocationException) when (invocationException.InnerException?.GetType().Name == "HostAbortedException")
            {
            }
            catch (TargetInvocationException invocationException)
            {
                exception = invocationException.InnerException ?? invocationException;
                _host.TrySetException(exception);
            }
            catch (Exception caught)
            {
                exception = caught;
                _host.TrySetException(caught);
            }
            finally
            {
                DisposeApplicationAssemblyScope();
                CurrentListener.Value = null;
                _entryPointCompleted(exception);
            }
        }

        private void DisposeApplicationAssemblyScope()
        {
            _applicationAssemblyScope?.Dispose();
            _applicationAssemblyScope = null;
        }
    }
}
