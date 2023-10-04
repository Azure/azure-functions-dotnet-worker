// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license informations


using AspNetIntegration;
using Microsoft.Extensions.Hosting;
    var host = new HostBuilder()
        .ConfigureFunctionsWorkerDefaults()
        .ConfigureFunctionsWebApplication()
        .Build();

    host.Run();
