using System;
using System.Linq;

using FluentAssertions;

using Xunit;

namespace Microsoft.Azure.Functions.Worker.Extensions.Tests.Http
{
    public class HttpTriggerTests
    {
        [Fact]
        public void Given_No_Parameter_When_HttpTrigger_Initialized_Then_It_Should_Return_Null()
        {
            var attr = new HttpTriggerAttribute();

            attr.Methods.Should().BeNull();
        }

        [Fact]
        public void Given_No_Method_When_HttpTrigger_Initialized_Then_It_Should_Return_Null()
        {
            var attr = new HttpTriggerAttribute(AuthorizationLevel.Anonymous);

            attr.Methods.Should().BeNull();
        }

        [Theory]
        [InlineData(AuthorizationLevel.Anonymous)]
        [InlineData(AuthorizationLevel.User)]
        [InlineData(AuthorizationLevel.Function)]
        [InlineData(AuthorizationLevel.System)]
        [InlineData(AuthorizationLevel.Admin)]
        public void Given_AuthorizationLevel_When_HttpTrigger_Initialized_Then_It_Should_Return_Result(AuthorizationLevel authLevel)
        {
            var attr = new HttpTriggerAttribute(authLevel);

            attr.AuthLevel.Should().Be(authLevel);
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("GET", "POST")]
        public void Given_HttpMethod_When_HttpTrigger_Initialized_Then_It_Should_Return_Result(params string[] methods)
        {
            var attr = new HttpTriggerAttribute(methods);

            attr.Methods.Should().HaveCount(methods.Length);
            attr.Methods.First().Should().Be(methods.First());
            attr.Methods.Last().Should().Be(methods.Last());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void Given_AuthorizationLevelKey_OfNull_When_HttpTrigger_Initialized_Then_It_Should_Return_AuthLevelFunction(string authLevelKey)
        {
            var attr = new HttpTriggerAttribute(authLevelKey);

            attr.AuthLevel.Should().Be(AuthorizationLevel.Function);
        }

        [Theory]
        [InlineData("LoremIpsum", "Anonymous")]
        [InlineData("LoremIpsum", "User")]
        [InlineData("LoremIpsum", "Function")]
        [InlineData("LoremIpsum", "System")]
        [InlineData("LoremIpsum", "Admin")]
        [InlineData("%LoremIpsum", "Anonymous")]
        [InlineData("%LoremIpsum", "User")]
        [InlineData("%LoremIpsum", "Function")]
        [InlineData("%LoremIpsum", "System")]
        [InlineData("%LoremIpsum", "Admin")]
        public void Given_AuthorizationLevelKey_When_HttpTrigger_Initialized_Then_It_Should_Return_AuthLevelFunction(string authLevelKey, string authLevel)
        {
            Environment.SetEnvironmentVariable(authLevelKey.Trim('%'), authLevel);

            var attr = new HttpTriggerAttribute(authLevelKey);

            attr.AuthLevel.Should().Be(AuthorizationLevel.Function);
        }

        [Theory]
        [InlineData("%LoremIpsum%", "Anonymous", AuthorizationLevel.Anonymous)]
        [InlineData("%LoremIpsum%", "User", AuthorizationLevel.User)]
        [InlineData("%LoremIpsum%", "Function", AuthorizationLevel.Function)]
        [InlineData("%LoremIpsum%", "System", AuthorizationLevel.System)]
        [InlineData("%LoremIpsum%", "Admin", AuthorizationLevel.Admin)]
        [InlineData("%LoremIpsum%", "anonymous", AuthorizationLevel.Anonymous)]
        [InlineData("%LoremIpsum%", "user", AuthorizationLevel.User)]
        [InlineData("%LoremIpsum%", "function", AuthorizationLevel.Function)]
        [InlineData("%LoremIpsum%", "system", AuthorizationLevel.System)]
        [InlineData("%LoremIpsum%", "admin", AuthorizationLevel.Admin)]
        public void Given_AuthorizationLevelKey_When_HttpTrigger_Initialized_Then_It_Should_Return_Result(string authLevelKey, string authLevel, AuthorizationLevel expected)
        {
            Environment.SetEnvironmentVariable(authLevelKey.Trim('%'), authLevel);

            var attr = new HttpTriggerAttribute(authLevelKey);

            attr.AuthLevel.Should().Be(expected);
        }

        [Theory]
        [InlineData("%LoremIpsum%", "Anonymous", "GET", AuthorizationLevel.Anonymous)]
        [InlineData("%LoremIpsum%", "User", "GET", AuthorizationLevel.User)]
        [InlineData("%LoremIpsum%", "Function", "GET", AuthorizationLevel.Function)]
        [InlineData("%LoremIpsum%", "System", "GET", AuthorizationLevel.System)]
        [InlineData("%LoremIpsum%", "Admin", "GET", AuthorizationLevel.Admin)]
        [InlineData("%LoremIpsum%", "anonymous", "GET", AuthorizationLevel.Anonymous)]
        [InlineData("%LoremIpsum%", "user", "GET", AuthorizationLevel.User)]
        [InlineData("%LoremIpsum%", "function", "GET", AuthorizationLevel.Function)]
        [InlineData("%LoremIpsum%", "system", "GET", AuthorizationLevel.System)]
        [InlineData("%LoremIpsum%", "admin", "GET", AuthorizationLevel.Admin)]
        public void Given_AuthorizationLevelKey_And_Method_When_HttpTrigger_Initialized_Then_It_Should_Return_Result(string authLevelKey, string authLevel, string method, AuthorizationLevel expected)
        {
            Environment.SetEnvironmentVariable(authLevelKey.Trim('%'), authLevel);

            var attr = new HttpTriggerAttribute(authLevelKey, method);

            attr.AuthLevel.Should().Be(expected);
            attr.Methods.Should().HaveCount(1);
            attr.Methods.First().Should().Be(method);
        }
    }
}
