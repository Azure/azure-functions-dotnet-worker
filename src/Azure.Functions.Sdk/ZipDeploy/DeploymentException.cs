// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Net;

namespace Azure.Functions.Sdk.ZipDeploy;

public sealed class DeploymentException : Exception
{
    public DeploymentException(string message)
        : base(message)
    {
    }

    public DeploymentException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public DeployStatus DeployStatus { get; init; } = DeployStatus.Unknown;

    public HttpStatusCode StatusCode { get; init; } = HttpStatusCode.InternalServerError;
}
