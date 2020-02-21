using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace CosmosDbCleanUp
{
    class Program
    {
        public const string DatabaseName = "notifications";
        public const string EventsCollectionName = "events";

        static async Task Main(string[] args)
        {
            try
            {
                var dbClient = CreateCosmosClient();
                while (true)
                {
                    Console.WriteLine("##################################");
                    Console.WriteLine("####### Cosmos DB Clean-Up #######");
                    Console.WriteLine("##################################");
                    Console.WriteLine("Please enter customerId:");
                    var customerId = Console.ReadLine();
                    List<ResponseDto> notificationsAtCustomer;
                    Console.WriteLine($"Starting to retrieve documents for {customerId}");
                    notificationsAtCustomer = await GetNotificationsForCustomer(customerId, dbClient);

                    /*foreach(ResponseDto ntf in notificationsAtCustomer) {
                        Console.WriteLine($"Id {ntf.Id} / upn {ntf.Upn}");
                    }*/

                    Console.WriteLine($"Finished to retrieve documents for {customerId}");

                    if (!notificationsAtCustomer.Any())
                        break;

                    Console.WriteLine($"Starting to delete documents for {customerId}");
                    DeleteAllNotifications(notificationsAtCustomer, dbClient);
                    Console.WriteLine($"Finished to delete documents for {customerId}");
                }
                Console.WriteLine("##################################");
                Console.WriteLine("############## DONE ##############");
                Console.WriteLine("##################################");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        private static void DeleteAllNotifications(List<ResponseDto> notifications, CosmosClient dbClient)
        {
            var tasks = new List<Task>();
            for (int i = 0; i < notifications.Count; i++)
            {
                tasks.Add(dbClient.GetContainer(DatabaseName, EventsCollectionName)
                    .DeleteItemAsync<ResponseDto>(notifications[i].Id,
                        new Microsoft.Azure.Cosmos.PartitionKey(notifications[i].Upn))
                    .ContinueWith(task =>
                    {
                        if (task.Result.StatusCode == HttpStatusCode.NoContent)
                        {
                            Console.WriteLine(i + 1 + " out of " + notifications.Count + " was deleted");
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Error while deleting with id: '" + notifications[i] +
                                              "' StatusCode: " + task.Result.StatusCode);
                            Console.ResetColor();
                        }
                    }
                    ));
            }
            Task.WaitAll(tasks.ToArray());
        }

        private static async Task<List<ResponseDto>> GetNotificationsForCustomer(string customerId, CosmosClient dbClient)
        {

            var query = $"Select * from events where events.customerId='{customerId}'";
            var feedIterator =
                dbClient.GetContainer(DatabaseName, EventsCollectionName).GetItemQueryIterator<ResponseDto>(
                    new QueryDefinition(query),
                    null,
                    new QueryRequestOptions()
                    {
                        MaxItemCount = 1000
                    });

            var retVal = new List<ResponseDto>();
            while (feedIterator.HasMoreResults)
            {
                FeedResponse<ResponseDto> response = await feedIterator.ReadNextAsync();

                retVal.AddRange(response);
                if (retVal.Count <= 1000)
                {
                    break;
                }
            }

            return retVal;
        }

        private static CosmosClient CreateCosmosClient()
        {
            var databaseUrl = ConfigurationManager.AppSettings["DatabaseUrl"];
            var databaseKey = ConfigurationManager.AppSettings["DatabaseKey"];
            return new CosmosClient(databaseUrl, databaseKey,
                new CosmosClientOptions()
                {
                    AllowBulkExecution = true,
                    MaxRetryAttemptsOnRateLimitedRequests = 9,
                    MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(30)
                });
        }

    }

    public class ResponseDto
    {
        public string Id { get; set; }
        public string Upn { get; set; }
    }

}
