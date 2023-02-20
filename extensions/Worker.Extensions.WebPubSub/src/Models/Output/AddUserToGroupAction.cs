// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Operation to add a user to group.
    /// </summary>
    public sealed class AddUserToGroupAction : WebPubSubAction
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
