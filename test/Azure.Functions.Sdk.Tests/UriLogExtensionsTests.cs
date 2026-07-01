// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Azure.Functions.Sdk.Tests;

public class UriLogExtensionsTests
{
    #region Uri overload

    [Fact]
    public void ToLogSafeString_Uri_StripsUserInfo()
    {
        Uri uri = new("https://user:PLACEHOLDER@functionapp.scm.test/api/zipdeploy");

        string result = uri.ToLogSafeString();

        result.Should().Be("https://functionapp.scm.test/api/zipdeploy");
    }

    [Fact]
    public void ToLogSafeString_Uri_StripsQueryString()
    {
        Uri uri = new("https://functionapp.scm.test/api/zipdeploy?isAsync=true&sig=PLACEHOLDER");

        string result = uri.ToLogSafeString();

        result.Should().Be("https://functionapp.scm.test/api/zipdeploy");
    }

    [Fact]
    public void ToLogSafeString_Uri_StripsFragment()
    {
        Uri uri = new("https://functionapp.scm.test/api/zipdeploy#PLACEHOLDER");

        string result = uri.ToLogSafeString();

        result.Should().Be("https://functionapp.scm.test/api/zipdeploy");
    }

    [Fact]
    public void ToLogSafeString_Uri_StripsUserInfoAndQueryAndFragment()
    {
        Uri uri = new("https://user:PLACEHOLDER@functionapp.scm.test/api/zipdeploy?sig=PLACEHOLDER#frag");

        string result = uri.ToLogSafeString();

        result.Should().Be("https://functionapp.scm.test/api/zipdeploy");
        result.Should().NotContain("PLACEHOLDER");
    }

    [Fact]
    public void ToLogSafeString_Uri_PreservesNonDefaultPort()
    {
        Uri uri = new("https://functionapp.scm.test:8443/api/zipdeploy?token=PLACEHOLDER");

        string result = uri.ToLogSafeString();

        result.Should().Be("https://functionapp.scm.test:8443/api/zipdeploy");
    }

    [Fact]
    public void ToLogSafeString_Uri_OmitsDefaultPort()
    {
        Uri uri = new("https://functionapp.scm.test:443/api/zipdeploy");

        string result = uri.ToLogSafeString();

        result.Should().Be("https://functionapp.scm.test/api/zipdeploy");
    }

    [Fact]
    public void ToLogSafeString_Uri_PreservesRootPath()
    {
        Uri uri = new("https://functionapp.scm.test/");

        string result = uri.ToLogSafeString();

        result.Should().Be("https://functionapp.scm.test/");
    }

    [Fact]
    public void ToLogSafeString_Uri_PreservesHttpScheme()
    {
        Uri uri = new("http://user:PLACEHOLDER@functionapp.scm.test/api/zipdeploy?sig=PLACEHOLDER");

        string result = uri.ToLogSafeString();

        result.Should().Be("http://functionapp.scm.test/api/zipdeploy");
    }

    [Fact]
    public void ToLogSafeString_Uri_Null_ReturnsEmpty()
    {
        Uri? uri = null;

        string result = uri.ToLogSafeString();

        result.Should().BeEmpty();
    }

    [Fact]
    public void ToLogSafeString_Uri_Relative_StripsQuery()
    {
        Uri uri = new("api/zipdeploy?isAsync=true&sig=PLACEHOLDER", UriKind.Relative);

        string result = uri.ToLogSafeString();

        result.Should().Be("api/zipdeploy");
        result.Should().NotContain("PLACEHOLDER");
    }

    #endregion

    #region string overload

    [Fact]
    public void ToLogSafeString_String_AbsoluteUrl_StripsUserInfoAndQuery()
    {
        string url = "https://deployUser:PLACEHOLDER@functionapp.scm.test/?sig=PLACEHOLDER";

        string result = url.ToLogSafeString();

        result.Should().Be("https://functionapp.scm.test/");
        result.Should().NotContain("PLACEHOLDER");
    }

    [Fact]
    public void ToLogSafeString_String_Null_ReturnsEmpty()
    {
        string? url = null;

        string result = url.ToLogSafeString();

        result.Should().BeEmpty();
    }

    [Fact]
    public void ToLogSafeString_String_Empty_ReturnsEmpty()
    {
        string result = string.Empty.ToLogSafeString();

        result.Should().BeEmpty();
    }

    [Fact]
    public void ToLogSafeString_String_NotAUrl_ReturnedUnchanged()
    {
        string url = "not-an-url";

        string result = url.ToLogSafeString();

        result.Should().Be("not-an-url");
    }

    [Fact]
    public void ToLogSafeString_String_NonAbsoluteWithCredentialsAndQuery_BestEffortRedaction()
    {
        // Missing scheme so it does not parse as an absolute URI; redaction is best-effort.
        string url = "//user:PLACEHOLDER@functionapp.scm.test/path?sig=PLACEHOLDER";

        string result = url.ToLogSafeString();

        result.Should().Be("//functionapp.scm.test/path");
        result.Should().NotContain("PLACEHOLDER");
    }

    #endregion
}
