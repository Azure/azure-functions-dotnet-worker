using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace FunctionsDotNetWorker.Configuration
{
    class DotNetApplicationBuilder : IDotNetApplicationBuilder
    {
        public IServiceCollection Services { get; private set; }

        public DotNetApplicationBuilder(IServiceCollection services)
        {
            Services = services;
        }
    }
}
