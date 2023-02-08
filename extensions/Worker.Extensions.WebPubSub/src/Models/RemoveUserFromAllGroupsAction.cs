// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Operation to remove user from all groups.
    /// </summary>
    public sealed class RemoveUserFromAllGroupsAction : WebPubSubAction
    {
        /// <summary>
        /// Target UserId.
        /// </summary>
        public string UserId { get; set; }
    }
}
