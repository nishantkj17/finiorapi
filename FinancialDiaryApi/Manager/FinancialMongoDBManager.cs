using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using FinancialDiaryWeb.Model;
using MongoDB.Driver.Builders;
using FinancialDiaryApi.Model;
using System.IO;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace FinancialDiaryWeb.Manager
{
	public class FinancialMongoDBManager
	{
		private MongoClient dbClient;
		public FinancialMongoDBManager()
		{
			dbClient = new MongoClient("mongodb://localhost:27017/?readPreference=primary&appname=MongoDB%20Compass&ssl=false");
		}

		private IMongoCollection<BsonDocument> GetMongoCollection(string collectionName)
		{
			IMongoDatabase db = dbClient.GetDatabase(Constants.Financials);
			return db.GetCollection<BsonDocument>(collectionName);
		}
		public async Task<int> AddInvestments(string fundName, string date, string amount, string profile)
		{
			var investmentRecord = GetMongoCollection(Constants.Diary);
			var doc = new BsonDocument
			{
				{Constants.fundName, fundName},
				{Constants.date, date},
				{Constants.amount, amount},
				{Constants.profile, profile }
			};

			investmentRecord.InsertOne(doc);
			return 0;
		}
		public async Task<int> AddDebt(string accountname, int currentBalance)
		{
			var debtRecord = GetMongoCollection(Constants.Debt);
			var doc = new BsonDocument
			{
				{Constants.accountname, accountname},
				{Constants.createddate, DateTime.Now.ToString(Constants.ddMMMMyyyy)},
				{Constants.currentBalance, currentBalance}
			};

			debtRecord.InsertOne(doc);
			return 0;
		}
		public async Task<IEnumerable<InvestmentDetails>> GetInvestmentDetails()
		{
			var investmentRecord = GetMongoCollection(Constants.Diary);
			var docs = investmentRecord.Find(new BsonDocument()).ToList();
			var outputData = new List<InvestmentDetails>();

			foreach (var item in docs)
			{
				var obj = new InvestmentDetails();
				obj.fundName = (string)item[Constants.fundName];
				obj.date = (string)item[Constants.date];
				obj.denomination = (string)item[Constants.amount];
				obj.profile = (string)item[Constants.profile];
				obj.id = Convert.ToString((ObjectId)item[Constants._id]);

				outputData.Add(obj);
			}
			return outputData;
		}

		public async Task<IEnumerable<InvestmentDetails>> GetFilteredInvestmentDetails(string date, string profile)
		{
			var investmentRecord = GetMongoCollection(Constants.Diary);

			var builder = Builders<BsonDocument>.Filter;
			FilterDefinition<BsonDocument> filter = null;

			var ifDateEmpty = (string.IsNullOrEmpty(date) || date.Equals(Constants.nullValue) || date.Equals(Constants.undefined));
			var ifProfileEmpty = (string.IsNullOrEmpty(profile) || profile.Equals(Constants.nullValue) || profile.Equals(Constants.undefined));

			if (!ifDateEmpty && !ifProfileEmpty)
			{
				filter = builder.Eq(Constants.date, date) & builder.Eq(Constants.profile, profile);
			}
			else if (!ifProfileEmpty)
			{
				builder.Eq(Constants.profile, profile);
			}
			else if (!ifDateEmpty)
			{
				filter = builder.Eq(Constants.date, date);
			}
			List<BsonDocument> docs = null;

			if (filter == null)
			{
				docs = investmentRecord.Find(new BsonDocument()).ToList();
			}
			else
			{
				docs = investmentRecord.Find(filter).ToList();
			}


			var outputData = new List<InvestmentDetails>();

			foreach (var item in docs)
			{
				var obj = new InvestmentDetails
				{
					fundName = (string)item[Constants.fundName],
					date = (string)item[Constants.date],
					denomination = (string)item[Constants.amount],
					profile = (string)item[Constants.profile],
					id = Convert.ToString((ObjectId)item[Constants._id])
				};
				outputData.Add(obj);
			}
			return outputData;
		}

		internal async Task<int> UpdateSIPDetails(InvestmentDetails model)
		{
			var filter = Builders<BsonDocument>.Filter.Eq(Constants._id, MongoDB.Bson.ObjectId.Parse(model.id));
			var update = Builders<BsonDocument>.Update.Set(Constants.fundName, model.fundName)
					.Set(Constants.date, model.date)
					.Set(Constants.amount, model.denomination)
					.Set(Constants.profile, model.profile);
			GetMongoCollection(Constants.Diary).UpdateOne(filter, update);
			return 0;
		}

		internal async Task<InvestmentReturnDataForChart> GetInvestmentReturnDataForChart()
		{
			var investmentReturnData = GetCombinedMutualFundReturnDetails();
			var count = investmentReturnData.Result.Count();
			var investedAmountData = new int[count];
			var currentValueData = new int[count];
			List<string> lineChartLabelsList = new List<string>();
			int counter = 0;
			foreach (var item in investmentReturnData.Result)
			{
				lineChartLabelsList.AddRange(new string[] { "" });
				investedAmountData[counter] = item.investedamount;
				currentValueData[counter] = item.currentvalue;
				counter++;
			}
			var chartData = new List<Returns>
			{
				new Returns { Label = Constants.InvestedAmountLabel, Data = investedAmountData, pointRadius=0 },
				new Returns { Label = Constants.CurrentvalueLabel, Data = currentValueData, pointRadius=0 }
			};

			return new InvestmentReturnDataForChart { InvestmentReturnChart = chartData, ChartLabels = lineChartLabelsList.ToArray() };
		}

		internal async Task<InvestmentReturnDataForChart> GetEquityInvestmentReturnDataForChart()
		{
			var docs = GetInvestmentReturnData(Constants.Equity, Constants.ByOldDate);

			List<string> lineChartLabelsList = new List<string>();
			var investedAmountData = new int[docs.Count];
			var currentValueData = new int[docs.Count];
			int counter = 0;
			foreach (var item in docs)
			{
				lineChartLabelsList.AddRange(new string[] { "" });
				investedAmountData[counter] = (int)item[Constants.investedamount];
				currentValueData[counter] = (int)item[Constants.currentvalue];
				counter++;
			}
			var chartData = new List<Returns>
			{
				new Returns { Label = Constants.InvestedAmountLabel, Data = investedAmountData,  pointRadius=0 },
				new Returns { Label = Constants.CurrentvalueLabel, Data = currentValueData, pointRadius=0 }
			};

			return new InvestmentReturnDataForChart { InvestmentReturnChart = chartData, ChartLabels = lineChartLabelsList.ToArray() };
		}

		internal async Task<InvestmentReturnDataForChart> GetPFInvestmentReturnDataForChart()
		{
			var docs = GetInvestmentReturnData(Constants.EPFO, Constants.ByOldDate);

			List<string> lineChartLabelsList = new List<string>();
			var contributionData = new int[docs.Count];
			var interest = new int[docs.Count];
			int counter = 0;
			foreach (var item in docs)
			{
				lineChartLabelsList.AddRange(new string[] { (string)item[Constants.profile] });
				contributionData[counter] = (int)item[Constants.contribution];
				interest[counter] = (int)item[Constants.interest];
				counter++;
			}
			var chartData = new List<Returns>();
			chartData.Add(new Returns { Label = Constants.Contribution, Data = contributionData });
			chartData.Add(new Returns { Label = Constants.Interest, Data = interest });

			return new InvestmentReturnDataForChart { InvestmentReturnChart = chartData, ChartLabels = lineChartLabelsList.ToArray() };
		}

		internal async Task<int> SaveProvidentFundDetails(int contribution, int interest, string type, string profile)
		{
			var investmentRecord = GetMongoCollection(Constants.EPFO);
			var doc = new BsonDocument
			{
				{Constants.contribution, contribution},
				{Constants.interest, interest},
				{Constants.type, type},
				{Constants.profile, profile},
				{Constants.createddate, DateTime.Now.ToString(Constants.ddMMMMyyyy) }
			};

			investmentRecord.InsertOne(doc);
			return 0;
		}

		internal async Task<int> SaveEquityInvestmentReturnDetails(int investedamount, int currentvalue)
		{
			var investmentRecord = GetMongoCollection(Constants.Equity);
			var returns = ((double)(currentvalue - investedamount) / (double)investedamount) * 100;
			var doc = new BsonDocument
			{
				{Constants.investedamount, investedamount},
				{Constants.currentvalue, currentvalue},
				{Constants.returns, Math.Round(returns, 2)},
				{Constants.createddate, DateTime.Now.ToString(Constants.ddMMMMyyyy) }
			};
			CollectionBackup();
			investmentRecord.InsertOne(doc);
			return 0;
		}

		internal async Task<InvestmentReturnDataForChart> GetIndividualInvestmentReturnDataForChart()
		{
			var investmentReturnData = GetInvestmentReturnDetails();
			var count = investmentReturnData.Result.Count()/2;
			var ranjanaInvestedAmountData = new int[count];
			var ranjanaCurrentValueData = new int[count];
			var nishantInvestedAmountData = new int[count];
			var nishantCurrentValueData = new int[count];
			List<string> lineChartLabelsList = new List<string>();
			int counterNishant = 0;
			int counterRanjana = 0;
			for (int i = 0; i < count; i++)
			{
				lineChartLabelsList.AddRange(new string[] { "" });
			}
			foreach (var item in investmentReturnData.Result)
			{
				if (item.profile == Constants.RanjanaJha)
				{
					
					ranjanaInvestedAmountData[counterRanjana] = item.investedamount;
					ranjanaCurrentValueData[counterRanjana] = item.currentvalue;
					counterRanjana++;
				}
				else
				{
					nishantInvestedAmountData[counterNishant] = item.investedamount;
					nishantCurrentValueData[counterNishant] = item.currentvalue;
					counterNishant++;
				}
			}
			var chartData = new List<Returns>
			{
				new Returns { Label = Constants.RInvestedAmount, Data = ranjanaInvestedAmountData, pointRadius=0 },
				new Returns { Label = Constants.RCurrentValue, Data = ranjanaCurrentValueData, pointRadius=0 },
				new Returns { Label = Constants.NInvestedAmount, Data = nishantInvestedAmountData, pointRadius=0 },
				new Returns { Label = Constants.NCurrentvalue, Data = nishantCurrentValueData, pointRadius=0 }
			};

			return new InvestmentReturnDataForChart { InvestmentReturnChart = chartData, ChartLabels = lineChartLabelsList.ToArray() };
		}
		internal async Task<int> DeleteSIPDetails(string id)
		{
			var deleteFilter = Builders<BsonDocument>.Filter.Eq(Constants._id, MongoDB.Bson.ObjectId.Parse(id));
			var investmentRecord = GetMongoCollection(Constants.Diary);
			investmentRecord.DeleteOne(deleteFilter);
			return 0;
		}

		internal async Task<int> SaveReturns(string profile, int investedamount, int currentvalue)
		{
			var investmentRecord = GetMongoCollection(Constants.Sum);
			var returns = ((double)(currentvalue - investedamount) / (double)investedamount) * 100;
			var doc = new BsonDocument
			{
				{Constants.profile, profile },
				{Constants.investedamount, investedamount},
				{Constants.currentvalue, currentvalue},
				{Constants.returns, Math.Round(returns, 2)},
				{Constants.createddate, DateTime.Now.ToString(Constants.ddMMMMyyyy) }
			};

			investmentRecord.InsertOne(doc);
			return 0;
		}

		internal async Task<IEnumerable<InvestmentReturns>> GetInvestmentReturnDetails()
		{
			var docs = GetInvestmentReturnData(Constants.Sum, Constants.ByOldDate);
			var outputData = new List<InvestmentReturns>();

			foreach (var item in docs)
			{
				var obj = new InvestmentReturns();
				obj.investedamount = (int)item[Constants.investedamount];
				obj.currentvalue = (int)item[Constants.currentvalue];
				obj.returns = (double)item[Constants.returns];
				obj.profile = (string)item[Constants.profile];
				obj.createddate = (string)item[Constants.createddate];
				obj.id = Convert.ToString((ObjectId)item[Constants._id]);
				outputData.Add(obj);
			}
			return outputData;
		}
		private List<BsonDocument> GetInvestmentReturnData(string collection, string order)
		{
			var investmentRecord = GetMongoCollection(collection);
			var data = new List<BsonDocument>();
			if(order.Equals(Constants.ByLatestDate))
			{
				data =investmentRecord.Find(new BsonDocument())
						   .Sort(Builders<BsonDocument>.Sort.Descending(Constants.createddate)
						   .Descending(Constants.createddate))
						   .ToList();
			}
			else
			{
				data = investmentRecord.Find(new BsonDocument())
						   .Sort(Builders<BsonDocument>.Sort.Ascending(Constants.createddate)
						   .Ascending(Constants.createddate))
						   .ToList();
			}
			return data;
		}

		private List<BsonDocument> GetBalanceDebtData(string collection, string order)
		{
			var investmentRecord = GetMongoCollection(collection);
			var data = new List<BsonDocument>();
			if (order.Equals(Constants.ByLatestDate))
			{
				//data = investmentRecord.Distinct("type",FilterDefinition<T>.Empty).Find(new BsonDocument())
				//		   .Sort(Builders<BsonDocument>.Sort.Descending(Constants.createddate)
				//		   .Descending(Constants.createddate))
				//		   .ToList();
			}
			else
			{
				data = investmentRecord.Find(new BsonDocument())
						   .Sort(Builders<BsonDocument>.Sort.Ascending(Constants.createddate)
						   .Ascending(Constants.createddate))
						   .ToList();
			}
			return data;
		}
		internal async Task<IEnumerable<InvestmentReturns>> GetCombinedMutualFundReturnDetails()
		{
			var docs = GetInvestmentReturnData(Constants.Sum, Constants.ByOldDate);

			var combinedInvestment = new Dictionary<string, int>();
			var outputData = new List<InvestmentReturns>();
			foreach (var item in docs)
			{
				if (!combinedInvestment.ContainsKey((string)item[Constants.createddate]))
				{
					combinedInvestment.Add((string)item[Constants.createddate], 0);
					var obj = new InvestmentReturns
					{
						createddate = (string)item[Constants.createddate],
						investedamount = (int)item[Constants.investedamount],
						currentvalue = (int)item[Constants.currentvalue]
					};
					outputData.Add(obj);
				}
				else
				{
					foreach (var entry in outputData)
					{
						if (entry.createddate.Equals((string)item[Constants.createddate]))
						{
							entry.investedamount += (int)item[Constants.investedamount];
							entry.currentvalue += (int)item[Constants.currentvalue];
							entry.returns = Math.Round(((double)(entry.currentvalue - entry.investedamount) / (double)entry.investedamount) * 100, 2);
						}
					}

				}
			}

			return outputData;
		}

		internal async Task<IEnumerable<InvestmentDetails>> GetSIPDetailsByFund()
		{
			var investmentRecord = GetMongoCollection(Constants.Diary);
			var docs = investmentRecord.Find(new BsonDocument()).ToList();
			var sipDetailsFund = new Dictionary<string, int>();
			var outputData = new List<InvestmentDetails>();
			foreach (var item in docs)
			{
				if (!sipDetailsFund.ContainsKey((string)item[Constants.fundName]))
				{
					sipDetailsFund.Add((string)item[Constants.fundName], Convert.ToInt32((string)item[Constants.amount]));
				}
				else
				{
					sipDetailsFund[(string)item[Constants.fundName]] += Convert.ToInt32((string)item[Constants.amount]);
				}
			}

			foreach (KeyValuePair<string, int> entry in sipDetailsFund.OrderBy(i => i.Key))
			{
				var obj = new InvestmentDetails();
				obj.fundName = entry.Key;
				obj.denomination = Convert.ToString(entry.Value);
				outputData.Add(obj);
			}
			return outputData;
		}

		internal async Task<IEnumerable<InvestmentDetails>> GetSIPDetailsByDate()
		{
			var investmentRecord = GetMongoCollection(Constants.Diary);
			var docs = investmentRecord.Find(new BsonDocument()).ToList();
			var sipDetailsFund = new Dictionary<string, int>();

			foreach (var item in docs)
			{
				if (!sipDetailsFund.ContainsKey((string)item[Constants.date]))
				{
					sipDetailsFund.Add((string)item[Constants.date], Convert.ToInt32((string)item[Constants.amount]));
				}
				else
				{
					sipDetailsFund[(string)item[Constants.date]] += Convert.ToInt32((string)item[Constants.amount]);
				}
			}
			var outputData = new List<InvestmentDetails>();
			foreach (KeyValuePair<string, int> entry in sipDetailsFund.OrderBy(i => i.Key))
			{
				var obj = new InvestmentDetails
				{
					date = entry.Key,
					denomination = Convert.ToString(entry.Value)
				};
				outputData.Add(obj);
			}
			return outputData;
		}

		internal async Task<int> SaveProvidentFundDetails(string profile, int investedamount, int currentvalue)
		{
			var investmentRecord = GetMongoCollection(Constants.Sum);
			var returns = ((double)(currentvalue - investedamount) / (double)investedamount) * 100;
			var doc = new BsonDocument
			{
				{Constants.profile, profile },
				{Constants.investedamount, investedamount},
				{Constants.currentvalue, currentvalue},
				{Constants.returns, Math.Round(returns, 2)},
				{Constants.createddate, DateTime.Now.ToString(Constants.ddMMMMyyyy) }
			};

			investmentRecord.InsertOne(doc);
			return 0;
		}

		internal async Task<DashboardData> GetDashBoardData()
		{
			double epfoData = 0;
			double ppfData = 0;
			foreach (var item in GetInvestmentReturnData(Constants.EPFO, Constants.ByLatestDate))
			{
				if (((string)item[Constants.type]).Equals(Constants.EPFO))
				{
					epfoData += (int)item[Constants.contribution] + (int)item[Constants.interest];
				}
				else
				{
					ppfData += (int)item[Constants.contribution] + (int)item[Constants.interest];
				}
			}
			double mutualFundData = GetCombinedMutualFundReturnDetails().Result.Last().currentvalue;
			double equityData = (int)GetInvestmentReturnData(Constants.Equity, Constants.ByLatestDate).FirstOrDefault()[Constants.currentvalue];
			
			var obj = new DashboardData
			{
				epfo = epfoData,
				equity = equityData,
				mutualfund = mutualFundData,
				ppf = ppfData
			};
			return obj;
		}

		private async void CollectionBackup()
		{
			string outputFileName="C:\\repos\\"; // initialize to the output file
			IMongoDatabase db = dbClient.GetDatabase(Constants.Financials);
			IMongoCollection<BsonDocument> collection;  // initialize to the collection to read from
			foreach (var item in db.ListCollectionsAsync().Result.ToListAsync<BsonDocument>().Result)
			{
				outputFileName += (string)item["name"] + ".json";
				collection = GetMongoCollection((string)item["name"]);
				using (var streamWriter = new StreamWriter(outputFileName))
				{
					await collection.Find(new BsonDocument())
						.ForEachAsync(async (document) =>
						{
							using (var stringWriter = new StringWriter())
							using (var jsonWriter = new JsonWriter(stringWriter))
							{
								var context = BsonSerializationContext.CreateRoot(jsonWriter);
								collection.DocumentSerializer.Serialize(context, document);
								var line = stringWriter.ToString();
								await streamWriter.WriteLineAsync(line);
							}
						});
					outputFileName = "C:\\repos\\";
				}
			}
	
		}
	}
}
