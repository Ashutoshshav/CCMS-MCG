using System.Linq;
using CCMS.Models;
using CCMS.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using CCMS.Models;
using Microsoft.AspNetCore.Authorization;


namespace CCMS.Controllers
{
    public class Chart : Controller
    {
        private readonly ApplicationDbContext context;
        private readonly IConfiguration _configuration;

        public Chart(ApplicationDbContext context, IConfiguration configuration)
        {
            this.context = context;
            _configuration = configuration;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {

            var secondChartData = await context.IMEI_Master
                .GroupBy(x => 1) // Group all rows into a single set for chart 1
                .Select(group => new ChartModel
                {
                    ON = group.Count(x => x.Status == "1"),
                    OFF = group.Count(x => x.Status == "2"),
                    NC = group.Count(x => x.Status == "3"),
                })
                .ToListAsync();

            Console.WriteLine($"secondChartData: {secondChartData}");
            // Query for second chart data (e.g., Status counts for another category)
            var firstChartData = await context.IMEI_Master
                .GroupBy(x => 1) // Group all rows into a single set
                .Select(group => new ChartModel
                {
                    ON = group.Count(x => x.Status == "1"),
                    OFF = group.Count(x => x.Status == "2"),
                    NC = group.Count(x => x.Status == "3"),
                    TotalNoOfStreetlight = group.Sum(x => string.IsNullOrEmpty(x.NoOfStreetlight) ? 0 : Convert.ToInt32(x.NoOfStreetlight)) // Sum of NoOfStreetlight
                })
                .ToListAsync();


            //var firstChartData = new List<ChartModel>
            //{
            //    new ChartModel { ON = 4856, OFF = 841, NC = 26546 },
            //};

            //var secondChartData = new List<ChartModel>
            //{
            //    new ChartModel { ON = 546, OFF = 165, NC = 164 },
            //};

            var viewModel = new ChartViewModel
            {
                FirstChart = firstChartData,
                SecondChart = secondChartData
            };

            return View(viewModel);
        }
    }
}
