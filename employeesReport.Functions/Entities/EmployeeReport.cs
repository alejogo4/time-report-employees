using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace employeesReport.Functions.Entities
{
    public class EmployeeReportEntity : TableEntity
    {
        public int Id { get; set; }
        public DateTime Date { get; set;  }
        public int Type { get; set; }

        public bool Consolidated { get; set; }
    }

    public class DataConsolidate
    {
        public int id { set; get; }
        public double minutes { set; get; }
    }
}
