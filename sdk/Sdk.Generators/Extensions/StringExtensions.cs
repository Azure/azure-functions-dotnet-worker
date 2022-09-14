﻿using System;

namespace Microsoft.Azure.Functions.Worker.Sdk.Generators
{
    internal static class StringExtensions
    {
        public static string TrimStringFromEnd(this string str, string end)
        {
            if(end is null)
            {
                throw new ArgumentNullException($"{nameof(str)} is null.");
            }

            return str.EndsWith(end) ? str.Substring(0, str.Length - end.Length) : str;
        }

        public static string TrimStringsFromEnd(this string str, string[] strings)
        {
            var result = str;

            foreach(string s in strings)
            {
                result = result.TrimStringFromEnd(s);
            }

            return result; 
        }
    }
}
