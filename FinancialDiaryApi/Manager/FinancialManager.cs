using FinancialDiaryWeb.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FinancialDiaryWeb.Manager
{
	public class FinancialManager
	{

		public async Task<int> AddInvestments(string fundName, string date, string amount, string profile)
		{
			return 0;
		}

		public async Task<IEnumerable<InvestmentDetails>> GetInvestmentDetails()
		{
			return null;
		}

		public async Task<IEnumerable<InvestmentDetails>> GetFilteredInvestmentDetails(string date, string profile)
		{
			return null;
		}

		internal async Task<int> SaveReturns(string profile, int investedamount, int currentvalue)
		{ return 0; }

		internal async Task<IEnumerable<InvestmentReturns>> GetInvestmentReturnDetails()
		{ return null; }

		internal async Task<IEnumerable<InvestmentReturns>> GetCombinedInvestmentReturnDetails()
		{ return null; }

		internal async Task<IEnumerable<InvestmentDetails>> GetTotalInvestmentDetailsByFund()
		{ return null; }

		internal async Task<IEnumerable<InvestmentDetails>> GetTotalInvestmentDetailsByDate()
		{ return null; }


	}
}
