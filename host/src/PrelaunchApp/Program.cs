// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;

namespace App
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // Using some types/methods from the framework assemblies.

            try
            {
                var dictionary = new ConcurrentDictionary<string, object>();
                dictionary.TryAdd("a", new Activity("activity1"));
                dictionary.TryAdd("b", Enumerable.Range(1, 5));
                dictionary.TryAdd("c", RuntimeInformation.FrameworkDescription);
                dictionary.TryAdd("d", Environment.ProcessId);

                var obj = new
                {
                    ItemCount = dictionary.Count,
                    ItemKeys = dictionary.Select(i => i.Key).ToImmutableArray(),
                    FirstItemKey = dictionary.Keys.FirstOrDefault(),
                    Items = dictionary.OrderBy(i => i.Key).ToImmutableDictionary()
                };

                using (var stream = new MemoryStream())
                {
                    await JsonSerializer.SerializeAsync(stream, obj, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    stream.Position = 0;
                    using (var reader = new StreamReader(stream))
                    {
                        string item = await reader.ReadToEndAsync();
                        Console.WriteLine(item);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
