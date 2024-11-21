// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FunctionsNetHost.Shared.Interop
{
    /// <summary>
    /// Represents a message that is sent from the native layer to the managed code to represent the specialization request.
    /// </summary>
    public sealed class SpecializeMessage
    {
        /// <summary>
        /// Gets or sets the environment variables to be set in the specialized worker process.
        /// </summary>
        public IDictionary<string, string> EnvironmentVariables { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Gets or sets the path to the application executable.
        /// </summary>
        public string ApplicationExecutablePath { get; set; } = string.Empty;

        public byte[] ToByteArray()
        {
            using (var memoryStream = new MemoryStream())
            using (var writer = new BinaryWriter(memoryStream, Encoding.UTF8))
            {
                writer.Write(ApplicationExecutablePath);

                writer.Write(EnvironmentVariables.Count);
                foreach (var kvp in EnvironmentVariables)
                {
                    writer.Write(kvp.Key);
                    writer.Write(kvp.Value);
                }

                return memoryStream.ToArray();
            }
        }

        public static SpecializeMessage FromByteArray(byte[] data)
        {
            using (var memoryStream = new MemoryStream(data))
            using (var reader = new BinaryReader(memoryStream, Encoding.UTF8))
            {
                var message = new SpecializeMessage();

                message.ApplicationExecutablePath = reader.ReadString();

                var count = reader.ReadInt32();
                for (var i = 0; i < count; i++)
                {
                    var key = reader.ReadString();
                    var value = reader.ReadString();
                    message.EnvironmentVariables[key] = value;
                }

                return message;
            }
        }
    }
}
