using System;
using System.Collections.Generic;
using System.Text;

namespace employeesReport.Common.Models
{
    public class EmployeeReport
    {
        public int Id { get; set; }
        public DateTime Date { get; set;  }
        public int Type { get; set; }

        public bool Consolidated { get; set; }
    }

    public class ConsolidateByDate
    {
        public string Date { get; set;  }
    }
}
