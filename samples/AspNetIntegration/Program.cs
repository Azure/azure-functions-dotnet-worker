// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license informations

using Microsoft.Extensions.Hosting;

FunctionsApplicationBuilder builder = FunctionsWebApplication.CreateBuilder(args);
IHost app = builder.Build();

app.Run();
