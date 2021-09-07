using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Threading.Tasks;
using employeesReport.Functions.Entities;
using System.Collections.Generic;
using System.Linq;

namespace employeesReport.Functions
{
   

    public static class ScheduledFunction
    {
        

        [FunctionName("ScheduledFunction")]
        public static async Task Run([TimerTrigger("0 */2 * * * *")] TimerInfo myTimer,
        [Table("timeReport", Connection = "AzureWebJobsStorage")] CloudTable timeReport,
        [Table("consolidate", Connection = "AzureWebJobsStorage")] CloudTable consolidate,
        ILogger log)
        {
            string filter = TableQuery.GenerateFilterConditionForBool("Consolidated", QueryComparisons.Equal, false);
            /*string filter2 = TableQuery.GenerateFilterConditionForInt("Type", QueryComparisons.Equal, 1);
            string filterFinal = TableQuery.CombineFilters(filter, TableOperators.And, filter2);*/
            TableQuery<EmployeeReportEntity> query = new TableQuery<EmployeeReportEntity>().Where(filter);

            TableQuerySegment<EmployeeReportEntity> _noConsolidate = await timeReport.ExecuteQuerySegmentedAsync(query, null);

            List<DataConsolidate> report = new List<DataConsolidate>();
            List<string> key = new List<string>();
            List<EmployeeReportEntity> noConsolidate = _noConsolidate.Results.OrderBy(e => e.Date).ToList();
            List<EmployeeReportEntity> noConsolidateInitial = noConsolidate.FindAll(e => e.Type == 0);

            foreach (var item in noConsolidateInitial)
            {
                var filter_end = noConsolidate.Find(e => e.Id == item.Id && e.Type == 1 && key.Find(i => i.Equals(e.RowKey)) == null);
                if (filter_end != null)
                {
                    double minutes = (filter_end.Date - item.Date).TotalMinutes;
                    var search = report.FindIndex(e => e.id == item.Id);
                    if (search == -1)
                    {
                        key.Add(item.RowKey);
                        key.Add(filter_end.RowKey);
                        report.Add(new DataConsolidate()
                        {
                            id = item.Id,
                            minutes = minutes
                        });
                    }
                    else
                    {
                        key.Add(item.RowKey);
                        key.Add(filter_end.RowKey);
                        DataConsolidate i = report[search];
                        i.id = item.Id;
                        i.minutes += minutes;
                        report[search] = i;
                    }


                }
            }

            foreach (var item in key)
            {
                TableOperation findOperation = TableOperation.Retrieve<EmployeeReportEntity>("TIME_REPORT", item);
                TableResult findResult = await timeReport.ExecuteAsync(findOperation);
                EmployeeReportEntity employeeEntity = (EmployeeReportEntity)findResult.Result;
                employeeEntity.Consolidated = true;
                TableOperation addOperation = TableOperation.Replace(employeeEntity);
                await timeReport.ExecuteAsync(addOperation);
            }




            foreach (var item in report)
            {
                ConsolidateEntity consolidateEntity = new ConsolidateEntity
                {
                    ETag = "*",
                    PartitionKey = "CONSOLIDATE",
                    RowKey = Guid.NewGuid().ToString(),
                    Id = item.id,
                    Date = DateTime.UtcNow,
                    Minutes = (int)Math.Round(item.minutes)
                };

                TableOperation addOperation = TableOperation.Insert(consolidateEntity);
                await consolidate.ExecuteAsync(addOperation);
            }



   
        }


    }
}
