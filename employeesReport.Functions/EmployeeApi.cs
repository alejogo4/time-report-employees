using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;
using employeesReport.Common.Models;
using employeesReport.Common.Responses;
using employeesReport.Functions.Entities;
using System.Collections.Generic;
using System.Linq;

namespace employeesReport.Functions
{
    public class DataConsolidate
    {
        public int id { set; get; }
        public double minutes { set; get; }
    }

    public static class EmployeeApi
    {
        [FunctionName(nameof(CreateTime))]
        public static async Task<IActionResult> CreateTime(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "timeReport")] HttpRequest req,
            [Table("timeReport", Connection = "AzureWebJobsStorage")] CloudTable timeReport,
            ILogger log)
        {
            log.LogInformation("Recieved a new register.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            EmployeeReport employeeReport = JsonConvert.DeserializeObject<EmployeeReport>(requestBody);

            if (employeeReport.Id == null || employeeReport.Id == 0)
            {
                return new BadRequestObjectResult(new Responses
                {
                    IsSuccess = false,
                    Message = "The request must have a id employee."
                });
            }

            if (employeeReport.Type != 0 && employeeReport.Type != 1)
            {
                return new BadRequestObjectResult(new Responses
                {
                    IsSuccess = false,
                    Message = "The request must have type 1 or 0."
                });
            }

            EmployeeReportEntity employeeReportEntity = new EmployeeReportEntity
            {
                ETag = "*",
                PartitionKey = "TIME_REPORT",
                RowKey = Guid.NewGuid().ToString(),
                Id = employeeReport.Id,
                Date = DateTime.UtcNow,
                Consolidated = false,
                Type = employeeReport.Type
            };

            TableOperation addOperation = TableOperation.Insert(employeeReportEntity);
            await timeReport.ExecuteAsync(addOperation);

            string message = "New register stored in table";
            log.LogInformation(message);

            return new OkObjectResult(new Responses
            {
                IsSuccess = true,
                Message = message,
                Result = employeeReportEntity
            });
        }




        [FunctionName(nameof(UpdateTime))]
        public static async Task<IActionResult> UpdateTime(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "timeReport/{id}")] HttpRequest req,
            [Table("timeReport", Connection = "AzureWebJobsStorage")] CloudTable timeReport,
            string id,
            ILogger log)
        {
            log.LogInformation($"Update report: {id}, received.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            EmployeeReport employee = JsonConvert.DeserializeObject<EmployeeReport>(requestBody);

            TableOperation findOperation = TableOperation.Retrieve<EmployeeReportEntity>("TIME_REPORT", id.ToString());
            TableResult findResult = await timeReport.ExecuteAsync(findOperation);
            if (findResult.Result == null)
            {
                return new BadRequestObjectResult(new Responses
                {
                    IsSuccess = false,
                    Message = "Employee report not found."
                });
            }


            EmployeeReportEntity employeeEntity = (EmployeeReportEntity)findResult.Result;
            if (employeeEntity.Type == 0 || employeeEntity.Type == 1)
            {
                employeeEntity.Type = employee.Type;
            }

            employeeEntity.Consolidated = employee.Consolidated;

            TableOperation addOperation = TableOperation.Replace(employeeEntity);

            await timeReport.ExecuteAsync(addOperation);

            string message = $"Employee: {id}, updated in table.";
            log.LogInformation(message);

            return new OkObjectResult(new Responses
            {
                IsSuccess = true,
                Message = message,
                Result = employeeEntity
            });
        }

        [FunctionName(nameof(GetAllReport))]
        public static async Task<IActionResult> GetAllReport(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "timeReport")] HttpRequest req,
            [Table("timeReport", Connection = "AzureWebJobsStorage")] CloudTable timeReport,
            ILogger log)
        {
            log.LogInformation("Get all report received.");

            TableQuery<EmployeeReportEntity> query = new TableQuery<EmployeeReportEntity>();
            TableQuerySegment<EmployeeReportEntity> reportTimes = await timeReport.ExecuteQuerySegmentedAsync(query, null);

            string message = "Retrieved all times report.";
            log.LogInformation(message);

            return new OkObjectResult(new Responses
            {
                IsSuccess = true,
                Message = message,
                Result = reportTimes
            });
        }

        [FunctionName(nameof(GetReportById))]
        public static IActionResult GetReportById(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "timeReport/{id}")] HttpRequest req,
            [Table("timeReport", "TIME_REPORT", "{id}", Connection = "AzureWebJobsStorage")] EmployeeReportEntity employeeReportEntity,
            ILogger log)
        {
            log.LogInformation($"Get report by id:  {employeeReportEntity.Id}, received.");

            if (employeeReportEntity == null)
            {
                return new BadRequestObjectResult(new Responses
                {
                    IsSuccess = false,
                    Message = "Report not found."
                });
            }

            string message = $"Report: {employeeReportEntity.Id}, retrieved.";
            log.LogInformation(message);

            return new OkObjectResult(new Responses
            {
                IsSuccess = true,
                Message = message,
                Result = employeeReportEntity
            });
        }

        [FunctionName(nameof(GetConsolidateByDate))]
        public static async Task<IActionResult> GetConsolidateByDate(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "consolidateByDate")] HttpRequest req,
            [Table("consolidate", Connection = "AzureWebJobsStorage")] CloudTable consolidate,
            ILogger log)
        {
            TableQuery<ConsolidateEntity> query = new TableQuery<ConsolidateEntity>();
            TableQuerySegment<ConsolidateEntity> reportTimes = await consolidate.ExecuteQuerySegmentedAsync(query, null);

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            ConsolidateByDate consolidateByDate = JsonConvert.DeserializeObject<ConsolidateByDate>(requestBody);

            List<ConsolidateEntity> reports = reportTimes.Results.Where(e => e.Date.ToString("yyyy/MM/dd").Equals(consolidateByDate.Date)).ToList();

            string message = "Retrieved all consolidate by date .";
            log.LogInformation(message);

            return new OkObjectResult(new Responses
            {
                IsSuccess = true,
                Message = message,
                Result = reports
            });
        }


        [FunctionName(nameof(DeleteReport))]
        public static async Task<IActionResult> DeleteReport(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "timeReport/{id}")] HttpRequest req,
            [Table("timeReport", "TIME_REPORT", "{id}", Connection = "AzureWebJobsStorage")] EmployeeReportEntity employeeReportEntity,
            [Table("timeReport", Connection = "AzureWebJobsStorage")] CloudTable timeReport,
            ILogger log)
        {
            log.LogInformation($"Delete todo: {employeeReportEntity.Id}, received.");

            if (employeeReportEntity == null)
            {
                return new BadRequestObjectResult(new Responses
                {
                    IsSuccess = false,
                    Message = "Report not found."
                });
            }

            await timeReport.ExecuteAsync(TableOperation.Delete(employeeReportEntity));
            string message = $"Report : {employeeReportEntity.Id}, deleted.";
            log.LogInformation(message);

            return new OkObjectResult(new Responses
            {
                IsSuccess = true,
                Message = message,
                Result = employeeReportEntity
            });
        }



        [FunctionName(nameof(ConsolidateReport))]
        public static async Task<IActionResult> ConsolidateReport(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "consolidate")] HttpRequest req,
        [Table("timeReport", Connection = "AzureWebJobsStorage")] CloudTable timeReport,
        [Table("consolidate", Connection = "AzureWebJobsStorage")] CloudTable consolidate)
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
            


            return new OkObjectResult(new Responses
            {
                IsSuccess = true,
                Message = "Consolidate successful",
                Result = new { RowKey = key, Report = report }
            }); 

        }
    }




}
