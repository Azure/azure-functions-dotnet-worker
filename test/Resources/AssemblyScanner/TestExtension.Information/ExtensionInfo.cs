// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// Carries only extension information (no WebJobs startup). Exercises TryGetExtensionReference
// via the Worker.Extensions.Abstractions marker attribute in isolation.
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

[assembly: ExtensionInformation("MyExtension", "1.2.3")]
