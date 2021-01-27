using System;
using System.Diagnostics;

namespace Microsoft.Azure.Functions.Worker.Sdk
{
    internal class IndentableLogger
    {
        private const int SpacesPerIndent = 2;
        private readonly Action<TraceLevel, string> _log;
        private int _indent = 0;

        public IndentableLogger(Action<TraceLevel, string> log)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public IDisposable Indent()
        {
            return new IndentDisposable(this);
        }

        private void PushIndent()
        {
            _indent++;
        }

        private void PopIndent()
        {
            if (--_indent < 0)
            {
                _indent = 0;
            }
        }

        public void LogMessage(string message)
        {
            _log(TraceLevel.Info, Indent(message));
        }

        public void LogError(string message)
        {
            _log(TraceLevel.Error, Indent(message));
        }

        public void LogWarning(string message)
        {
            _log(TraceLevel.Warning, Indent(message));
        }

        private string Indent(string message)
        {
            return message.PadLeft(message.Length + (_indent * SpacesPerIndent));
        }

        private class IndentDisposable : IDisposable
        {
            private readonly IndentableLogger _logger;

            public IndentDisposable(IndentableLogger logger)
            {
                _logger = logger;
                _logger.PushIndent();
            }

            public void Dispose()
            {
                _logger.PopIndent();
            }
        }
    }

}
