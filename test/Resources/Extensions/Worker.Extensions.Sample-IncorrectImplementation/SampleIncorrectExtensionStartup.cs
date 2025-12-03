using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Core;
using Worker.Extensions.Sample_IncorrectImplementation;

[assembly: WorkerExtensionStartup(typeof(SampleIncorrectExtensionStartup))]


namespace Worker.Extensions.Sample_IncorrectImplementation
{
    /// <summary>
    /// An incorrect extension implementation(Missing parameterless constructor, Missing base class)
    /// </summary>
    public sealed class SampleIncorrectExtensionStartup 
    {
        public SampleIncorrectExtensionStartup(string foo)
        {

        }
        public void Configure(IFunctionsWorkerApplicationBuilder applicationBuilder)
        {
           
        }
    }
}
