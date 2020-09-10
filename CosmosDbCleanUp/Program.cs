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

				Console.WriteLine("##################################");
				Console.WriteLine("####### Cosmos DB Clean-Up #######");
				Console.WriteLine("##################################");
				Console.WriteLine("Please enter customerId:");
				var customerId = Console.ReadLine();

				Console.WriteLine($"Starting to delete documents for {customerId}");
				var chunkSize = Int32.Parse(ConfigurationManager.AppSettings["DeletionChunkSize"]);
				await DeletionInChunks(customerId, dbClient, chunkSize);

				Console.WriteLine($"Finished to delete documents for {customerId}");

				Console.WriteLine("##################################");
				Console.WriteLine("############## DONE ##############");
				Console.WriteLine("##################################");
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}
		}

		/**
         * Perform deletion of notifications of a customerId in chunks
         */
		private static async Task DeletionInChunks(string customerId, CosmosClient dbClient, int chunkSize)
		{
			while (true)
			{
				List<ResponseDto> notificationsAtCustomer;
				Console.WriteLine($"Retrieving chunk of {chunkSize} documents for customer {customerId}");
				notificationsAtCustomer = await GetNotificationsForCustomer(customerId, dbClient, chunkSize);
				Console.WriteLine($"Retrieve {notificationsAtCustomer.Count} notifications");

				foreach (ResponseDto ntf in notificationsAtCustomer)
				{
					Console.WriteLine($"Id {ntf.Id} / upn {ntf.Upn}");
				}

				if (!notificationsAtCustomer.Any())
					break;

				Console.WriteLine($"Executing deletion of {notificationsAtCustomer} for customer {customerId}");
				DeleteAllNotifications(notificationsAtCustomer, dbClient);
			}
		}

		/**
         * Delete all notifications in the provided notification list from notifications Db
         */
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

		/**
         * Retrieve all notifications belonging to a customer as a list from notifications db
         */
		private static async Task<List<ResponseDto>> GetNotificationsForCustomer(string customerId, CosmosClient dbClient, int chunkSize)
		{
			Console.WriteLine($"Starting CosmosDb Query to retrieve all notifications for customer {customerId}");
			var query = $"Select * from events where events.customerId='{customerId}'";
			var feedIterator =
				dbClient.GetContainer(DatabaseName, EventsCollectionName).GetItemQueryIterator<ResponseDto>(
					new QueryDefinition(query),
					null,
					new QueryRequestOptions()
					{
						MaxItemCount = chunkSize
					});

			var retVal = new List<ResponseDto>();
			while (feedIterator.HasMoreResults)
			{
				FeedResponse<ResponseDto> response = await feedIterator.ReadNextAsync();

				retVal.AddRange(response);
				if (retVal.Count >= chunkSize)
				{
					break;
				}
			}

			return retVal;
		}

		/**
         * Setup Cosmos Db connection
         */
		private static CosmosClient CreateCosmosClient()
		{
			var databaseUrl = ConfigurationManager.AppSettings["DatabaseUrl"];
			var databaseKey = ConfigurationManager.AppSettings["DatabaseKey"];
			return new CosmosClient(databaseUrl, databaseKey,
				new CosmosClientOptions()
				{
					AllowBulkExecution = true,
					MaxRetryAttemptsOnRateLimitedRequests = Int32.Parse(ConfigurationManager.AppSettings["MaxRetriesOnRateLimit"]),
					MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(Int32.Parse(ConfigurationManager.AppSettings["MaxRetryWaitTime"]))
				});
		}

	}

	public class ResponseDto
	{
		public string Id { get; set; }
		public string Upn { get; set; }
	}

}
