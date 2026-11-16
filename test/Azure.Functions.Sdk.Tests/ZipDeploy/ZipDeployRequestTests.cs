// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions.Execution;

namespace Azure.Functions.Sdk.ZipDeploy.Tests;

public sealed class ZipDeployRequestTests
{
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
        MockFileSystem fileSystem = new();
        fileSystem.AddFile("deploy.zip", new MockFileData("zip content"));
        ZipDeployRequest request = new("User", "Pass", fileSystem.FileInfo.New("deploy.zip"))
        {
            UseBlobContainer = true,
        };

        // act
        using HttpRequestMessage message = request.CreateRequestMessage();

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
            message.Content.Headers.ContentEncoding.Should().BeEmpty();
        }
    }

    private static void GetUriCore(Uri? baseUri, Uri expected, bool blob)
    {
        // arrange
        expected = baseUri is null ? expected : new(baseUri, expected);
        ZipDeployRequest request = new("User", "Pass", new MockFileSystem().FileInfo.New("deploy.zip"))
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
