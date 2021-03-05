﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

﻿using System;

namespace Microsoft.Azure.Functions.Worker.Extensions.Abstractions
{
    public interface IBatchedInput
    {
        public bool IsBatched { get; set; }
    }
}
