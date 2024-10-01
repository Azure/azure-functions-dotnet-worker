using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.Azure.Functions.SdkGeneratorTests.Helpers
{
    internal sealed class AnalyzerConfigOptions : AnalyzerConfigOptionsProvider
    {
        private readonly Options _options;

        public AnalyzerConfigOptions(
            Dictionary<string, string> options)
        {
            _options = new Options(options);
        }

        public override CodeAnalysis.Diagnostics.AnalyzerConfigOptions GlobalOptions => _options;

        public override CodeAnalysis.Diagnostics.AnalyzerConfigOptions GetOptions(SyntaxTree tree) => _options;
        public override CodeAnalysis.Diagnostics.AnalyzerConfigOptions GetOptions(AdditionalText textFile) => _options;

        private class Options : CodeAnalysis.Diagnostics.AnalyzerConfigOptions
        {
            private readonly Dictionary<string, string> options;

            public Options(
                Dictionary<string, string> options)
            {
                this.options = options;
            }

            public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
            {
                return options.TryGetValue(key, out value);
            }
        }
    }
}
