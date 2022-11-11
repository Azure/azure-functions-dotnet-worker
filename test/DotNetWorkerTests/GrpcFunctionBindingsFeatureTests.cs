using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Azure.Core.Serialization;
using Castle.DynamicProxy;
using Google.Protobuf;
using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Azure.Functions.Worker.Grpc.Features;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Azure.Functions.Worker.OutputBindings;
using Microsoft.Azure.Functions.Worker.Pipeline;
using Microsoft.Azure.Functions.Worker.Tests.Features;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using static System.Net.Mime.MediaTypeNames;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public class GrpcFunctionBindingsFeatureTests
    {
        private readonly Mock<IOutputBindingsInfoProvider> _mockOutputBindingsInfoProvider = new(MockBehavior.Strict);
        private readonly Mock<IFunctionsApplication> _mockApplication = new(MockBehavior.Strict);
        private readonly Mock<IInvocationFeaturesFactory> _mockInvocationFeaturesFactory = new(MockBehavior.Strict);
        private TestFunctionContext _context = new();

        public GrpcFunctionBindingsFeatureTests()
        {
            _mockApplication
                .Setup(m => m.CreateContext(It.IsAny<IInvocationFeatures>(), It.IsAny<CancellationToken>()))
                .Returns((IInvocationFeatures f, CancellationToken ct) =>
                {
                    _context = new TestFunctionContext(f, ct);
                    return _context;
                });

            _mockInvocationFeaturesFactory
                .Setup(m => m.Create())
                .Returns(new InvocationFeatures(Enumerable.Empty<IInvocationFeatureProvider>()));
        }

        [Fact]
        public void BindFunctionInputAsync1_Populates_ParametersUsingConverters()
        {
            var invocationId = "5fb3a9b4-0b38-450a-9d46-35946e7edea7";
            var request = TestUtility.CreateInvocationRequest(invocationId);
            request.TriggerMetadata.Add("myBlob", new TypedData() { ModelBindingData = new ModelBindingData() { 
                Version = "1.1.1",
                Source = "blob",
                Content = ByteString.CopyFromUtf8("stringText")} });

            IInvocationFeatures invocationFeatures = _mockInvocationFeaturesFactory.Object.Create();

            var context = _mockApplication.Object.CreateContext(invocationFeatures, new CancellationTokenSource().Token);
            var a = new GrpcFunctionBindingsFeature(context, request, _mockOutputBindingsInfoProvider.Object);
            Assert.True(a.TriggerMetadata.Count() == 1);
            a.TriggerMetadata.TryGetValue("myBlob", out object b);
            Assert.True(b.GetType() == typeof(GrpcModelBindingData));
            var c = (GrpcModelBindingData)b;
            Assert.True(c.Version == "1.1.1");
            Assert.True(c.Content.GetType() == typeof(BinaryData));
        }

        [Fact]
        public void BindFunctionInputAsync2_Populates_ParametersUsingConverters()
        {
            var invocationId = "5fb3a9b4-0b38-450a-9d46-35946e7edea7";
            var request = TestUtility.CreateInvocationRequest(invocationId);
            request.InputData.Insert(0, new ParameterBinding()
            {
                Name = "myBlob",
                Data = new TypedData()
                {
                    ModelBindingData = new ModelBindingData()
                    {
                        Version = "1.1.1",
                        Source = "blob",
                        Content = ByteString.CopyFromUtf8("stringText")
                    }
                }
            });

            IInvocationFeatures invocationFeatures = _mockInvocationFeaturesFactory.Object.Create();

            var context = _mockApplication.Object.CreateContext(invocationFeatures, new CancellationTokenSource().Token);
            var a = new GrpcFunctionBindingsFeature(context, request, _mockOutputBindingsInfoProvider.Object);
            Assert.True(a.InputData.Count() == 1);
            a.InputData.TryGetValue("myBlob", out object b);
            Assert.True(b.GetType() == typeof(GrpcModelBindingData));
            var c = (GrpcModelBindingData)b;
            Assert.True(c.Version == "1.1.1");
            Assert.True(c.Content.GetType() == typeof(BinaryData));
        }
    }
}
