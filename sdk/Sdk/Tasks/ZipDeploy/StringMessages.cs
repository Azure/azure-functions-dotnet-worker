// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

﻿namespace Microsoft.NET.Sdk.Functions.Tasks
{
    public static class StringMessages
    {
        public const string DeploymentStatus = "Deployment status is {0}.";
        public const string DeploymentStatusPolling = "Polling for deployment status...";
        public const string NeitherSiteNameNorPublishUrlGivenError = "Neither SiteName nor PublishUrl was given a value.";
        public const string PublishingZipViaZipDeploy = "Publishing {0} to {1}...";
        public const string ZipDeployDeploymentStatus = "Checking the deployment status...";
        public const string ZipDeployFailureErrorMessage = "The attempt to publish the ZIP file through {0} failed with HTTP status code {1}.";
        public const string ZipDeploymentFailed = "Zip Deployment failed.";
        public const string ZipDeploymentSucceeded = "Zip Deployment succeeded.";
        public const string ZipFileUploaded = "Uploaded the Zip file to the target.";
    }
}
