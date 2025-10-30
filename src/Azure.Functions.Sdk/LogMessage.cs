// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Globalization;
using NuGet.Common;

namespace Azure.Functions.Sdk;

internal readonly struct LogMessage
{
    public static readonly LogMessage Error_CannotRunFuncCli
        = new(nameof(Strings.AZFW0100_Error_CannotRunFuncCli));

    public static readonly LogMessage Error_ExtensionPackageConflict
        = new(nameof(Strings.AZFW0101_Error_ExtensionPackageConflict));

    public static readonly LogMessage Warning_ExtensionPackageDuplicate
        = new(nameof(Strings.AZFW0102_Warning_ExtensionPackageDuplicate));

    public static readonly LogMessage Error_InvalidExtensionPackageVersion
        = new(nameof(Strings.AZFW0103_Error_InvalidExtensionPackageVersion));

    public static readonly LogMessage Warning_EndOfLifeFunctionsVersion
        = new(nameof(Strings.AZFW0104_Warning_EndOfLifeFunctionsVersion));

    public static readonly LogMessage Error_UsingLegacyFunctionsSdk
        = new(nameof(Strings.AZFW0105_Error_UsingLegacyFunctionsSdk));

    public static readonly LogMessage Error_UnknownFunctionsVersion
        = new(nameof(Strings.AZFW0106_Error_UnknownFunctionsVersion));

    public static readonly LogMessage Warning_UnsupportedTargetFramework
        = new(nameof(Strings.AZFW0107_Warning_UnsupportedTargetFramework));

    public static readonly LogMessage Error_CustomFunctionPackageReferencesNotAllowed
        = new(nameof(Strings.AZFW0108_Error_CustomFunctionPackageReferencesNotAllowed));

    /// <summary>
    /// Initializes a new instance of the <see cref="LogMessage"/> struct.
    /// Parses the <see cref="Level"/> and <see cref="Code"/> properties from the given <paramref name="id"/>.
    /// LogCodes must:
    /// - Be defined in the 'Strings.resx' file with the identifier <paramref name="id"/>.
    /// - Follow the format: (?:<LogCode>_)(?<LogLevel>_).*
    /// </summary>
    /// <param name="id">The resource identifier.</param>
    public LogMessage(string id)
    {
        Id = Throw.IfNullOrEmpty(id);
        (Level, Code) = ParseId(id);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LogMessage"/> struct.
    /// </summary>
    /// <param name="level">The level of the log message.</param>
    /// <param name="id">The resource identifier for the log message.</param>
    /// <param name="code">The code of the log message. Can be null.</param>
    public LogMessage(LogLevel level, string id, string? code = null)
    {
        Id = Throw.IfNullOrEmpty(id);
        Level = level;
        Code = code;
    }

    public LogLevel Level { get; }

    public string Id { get; }

    public string? Code { get; }

    public string? HelpKeyword => Code is null ? null : $"AzureFunctions.{Code}";

    /// <summary>
    /// Gets a <see cref="LogMessage"/> from its identifier.
    /// </summary>
    /// <param name="id">The resource identifier.</param>
    /// <returns>The <see cref="LogMessage"/>.</returns>
    public static LogMessage FromId(string id)
    {
        Throw.IfNullOrEmpty(id);
        return id switch
        {
            nameof(Error_CannotRunFuncCli) => Error_CannotRunFuncCli,
            nameof(Error_ExtensionPackageConflict) => Error_ExtensionPackageConflict,
            nameof(Warning_ExtensionPackageDuplicate) => Warning_ExtensionPackageDuplicate,
            nameof(Error_InvalidExtensionPackageVersion) => Error_InvalidExtensionPackageVersion,
            nameof(Warning_EndOfLifeFunctionsVersion) => Warning_EndOfLifeFunctionsVersion,
            nameof(Error_UsingLegacyFunctionsSdk) => Error_UsingLegacyFunctionsSdk,
            nameof(Error_UnknownFunctionsVersion) => Error_UnknownFunctionsVersion,
            nameof(Warning_UnsupportedTargetFramework) => Warning_UnsupportedTargetFramework,
            nameof(Error_CustomFunctionPackageReferencesNotAllowed) => Error_CustomFunctionPackageReferencesNotAllowed,
            _ => throw new ArgumentException($"Log message with id '{id}' not found.", nameof(id)),
        };
    }

    public string Format(params object[] args) => Format(CultureInfo.CurrentUICulture, args);

    public string Format(CultureInfo culture, params object[] args)
    {
        string resource = Strings.GetResourceString(Id)
            ?? throw new InvalidOperationException($"Resource string for id '{Id}' not found.");
        return string.Format(culture, resource, args);
    }

    private static (LogLevel Level, string? Code) ParseId(string id)
    {
        static bool TryParseLogLevel(ReadOnlySpan<char> span, out LogLevel? level)
        {
            switch (span)
            {
                case "Error":
                    level = LogLevel.Error;
                    return true;
                case "Warning":
                    level = LogLevel.Warning;
                    return true;
                case "Minimal" or "High":
                    level = LogLevel.Minimal;
                    return true;
                case "Info" or "Information" or "Normal":
                    level = LogLevel.Information;
                    return true;
                case "Verbose" or "Low":
                    level = LogLevel.Verbose;
                    return true;
                case "Debug":
                    level = LogLevel.Debug;
                    return true;
                default:
                    level = null;
                    return false;
            }
        }

        static bool TryParseCode(ReadOnlySpan<char> span, out string? code)
        {
            if (span.Length != 8)
            {
                code = null;
                return false;
            }

            if (!span.StartsWith("AZFW".AsSpan(), StringComparison.Ordinal))
            {
                code = null;
                return false;
            }

            code = span.ToString();
            return true;
        }

        LogLevel? level = null;
        string? code = null;
        int start = 0;
        int end = id.IndexOf('_');
        int iteration = 0;
        while (end > 0 && iteration < 2)
        {
            ReadOnlySpan<char> span = id.AsSpan(start, end - start);
            if (iteration == 0 && code is null && TryParseCode(span, out code))
            {
                // Code can only be the first segment.
            }
            else if (level is null && TryParseLogLevel(span, out level))
            {
                // Level can be first or second segment.
            }

            start = end + 1;
            end = id.IndexOf('_', start);
            iteration++;
        }

        return (level ?? LogLevel.Verbose, code);
    }
}
