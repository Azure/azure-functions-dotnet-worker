// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Linq;
using System;

namespace Microsoft.Azure.Functions.Worker.Sdk
{
    internal static class StringExtensions
    {
        /// <summary>
        /// Returns a copy of the string where the first character is in lower case.
        /// </summary>
        public static string ToLowerFirstCharacter(this string str)
        {
            if (!string.IsNullOrEmpty(str))
            {
                return Char.ToLowerInvariant(str.First()) + str.Substring(1);
            }
            else
            {
                return str;
            }
        }
    }
}
