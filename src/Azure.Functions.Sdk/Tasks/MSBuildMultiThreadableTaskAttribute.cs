// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Build.Framework;

// Polyfill for MSBuild 18+ multi-threaded task opt-in while compiling against Microsoft.Build.Framework 17.11.48.
// MSBuild's TaskRouter recognizes this attribute by full type name.
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
internal sealed class MSBuildMultiThreadableTaskAttribute : Attribute
{
}
