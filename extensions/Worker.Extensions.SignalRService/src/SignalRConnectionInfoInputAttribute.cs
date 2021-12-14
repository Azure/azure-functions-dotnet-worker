// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Attribute used to bind necessary information for a SignalR client to connect to SignalR Service.
    /// <para> 
    /// The connection info object will have the following properties:
    /// <code>
    /// public class MyConnectionInfo 
    /// {
    /// public string Url { get; set; }
    /// public string AccessToken { get; set; }
    /// } 
    /// </code>
    /// </para>
    /// </summary>
    public sealed class SignalRConnectionInfoInputAttribute : SignalRNegotiationBaseAttribute
    {
    }
}
