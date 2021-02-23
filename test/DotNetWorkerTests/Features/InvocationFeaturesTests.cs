// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using Moq;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests.Features
{
    public class InvocationFeaturesTests
    {
        [Fact]
        public void Get_Retrieves_Set_Services()
        {
            var features = new InvocationFeatures(Enumerable.Empty<IInvocationFeatureProvider>());

            features.Set<string>("test");

            string result = features.Get<string>();

            Assert.Equal("test", result);
        }

        [Fact]
        public void Test()
        {
            var testProvider = new TestFeatureProvider(t => t.Name switch { nameof(String) => "test", _ => null });
            var features = new InvocationFeatures(new[] { testProvider });

            var result = features.Get<string>();
            var result2 = features.Get<InvocationFeaturesTests>();

            Assert.NotNull(result);
            Assert.Null(result2);
            Assert.Equal("test", result);
            Assert.Equal(2, testProvider.Called);
            Assert.Equal(1, testProvider.Matched);
        }

        private class TestFeatureProvider : IInvocationFeatureProvider
        {
            private readonly Func<Type, object> _create;

            public TestFeatureProvider(Func<Type, object> create)
            {
                _create = create;
            }

            public int Matched { get; private set; }
            
            public int Called { get; private set; }

            public bool TryCreate(Type type, out object feature)
            {
                feature = _create(type);

                if (feature is not null)
                {
                    Matched++;
                }

                Called++;

                return feature is not null;
            }
        }
    }
}
