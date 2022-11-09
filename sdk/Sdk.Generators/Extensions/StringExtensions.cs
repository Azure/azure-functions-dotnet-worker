// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Functions.Worker.Sdk.Generators
{
    internal static class StringExtensions
    {
        public static string TrimStringFromEnd(this string str, string end)
        {
            if (end is null)
            {
                throw new ArgumentNullException(nameof(end));
            }

            return str.EndsWith(end) ? str.Substring(0, str.Length - end.Length) : str;
        }

        public static string TrimStringsFromEnd(this string str, IReadOnlyList<string> strings)
        {
            var result = str;

            foreach (string s in strings)
            {
                result = result.TrimStringFromEnd(s);
            }

            return result; 
        }

        public static string UppercaseFirst(this string str)
        {
            // Check for empty string.
            if (string.IsNullOrEmpty(str))
            {
                return string.Empty;
            }

            if (!char.IsUpper(str[0]))
            {
                // Return char and concat substring.
                return char.ToUpper(str[0]) + str.Substring(1);
            }
            else
            {
                return str;
            }

        }
    }
}
