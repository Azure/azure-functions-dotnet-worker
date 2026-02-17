// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#if NETFRAMEWORK

using System.Runtime.InteropServices;

namespace System
{
    public static class SystemExtensions
    {
        extension(OperatingSystem)
        {
            public static bool IsWindows() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        }

        extension(string? str)
        {
            public bool StartsWith(char c)
            {
                return !string.IsNullOrEmpty(str) && str![0] == c;
            }
        }
    }
}

namespace System.Collections.Generic
{
    public struct KeyValuePair
    {
        public static KeyValuePair<TKey, TValue> Create<TKey, TValue>(TKey key, TValue value)
            => new(key, value);
    }

    public static class CollectionExtensions
    {
        extension<TKey, TValue>(KeyValuePair<TKey, TValue> kvp)
        {
            public void Deconstruct(out TKey key, out TValue value)
                => (key, value) = (kvp.Key, kvp.Value);
        }
    }
}

namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public sealed class MaybeNullWhenAttribute(bool returnValue) : Attribute
    {
        public bool ReturnValue { get; } = returnValue;
    }
}

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public sealed class ModuleInitializerAttribute : Attribute
    {
    }
}

namespace System.IO
{
    public static class IOExtensions
    {
        extension(Path)
        {
            public static string GetRelativePath(string fromPath, string toPath)
            {
                // Ensure paths end with a separator if they are directories
                if (!fromPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
                {
                    fromPath += Path.DirectorySeparatorChar;
                }

                Uri fromUri = new Uri(fromPath);
                Uri toUri = new Uri(toPath);

                Uri relativeUri = fromUri.MakeRelativeUri(toUri);
                string relativePath = Uri.UnescapeDataString(relativeUri.ToString());

                return relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }
        }
    }
}
#endif
