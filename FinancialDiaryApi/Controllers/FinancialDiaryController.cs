using FinancialDiaryWeb.Manager;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FinancialDiaryApi.Manager;
using FinancialDiaryWeb.Model;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;

namespace FinancialDiaryWeb.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class FinancialDiaryController : ControllerBase
	{


		private readonly ILogger<FinancialDiaryController> _logger;

		public FinancialDiaryController(ILogger<FinancialDiaryController> logger)
		{
			_logger = logger;
		}



		[HttpPost]
		[Route("addinvestment")]
		public async Task<ActionResult> AddInvestment([FromForm] InvestmentDetails model)
		{
			var obj = new FinancialMongoDbManager();
			await obj.AddInvestments(model.fundName, model.date, model.denomination, model.profile, model.user);
			return Ok();
		}

		[HttpPost]
		[Route("adddebt")]
		public async Task<ActionResult> AddDebt([FromForm] DebtDetails model)
		{
			var obj = new FinancialMongoDbManager();
			await obj.AddDebt(model.accountname, model.currentbalance, model.user);
			return Ok();
		}

		[HttpPost]
		[Route("savereturns")]
		public async Task<ActionResult> SaveReturns([FromForm] InvestmentReturns model)
		{
			var obj = new FinancialMongoDbManager();
			await obj.SaveReturns(model.profile, model.investedamount, model.currentvalue, model.user);
			return Ok();
		}


		[HttpGet]
		[Route("getreturns")]
		public async Task<ActionResult> GetInvestmentReturnDetails(string user)
		{
			var obj = new FinancialMongoDbManager();
			return Ok(await obj.GetInvestmentReturnDetails(user));
		}


		[HttpGet]
		[Route("getcombinedreturns")]
		public async Task<ActionResult> GetCombinedInvestmentReturnDetails(string user)
		{
			var obj = new FinancialMongoDbManager();
			return Ok(await obj.GetCombinedMutualFundReturnDetails(null, user));
		}

		[HttpGet]
		[Route("getinvestmentdetails")]
		public async Task<ActionResult> GetInvestmentDetails(string user)
		{
			var obj = new FinancialMongoDbManager();
			return Ok(await obj.GetInvestmentDetails(user));
		}

		[HttpGet]
		[Route("getfilteredinvestmentdetails")]
		public async Task<ActionResult> GetFilteredInvestmentDetails(string date, string profile, string user)
		{
			var obj = new FinancialMongoDbManager();
			return Ok(await obj.GetFilteredInvestmentDetails(date, profile, user));
		}

		[HttpGet]
		[Route("gettotalsipdetailsbydate")]
		public async Task<ActionResult> GetTotalSipDetailsByDate(string user)
		{
			var obj = new FinancialMongoDbManager();
			return Ok(await obj.GetSIPDetailsByDate(user));
		}

		[HttpGet]
		[Route("gettotalsipdetailsbyfund")]
		public async Task<ActionResult> GetTotalSipDetailsByFund(string user)
		{
			var obj = new FinancialMongoDbManager();
			return Ok(await obj.GetSIPDetailsByFund(user));
		}

		[HttpPost]
		[Route("updatesipdetails")]
		public async Task<ActionResult> UpdateSIPDetails([FromForm] InvestmentDetails model)
		{
			var obj = new FinancialMongoDbManager();
			return Ok(await obj.UpdateSIPDetails(model));
		}
		[HttpGet]
		[Route("deletesipdetails")]
		public async Task<ActionResult> DeleteSIPDetails(string id, string user)
		{
			var obj = new FinancialMongoDbManager();
			return Ok(await obj.DeleteSIPDetails(id, user));
		}

		[HttpGet]
		[Route("getinvestmentdataforchart")]
		public async Task<ActionResult> GetInvestmentDataforChart(string user)
		{
			var obj = new FinancialMongoDbManager();
			return Ok(await obj.GetInvestmentReturnDataForChart(user));
		}

		[HttpGet]
		[Route("getindividualinvestmentdataforchart")]
		public async Task<ActionResult> GetIndividualInvestmentDataforChart(string user)
		{
			var obj = new FinancialMongoDbManager();
			return Ok(await obj.GetIndividualInvestmentReturnDataForChart(user));
		}

		[HttpGet]
		[Route("getequityinvestmentreturndata")]
		public async Task<ActionResult> GetEquityInvestmentDataforChart(string user)
		{
			var obj = new FinancialMongoDbManager();
			return Ok(await obj.GetEquityInvestmentReturnDataForChart(user));
		}

		[HttpPost]
		[Route("saveequityinvestmentreturndata")]
		public async Task<ActionResult> SaveEquityInvestmentDataforChart([FromForm] InvestmentReturns model)
		{
			var obj = new FinancialMongoDbManager();
			return Ok(await obj.SaveEquityInvestmentReturnDetails(model.investedamount, model.currentvalue, model.user));
		}
		[HttpPost]
		[Route("saveprovidentfunddetails")]
		public async Task<ActionResult> SaveProvidentFundDetails([FromForm] ProvidentFundDetails model)
		{
			var obj = new FinancialMongoDbManager();
			return Ok(await obj.SaveProvidentFundDetails(model.epfoPrimaryBalance,  model.ppfBalance, model.type, model.profile, model.user));
		}

		[HttpGet]
		[Route("getpfreturndataforchart")]
		public async Task<ActionResult> GetPFInvestmentDataforChart(string user)
		{
			var obj = new FinancialMongoDbManager();
			return Ok(await obj.GetPFInvestmentReturnDataForChart(user));
		}

		[HttpGet]
		[Route("getassetsdashboarddata")]
		public async Task<ActionResult> GetAssetsDashBoardData(string user)
		{
			var obj = new FinancialMongoDbManager();
			return Ok(await obj.GetAssetsDashBoardData(user));
		}

		[HttpGet]
		[Route("getdebtaccountname")]
		public async Task<ActionResult> GetDebtAccountName(string user)
		{
			var obj = new FinancialMongoDbManager();
			return Ok(await obj.GetDebtAccountName(user));
		}

		[HttpGet]
		[Route("getinvestmentaccountname")]
		public async Task<ActionResult> GetInvestmentAccountName(string user)
		{
			var obj = new FinancialMongoDbManager();
			return Ok(await obj.GetInvestmentAccountName(user));
		}

		[HttpGet]
		[Route("getdebtsdashboarddata")]
		public async Task<ActionResult> GetDebtsDashBoardData(string user)
		{
			var obj = new FinancialMongoDbManager();
			return Ok(await obj.GetDebtsDashBoardData(user));
		}

		[HttpGet]
		[Route("refreshdebtinvestmentforchart")]
		public async Task<ActionResult> RefreshDebtInvestmentForChart(string user)
		{
			var obj = new FinancialMongoDbManager();
			return Ok(await obj.RefreshDebtAndInvestmentDataForChart(user));
		}

		[HttpGet]
		[Route("getdebtinvestmentforchart")]
		public async Task<ActionResult> GetDebtInvestmentDataforChart(string user)
		{
			var obj = new FinancialMongoDbManager();
			return Ok(await obj.GetDebtAndInvestmentForChart(user));
		}

		[HttpGet]
		[Route("getconfigurationsettings")]
		public async Task<ActionResult> GetConfigurationSettings(string user)
		{
			var obj = new FinancialMongoDbManager();
			return Ok(await obj.GetConfigurationSettings(user));
		}

		[HttpPost]
		[Route("saveprofilessettings")]
		public async Task<ActionResult> SaveProfileSettings([FromForm] ConfigurationModel model)
		{
			var obj = new FinancialMongoDbManager();
			return Ok(await obj.SaveProfileSettings(model.user, model.profiles));
		}

		[HttpPost]
		[Route("savedebtaccountsettings")]
		public async Task<ActionResult> SaveDebtAccountSettings([FromForm] ConfigurationModel model)
		{
			var obj = new FinancialMongoDbManager();
			return Ok(await obj.SaveDebtAccountSettings(model.user, model.debtaccount));
		}
		[HttpPost]
		[Route("saveinvestmentaccountsettings")]
		public async Task<ActionResult> SaveInvestmentAccountSettings([FromForm] ConfigurationModel model)
		{
			var obj = new FinancialMongoDbManager();
			return Ok(await obj.SaveInvestmentAccountSettings(model.user, model.investmentaccount));
		}
	}

}
