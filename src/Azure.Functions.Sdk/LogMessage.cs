// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Globalization;
using NuGet.Common;

namespace Azure.Functions.Sdk;

/// <summary>
/// A helper struct representing a log message for MSBuild tasks.
/// </summary>
internal readonly struct LogMessage
{
    /// <summary>
    /// Log message for when the Func CLI cannot be run.
    /// </summary>
    public static readonly LogMessage Error_CannotRunFuncCli
        = new(nameof(Strings.AZFW0100_Error_CannotRunFuncCli));

    /// <summary>
    /// Log message for when there is a conflict between extension packages.
    /// </summary>
    public static readonly LogMessage Error_ExtensionPackageConflict
        = new(nameof(Strings.AZFW0101_Error_ExtensionPackageConflict));

    /// <summary>
    /// Log message for when there is a duplicate extension package.
    /// </summary>
    public static readonly LogMessage Warning_ExtensionPackageDuplicate
        = new(nameof(Strings.AZFW0102_Warning_ExtensionPackageDuplicate));

    /// <summary>
    /// Log message for when an extension package version is invalid.
    /// </summary>
    public static readonly LogMessage Error_InvalidExtensionPackageVersion
        = new(nameof(Strings.AZFW0103_Error_InvalidExtensionPackageVersion));

    /// <summary>
    /// Log message for when an end-of-life Functions version is used.
    /// </summary>
    public static readonly LogMessage Warning_EndOfLifeFunctionsVersion
        = new(nameof(Strings.AZFW0104_Warning_EndOfLifeFunctionsVersion));

    /// <summary>
    /// Log message for when an incompatible Functions SDK is used.
    /// </summary>
    public static readonly LogMessage Error_UsingIncompatibleSdk
        = new(nameof(Strings.AZFW0105_Error_UsingIncompatibleSdk));

    /// <summary>
    /// Log message for when an unknown Functions version is specified.
    /// </summary>
    public static readonly LogMessage Error_UnknownFunctionsVersion
        = new(nameof(Strings.AZFW0106_Error_UnknownFunctionsVersion));

    /// <summary>
    /// Log message for when an unsupported target framework is used.
    /// </summary>
    public static readonly LogMessage Warning_UnsupportedTargetFramework
        = new(nameof(Strings.AZFW0107_Warning_UnsupportedTargetFramework));

    public static readonly LogMessage Warning_ExtensionsNotRestored
        = new(nameof(Strings.AZFW0108_Warning_ExtensionsNotRestored));

    /// <summary>
    /// Initializes a new instance of the <see cref="LogMessage"/> struct.
    /// Parses the <see cref="Level"/> and <see cref="Code"/> properties from the given <paramref name="id"/>.
    /// LogMessages must:
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

    /// <summary>
    /// Gets the level of the log message.
    /// </summary>
    public LogLevel Level { get; }

    /// <summary>
    /// Gets the identifier of the log message.
    /// </summary>
    /// <remarks>
    /// This identifier must correspond to a resource string in the <see cref="Strings"/> resource file.
    /// </remarks>
    public string Id { get; }

    /// <summary>
    /// Gets the code of the log message, if available.
    /// </summary>
    public string? Code { get; }

    /// <summary>
    /// Gets the help keyword for the log message.
    /// </summary>
    /// <remarks>
    /// The help keyword is derived from the <see cref="Code"/> property.
    /// If the <see cref="Code"/> is null, the help keyword will also be null.
    /// </remarks>
    public string? HelpKeyword => Code is null ? null : $"AzureFunctions.{Code}";

    /// <summary>
    /// Gets the help link for the log message.
    /// </summary>
    /// <remarks>
    /// The help link is derived from the <see cref="Code"/> property.
    /// If the <see cref="Code"/> is null, the help link will also be null.
    /// </remarks>
    public string? HelpLink => Code is null ? null : $"https://aka.ms/azure-functions/{Code}";

    /// <summary>
    /// Gets the raw resource string value for the log message.
    /// </summary>
    public string? RawValue => Strings.GetResourceString(Id);

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
            nameof(Error_UsingIncompatibleSdk) => Error_UsingIncompatibleSdk,
            nameof(Error_UnknownFunctionsVersion) => Error_UnknownFunctionsVersion,
            nameof(Warning_UnsupportedTargetFramework) => Warning_UnsupportedTargetFramework,
            nameof(Warning_ExtensionsNotRestored) => Warning_ExtensionsNotRestored,
            _ => throw new ArgumentException($"Log message with id '{id}' not found.", nameof(id)),
        };
    }

    /// <summary>
    /// Formats the log message with the given arguments and <see cref="CultureInfo.CurrentUICulture" />.
    /// </summary>
    /// <param name="args">The arguments to use, if any.</param>
    /// <returns>The formatted message.</returns>
    public string Format(params object[] args) => Format(CultureInfo.CurrentUICulture, args);

    /// <summary>
    /// Formats the log message with the given culture and arguments.
    /// </summary>
    /// <param name="culture">The culture info to use.</param>
    /// <param name="args">The arguments to use, if any.</param>
    /// <returns>The formatted message.</returns>
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
