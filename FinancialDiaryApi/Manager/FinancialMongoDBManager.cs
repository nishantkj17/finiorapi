using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FinancialDiaryApi.Model;
using FinancialDiaryWeb.Model;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace FinancialDiaryApi.Manager
{
	public class FinancialMongoDbManager
	{
		private MongoClient _dbClient;
		public FinancialMongoDbManager()
		{
			_dbClient = new MongoClient("mongodb://localhost:27017/?readPreference=primary&appname=MongoDB%20Compass&ssl=false");
		}

		private IMongoCollection<BsonDocument> GetMongoCollection(string collectionName)
		{
			IMongoDatabase db = _dbClient.GetDatabase(Constants.Financials);
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

			await investmentRecord.InsertOneAsync(doc);
			return 0;
		}
		public async Task<int> AddDebt(string accountname, int currentBalance)
		{
			var debtRecord = GetMongoCollection(Constants.Debt);
			var doc = new BsonDocument
			{
				{Constants.accountname, accountname},
				{Constants.createddate, DateTime.Now},
				{Constants.currentBalance, currentBalance}
			};

			await debtRecord.InsertOneAsync(doc);
			return 0;
		}
		public async Task<IEnumerable<InvestmentDetails>> GetInvestmentDetails()
		{
			var investmentRecord = GetMongoCollection(Constants.Diary);
			var docs = investmentRecord.Find(new BsonDocument()).ToList();

			return docs.Select(item => new InvestmentDetails
			{
				fundName = (string)item[Constants.fundName],
				date = (string)item[Constants.date],
				denomination = (string)item[Constants.amount],
				profile = (string)item[Constants.profile],
				id = Convert.ToString((ObjectId)item[Constants._id])
			})
				.ToList();
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
				filter = builder.Eq(Constants.profile, profile);
			}
			else if (!ifDateEmpty)
			{
				filter = builder.Eq(Constants.date, date);
			}
			List<BsonDocument> docs = null;

			docs = filter == null ? investmentRecord.Find(new BsonDocument()).ToList() : investmentRecord.Find(filter).ToList();

			return docs.Select(item => new InvestmentDetails
			{
				fundName = (string)item[Constants.fundName],
				date = (string)item[Constants.date],
				denomination = (string)item[Constants.amount],
				profile = (string)item[Constants.profile],
				id = Convert.ToString((ObjectId)item[Constants._id])
			})
				.ToList();
		}

		internal async Task<int> UpdateSIPDetails(InvestmentDetails model)
		{
			var filter = Builders<BsonDocument>.Filter.Eq(Constants._id, MongoDB.Bson.ObjectId.Parse(model.id));
			var update = Builders<BsonDocument>.Update.Set(Constants.fundName, model.fundName)
					.Set(Constants.date, model.date)
					.Set(Constants.amount, model.denomination)
					.Set(Constants.profile, model.profile);
			await GetMongoCollection(Constants.Diary).UpdateOneAsync(filter, update);
			return 0;
		}

		internal async Task<InvestmentReturnDataForChart> GetInvestmentReturnDataForChart()
		{
			var investmentReturnData = GetCombinedMutualFundReturnDetails();
			var count = investmentReturnData.Result.Count();
			var investedAmountData = new double[count];
			var currentValueData = new double[count];
			var lineChartLabelsList = new List<string>();
			var counter = 0;
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

			var lineChartLabelsList = new List<string>();
			var investedAmountData = new double[docs.Count];
			var currentValueData = new double[docs.Count];
			var counter = 0;
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

			var lineChartLabelsList = new List<string>();
			var counter = 0;

			//-------------------------------------------------------------------------------------------------------------------------------
			var epfoPrimaryBalance = new double[docs.Count];
			var epfoSecondaryBalance = new double[docs.Count];
			var ppfBalance = new double[docs.Count];

			foreach (var item in docs)
			{
				lineChartLabelsList.AddRange(new string[] { "" });
				if (Convert.ToString(item[Constants.type]).Equals(Constants.EPFO))
				{
					if (Convert.ToString(item[Constants.profile]).Contains("Nishant"))
					{
						epfoSecondaryBalance[counter] = (double)item[Constants.epfoPrimaryBalance];
					}
					else
					{
						epfoPrimaryBalance[counter] = (double)item[Constants.epfoPrimaryBalance];
					}
				}
				else
				{
					ppfBalance[counter] = (double)item[Constants.ppfBalance];
				}
				counter++;
			}
			epfoPrimaryBalance = epfoPrimaryBalance.Where(x => x != 0).ToArray();
			if (lineChartLabelsList.Count > (lineChartLabelsList.Count - epfoPrimaryBalance.Length) / 2)
			{
				lineChartLabelsList.RemoveRange((lineChartLabelsList.Count - epfoPrimaryBalance.Length) / 2, (lineChartLabelsList.Count - epfoPrimaryBalance.Length));
			}

			var chartData = new List<Returns>
			{
				new Returns { Label = Constants.NCurrentvalue, Data = epfoSecondaryBalance.Where(x =>  x != 0).ToArray(),  pointRadius=0 },
				new Returns { Label = Constants.RCurrentValue, Data = epfoPrimaryBalance, pointRadius=0 },
				new Returns { Label = Constants.ppf, Data = ppfBalance.Where(x =>  x != 0).ToArray(), pointRadius=0 }
			};

			return new InvestmentReturnDataForChart { InvestmentReturnChart = chartData, ChartLabels = lineChartLabelsList.ToArray() };
		}

		internal async Task<int> SaveProvidentFundDetails(double primaryBalance, double ppfBalance, string type, string profile)
		{
			var investmentRecord = GetMongoCollection(Constants.EPFO);
			var doc = new BsonDocument
			{
				{Constants.epfoPrimaryBalance, primaryBalance},
				{Constants.ppfBalance, ppfBalance},
				{Constants.type, type},
				{Constants.profile, profile},
				{Constants.createddate, DateTime.Now }
			};

			await investmentRecord.InsertOneAsync(doc);
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
				{Constants.createddate, DateTime.Now }
			};
			CollectionBackup();
			await investmentRecord.InsertOneAsync(doc);
			return 0;
		}

		internal async Task<InvestmentReturnDataForChart> GetIndividualInvestmentReturnDataForChart()
		{
			var investmentReturnData = GetInvestmentReturnDetails();
			var count = investmentReturnData.Result.Count();
			var primaryInvestedAmountData = new double[count];
			var primaryCurrentValueData = new double[count];
			var secondaryInvestedAmountData = new double[count];
			var secondaryCurrentValueData = new double[count];
			var lineChartLabelsList = new List<string>();
			var secondaryCounter = 0;
			var primaryCounter = 0;
			for (var i = 0; i < count; i++)
			{
				lineChartLabelsList.AddRange(new string[] { "" });
			}
			foreach (var item in investmentReturnData.Result)
			{
				if (item.profile == Constants.RanjanaJha)
				{

					primaryInvestedAmountData[primaryCounter] = item.investedamount;
					primaryCurrentValueData[primaryCounter] = item.currentvalue;
					primaryCounter++;
				}
				else
				{
					secondaryInvestedAmountData[secondaryCounter] = item.investedamount;
					secondaryCurrentValueData[secondaryCounter] = item.currentvalue;
					secondaryCounter++;
				}
			}

			primaryInvestedAmountData = primaryInvestedAmountData.Where(x => x != 0).ToArray();

			if (lineChartLabelsList.Count > (lineChartLabelsList.Count - primaryInvestedAmountData.Length) / 2)
			{
				lineChartLabelsList.RemoveRange((lineChartLabelsList.Count - primaryInvestedAmountData.Length) / 2, (lineChartLabelsList.Count - primaryInvestedAmountData.Length));
			}
			var chartData = new List<Returns>
			{
				new Returns { Label = Constants.RInvestedAmount, Data = primaryInvestedAmountData, pointRadius=0 },
				new Returns { Label = Constants.RCurrentValue, Data = primaryCurrentValueData.Where(x => x != 0).ToArray(), pointRadius=0 },
				new Returns { Label = Constants.NInvestedAmount, Data = secondaryInvestedAmountData.Where(x => x != 0).ToArray(), pointRadius=0 },
				new Returns { Label = Constants.NCurrentvalue, Data = secondaryCurrentValueData.Where(x => x != 0).ToArray(), pointRadius=0 }
			};

			return new InvestmentReturnDataForChart { InvestmentReturnChart = chartData, ChartLabels = lineChartLabelsList.ToArray() };
		}

		internal async Task<InvestmentReturnDataForChart> GetDebtAndInvestmentForChart()
		{
			var docs = GetInvestmentReturnData(Constants.DebtAndInvestment, Constants.ByOldDate);

			var lineChartLabelsList = new List<string>();
			var investedAmountData = new double[docs.Count];
			var debtData = new double[docs.Count];
			var counter = 0;
			foreach (var item in docs)
			{
				lineChartLabelsList.AddRange(new string[] { "" });
				investedAmountData[counter] = (double)item[Constants.totalinvestments];
				debtData[counter] = (double)item[Constants.totaldebt];
				counter++;
			}
			var chartData = new List<Returns>
			{
				new Returns { Label = Constants.Debt, Data = debtData,  pointRadius=2 },
				new Returns { Label = Constants.SavingsInvestment, Data = investedAmountData, pointRadius=2 }
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
				{Constants.createddate, DateTime.Now }
			};

			await investmentRecord.InsertOneAsync(doc);
			return 0;
		}

		internal async Task<IEnumerable<InvestmentReturns>> GetInvestmentReturnDetails()
		{
			var docs = GetInvestmentReturnData(Constants.Sum, Constants.ByOldDate);

			return docs.Select(item => new InvestmentReturns
			{
				investedamount = (int)item[Constants.investedamount],
				currentvalue = (int)item[Constants.currentvalue],
				returns = (double)item[Constants.returns],
				profile = (string)item[Constants.profile],
				createddate = Convert.ToDateTime(item[Constants.createddate]).ToString(Constants.ddMMMMyyyy),
				id = Convert.ToString((ObjectId)item[Constants._id])
			})
				.ToList();
		}
		private List<BsonDocument> GetInvestmentReturnData(string collection, string order)
		{
			var investmentRecord = GetMongoCollection(collection);
			List<BsonDocument> data;
			if (order.Equals(Constants.ByLatestDate))
			{
				data = investmentRecord.Find(new BsonDocument())
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

		internal async Task<IEnumerable<InvestmentReturns>> GetCombinedMutualFundReturnDetails()
		{
			var docs = GetInvestmentReturnData(Constants.Sum, Constants.ByOldDate);
			var combinedInvestment = new Dictionary<string, int>();
			var outputData = new List<InvestmentReturns>();
			foreach (var item in docs)
			{
				var keyDate = Convert.ToDateTime(item[Constants.createddate]).ToString(Constants.ddMMMMyyyy);
				if (!combinedInvestment.ContainsKey(keyDate))
				{
					combinedInvestment.Add(keyDate, 0);
					var obj = new InvestmentReturns
					{
						createddate = Convert.ToDateTime(item[Constants.createddate]).ToString(Constants.ddMMMMyyyy),
						investedamount = (int)item[Constants.investedamount],
						currentvalue = (int)item[Constants.currentvalue]
					};
					outputData.Add(obj);
				}
				else
				{
					foreach (var entry in outputData.Where(entry => entry.createddate.Equals(Convert.ToDateTime(item[Constants.createddate]).ToString(Constants.ddMMMMyyyy))))
					{
						entry.investedamount += (int)item[Constants.investedamount];
						entry.currentvalue += (int)item[Constants.currentvalue];
						entry.returns = Math.Round(((double)(entry.currentvalue - entry.investedamount) / (double)entry.investedamount) * 100, 2);
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

			return sipDetailsFund.OrderBy(i => i.Key).Select(entry
				=> new InvestmentDetails { fundName = entry.Key, denomination = Convert.ToString(entry.Value) }).ToList();
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

			return sipDetailsFund.OrderBy(i => i.Key).Select(entry
				=> new InvestmentDetails { date = entry.Key, denomination = Convert.ToString(entry.Value) }).ToList();
		}

		internal async Task<IEnumerable<DashboardAssetDetails>> GetAssetsDashBoardData()
		{
			double epfoData = 0;
			double ppfData = 0;
			double epfoDataPrevious = 0;
			double ppfDataPrevious = 0;
			var mutualFundDashBoardData = GetCombinedMutualFundReturnDetails().Result.Reverse().Take(2).ToList();

			var equityDashBoardData =
				GetInvestmentReturnData(Constants.Equity, Constants.ByLatestDate).Take(2).ToList();


			foreach (var item in GetInvestmentReturnData(Constants.EPFO, Constants.ByLatestDate).GetRange(0, 3))
			{
				if (((string)item[Constants.type]).Equals(Constants.EPFO))
				{
					epfoData += (double)item[Constants.epfoPrimaryBalance];
				}
				else
				{
					ppfData += (double)item[Constants.ppfBalance];
				}
			}

			foreach (var item in GetInvestmentReturnData(Constants.EPFO, Constants.ByLatestDate).GetRange(3, 3))
			{
				if (((string)item[Constants.type]).Equals(Constants.EPFO))
				{
					epfoDataPrevious += (double)item[Constants.epfoPrimaryBalance];
				}
				else
				{
					ppfDataPrevious += (double)item[Constants.ppfBalance];
				}
			}

			var outputData = new List<DashboardAssetDetails>
			{
				new DashboardAssetDetails
				{
					cardclass = "bg-primary", investmenttype = Constants.mutualfund, currentvalue = mutualFundDashBoardData.First().currentvalue,
					increased = (mutualFundDashBoardData.First().returns > mutualFundDashBoardData.Last().returns)
				},
				new DashboardAssetDetails
				{
					cardclass = "bg-success", investmenttype = Constants.Equity, currentvalue = (int)equityDashBoardData.FirstOrDefault()?[Constants.currentvalue],
					increased = (equityDashBoardData.First()[Constants.returns] > equityDashBoardData.Last()[Constants.returns])
				},
				new DashboardAssetDetails
				{
					cardclass = "bg-warning", investmenttype = Constants.EPFO, currentvalue = epfoData, increased = (epfoData > epfoDataPrevious)
				},
				new DashboardAssetDetails
				{
					cardclass = "bg-primary", investmenttype = Constants.ppf, currentvalue = ppfData, increased = (ppfData > ppfDataPrevious)
				}
			};

			return outputData;
		}

		internal async Task<IEnumerable<DebtDetails>> GetDebtsDashBoardData()
		{
			var debtRecords = GetMongoCollection(Constants.Debt);
			var recordExist = debtRecords.Find(new BsonDocument()).ToList();
			if (recordExist.Count <= 0) return null;

			var latestDate = (DateTime)debtRecords.Find(new BsonDocument())
				.Sort(Builders<BsonDocument>.Sort.Descending(Constants.createddate)
					.Descending(Constants.createddate))
				.ToList().FirstOrDefault()?[Constants.createddate];

			var start = new DateTime(latestDate.Year, latestDate.Month, latestDate.Day - 1);
			var end = new DateTime(latestDate.Year, latestDate.Month, latestDate.Day + 1);

			var filter = Builders<BsonDocument>.Filter.Gte(Constants.createddate, start) &
						 Builders<BsonDocument>.Filter.Lte(Constants.createddate, end) &
						 Builders<BsonDocument>.Filter.Gte(Constants.currentBalance, 0);

			var docs = filter == null
				? debtRecords.Find(new BsonDocument()).ToList()
				: debtRecords.Find(filter).ToList();
			return docs.Select(item => new DebtDetails
			{
				accountname = (string)item[Constants.accountname],
				currentbalance = (int)item[Constants.currentBalance],
				createddate = (DateTime)item[Constants.createddate],
				id = Convert.ToString((ObjectId)item[Constants._id])
			}).ToList();
		}

		internal async Task<int> RefreshDebtAndInvestmentDataForChart()
		{
			var debtInvestmentRecord = GetMongoCollection(Constants.DebtAndInvestment);
			var recordExist = debtInvestmentRecord.Find(new BsonDocument()).ToList();
			if (recordExist.Count > 0)
			{
				var latestDate = (DateTime)debtInvestmentRecord.Find(new BsonDocument())
					.Sort(Builders<BsonDocument>.Sort.Descending(Constants.createddate)
						.Descending(Constants.createddate))
					.ToList().FirstOrDefault()?[Constants.createddate];

				var filter = Builders<BsonDocument>.Filter.Eq(Constants.createddate, latestDate);

				var docs = filter == null
					? debtInvestmentRecord.Find(new BsonDocument()).ToList()
					: debtInvestmentRecord.Find(filter).ToList();
				if (docs.Count > 0)
				{
					var lastEntry = (DateTime)docs[0][Constants.createddate];
					if (lastEntry.Month == DateTime.Now.Month)
						return 0;
				}
			}

			var assetData = GetAssetsDashBoardData().Result;
			var debtData = GetDebtsDashBoardData().Result;
			double cumulativeDebt = debtData.Sum(item => item.currentbalance);
			double cumulativeAsset = assetData.Sum(item => item.currentvalue);
			var doc = new BsonDocument
			{
				{Constants.totaldebt, cumulativeDebt},
				{Constants.totalinvestments, cumulativeAsset},
				{Constants.createddate, DateTime.Now}
			};

			await debtInvestmentRecord.InsertOneAsync(doc);
			return 1;
		}

		internal async Task<List<string>> GetDebtAccountName()
		{
			var debtAccounts = GetMongoCollection(Constants.DebtAccounts).Find(new BsonDocument()).ToList();
			return debtAccounts.Select(item => (string)item[Constants.name]).ToList();
		}

		private async void CollectionBackup()
		{
			var outputFileName = Constants.outputPath; // initialize to the output file
			var db = _dbClient.GetDatabase(Constants.Financials);
			IMongoCollection<BsonDocument> collection;  // initialize to the collection to read from
			foreach (var item in db.ListCollectionsAsync().Result.ToListAsync<BsonDocument>().Result)
			{
				outputFileName += (string)item[Constants.name] + ".json";
				collection = GetMongoCollection((string)item[Constants.name]);
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
					outputFileName = Constants.outputPath;
				}
			}

		}
	}
}
