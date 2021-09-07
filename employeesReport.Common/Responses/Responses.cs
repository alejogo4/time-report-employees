using System;
using System.Collections.Generic;
using System.Text;

namespace employeesReport.Common.Responses
{
    public class Responses
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }

        public object Result { get; set; }
    }
}
