using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace FinancialDiaryWeb.Model
{
	public class GenericModel
	{
	}
	public class InvestmentDetails
	{
		public string fundName { get; set; }
		public string date { get; set; }
		public string denomination { get; set; }
		public string profile { get; set; }
		public string id { get;  set; }
		public string user { get; set; }
	}

	public class InvestmentReturns
	{
		public string profile { get; set; }
		public string createddate { get; set; }
		public int investedamount { get; set; }
		public int currentvalue { get; set; }
		public double returns { get; set; }
		public string id { get; set; }
		public string type { get; set; }
		public string user { get; set; }
	}

	public class InvestmentDetailsByDate
	{
		public string date { get; set; }
		public string denomination { get; set; }
		public string user { get; set; }
	}

	public class InvestmentReturnDataForChart
	{
		public List<Returns> InvestmentReturnChart { get; set; }
		public string[] ChartLabels { get; set; }
		public string user { get; set; }
	}

	public class DebtDataForCharts
	{
		public List<Debts> DebtDataForChart { get; set; }
		public string[] ChartLabels { get; set; }
		public string user { get; set; }
	}
	public class Returns
	{
		public double[] Data { get; set; }
		public string Label { get; set; }

		public int pointRadius { get; set; }
		public string user { get; set; }
	}
	public class Debts
	{
		public int[] Data { get; set; }
		public string Label { get; set; }

		public int pointRadius { get; set; }
		public string user { get; set; }
	}
	public class DashboardData
	{
		public double epfo { get; set; }
		public double mutualfund { get; set; }
		public double equity { get; set; }
		public string date { get; set; }
		public double ppf { get; set; }
		public string cardclass { get; set; }
		public string user { get; set; }
	}

	public class DashboardAssetDetails
	{
		public string cardclass { get; set; }
		public string investmenttype { get; set; }
		public double currentvalue { get; set; }
		public bool increased { get; set; }
		public string user { get; set; }
	}
	public class DebtDetails
	{
		public string accountname { get; set; }
		public DateTime createddate { get; set; }
		public int currentbalance { get; set; }
		public string id { get; set; }
		public string user { get; set; }
	}

	public class DebtAndInvestmentDetails
	{
		public string totaldebt { get; set; }
		public string totalinvestments { get; set; }
		public string id { get; set; }
		public string user { get; set; }
	}
	public class ProvidentFundDetails
	{
		public string type { get; set; }
		public string profile { get; set; }
		public double epfoPrimaryBalance { get; set; }
		public double ppfBalance { get; set; }
		public string id { get; set; }
		public string user { get; set; }
	}

	public class Configuration
	{
		public List<string> profile { get; set; }
		public List<string> debtaccount { get; set; }
	}
	public class ConfigurationModel
	{
		public string profiles { get; set; }
		public string debtaccount { get; set; }
		public string investmentaccount { get; set; }
		public string user { get; set; }
	}
	public class DashBoardChangeData
	{
		public double assetchange { get; set; }
		public bool assetincreased { get; set; }
		public double assetchangepercentage { get; set; }
		public double debtchange { get; set; }
		public bool debtincreased { get; set; }
		public double debtchangepercentage { get; set; }
	}
}
