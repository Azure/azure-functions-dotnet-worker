// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ExternalExtensionProject.App;

public class QueueFunction(ILogger<QueueFunction> logger)
{
    /// <summary>
    /// This function demonstrates binding to a single <see cref="QueueMessage"/>.
    /// </summary>
    [Function(nameof(QueueMessageFunction))]
    public void QueueMessageFunction([QueueTrigger("input-queue")] QueueMessage message)
    {
        logger.LogInformation(message.MessageText);
    }
}
