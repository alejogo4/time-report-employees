using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace employeesReport.Test.Helpers
{
    public class ListLogger : ILogger
    {
        public ListLogger()
        {
            Logs = new List<string>();
        }

        public IList<string> Logs;

        public IDisposable BeginScope<TState>(TState state)
        {
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return false;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            string message = formatter(state, exception);
            Logs.Add(message);
        }
    }
}
