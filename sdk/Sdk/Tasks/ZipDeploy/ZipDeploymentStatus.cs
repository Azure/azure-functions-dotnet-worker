// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Utilities;
using Microsoft.NET.Sdk.Functions.Http;
using Microsoft.NET.Sdk.Functions.Tasks;
using Task = System.Threading.Tasks.Task;

// IMPORTANT: Do not modify this file directly with major changes
// This file is a copy from this project (with minor updates) -- https://github.com/Azure/azure-functions-vs-build-sdk/blob/b0e54a832a92119e00a2b1796258fcf88e0d6109/src/Microsoft.NET.Sdk.Functions.MSBuild/Microsoft.NET.Sdk.Functions.MSBuild.csproj
// Please make any changes upstream first.

namespace Microsoft.NET.Sdk.Functions.MSBuild.Tasks
{
    internal class ZipDeploymentStatus
    {
        private const int MaxMinutesToWait = 3;
        private const int StatusRefreshDelaySeconds = 3;
        private const int RetryCount = 3;
        private const int RetryDelaySeconds = 1;

        private readonly IHttpClient _client;
        private readonly string _userAgent;
        private readonly TaskLoggingHelper _log;
        private readonly bool _logMessages;

        public ZipDeploymentStatus(IHttpClient client, string userAgent, TaskLoggingHelper log, bool logMessages)
        {
            _client = client;
            _userAgent = userAgent;
            _log = log;
            _logMessages = logMessages;
        }

        public async Task<DeployStatus> PollDeploymentStatusAsync(string deploymentUrl, string userName, string password)
        {
            var deployStatus = DeployStatus.Pending;
            var deployStatusText = string.Empty;
            var tokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(MaxMinutesToWait));

            if (_logMessages)
            {
                _log.LogMessage(StringMessages.DeploymentStatusPolling);
            }
            while (!tokenSource.IsCancellationRequested 
                && deployStatus != DeployStatus.Success 
                && deployStatus != DeployStatus.PartialSuccess
                && deployStatus != DeployStatus.Failed 
                && deployStatus != DeployStatus.Conflict
                && deployStatus != DeployStatus.Unknown)
            {
                try
                {
                    (deployStatus, deployStatusText) = await GetDeploymentStatusAsync(deploymentUrl, userName, password, RetryCount, TimeSpan.FromSeconds(RetryDelaySeconds), tokenSource);
                    if (_logMessages)
                    {
                        var deployStatusName = Enum.GetName(typeof(DeployStatus), deployStatus);

                        var message = string.IsNullOrEmpty(deployStatusText)
                            ? string.Format(StringMessages.DeploymentStatus, deployStatusName)
                            : string.Format(StringMessages.DeploymentStatusWithText, deployStatusName, deployStatusText);

                        _log.LogMessage(message);
                    }
                }
                catch (HttpRequestException)
                {
                    return DeployStatus.Unknown;
                }

                await Task.Delay(TimeSpan.FromSeconds(StatusRefreshDelaySeconds));
            }

            return deployStatus;
        }

        private async Task<(DeployStatus, string)> GetDeploymentStatusAsync(string deploymentUrl, string userName, string password, int retryCount, TimeSpan retryDelay, CancellationTokenSource cts)
        {
            var status = DeployStatus.Unknown;
            var statusText = string.Empty;

            IDictionary<string, object>? json = await InvokeGetRequestWithRetryAsync<Dictionary<string, object>>(deploymentUrl, userName, password, retryCount, retryDelay, cts);

            if (json is not null)
            {
                // status
                if (TryParseDeploymentStatus(json, out DeployStatus result))
                {
                    status = result;
                }

                // status text message
                if (TryParseDeploymentStatusText(json, out string text))
                {
                    statusText = text;
                }
            }

            return (status, statusText);
        }

        private static bool TryParseDeploymentStatus(IDictionary<string, object> json, out DeployStatus status)
        {
            status = DeployStatus.Unknown;

            if (json.TryGetValue("status", out object statusObj)
                && int.TryParse(statusObj.ToString(), out int statusInt)
                && Enum.TryParse(statusInt.ToString(), out status))
            {
                return true;
            }

            return false;
        }

        private static bool TryParseDeploymentStatusText(IDictionary<string, object> json, out string statusText)
        {
            statusText = string.Empty;

            if (json.TryGetValue("status_text", out var textObj)
                && textObj is not null)
            {
                statusText = textObj.ToString();
                return true;
            }

            return false;
        }

        private async Task<T?> InvokeGetRequestWithRetryAsync<T>(string url, string userName, string password, int retryCount, TimeSpan retryDelay, CancellationTokenSource cts)
        {
            IHttpResponse? response = null;
            await RetryAsync(async () =>
            {
                response = await _client.GetRequestAsync(new Uri(url, UriKind.RelativeOrAbsolute), userName, password, _userAgent, cts.Token);
            }, retryCount, retryDelay);

            if (response!.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.Accepted)
            {
                return default;
            }
            else
            {
                using (var stream = await response.GetResponseBodyAsync())
                {
                    return await JsonSerializer.DeserializeAsync<T>(stream);
                }
            }
        }

        private async Task RetryAsync(Func<Task> func, int retryCount, TimeSpan retryDelay)
        {
            while (true)
            {
                try
                {
                    await func();
                    return;
                }
                catch (Exception e)
                {
                    if (retryCount <= 0)
                    {
                        throw e;
                    }
                    retryCount--;
                }

                await Task.Delay(retryDelay);
            }
        }
    }
}
