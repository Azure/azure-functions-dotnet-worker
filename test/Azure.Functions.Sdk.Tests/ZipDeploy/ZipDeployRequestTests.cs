// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Net.Http;
using System.Text;
using AwesomeAssertions.Execution;

namespace Azure.Functions.Sdk.ZipDeploy.Tests;

public sealed class ZipDeployRequestTests
{
    private static readonly Stream _content = new MemoryStream();

    [Theory]
    [InlineData(null)]
    [InlineData("https://base.uri.test")]
    public void GetUri_Blob_ReturnsUri(string? baseUriStr)
    {
        Uri? baseUri = baseUriStr is null ? null : new(baseUriStr);
        GetUriCore(baseUri, ZipDeployRequest.PublishPath, true);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("https://base.uri.test")]
    public void GetUri_NoBlob_ReturnsUri(string? baseUriStr)
    {
        Uri? baseUri = baseUriStr is null ? null : new(baseUriStr);
        GetUriCore(baseUri, ZipDeployRequest.ZipDeployPath, false);
    }

    [Fact]
    public void CreateRequestMessage_Expected()
    {
        // arrange
        ZipDeployRequest request = new("User", "Pass", _content)
        {
            UseBlobContainer = true,
        };

        // act
        HttpRequestMessage message = request.CreateRequestMessage();

        // assert
        message.Should().NotBeNull();

        using (new AssertionScope("Message"))
        {
            message.Method.Should().Be(HttpMethod.Post);
            message.RequestUri.Should().Be(ZipDeployRequest.PublishPath);
        }

        message.Content.Should().NotBeNull();
        using (new AssertionScope("Content"))
        {
            message.Content!.Headers.ContentType!.MediaType.Should().Be("application/zip");
            message.Content.Headers.ContentEncoding.Should().ContainSingle(Encoding.UTF8.WebName);
        }
    }

    private static void GetUriCore(Uri? baseUri, Uri expected, bool blob)
    {
        // arrange
        expected = baseUri is null ? expected : new(baseUri, expected);
        ZipDeployRequest request = new("User", "Pass", _content)
        {
            UseBlobContainer = blob,
        };

        // act
        Uri actual = request.GetUri(baseUri);

        // assert
        actual.Should().NotBeNull();
        actual.IsAbsoluteUri.Should().Be(baseUri is not null);
        actual.Should().Be(expected);
    }
}
