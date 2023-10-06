namespace Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore
{
    /// <summary>
    /// Represents host.json. Internal class for deserializing.
    /// </summary>
    internal class HostJsonModel
    {
        public HostJsonExtensionModel Extensions { get; set; } = default!;
    }

    internal class HostJsonExtensionModel
    {
        public HostJsonExtensionHttpModel Http { get; set; } = default!;
    }

    internal class HostJsonExtensionHttpModel
    {
        public string RoutePrefix { get; set; } = default!;
    }
}
