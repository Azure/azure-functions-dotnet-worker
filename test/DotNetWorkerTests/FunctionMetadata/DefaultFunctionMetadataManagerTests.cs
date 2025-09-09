using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests.FunctionMetadata
{
    public class DefaultFunctionMetadataManagerTests
    {
        [Fact]
        public async Task GetFunctionMetadataAsync_ReturnsTransformedMetadata()
        {
            var mockProvider = new Mock<IFunctionMetadataProvider>(MockBehavior.Strict);
            var mockTransformer = new Mock<IFunctionMetadataTransformer>(MockBehavior.Strict);
            var mockLogger = new Mock<ILogger<DefaultFunctionMetadataManager>>();

            var metadata = new List<IFunctionMetadata> { new TestFunctionMetadata() }.ToImmutableArray();
            mockProvider.Setup(p => p.GetFunctionMetadataAsync(It.IsAny<string>())).ReturnsAsync(metadata);

            mockTransformer.SetupGet(t => t.Name).Returns("TestTransformer");
            mockTransformer.Setup(t => t.Transform(It.IsAny<IList<IFunctionMetadata>>()));

            var manager = new DefaultFunctionMetadataManager(
                mockProvider.Object,
                new[] { mockTransformer.Object },
                mockLogger.Object);

            var result = await manager.GetFunctionMetadataAsync("test");

            Assert.Single(result);
            mockProvider.Verify(p => p.GetFunctionMetadataAsync("test"), Times.Once);
            mockTransformer.Verify(t => t.Transform(It.IsAny<IList<IFunctionMetadata>>()), Times.Once);
        }

        [Fact]
        public async Task GetFunctionMetadataAsync_NoTransformers_ReturnsOriginalMetadata()
        {
            var mockProvider = new Mock<IFunctionMetadataProvider>(MockBehavior.Strict);
            var mockLogger = new Mock<ILogger<DefaultFunctionMetadataManager>>();
            var metadata = new List<IFunctionMetadata> { new TestFunctionMetadata() }.ToImmutableArray();
            mockProvider.Setup(p => p.GetFunctionMetadataAsync(It.IsAny<string>())).ReturnsAsync(metadata);

            var manager = new DefaultFunctionMetadataManager(
                mockProvider.Object,
                Array.Empty<IFunctionMetadataTransformer>(),
                mockLogger.Object);

            var result = await manager.GetFunctionMetadataAsync("test");

            Assert.Single(result);
            mockProvider.Verify(p => p.GetFunctionMetadataAsync("test"), Times.Once);
        }

        [Fact]
        public async Task GetFunctionMetadataAsync_TransformerThrows_LogsAndThrows()
        {
            var mockProvider = new Mock<IFunctionMetadataProvider>(MockBehavior.Strict);
            var mockTransformer = new Mock<IFunctionMetadataTransformer>(MockBehavior.Strict);
            var mockLogger = new Mock<ILogger<DefaultFunctionMetadataManager>>();
            var metadata = ImmutableArray<IFunctionMetadata>.Empty;

            mockProvider.Setup(p => p.GetFunctionMetadataAsync(It.IsAny<string>())).ReturnsAsync(metadata);
            mockTransformer.SetupGet(t => t.Name).Returns("ThrowingTransformer");
            mockTransformer.Setup(t => t.Transform(It.IsAny<IList<IFunctionMetadata>>()))
                .Throws(new InvalidOperationException("fail"));

            var manager = new DefaultFunctionMetadataManager(
                mockProvider.Object,
                new[] { mockTransformer.Object },
                mockLogger.Object);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => manager.GetFunctionMetadataAsync("test"));
            Assert.Equal("fail", ex.Message);
            mockLogger.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("ThrowingTransformer")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }

        private class TestFunctionMetadata : IFunctionMetadata
        {
            public string? FunctionId => "id";
            public bool IsProxy => false;
            public string? Language => "dotnet";
            public bool ManagedDependencyEnabled => false;
            public string? Name => "Test";
            public string? EntryPoint => "Test.Run";
            public IList<string>? RawBindings => new List<string>();
            public string? ScriptFile => "Test.dll";
            public IRetryOptions? Retry => null;
        }
    }
}
