// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.NET.Sdk.Functions.Http;
using Microsoft.NET.Sdk.Functions.MSBuild.Tasks;

// IMPORTANT: Do not modify this file directly with major changes
// This file is a copy from this project (with minor updates) -- https://github.com/Azure/azure-functions-vs-build-sdk/blob/b0e54a832a92119e00a2b1796258fcf88e0d6109/src/Microsoft.NET.Sdk.Functions.MSBuild/Microsoft.NET.Sdk.Functions.MSBuild.csproj
// Please make any changes upstream first.

namespace Microsoft.NET.Sdk.Functions.Tasks
{
#if NET472
    [LoadInSeparateAppDomain]
    public class ZipDeployTask : AppDomainIsolatedTask
#else
    public class ZipDeployTask : Task
#endif
    {
        private const string UserAgentName = "functions-core-tools";

        [Required]
        public string? ZipToPublishPath { get; set; }

        [Required]
        public string? DeploymentUsername { get; set; }

        [Required]
        public string? DeploymentPassword { get; set; }

        [Required]
        public string? UserAgentVersion { get; set; }

        public string? PublishUrl { get; set; }


        /// <summary>
        /// Our fallback if PublishUrl is not given, which is the case for ZIP Deploy profiles created prior to 15.8 Preview 4.
        /// Using this will fail if the site is a slot.
        /// </summary>
        public string? SiteName { get; set; }

        public override bool Execute()
        { 
            using (DefaultHttpClient client = new DefaultHttpClient())
            {
                System.Threading.Tasks.Task<bool> t = ZipDeployAsync(ZipToPublishPath!, DeploymentUsername!, DeploymentPassword!, PublishUrl, SiteName!, UserAgentVersion!, client, true);
                t.Wait();
                return t.Result;
            }
        }

        internal async System.Threading.Tasks.Task<bool> ZipDeployAsync(string zipToPublishPath, string userName, string password, string? publishUrl, string siteName, string userAgentVersion, IHttpClient client, bool logMessages)
        {
            if (!File.Exists(zipToPublishPath) || client == null)
            {
                return false;
            }

            string zipDeployPublishUrl;
            if (!string.IsNullOrEmpty(publishUrl))
            {
                if (!publishUrl!.EndsWith("/"))
                {
                    publishUrl += "/";
                }

                zipDeployPublishUrl = publishUrl + "api/zipdeploy";
            }
            else if (!string.IsNullOrEmpty(siteName))
            {
                zipDeployPublishUrl = $"https://{siteName}.scm.azurewebsites.net/api/zipdeploy";
            }
            else
            {
                if (logMessages)
                {
                    Log.LogError(StringMessages.NeitherSiteNameNorPublishUrlGivenError);
                }

                return false;
            }

            if (logMessages)
            {
                Log.LogMessage(MessageImportance.High, String.Format(StringMessages.PublishingZipViaZipDeploy, zipToPublishPath, zipDeployPublishUrl));
            }

            // use the async version of the api
            Uri uri = new Uri($"{zipDeployPublishUrl}?isAsync=true", UriKind.Absolute);
            string userAgent = $"{UserAgentName}/{userAgentVersion}";
            FileStream stream = File.OpenRead(zipToPublishPath);
            IHttpResponse response = await client.PostWithBasicAuthAsync(uri, userName, password, "application/zip", userAgent, Encoding.UTF8, stream);
            if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.Accepted)
            {
                if (logMessages)
                {
                    Log.LogError(String.Format(StringMessages.ZipDeployFailureErrorMessage, zipDeployPublishUrl, response.StatusCode));
                }

                return false;
            }
            else
            {
                if (logMessages)
                {
                    Log.LogMessage(StringMessages.ZipFileUploaded);
                }

                string deploymentUrl = response.GetHeader("Location").FirstOrDefault();
                if (!string.IsNullOrEmpty(deploymentUrl))
                {
                    ZipDeploymentStatus deploymentStatus = new ZipDeploymentStatus(client, userAgent, Log, logMessages);
                    DeployStatus status = await deploymentStatus.PollDeploymentStatusAsync(deploymentUrl, userName, password);
                    if (status == DeployStatus.Success)
                    {
                        Log.LogMessage(MessageImportance.High, StringMessages.ZipDeploymentSucceeded);
                        return true;
                    }
                    else if (status == DeployStatus.Failed || status == DeployStatus.Unknown)
                    {
                        Log.LogError(String.Format(StringMessages.ZipDeployFailureErrorMessage, zipDeployPublishUrl, status));
                        return false;
                    }
                }
            }

            return true;
        }
    }
}

