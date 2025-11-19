// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using NuGet.Packaging;

namespace Azure.Functions.Sdk;

/// <summary>
/// A Fowler-Noll-Vo (FNV) 64-bit hash function that supports non-cryptographic hashing to optimize speed and minimize collisions.
/// https://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function
/// </summary>
/// <remarks>
/// Code from https://github.com/NuGet/NuGet.Client/blob/6.14.1.1/src/NuGet.Core/NuGet.ProjectModel/FnvHash64Function.cs
/// </remarks>
internal sealed class FnvHash64Function : IHashFunction
{
    private ulong _hash;

    public void Update(byte[] data, int offset, int count)
    {
        Throw.IfNull(data);
        Throw.IfLessThan(count, 0);

        _hash = _hash == 0
            ? FnvHash64.Hash(data.AsSpan(offset, count))
            : FnvHash64.Update(_hash, data.AsSpan(offset, count));
    }

    public byte[] GetHashBytes()
    {
        return BitConverter.GetBytes(_hash);
    }

    public string GetHash()
    {
        return Convert.ToBase64String(GetHashBytes());
    }

    // Interface is Disposable for SHA512 - this class has nothing to dispose.
    public void Dispose() { }

    internal static class FnvHash64
    {
        public const ulong Offset = 14695981039346656037;
        public const ulong Prime = 1099511628211;

        public static ulong Hash(ReadOnlySpan<byte> data)
        {
            ulong hash = Offset;

            unchecked
            {
                foreach (byte b in data)
                {
                    hash = (hash ^ b) * Prime;
                }
            }

            return hash;
        }

        public static ulong Combine(ulong left, ulong right)
        {
            unchecked
            {
                return (left ^ right) * Prime;
            }
        }

        public static ulong Update(ulong current, ReadOnlySpan<byte> data)
        {
            return Combine(current, Hash(data));
        }
    }
}
