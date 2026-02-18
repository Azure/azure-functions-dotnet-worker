// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections;
using System.Globalization;
using System.Resources;
using NuGet.Common;

namespace Azure.Functions.Sdk.Tests;

public class LogMessageTests
{
    #region Constructor Tests - Single Parameter (string id)

    [Fact]
    public void Ctor_WithStringId_Null_Throws()
    {
        Action act = () => new LogMessage(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("id");
    }

    [Fact]
    public void Ctor_WithStringId_Empty_Throws()
    {
        Action act = () => new LogMessage(string.Empty);
        act.Should().Throw<ArgumentException>().WithParameterName("id");
    }

    // We cannot have NuGet types directly in InlineData (xunit crashes), so use int instead.
    // Crash is due to msbuild assembly trickery, not xunit issue.
    [Theory]
    [InlineData("AZFW0100_Error_CannotRunFuncCli", (int)LogLevel.Error, "AZFW0100")]
    [InlineData("AZFW0102_Warning_ExtensionPackageDuplicate", (int)LogLevel.Warning, "AZFW0102")]
    [InlineData("Error_SomeError", (int)LogLevel.Error, null)]
    [InlineData("Warning_SomeWarning", (int)LogLevel.Warning, null)]
    [InlineData("Info_SomeInfo", (int)LogLevel.Information, null)]
    [InlineData("Information_SomeInfo", (int)LogLevel.Information, null)]
    [InlineData("Normal_SomeInfo", (int)LogLevel.Information, null)]
    [InlineData("Minimal_SomeInfo", (int)LogLevel.Minimal, null)]
    [InlineData("High_SomeInfo", (int)LogLevel.Minimal, null)]
    [InlineData("Verbose_SomeInfo", (int)LogLevel.Verbose, null)]
    [InlineData("Low_SomeInfo", (int)LogLevel.Verbose, null)]
    [InlineData("Debug_SomeInfo", (int)LogLevel.Debug, null)]
    [InlineData("UnknownLevel_SomeInfo", (int)LogLevel.Verbose, null)]
    [InlineData("SomeRandomId", (int)LogLevel.Verbose, null)]
    public void Ctor_WithStringId_ParsesLevelAndCodeCorrectly(string id, int expectedLevel, string? expectedCode)
    {
        LogMessage logMessage = new(id);

        logMessage.Id.Should().Be(id);
        logMessage.Level.Should().Be((LogLevel)expectedLevel);
        logMessage.Code.Should().Be(expectedCode);
    }

    [Theory]
    [InlineData("AZFW1234_Error_Something")]
    [InlineData("AZFW5678_Warning_Something")]
    [InlineData("AZFW9999_Debug_Something")]
    public void Ctor_WithStringId_ParsesAzureCodeCorrectly(string id)
    {
        LogMessage logMessage = new(id);

        logMessage.Id.Should().Be(id);
        logMessage.Code.Should().StartWith("AZFW");
        logMessage.Code.Should().HaveLength(8);
    }

    [Theory]
    [InlineData("AZFW12_Error_Something")] // Too short
    [InlineData("AZFW123456_Error_Something")] // Too long
    [InlineData("ZZZZ1234_Error_Something")] // Wrong prefix
    public void Ctor_WithStringId_InvalidCodeFormat_DoesNotParseCode(string id)
    {
        LogMessage logMessage = new(id);

        logMessage.Id.Should().Be(id);
        logMessage.Code.Should().BeNull();
    }

    #endregion

    #region Constructor Tests - Three Parameters (LogLevel, string, string?)

    [Fact]
    public void Ctor_WithThreeParams_NullId_Throws()
    {
        Action act = () => new LogMessage(LogLevel.Error, null!, "CODE123");
        act.Should().Throw<ArgumentNullException>().WithParameterName("id");
    }

    [Fact]
    public void Ctor_WithThreeParams_EmptyId_Throws()
    {
        Action act = () => new LogMessage(LogLevel.Error, string.Empty, "CODE123");
        act.Should().Throw<ArgumentException>().WithParameterName("id");
    }

    // We cannot have NuGet types directly in InlineData (xunit crashes), so use int instead.
    // Crash is due to msbuild assembly trickery, not xunit issue.
    [Theory]
    [InlineData((int)LogLevel.Error, "TestId", "CODE123")]
    [InlineData((int)LogLevel.Warning, "AnotherTestId", null)]
    [InlineData((int)LogLevel.Information, "InfoId", "")]
    [InlineData((int)LogLevel.Debug, "DebugId", "DEBUG001")]
    public void Ctor_WithThreeParams_SetsPropertiesCorrectly(int level, string id, string? code)
    {
        LogMessage logMessage = new((LogLevel)level, id, code);

        logMessage.Level.Should().Be((LogLevel)level);
        logMessage.Id.Should().Be(id);
        logMessage.Code.Should().Be(code);
    }

    #endregion

    #region FromId Tests

    [Fact]
    public void FromId_Null_Throws()
    {
        Action act = () => LogMessage.FromId(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("id");
    }

    [Fact]
    public void FromId_Empty_Throws()
    {
        Action act = () => LogMessage.FromId(string.Empty);
        act.Should().Throw<ArgumentException>().WithParameterName("id");
    }

    [Theory]
    [MemberData(nameof(KnownStringResources))]
    public void FromId_AllCodedStrings_ShouldSucceed(string key)
    {
        int start = key.IndexOf('_') + 1;
        string id = key[start..];

        Func<LogMessage> message = () => LogMessage.FromId(id);
        message.Should().NotThrow("because '{0}' is not supported in LogMessage.FromId", id)
            .Subject.Id.Should().Be(key);
    }

    [Theory]
    [MemberData(nameof(KnownLogMessages))]
    public void FromId_KnownId_ShouldSucceed(string id)
    {
        // Get this a different way to ensure we are getting the static field value.
        LogMessage expected = (LogMessage)typeof(LogMessage)
            .GetField(id, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            !.GetValue(null)!;

        Func<LogMessage> message = () => LogMessage.FromId(id);
        LogMessage result = message.Should().NotThrow("because '{0}' is not supported in LogMessage.FromId", id).Subject;

        result.Should().Be(expected);
        result.Id.Should().Be(expected.Id);
        result.Level.Should().Be(expected.Level);
        result.Code.Should().Be(expected.Code);
        Strings.GetResourceString(expected.Id).Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("UnknownId")]
    [InlineData("AZFW9999_Error_UnknownError")]
    public void FromId_UnknownId_Throws(string id)
    {
        Action act = () => LogMessage.FromId(id);
        act.Should().Throw<ArgumentException>().WithParameterName(nameof(id));
    }

    #endregion

    #region Format Tests

    [Theory]
    [InlineData(nameof(LogMessage.Error_CannotRunFuncCli))]
    [InlineData(nameof(LogMessage.Error_UnknownFunctionsVersion), "v3")]
    public void Format_KnownMessage_ReturnsMessage(string id, params string[] args)
    {
        // arrange
        CultureInfo culture = CultureInfo.InvariantCulture;
        LogMessage log = LogMessage.FromId(id);
        string expected = string.Format(culture, Strings.GetResourceString(log.Id)!, args);

        // act
        string message = log.Format(culture, args);

        // assert
        message.Should().Be(expected);
    }

    #endregion

    #region Edge Cases and Complex Parsing Tests

    [Theory]
    [InlineData("_Error_Something")] // Starts with underscore
    [InlineData("Error_")] // Ends with underscore
    [InlineData("Error")] // No underscore
    [InlineData("Error__Something")] // Double underscore
    public void Ctor_WithStringId_EdgeCases_HandlesGracefully(string id)
    {
        LogMessage logMessage = new(id);

        logMessage.Id.Should().Be(id);
        logMessage.Level.Should().BeDefined();
        logMessage.Code.Should().BeNull();
    }

    [Theory]
    [InlineData("AZFW0100_Error_CannotRunFuncCli_ExtraStuff")]
    [InlineData("AZFW0100_Error_ExtensionPackageConflict_WithAdditionalInfo")]
    public void Ctor_WithStringId_LongIdWithMultipleUnderscores_ParsesCorrectly(string id)
    {
        LogMessage logMessage = new(id);

        logMessage.Id.Should().Be(id);
        logMessage.Code.Should().StartWith("AZFW0100");
        logMessage.Level.Should().Be(LogLevel.Error);
    }

    #endregion

    public static TheoryData<string> KnownLogMessages()
    {
        TheoryData<string> data = [];
        foreach (var field in typeof(LogMessage)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.FieldType == typeof(LogMessage)))
        {
            // We don't include LogMessage directly for two reasons:
            // 1. it is internal, would need to wrap it
            // 2. we want to keep the test data simple and focused on the ID, so it shows as individual tests
            //    in the explorer.
            data.Add(field.Name);
        }

        return data;
    }

    public static TheoryData<string> KnownStringResources()
    {
        TheoryData<string> data = [];
        ResourceSet rs = Strings.ResourceManager.GetResourceSet(CultureInfo.InvariantCulture, true, true)!;

        foreach (DictionaryEntry entry in rs)
        {
            if (entry.Key is string key && key.StartsWith("AZFW"))
            {
                data.Add(key);
            }
        }

        return data;
    }
}
