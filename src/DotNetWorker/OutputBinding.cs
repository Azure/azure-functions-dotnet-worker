// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker
{
    public abstract class OutputBinding<T>
    {
        internal abstract T GetValue();

        public abstract void SetValue(T value);
    }
}
