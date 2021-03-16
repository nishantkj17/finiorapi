using FinancialDiaryWeb.Manager;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FinancialDiaryApi.Manager;
using FinancialDiaryWeb.Model;

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
			FinancialMongoDbManager obj = new FinancialMongoDbManager();
			await obj.AddInvestments(model.fundName, model.date, model.denomination, model.profile);
			return Ok();
		}

		[HttpPost]
		[Route("adddebt")]
		public async Task<ActionResult> AddDebt([FromForm] DebtDetails model)
		{
			FinancialMongoDbManager obj = new FinancialMongoDbManager();
			await obj.AddDebt(model.accountname, model.currentbalance);
			return Ok();
		}

		[HttpPost]
		[Route("savereturns")]
		public async Task<ActionResult> SaveReturns([FromForm] InvestmentReturns model)
		{
			FinancialMongoDbManager obj = new FinancialMongoDbManager();
			await obj.SaveReturns(model.profile, model.investedamount, model.currentvalue);
			return Ok();
		}


		[HttpGet]
		[Route("getreturns")]
		public async Task<ActionResult> GetInvestmentReturnDetails()
		{
			FinancialMongoDbManager obj = new FinancialMongoDbManager();
			return Ok(await obj.GetInvestmentReturnDetails());
		}


		[HttpGet]
		[Route("getcombinedreturns")]
		public async Task<ActionResult> GetCombinedInvestmentReturnDetails()
		{
			FinancialMongoDbManager obj = new FinancialMongoDbManager();
			return Ok(await obj.GetCombinedMutualFundReturnDetails());
		}

		[HttpGet]
		[Route("getinvestmentdetails")]
		public async Task<ActionResult> GetInvestmentDetails()
		{
			FinancialMongoDbManager obj = new FinancialMongoDbManager();
			return Ok(await obj.GetInvestmentDetails());
		}

		[HttpGet]
		[Route("getfilteredinvestmentdetails")]
		public async Task<ActionResult> GetFilteredInvestmentDetails(string date, string profile)
		{
			FinancialMongoDbManager obj = new FinancialMongoDbManager();
			return Ok(await obj.GetFilteredInvestmentDetails(date, profile));
		}

		[HttpGet]
		[Route("gettotalsipdetailsbydate")]
		public async Task<ActionResult> GetTotalSipDetailsByDate()
		{
			FinancialMongoDbManager obj = new FinancialMongoDbManager();
			return Ok(await obj.GetSIPDetailsByDate());
		}

		[HttpGet]
		[Route("gettotalsipdetailsbyfund")]
		public async Task<ActionResult> GetTotalSipDetailsByFund()
		{
			FinancialMongoDbManager obj = new FinancialMongoDbManager();
			return Ok(await obj.GetSIPDetailsByFund());
		}

		[HttpPost]
		[Route("updatesipdetails")]
		public async Task<ActionResult> UpdateSIPDetails([FromForm] InvestmentDetails model)
		{
			FinancialMongoDbManager obj = new FinancialMongoDbManager();
			return Ok(await obj.UpdateSIPDetails(model));
		}
		[HttpGet]
		[Route("deletesipdetails")]
		public async Task<ActionResult> DeleteSIPDetails(string id)
		{
			FinancialMongoDbManager obj = new FinancialMongoDbManager();
			return Ok(await obj.DeleteSIPDetails(id));
		}

		[HttpGet]
		[Route("getinvestmentdataforchart")]
		public async Task<ActionResult> GetInvestmentDataforChart()
		{
			FinancialMongoDbManager obj = new FinancialMongoDbManager();
			return Ok(await obj.GetInvestmentReturnDataForChart());
		}

		[HttpGet]
		[Route("getindividualinvestmentdataforchart")]
		public async Task<ActionResult> GetIndividualInvestmentDataforChart()
		{
			FinancialMongoDbManager obj = new FinancialMongoDbManager();
			return Ok(await obj.GetIndividualInvestmentReturnDataForChart());
		}

		[HttpGet]
		[Route("getequityinvestmentreturndata")]
		public async Task<ActionResult> GetEquityInvestmentDataforChart()
		{
			FinancialMongoDbManager obj = new FinancialMongoDbManager();
			return Ok(await obj.GetEquityInvestmentReturnDataForChart());
		}

		[HttpPost]
		[Route("saveequityinvestmentreturndata")]
		public async Task<ActionResult> SaveEquityInvestmentDataforChart([FromForm] InvestmentReturns model)
		{
			FinancialMongoDbManager obj = new FinancialMongoDbManager();
			return Ok(await obj.SaveEquityInvestmentReturnDetails(model.investedamount, model.currentvalue));
		}
		[HttpPost]
		[Route("saveprovidentfunddetails")]
		public async Task<ActionResult> SaveProvidentFundDetails([FromForm] InvestmentReturns model)
		{
			FinancialMongoDbManager obj = new FinancialMongoDbManager();
			return Ok(await obj.SaveProvidentFundDetails(model.investedamount, model.currentvalue, model.type, model.profile));
		}

		[HttpGet]
		[Route("getpfreturndataforchart")]
		public async Task<ActionResult> GetPFInvestmentDataforChart()
		{
			FinancialMongoDbManager obj = new FinancialMongoDbManager();
			return Ok(await obj.GetPFInvestmentReturnDataForChart());
		}

		[HttpGet]
		[Route("getdashboarddata")]
		public async Task<ActionResult> GetDashBoardData()
		{
			FinancialMongoDbManager obj = new FinancialMongoDbManager();
			return Ok(await obj.GetDashBoardData());
		}
	}

}
