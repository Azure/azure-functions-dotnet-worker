// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Operation to remove a user from group.
    /// </summary>
    public sealed class RemoveUserFromGroupAction : WebPubSubAction
    {
        /// <summary>
        /// Target userId.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Target group name.
        /// </summary>
        public string Group { get; set; }
    }
}
