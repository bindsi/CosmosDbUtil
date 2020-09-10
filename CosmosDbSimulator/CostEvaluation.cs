using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CosmosDbUtil
{
	public class CostEvaluation
	{
		public const string DatabaseName = "costEvaluation";
		public const string EventsCollectionName = "vehicles";

		[FunctionName("CreateCosmosDbDocs")]
		public async Task<IActionResult> CreateCosmosDbDocs(
			[HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "costEvaluation/createCosmosDbDocs")] HttpRequest req,
			ILogger log)
		{
			try
			{
				var dbClient = CreateCosmosClient();
				var container = dbClient.GetContainer(DatabaseName, EventsCollectionName);

				var tasks = new List<Task>();
				for (int i = 0; i < 200; i++)
				{
					var task = CreateDocumentsAsync(container, i * 1000);
					tasks.Add(task);
				}

				Task.WaitAll(tasks.ToArray());
				return (ActionResult)new OkObjectResult($"Succeed");
			}
			catch (CosmosException ce)
			{
				return new ObjectResult(new { StatusCode = 500, ErrorMessage = ce.Message });
			}
			catch (Exception e)
			{
				return new BadRequestObjectResult(e.Message);
			}

		}

		[FunctionName("UpdateCosmosDbDocs")]
		public async Task UpdateCosmosDbDocs([TimerTrigger("*/15 * * * * *")] TimerInfo myTimer, ILogger log)
		{
			try
			{
				var dbClient = CreateCosmosClient();
				var container = dbClient.GetContainer(DatabaseName, EventsCollectionName);
				await UpdateDocumentAsync(container);
			}
			catch (CosmosException ce)
			{
				log.LogError("Error: - " + ce.Message);
			}
			catch (Exception e)
			{
				log.LogError("Error: - " + e.Message);
			}
		}

		[FunctionName("GetCosmosDbDocs")]
		public async Task<IActionResult> GetCosmosDbDocs(
			[HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "costEvaluation/getCosmosDbDocs")]
			HttpRequest req,
			ILogger log)
		{
			try
			{
				var dbClient = CreateCosmosClient();
				var container = dbClient.GetContainer(DatabaseName, EventsCollectionName);
				var customerId = new Random().Next(1, 2000);
				var documents = await GetDocumentsForCustomer($"Select * from events",
					customerId.ToString(), container);

				return (ActionResult)new OkObjectResult(documents);
			}
			catch (CosmosException ce)
			{
				return new ObjectResult(new { StatusCode = 500, ErrorMessage = ce.Message });
			}
			catch (Exception e)
			{
				return new BadRequestObjectResult(e.Message);
			}
		}

		private async Task UpdateDocumentAsync(Container container)
		{
			var customerId = new Random().Next(1, 2000);
			var documents = await GetDocumentsForCustomer($"Select * from events",
				customerId.ToString(), container);

			foreach (var group in documents)
			{
				foreach (var doc in group.Vehicles)
				{
					doc.chargingStatus = doc.chargingStatus == "CHARGING" ? "NOT CHARGING" : "CHARGING";
					var response = await container.UpsertItemAsync<VehicleDto>(doc, new PartitionKey(doc.customerId));
					Console.WriteLine(response?.RequestCharge);
				}

			}

		}

		private async Task CreateDocumentsAsync(Container container, int startRange)
		{
			var json = await File.ReadAllTextAsync(@".\Payload\Vehicle.json");
			var vehicle = JsonConvert.DeserializeObject<VehicleDto>(json);
			for (int i = startRange + 1; i <= startRange + 1000; i++)
			{
				var customerId = new Random().Next(2000);
				var vin = $"WDS1111111P{i:D6}";
				vehicle.id = vin;
				vehicle.vin = vin;
				vehicle.customerId = customerId.ToString();
				var response = await container.UpsertItemAsync<VehicleDto>(vehicle, new PartitionKey(customerId.ToString()));

			}
		}

		private async Task<List<VehicleResponseDto>> GetDocumentsForCustomer(string query, string customerId, Container container)
		{
			var feedIterator =
				container.GetItemQueryIterator<VehicleDto>(
					new QueryDefinition(query),
					null,
					new QueryRequestOptions()
					{
						MaxItemCount = 1000,
						PartitionKey = new PartitionKey(customerId)
					});

			var retVal = new List<VehicleResponseDto>();
			while (feedIterator.HasMoreResults)
			{
				FeedResponse<VehicleDto> response = await feedIterator.ReadNextAsync();
				var vehicles = new VehicleResponseDto
				{
					Vehicles = new List<VehicleDto>(response),
					RUs = response.RequestCharge
				};
				retVal.Add(vehicles);
			}

			return retVal;
		}

		private static CosmosClient CreateCosmosClient()
		{
			var databaseUrl = Environment.GetEnvironmentVariable("DatabaseUrl");
			var databaseKey = Environment.GetEnvironmentVariable("DatabaseKey");
			return new CosmosClient(databaseUrl, databaseKey,
				new CosmosClientOptions()
				{
					AllowBulkExecution = true
				});
		}

	}

}
