using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace employeesReport.Functions.Entities
{
    public class ConsolidateEntity : TableEntity
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public int Minutes { get; set; }

    }
}
