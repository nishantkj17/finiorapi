using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FinancialDiaryApi.Model;
using FinancialDiaryWeb.Model;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace FinancialDiaryApi.Manager
{
	public class FinancialMongoDbManager
	{
		private MongoClient _dbClientCloud;
		public IConfiguration Configuration { get; }
		public FinancialMongoDbManager()
		{
			var cn = GetConnectionString();
			_dbClientCloud = new MongoClient(GetConnectionString());
		}
		public static string GetConnectionString()
		{
			return Startup.ConnectionString;
		}
		private IMongoCollection<BsonDocument> GetMongoCollection(string collectionName)
		{
			IMongoDatabase db = _dbClientCloud.GetDatabase(Constants.Financials);
			return db.GetCollection<BsonDocument>(collectionName);
		}

		public async Task<int> AddInvestments(string fundName, string date, string amount, string profile, string user)
		{
			var investmentRecord = GetMongoCollection(Constants.Diary);
			var doc = new BsonDocument
			{
				{Constants.fundName, fundName},
				{Constants.date, date},
				{Constants.amount, amount},
				{Constants.profile, profile },
				{Constants.user, user }
			};

			await investmentRecord.InsertOneAsync(doc);
			return 0;
		}
		public async Task<int> AddDebt(string accountname, int currentBalance, string user)
		{
			var debtRecord = GetMongoCollection(Constants.Debt);
			var doc = new BsonDocument
			{
				{Constants.accountname, accountname},
				{Constants.createddate, DateTime.Now},
				{Constants.currentBalance, currentBalance},
				{Constants.user, user}
			};

			await debtRecord.InsertOneAsync(doc);
			return 0;
		}
		public async Task<IEnumerable<InvestmentDetails>> GetInvestmentDetails(string user)
		{
			var filter = Builders<BsonDocument>.Filter.Eq(Constants.profile, user);
			var investmentRecord = GetMongoCollection(Constants.Diary);
			var docs = investmentRecord.Find(filter).ToList();

			return docs.Select(item => new InvestmentDetails
			{
				fundName = (string)item[Constants.fundName],
				date = (string)item[Constants.date],
				denomination = (string)item[Constants.amount],
				profile = (string)item[Constants.profile],
				id = Convert.ToString((ObjectId)item[Constants._id]),
				user = Convert.ToString((string)item[Constants.user])
			})
				.ToList();
		}

		public async Task<IEnumerable<InvestmentDetails>> GetFilteredInvestmentDetails(string date, string profile, string user)
		{
			var investmentRecord = GetMongoCollection(Constants.Diary);

			var builder = Builders<BsonDocument>.Filter;
			FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq(Constants.user, user);

			var ifDateEmpty = (string.IsNullOrEmpty(date) || date.Equals(Constants.nullValue) || date.Equals(Constants.undefined));
			var ifProfileEmpty = (string.IsNullOrEmpty(profile) || profile.Equals(Constants.nullValue) || profile.Equals(Constants.undefined));

			if (!ifDateEmpty && !ifProfileEmpty)
			{
				filter &= builder.Eq(Constants.date, date) & builder.Eq(Constants.profile, profile);
			}
			else if (!ifProfileEmpty)
			{
				filter &= builder.Eq(Constants.profile, profile);
			}
			else if (!ifDateEmpty)
			{
				filter &= builder.Eq(Constants.date, date);
			}

			var docs = investmentRecord.Find(filter).ToList();

			return docs.Select(item => new InvestmentDetails
			{
				fundName = (string)item[Constants.fundName],
				date = (string)item[Constants.date],
				denomination = (string)item[Constants.amount],
				profile = (string)item[Constants.profile],
				id = Convert.ToString((ObjectId)item[Constants._id]),
				user = Convert.ToString((string)item[Constants.user])
			})
				.ToList();
		}

		internal async Task<int> UpdateSIPDetails(InvestmentDetails model)
		{
			var filter = Builders<BsonDocument>.Filter.Eq(Constants._id, MongoDB.Bson.ObjectId.Parse(model.id)) &
						 Builders<BsonDocument>.Filter.Eq(Constants.user, model.user);

			var update = Builders<BsonDocument>.Update.Set(Constants.fundName, model.fundName)
					.Set(Constants.date, model.date)
					.Set(Constants.amount, model.denomination)
					.Set(Constants.profile, model.profile);

			await GetMongoCollection(Constants.Diary).UpdateOneAsync(filter, update);
			return 0;
		}

		internal async Task<InvestmentReturnDataForChart> GetInvestmentReturnDataForChart(string user)
		{
			var investmentReturnData = GetCombinedMutualFundReturnDetails(null, user);
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

		internal async Task<InvestmentReturnDataForChart> GetEquityInvestmentReturnDataForChart(string user)
		{
			var docs = GetInvestmentReturnData(Constants.Equity, Constants.ByOldDate, user);

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

		internal async Task<InvestmentReturnDataForChart> GetPFInvestmentReturnDataForChart(string user)
		{
			var docs = GetInvestmentReturnData(Constants.EPFO, Constants.ByOldDate, user);

			var lineChartLabelsList = new List<string>();
			var counter = 0;

			//-------------------------------------------------------------------------------------------------------------------------------
			var epfoPrimaryBalance = new double[docs.Count];
			var epfoSecondaryBalance = new double[docs.Count];
			var ppfBalance = new double[docs.Count];
			var profiles = GetProfileName(user).Result;
			profiles.Sort();
			foreach (var item in docs)
			{
				lineChartLabelsList.AddRange(new string[] { "" });
				if (Convert.ToString(item[Constants.type]).Equals(Constants.EPFO))
				{
					if (Convert.ToString(item[Constants.profile]).Equals(profiles[0]))
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


			var chartData = new List<Returns>();
			var isLabelCounterMoreThanActualData = lineChartLabelsList.Count > (lineChartLabelsList.Count - epfoPrimaryBalance.Length) / 2;
			if (profiles.Count > 1)
			{
				if (isLabelCounterMoreThanActualData)
				{
					var totalRange = (lineChartLabelsList.Count - epfoPrimaryBalance.Length) / 2 + (lineChartLabelsList.Count - epfoPrimaryBalance.Length);
					if (totalRange <= lineChartLabelsList.Count)
					{
						lineChartLabelsList.RemoveRange((lineChartLabelsList.Count - epfoPrimaryBalance.Length) / 2, (lineChartLabelsList.Count - epfoPrimaryBalance.Length));
					}
					else
					{
						lineChartLabelsList.RemoveRange(0, (lineChartLabelsList.Count - epfoPrimaryBalance.Length) / 2);
						//lineChartLabelsList.RemoveRange((lineChartLabelsList.Count - epfoPrimaryBalance.Length) / 2, totalRange-lineChartLabelsList.Count);
					}
				}
				chartData = new List<Returns>()
				{
					new Returns { Label = String.Format(Constants.CurrentValue, profiles[0][0]), Data = epfoSecondaryBalance.Where(x =>  x != 0).ToArray(),  pointRadius=0 },
					new Returns { Label = String.Format(Constants.CurrentValue, profiles[1][0]), Data = epfoPrimaryBalance, pointRadius=0 },
					new Returns { Label = Constants.ppf, Data = ppfBalance.Where(x =>  x != 0).ToArray(), pointRadius=0 }
				};
			}
			else
			{
				if (isLabelCounterMoreThanActualData)
				{
					lineChartLabelsList.RemoveRange(0, (lineChartLabelsList.Count / 2));
				}
				chartData = new List<Returns>
				{
					new Returns { Label = String.Format(Constants.CurrentValue, profiles[0][0]), Data = epfoSecondaryBalance.Where(x =>  x != 0).ToArray(),  pointRadius=0 },
					new Returns { Label = Constants.ppf, Data = ppfBalance.Where(x =>  x != 0).ToArray(), pointRadius=0 }
				};
			}
			return new InvestmentReturnDataForChart { InvestmentReturnChart = chartData, ChartLabels = lineChartLabelsList.ToArray() };
		}

		internal async Task<int> SaveProvidentFundDetails(double primaryBalance, double ppfBalance, string type, string profile, string user)
		{
			var investmentRecord = GetMongoCollection(Constants.EPFO);
			var doc = new BsonDocument
			{
				{Constants.epfoPrimaryBalance, primaryBalance},
				{Constants.ppfBalance, ppfBalance},
				{Constants.type, type},
				{Constants.profile, profile},
				{Constants.createddate, DateTime.Now },
				{Constants.user, user }
			};

			await investmentRecord.InsertOneAsync(doc);
			return 0;
		}

		internal async Task<int> SaveEquityInvestmentReturnDetails(int investedamount, int currentvalue, string user)
		{
			var investmentRecord = GetMongoCollection(Constants.Equity);
			var returns = ((double)(currentvalue - investedamount) / (double)investedamount) * 100;
			var doc = new BsonDocument
			{
				{Constants.investedamount, investedamount},
				{Constants.currentvalue, currentvalue},
				{Constants.returns, Math.Round(returns, 2)},
				{Constants.createddate, DateTime.Now },
				{Constants.user, user }
			};

			await investmentRecord.InsertOneAsync(doc);
			return 0;
		}

		internal async Task<InvestmentReturnDataForChart> GetIndividualInvestmentReturnDataForChart(string user)
		{
			var investmentReturnData = GetInvestmentReturnDetails(user);
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
			var profiles = GetProfileName(user).Result;
			profiles.Sort();
			foreach (var item in investmentReturnData.Result)
			{
				if (item.profile == profiles[0])
				{
					secondaryInvestedAmountData[secondaryCounter] = item.investedamount;
					secondaryCurrentValueData[secondaryCounter] = item.currentvalue;
					secondaryCounter++;
				}
				else
				{
					primaryInvestedAmountData[primaryCounter] = item.investedamount;
					primaryCurrentValueData[primaryCounter] = item.currentvalue;
					primaryCounter++;
				}
			}

			primaryInvestedAmountData = primaryInvestedAmountData.Where(x => x != 0).ToArray();

			if (lineChartLabelsList.Count > (lineChartLabelsList.Count - primaryInvestedAmountData.Length) / 2)
			{
				lineChartLabelsList.RemoveRange((lineChartLabelsList.Count - primaryInvestedAmountData.Length) / 2, (lineChartLabelsList.Count - primaryInvestedAmountData.Length));
			}
			var chartData = new List<Returns>();
			if (profiles.Count > 1)
			{
				chartData = new List<Returns>
				{
					new Returns { Label = String.Format(Constants.InvestedAmount, profiles[1][0]), Data = primaryInvestedAmountData, pointRadius=0 },
					new Returns { Label = String.Format(Constants.CurrentValue, profiles[1][0]), Data = primaryCurrentValueData.Where(x => x != 0).ToArray(), pointRadius=0 },
					new Returns { Label = String.Format(Constants.InvestedAmount, profiles[0][0]), Data = secondaryInvestedAmountData.Where(x => x != 0).ToArray(), pointRadius=0 },
					new Returns { Label = String.Format(Constants.CurrentValue, profiles[0][0]), Data = secondaryCurrentValueData.Where(x => x != 0).ToArray(), pointRadius=0 }
				};
			}
			else
			{
				chartData = new List<Returns>
				{
					new Returns { Label = String.Format(Constants.InvestedAmount, profiles[0][0]), Data = secondaryInvestedAmountData.Where(x => x != 0).ToArray(), pointRadius=0 },
					new Returns { Label = String.Format(Constants.CurrentValue, profiles[0][0]), Data = secondaryCurrentValueData.Where(x => x != 0).ToArray(), pointRadius=0 }
				};
			}

			return new InvestmentReturnDataForChart { InvestmentReturnChart = chartData, ChartLabels = lineChartLabelsList.ToArray() };
		}

		internal async Task<InvestmentReturnDataForChart> GetDebtAndInvestmentForChart(string user)
		{
			var docs = GetInvestmentReturnData(Constants.DebtAndInvestment, Constants.ByOldDate, user);

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
				new Returns { Label = Constants.Debt, Data = debtData,  pointRadius=1 },
				new Returns { Label = Constants.SavingsInvestment, Data = investedAmountData, pointRadius=2 }
			};
			//CollectionBackup();
			//ExportjsonToMongo();
			return new InvestmentReturnDataForChart { InvestmentReturnChart = chartData, ChartLabels = lineChartLabelsList.ToArray() };
		}

		internal async Task<DebtDataForCharts> GetDebtDataForChart(string user)
		{
			var docs = GetInvestmentReturnData(Constants.Debt, Constants.ByOldDate, user);

			var lineChartLabelsList = new List<string>();

			var debtData = new Dictionary<string, List<int>>();
			var totalEntries = 0;
			foreach (var item in docs)
			{
				if (!debtData.ContainsKey((string)item[Constants.accountname]))
				{
					debtData.Add((string)item[Constants.accountname], new List<int> { (int)item[Constants.currentBalance] });
					
				}
				else
				{
					var data = debtData[(string)item[Constants.accountname]];
					data.Add((int)item[Constants.currentBalance]);
					debtData.Remove((string)item[Constants.accountname]);
					debtData.Add((string)item[Constants.accountname], data);
					
				}
			}

			foreach (var item in debtData)
			{
				if(item.Value.Count> totalEntries)
				{
					totalEntries = item.Value.Count;
				}
			}

			var chartData = new List<Debts>();
			var debtChartData= new Dictionary<string, List<int>>();
			foreach (var item in debtData)
			{
				if (item.Value.Count < totalEntries)
				{
					debtChartData.Add(item.Key, item.Value);
					if (GetDebtAccountName(user).Result.Contains(item.Key))
					{
						var zeroEntryToAdd = totalEntries - item.Value.Count;
						var tempData = item.Value;
						var data1 = new List<int>();
						var i = 0;
						while (i < zeroEntryToAdd)
						{
							data1.Add(0);
							i++;
						}
						data1.AddRange(tempData);
						debtChartData[item.Key] = data1;
						chartData.Add(new Debts { Label = item.Key, Data = data1.ToArray(), pointRadius = 1 });
					}
					else 
					{
						
						var data = item.Value;
						var i = 0;
						while (i < (totalEntries - item.Value.Count))
						{
							data.Add(0);
							debtChartData[item.Key] = data;
						}
						chartData.Add(new Debts { Label = item.Key, Data = item.Value.ToArray(), pointRadius = 1 });
					}
				}
				else
				{
					debtChartData.Add(item.Key, item.Value);
					chartData.Add(new Debts { Label = item.Key, Data = item.Value.ToArray(), pointRadius = 1 });

				}
				
			}
			for(int i=0; i< totalEntries; i++)
			{
				lineChartLabelsList.AddRange(new string[] { "" });
			}
			//CollectionBackup();
			//ExportjsonToMongo();
			return new DebtDataForCharts { DebtDataForChart = chartData, ChartLabels = lineChartLabelsList.ToArray() };
		}

		internal async Task<DashBoardChangeData> GetDashboardChangeData(string user)
		{
			var docs = GetTopInvestmentReturnData(Constants.DebtAndInvestment, Constants.ByLatestDate, 2, user);
			if (docs.Count < 2)
			{
				return new DashBoardChangeData();
			}
			var changeInAsset = (double)docs.First()[Constants.totalinvestments] - (double)docs.Last()[Constants.totalinvestments];
			var changeInDebt = (double)docs.First()[Constants.totaldebt] - (double)docs.Last()[Constants.totaldebt];

			var obj = new DashBoardChangeData
			{
				assetincreased = changeInAsset > 0,
				assetchange = changeInAsset > 0 ? changeInAsset : changeInAsset * -1,
				assetchangepercentage = Math.Round((changeInAsset / (double)docs.Last()[Constants.totalinvestments]) * 100, 2),
				debtincreased = changeInDebt > 0,
				debtchange = changeInDebt > 0 ? changeInDebt : changeInDebt * -1,
				debtchangepercentage = Math.Round(((changeInDebt > 0 ? changeInDebt : changeInDebt * -1) / (double)docs.Last()[Constants.totaldebt]) * 100, 2)
			};

			return obj;
		}
		internal async Task<int> DeleteSIPDetails(string id, string user)
		{
			var deleteFilter = Builders<BsonDocument>.Filter.Eq(Constants._id, MongoDB.Bson.ObjectId.Parse(id)) &
							   Builders<BsonDocument>.Filter.Eq(Constants.user, user);
			var investmentRecord = GetMongoCollection(Constants.Diary);
			investmentRecord.DeleteOne(deleteFilter);
			return 0;
		}

		internal async Task<int> SaveReturns(string profile, int investedamount, int currentvalue, string user)
		{
			var userFilter = Builders<BsonDocument>.Filter.Eq(Constants.user, user) &
					 Builders<BsonDocument>.Filter.Eq(Constants.profile, profile);
			var mutualFundRecord = GetMongoCollection(Constants.Sum);
			var recordExist = mutualFundRecord.Find(userFilter).ToList();
			if (recordExist.Count > 0)
			{
				var latestDate = (DateTime)mutualFundRecord.Find(userFilter)
					.Sort(Builders<BsonDocument>.Sort.Descending(Constants.createddate)
						.Descending(Constants.createddate))
					.ToList().FirstOrDefault()?[Constants.createddate];

				var filter = Builders<BsonDocument>.Filter.Eq(Constants.createddate, latestDate) &
					 Builders<BsonDocument>.Filter.Eq(Constants.user, user) &
					 Builders<BsonDocument>.Filter.Eq(Constants.profile, profile);

				var docs = filter == null
					? mutualFundRecord.Find(new BsonDocument()).ToList()
					: mutualFundRecord.Find(filter).ToList();
				if (docs.Count > 0)
				{
					var lastEntry = (DateTime)docs[0][Constants.createddate];
					if (lastEntry.Date == DateTime.Now.Date)
						return 0;
				}
			}
			var investmentRecord = GetMongoCollection(Constants.Sum);
			var returns = ((double)(currentvalue - investedamount) / (double)investedamount) * 100;
			var doc = new BsonDocument
			{
				{Constants.profile, profile },
				{Constants.investedamount, investedamount},
				{Constants.currentvalue, currentvalue},
				{Constants.returns, Math.Round(returns, 2)},
				{Constants.createddate, DateTime.Now },
				{Constants.user, user }
			};

			await investmentRecord.InsertOneAsync(doc);
			return 1;
		}

		internal async Task<IEnumerable<InvestmentReturns>> GetInvestmentReturnDetails(string user)
		{
			var docs = GetInvestmentReturnData(Constants.Sum, Constants.ByOldDate, user);

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
		private List<BsonDocument> GetInvestmentReturnData(string collection, string order, string user)
		{
			var investmentRecord = GetMongoCollection(collection);
			List<BsonDocument> data;
			var filter = Builders<BsonDocument>.Filter.Eq(Constants.user, user);

			if (order.Equals(Constants.ByLatestDate))
			{
				data = investmentRecord.Find(filter)
					.Sort(Builders<BsonDocument>.Sort.Descending(Constants.createddate)
						.Descending(Constants.createddate))
					.ToList();
			}
			else
			{
				data = investmentRecord.Find(filter)
					.Sort(Builders<BsonDocument>.Sort.Ascending(Constants.createddate)
						.Ascending(Constants.createddate))
					.ToList();
			}

			return data;
		}
		private List<BsonDocument> GetTopInvestmentReturnData(string collection, string order, int limit, string user)
		{
			var filter = Builders<BsonDocument>.Filter.Eq(Constants.user, user);
			var investmentRecord = GetMongoCollection(collection);
			List<BsonDocument> data;
			if (order.Equals(Constants.ByLatestDate))
			{
				data = investmentRecord.Find(filter)
					.Sort(Builders<BsonDocument>.Sort.Descending(Constants.createddate)
						.Descending(Constants.createddate)).Limit(limit)
					.ToList();
			}
			else
			{
				data = investmentRecord.Find(filter)
					.Sort(Builders<BsonDocument>.Sort.Ascending(Constants.createddate)
						.Ascending(Constants.createddate)).Limit(limit)
					.ToList();
			}
			return data;
		}
		internal async Task<IEnumerable<InvestmentReturns>> GetCombinedMutualFundReturnDetails(List<BsonDocument> docs, string user)
		{
			docs ??= GetInvestmentReturnData(Constants.Sum, Constants.ByOldDate, user);
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

		internal async Task<IEnumerable<InvestmentDetails>> GetSIPDetailsByFund(string user)
		{
			var filter = Builders<BsonDocument>.Filter.Eq(Constants.user, user);
			var sipDetailsDocument = GetMongoCollection(Constants.Diary);
			var docs = sipDetailsDocument.Find(filter).ToList();
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

		internal async Task<IEnumerable<InvestmentDetails>> GetSIPDetailsByDate(string user)
		{
			var filter = Builders<BsonDocument>.Filter.Eq(Constants.user, user);
			var sipDetailsDocument = GetMongoCollection(Constants.Diary);
			var docs = sipDetailsDocument.Find(filter).ToList();
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

		internal async Task<IEnumerable<DashboardAssetDetails>> GetAssetsDashBoardData(string user)
		{
			double epfoData = 0;
			double ppfData = 0;
			double epfoDataPrevious = 0;
			double ppfDataPrevious = 0;
			var docs = GetTopInvestmentReturnData(Constants.Sum, Constants.ByLatestDate, 4, user);
			var mutualFundDashBoardData = GetCombinedMutualFundReturnDetails(docs, user).Result.ToList();

			var equityDashBoardData =
				GetTopInvestmentReturnData(Constants.Equity, Constants.ByLatestDate, 2, user).ToList();

			var pfDocuments = GetTopInvestmentReturnData(Constants.EPFO, Constants.ByLatestDate, 6, user);

			//List<BsonDocument> pfDataForIteration = null;
			if (pfDocuments.Count > 2)
			{
				//pfDataForIteration = pfDocuments.GetRange(0, 3);
				foreach (var item in pfDocuments.GetRange(0, 3))
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
				if (pfDocuments.Count > 5)
				{
					foreach (var item in pfDocuments.GetRange(3, 3))
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
				}
			}
			else
			{
				foreach (var item in pfDocuments)
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

		internal async Task<IEnumerable<DebtDetails>> GetDebtsDashBoardData(string user)
		{
			var debtRecords = GetMongoCollection(Constants.Debt);
			var userFilter = Builders<BsonDocument>.Filter.Eq(Constants.user, user);
			var recordExist = debtRecords.Find(userFilter).ToList();
			if (recordExist.Count <= 0) return null;

			var latestDate = (DateTime)debtRecords.Find(userFilter)
				.Sort(Builders<BsonDocument>.Sort.Descending(Constants.createddate)
					.Descending(Constants.createddate))
				.ToList().FirstOrDefault()?[Constants.createddate];

			var start = new DateTime(latestDate.Year, latestDate.Month, latestDate.Day - 1);
			var end = new DateTime(latestDate.Year, latestDate.Month, latestDate.Day + 1);

			var filter = Builders<BsonDocument>.Filter.Gte(Constants.createddate, start) &
						 Builders<BsonDocument>.Filter.Lte(Constants.createddate, end) &
						 Builders<BsonDocument>.Filter.Gt(Constants.currentBalance, 0) &
						 Builders<BsonDocument>.Filter.Eq(Constants.user, user);

			var docs = debtRecords.Find(filter).ToList();
			return docs.Select(item => new DebtDetails
			{
				accountname = (string)item[Constants.accountname],
				currentbalance = (int)item[Constants.currentBalance],
				createddate = (DateTime)item[Constants.createddate],
				id = Convert.ToString((ObjectId)item[Constants._id])
			}).ToList();
		}

		internal async Task<int> RefreshDebtAndInvestmentDataForChart(string user)
		{
			//var userFilter = Builders<BsonDocument>.Filter.Eq(Constants.user, user);
			var debtInvestmentRecord = GetMongoCollection(Constants.DebtAndInvestment);
			//var recordExist = debtInvestmentRecord.Find(userFilter).ToList();
			//if (recordExist.Count > 0)
			//{
			//	var latestDate = (DateTime)debtInvestmentRecord.Find(userFilter)
			//		.Sort(Builders<BsonDocument>.Sort.Descending(Constants.createddate)
			//			.Descending(Constants.createddate))
			//		.ToList().FirstOrDefault()?[Constants.createddate];

			//	var filter = Builders<BsonDocument>.Filter.Eq(Constants.createddate, latestDate)&
			//		 Builders<BsonDocument>.Filter.Eq(Constants.user, user); 

			//	var docs = filter == null
			//		? debtInvestmentRecord.Find(new BsonDocument()).ToList()
			//		: debtInvestmentRecord.Find(filter).ToList();
			//	if (docs.Count > 0)
			//	{
			//		var lastEntry = (DateTime)docs[0][Constants.createddate];
			//		if (lastEntry.Month == DateTime.Now.Month)
			//			return 0;
			//	}
			//}

			var assetData = GetAssetsDashBoardData(user).Result;
			var debtData = GetDebtsDashBoardData(user).Result;
			double cumulativeDebt = debtData.Sum(item => item.currentbalance);
			double cumulativeAsset = assetData.Sum(item => item.currentvalue);
			var doc = new BsonDocument
			{
				{Constants.totaldebt, cumulativeDebt},
				{Constants.totalinvestments, cumulativeAsset},
				{Constants.user, user },
				{Constants.createddate, DateTime.Now}
			};

			await debtInvestmentRecord.InsertOneAsync(doc);
			return 1;
		}

		internal async Task<List<string>> GetDebtAccountName(string user)
		{
			var filter = Builders<BsonDocument>.Filter.Eq(Constants.user, user);
			var debtAccounts = GetMongoCollection(Constants.DebtAccounts).Find(filter).ToList();
			return debtAccounts.Select(item => (string)item[Constants.name]).ToList();
		}

		internal async Task<List<string>> GetProfileName(string user)
		{
			var filter = Builders<BsonDocument>.Filter.Eq(Constants.user, user);
			var debtAccounts = GetMongoCollection(Constants.profile).Find(filter).ToList();
			return debtAccounts.Select(item => (string)item[Constants.name]).ToList();
		}

		internal async Task<List<string>> GetInvestmentAccountName(string user)
		{
			var filter = Builders<BsonDocument>.Filter.Eq(Constants.user, user);
			var debtAccounts = GetMongoCollection(Constants.InvestmentAccounts).Find(filter).ToList();
			return debtAccounts.Select(item => (string)item[Constants.name]).ToList();
		}

		internal async Task<Configuration> GetConfigurationSettings(string user)
		{
			var filter = Builders<BsonDocument>.Filter.Eq(Constants.user, user);
			var debtAccounts = GetMongoCollection(Constants.DebtAccounts).Find(filter).ToList();
			var profiles = GetMongoCollection(Constants.profile).Find(filter).ToList();
			return new Configuration
			{
				profile = profiles.Select(item => (string)item[Constants.name]).ToList(),
				debtaccount = debtAccounts.Select(item => (string)item[Constants.name]).ToList()
			};
		}

		internal async Task<int> SaveProfileSettings(string user, string profiles)
		{
			var userFilter = Builders<BsonDocument>.Filter.Eq(Constants.user, user);
			var profileRecord = GetMongoCollection(Constants.profile);
			var recordExist = profileRecord.Find(userFilter).ToList();
			if (recordExist.Count > 1)
			{
				return 0;
			}
			var profileData = profiles.Split(',');

			foreach (var item in profileData)
			{
				var doc = new BsonDocument
			{
				{Constants.name, item},
				{Constants.user, user }
			};

				await profileRecord.InsertOneAsync(doc);
			}
			return 1;
		}

		internal async Task<int> SaveDebtAccountSettings(string user, string debtAccount)
		{
			var debtAccountData = debtAccount.Split(',');
			var debtAccountDataRecord = GetMongoCollection(Constants.DebtAccounts);
			foreach (var item in debtAccountData)
			{
				var doc = new BsonDocument
			{
				{Constants.name, item},
				{Constants.user, user }
			};

				await debtAccountDataRecord.InsertOneAsync(doc);
			}
			return 1;
		}

		internal async Task<int> SaveInvestmentAccountSettings(string user, string investmentAccount)
		{
			var investmentAccountData = investmentAccount.Split(',');
			var investmentAccountDataRecord = GetMongoCollection(Constants.InvestmentAccounts);
			foreach (var item in investmentAccountData)
			{
				var doc = new BsonDocument
			{
				{Constants.name, item},
				{Constants.user, user }
			};

				await investmentAccountDataRecord.InsertOneAsync(doc);
			}
			return 1;
		}

		internal async Task<int> DeleteConfigSettings(string id, string user)
		{
			var deleteFilter = Builders<BsonDocument>.Filter.Eq(Constants._id, MongoDB.Bson.ObjectId.Parse(id)) &
							   Builders<BsonDocument>.Filter.Eq(Constants.user, user);
			var investmentRecord = GetMongoCollection(Constants.Diary);
			investmentRecord.DeleteOne(deleteFilter);
			return 0;
		}
		private async void CollectionBackup()
		{
			var outputFileName = Constants.outputPath; // initialize to the output file
			var db = _dbClientCloud.GetDatabase(Constants.Financials);
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

		private async void ExportjsonToMongo()
		{
			string[] filePaths = { "Sum", "Debt", "Diary", "DebtAccounts", "DebtAndInvestment", "EPFO", "Equity", "InvestmentAccounts" }; ;

			foreach (var item in filePaths)
			{

				IMongoCollection<BsonDocument> collection = GetMongoCollection(item); // initialize to the collection to write to.
				var inputFile = Constants.outputPath + item + ".json";
				using (var streamReader = new StreamReader(inputFile))
				{
					string line;
					while ((line = await streamReader.ReadLineAsync()) != null)
					{
						using (var jsonReader = new JsonReader(line))
						{
							var context = BsonDeserializationContext.CreateRoot(jsonReader);
							var document = collection.DocumentSerializer.Deserialize(context);
							await collection.InsertOneAsync(document);
						}
					}
				}
			}

		}
	}
}
