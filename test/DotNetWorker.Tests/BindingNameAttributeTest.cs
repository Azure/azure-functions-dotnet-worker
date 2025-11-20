// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public class BindingNameAttributeTest
    {
        [Fact]
        public void Ensure_BindingPropertyNameAttribute_Has_Single_Constructor()
        {
            // This test is to ensure that the BindingPropertyNameAttribute constructors are not modified
            // as our metadata generation code makes the assumption that this type has only one
            // constructor which accepts the binding property name which is of string type.

            var allConstructors = typeof(BindingPropertyNameAttribute).GetConstructors();

            Assert.Single(allConstructors);

            var constructorParameters = allConstructors[0].GetParameters();
            Assert.Single(constructorParameters);
            Assert.Equal(typeof(string), constructorParameters[0].ParameterType);
        }
    }
}
