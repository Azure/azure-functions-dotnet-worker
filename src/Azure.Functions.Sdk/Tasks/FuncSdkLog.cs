// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Build.Framework;

namespace Azure.Functions.Sdk.Tasks;

public class FuncSdkLog : Microsoft.Build.Utilities.Task
{
    [Required]
    public string Resource { get; set; } = string.Empty;

    public string[] Arguments { get; set; } = [];

    public override bool Execute()
    {
        Log.LogMessage(LogMessage.FromId(Resource), Arguments);
        return !Log.HasLoggedErrors;
    }
}
