using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using System;
using System.IO;
using employeesReport.Common.Models;
using employeesReport.Functions.Entities;

namespace employeesReport.Test.Helpers
{
    public class TestFactory
    {
        public static EmployeeReportEntity GetTodoEntity()
        {
            return new EmployeeReportEntity
            {
                ETag = "*",
                PartitionKey = "TIME_REPORT",
                RowKey = Guid.NewGuid().ToString(),
                Id = 1,
                Date = DateTime.UtcNow,
                Consolidated = false,
                Type = 0
            };
        }

        public static DefaultHttpRequest CreateHttpRequest(Guid todoId, EmployeeReport todoRequest)
        {
            string request = JsonConvert.SerializeObject(todoRequest);
            return new DefaultHttpRequest(new DefaultHttpContext())
            {
                Body = GenerateStreamFromString(request),
                Path = $"/{todoId}"
            };
        }

        public static DefaultHttpRequest CreateHttpRequest(Guid todoId)
        {
            return new DefaultHttpRequest(new DefaultHttpContext())
            {
                Path = $"/{todoId}"
            };
        }

        public static DefaultHttpRequest CreateHttpRequest(EmployeeReport todoRequest)
        {
            string request = JsonConvert.SerializeObject(todoRequest);
            return new DefaultHttpRequest(new DefaultHttpContext())
            {
                Body = GenerateStreamFromString(request)
            };
        }

        public static DefaultHttpRequest CreateHttpRequest()
        {
            return new DefaultHttpRequest(new DefaultHttpContext());
        }

        public static EmployeeReport GetTodoRequest()
        {
            return new EmployeeReport
            {
                Date = DateTime.UtcNow,
                Id = 1,
                Type = 0,
                Consolidated = false
            };
        }

        public static Stream GenerateStreamFromString(string stringToConvert)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(stringToConvert);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        public static ILogger CreateLogger(LoggerTypes type = LoggerTypes.Null)
        {
            ILogger logger;
            if (type == LoggerTypes.List)
            {
                logger = new ListLogger();
            }
            else
            {
                logger = NullLoggerFactory.Instance.CreateLogger("Null Logger");
            }

            return logger;
        }
    }
}
