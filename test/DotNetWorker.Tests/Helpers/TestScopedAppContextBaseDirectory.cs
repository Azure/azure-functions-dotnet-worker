// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker.Tests;

internal sealed class TestScopedAppContextBaseDirectory : IDisposable
{
    private const string BaseDirectoryKey = "APP_CONTEXT_BASE_DIRECTORY";
    private readonly object _previousValue;
    private bool _disposed;

    public TestScopedAppContextBaseDirectory(string directory)
    {
        _previousValue = AppContext.GetData(BaseDirectoryKey);
        AppContext.SetData(BaseDirectoryKey, directory);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            AppContext.SetData(BaseDirectoryKey, _previousValue);
            _disposed = true;
        }
    }
}
