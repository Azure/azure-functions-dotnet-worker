// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Azure.Functions.Worker.Extensions.Http.Converters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore
{
    internal sealed class FromBodyConversionFeature : IFromBodyConversionFeature
    {
        public static FromBodyConversionFeature Instance { get; } = new();

        public async ValueTask<object?> ConvertAsync(FunctionContext context, Type targetType)
        {
            var httpContext = context.GetHttpContext()
                ?? throw new InvalidOperationException($"The '{nameof(FromBodyConversionFeature)} expects an '{nameof(HttpContext)}' instance in the current context.");

            var metadata = httpContext.RequestServices
                           .GetService<IModelMetadataProvider>()?
                           .GetMetadataForType(targetType);

            if (metadata is null)
            {
                context.GetLogger<FromBodyConversionFeature>()
                    .LogWarning("Unable to resolve a model metadata provider for the target type ({TargetType}).", targetType);

                return null;
            }

            var binderModelName = metadata.BinderModelName ?? string.Empty;

            var modelBinder = (httpContext.RequestServices
               .GetService<IModelBinderFactory>()?
               .CreateBinder(new ModelBinderFactoryContext
               {
                   Metadata = metadata,
                   BindingInfo = new BindingInfo()
                   {
                       BinderModelName = binderModelName,
                       BindingSource = BindingSource.Body,
                       BinderType = metadata.BinderType,
                       PropertyFilterProvider = metadata.PropertyFilterProvider,
                   },
                   CacheToken = null
               })) 
               ?? throw new InvalidOperationException($"Unable to resolve a request body model binder for the target type ({metadata.BinderType}.");

            var modelBindingContext = new DefaultModelBindingContext
            {
                ModelMetadata = metadata,
                ModelName = binderModelName,
                BindingSource = BindingSource.Body,
                ModelState = new ModelStateDictionary(),
                ActionContext = new ActionContext
                {
                    HttpContext = httpContext
                }
            };

            await modelBinder.BindModelAsync(modelBindingContext);

            if (modelBindingContext.Result.IsModelSet)
            {
                return modelBindingContext.Result.Model;
            }
            else if (!modelBindingContext.ModelState.IsValid)
            {
                var messageBuilder = new StringBuilder();
                foreach (var key in modelBindingContext.ModelState.Keys)
                {
                    var dictionary = modelBindingContext.ModelState[key];

                    if (dictionary == null)
                    {
                        continue;
                    }

                    foreach (var error in dictionary.Errors)
                    {
                        if (error is null)
                        {
                           continue;
                        }

                        var message = string.IsNullOrEmpty(error.ErrorMessage)
                            ? error.Exception?.Message
                            : error.ErrorMessage;

                        messageBuilder.AppendLine(message);
                    }
                }

                throw new InvalidOperationException(messageBuilder.ToString());
            }

            context.GetLogger<FromBodyConversionFeature>()
                .LogWarning("Unable to bind the request body to the target type ({TargetType}).", targetType);

            return null;
        }
    }
}
