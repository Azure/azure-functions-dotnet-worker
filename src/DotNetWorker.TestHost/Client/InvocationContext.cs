using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Functions.Worker.TestHost
{
    public class InvocationContext
    {
        private ITriggerMetadataSetup? _triggerSetup;

        internal InvocationContext()
        {
        }

        // Do we need a ref back to the Worker here? Examples:
        // - access to FunctionDefintion?
        // - access to host.json -- .WithHostJson()

        public IDictionary<string, object?> InputBindingsPayload { get; } = new Dictionary<string, object?>();

        public ITriggerMetadataSetup? TriggerSetup
        {
            get
            {
                return _triggerSetup;
            }
            set
            {
                if (_triggerSetup is not null)
                {
                    throw new InvalidOperationException($"{nameof(TriggerSetup)} can only be set once.");
                }

                _triggerSetup = value;
            }
        }
    }
}
