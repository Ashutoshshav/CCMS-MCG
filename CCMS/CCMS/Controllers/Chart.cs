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
using DocumentFormat.OpenXml.InkML;


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
            //var firstChartData = await (from imei in context.IMEI_Master
            //                            join vis in context.VisLive on imei.IMEI_no equals vis.IMEI_no
            //                            group new { imei, vis } by 1 into grouped
            //                            select new ChartModel
            //                            {
            //                                ON = grouped.Where(x => x.vis.P10 == 1)
            //                                             .Sum(x => string.IsNullOrEmpty(x.imei.NoOfStreetlight)
            //                                                        ? 0
            //                                                        : Convert.ToInt32(x.imei.NoOfStreetlight)),
            //                                OFF = grouped.Where(x => x.vis.P10 == 0)
            //                                              .Sum(x => string.IsNullOrEmpty(x.imei.NoOfStreetlight)
            //                                                         ? 0
            //                                                         : Convert.ToInt32(x.imei.NoOfStreetlight)),
            //                                NC = grouped.Where(x => x.vis.P10 == 2)
            //                                             .Sum(x => string.IsNullOrEmpty(x.imei.NoOfStreetlight)
            //                                                        ? 0
            //                                                        : Convert.ToInt32(x.imei.NoOfStreetlight)),
            //                                TotalNoOfStreetlight = grouped.Sum(x => string.IsNullOrEmpty(x.imei.NoOfStreetlight)
            //                                                                         ? 0
            //                                                                         : Convert.ToInt32(x.imei.NoOfStreetlight))
            //                            }).ToListAsync();

            var firstChartData = await (from imei in context.IMEI_Master
                                        join ss in context.SiteSummary on imei.UID equals ss.UID
                                        group new { imei, ss } by 1 into grouped
                                        select new ChartModel
                                        {
                                            ON = grouped.Where(x => x.ss.LIGHTSTS == 1)
                                                         .Sum(x => string.IsNullOrEmpty(x.imei.NoOfStreetlight)
                                                                    ? 0
                                                                    : Convert.ToInt32(x.imei.NoOfStreetlight)),
                                            OFF = grouped.Where(x => x.ss.LIGHTSTS == 0)
                                                          .Sum(x => string.IsNullOrEmpty(x.imei.NoOfStreetlight)
                                                                     ? 0
                                                                     : Convert.ToInt32(x.imei.NoOfStreetlight)),
                                            NC = grouped.Where(x => x.ss.LIGHTSTS == 3)
                                                         .Sum(x => string.IsNullOrEmpty(x.imei.NoOfStreetlight)
                                                                    ? 0
                                                                    : Convert.ToInt32(x.imei.NoOfStreetlight)),
                                            TotalNoOfStreetlight = grouped.Sum(x => string.IsNullOrEmpty(x.imei.NoOfStreetlight)
                                                                                     ? 0
                                                                                     : Convert.ToInt32(x.imei.NoOfStreetlight))
                                        }).ToListAsync();

            //var secondChartData = await context.db_Live_info
            //     .GroupBy(x => 1) // Group all rows into a single set
            //     .Select(group => new ChartModel
            //     {
            //         ON = group.Count(x => x.P10 == 1 && x.DUR < 1500),
            //         OFF = group.Count(x => x.P10 == 0 && x.DUR < 1500),
            //         NC = group.Count(x => x.DUR > 1500),
            //     })
            //     .ToListAsync();


            //            int totalON = 0;
            //            int totalOFF = 0;
            //            int totalNC = 0;

            //            // Fetch the distinct UIDs from the IMEI_Master table
            //            var uidList = await context.IMEI_Master
            //                                       .Select(im => im.UID)
            //                                       .Distinct()
            //                                       .ToListAsync();

            //            // For each UID, get the associated IMEI_no and count ON, OFF, NC in db_Live_info
            //            foreach (var uid in uidList)
            //            {
            //                // Get the IMEI_no(s) for the current UID
            //                var imeiList = await context.IMEI_Master
            //                                            .Where(im => im.UID == uid)
            //                                            .Select(im => im.IMEI_no)
            //                                            .ToListAsync();

            //                // For each IMEI_no, count ON, OFF, and NC in db_Live_info
            //                foreach (var imeiNo in imeiList)
            //                {
            //                    var onCount = await context.db_Live_info
            //                        .Where(dli => dli.IMEI_no == imeiNo && dli.P10 == 1 && dli.DUR < 1500)
            //                        .CountAsync();

            //                    var offCount = await context.db_Live_info
            //                        .Where(dli => dli.IMEI_no == imeiNo && dli.P10 == 0 && dli.DUR < 1500)
            //                        .CountAsync();

            //                    var ncCount = await context.db_Live_info
            //                        .Where(dli => dli.IMEI_no == imeiNo && dli.DUR > 1500)
            //                        .CountAsync();

            //                    // Add the counts to the totals
            //                    totalON += onCount;
            //                    totalOFF += offCount;
            //                    totalNC += ncCount;
            //                }
            //            }

            //            // Store the result in a list
            //            var secondChartData = new List<ChartModel>
            //{
            //    new ChartModel
            //    {
            //        ON = totalON,
            //        OFF = totalOFF,
            //        NC = totalNC
            //    }
            //};

            //            var imeiData = await context.IMEI_Master
            //                                        .Select(im => new { im.UID, im.IMEI_no })
            //                                        .ToListAsync();

            //            // Group the data by UID to count ON, OFF, and NC in a single query
            //            var imeiCounts = await context.db_Live_info
            //                .Where(dli => imeiData.Select(im => im.IMEI_no).Contains(dli.IMEI_no)) // filter by the IMEI_no's from IMEI_Master
            //                .GroupBy(dli => dli.IMEI_no) // Group by IMEI_no
            //                .Select(group => new
            //                {
            //                    IMEI_no = group.Key,
            //                    ON = group.Count(dli => dli.P10 == 1 && dli.DUR < 1500),
            //                    OFF = group.Count(dli => dli.P10 == 0 && dli.DUR < 1500),
            //                    NC = group.Count(dli => dli.DUR > 1500)
            //                })
            //                .ToListAsync();

            //            // Initialize total counts
            //            int totalON = 0;
            //            int totalOFF = 0;
            //            int totalNC = 0;

            //            // Sum the counts from the grouped data
            //            foreach (var count in imeiCounts)
            //            {
            //                totalON += count.ON;
            //                totalOFF += count.OFF;
            //                totalNC += count.NC;
            //            }

            //            // Store the result in a list
            //            var secondChartData = new List<ChartModel>
            //{
            //    new ChartModel
            //    {
            //        ON = totalON,
            //        OFF = totalOFF,
            //        NC = totalNC
            //    }
            //};

            //            int totalON = 0;
            //int totalOFF = 0;
            //int totalNC = 0;

            //// Fetch the distinct IMEI_no values from the IMEI_Master table
            //var imeiList = await context.IMEI_Master
            //                            .Select(im => im.IMEI_no)
            //                            .Distinct()
            //                            .ToListAsync();

            //// Query the db_Live_info table for ON, OFF, and NC counts for all IMEI_no values in one go
            //var imeiCounts = await context.db_Live_info
            //    .Where(dli => imeiList.Contains(dli.IMEI_no))  // Filter by the IMEI_no's fetched
            //    .GroupBy(dli => dli.IMEI_no)  // Group by IMEI_no
            //    .Select(group => new
            //    {
            //        IMEI_no = group.Key,
            //        ON = group.Count(dli => dli.P10 == 1 && dli.DUR < 1500),
            //        OFF = group.Count(dli => dli.P10 == 0 && dli.DUR < 1500),
            //        NC = group.Count(dli => dli.DUR > 1500)
            //    })
            //    .ToListAsync();

            //// Sum the counts across all IMEI_no's
            //foreach (var count in imeiCounts)
            //{
            //    totalON += count.ON;
            //    totalOFF += count.OFF;
            //    totalNC += count.NC;
            //}

            //// Store the result in a list
            //var secondChartData = new List<ChartModel>
            //{
            //    new ChartModel
            //    {
            //        ON = totalON,
            //        OFF = totalOFF,
            //        NC = totalNC
            //    }
            //};

            // Fetch data grouped by UID
            var result = (from master in context.IMEI_Master
                          join live in context.VisLive
                          on master.IMEI_no equals live.IMEI_no
                          let dur = EF.Functions.DateDiffMinute(live.DTIME_SYS, DateTime.Now)
                          group new { master.UID, live.P10, dur } by master.UID into g
                          select new
                          {
                              UID = g.Key,
                              ON_Count = g.Count(x => x.P10 == 1 && x.dur < 1500),
                              OFF_Count = g.Count(x => x.P10 == 0 && x.dur < 1500),
                              NC_Count = g.Count(x => x.dur > 1500)
                          }).ToList();

            // Transform result into chart data
            var totalCounts = result.Select(r => new ChartModel
            {
                ON = r.ON_Count,
                OFF = r.OFF_Count,
                NC = r.NC_Count
            }).ToList();

            // Calculate total ON, OFF, and NC counts
            // Calculate total ON, OFF, and NC counts
            //            var secondChartData = new List<ChartModel>
            //{
            //    new ChartModel
            //    {
            //        ON = result.Sum(r => r.ON_Count),
            //        OFF = result.Sum(r => r.OFF_Count),
            //        NC = result.Sum(r => r.NC_Count)
            //    }
            //};

            var dashboardData = await context.Dashboardinfo.FirstOrDefaultAsync();

            var secondChartData = new List<ChartModel>
{
    new ChartModel
    {
        ON = dashboardData.ONCOUNT,
        OFF = dashboardData.OFFCOUNT,
        NC = dashboardData.NCCOUNT
    }
};

            // Output totals for debugging
            //Console.WriteLine($"Total ON: {secondChartData.ON}");
            //Console.WriteLine($"Total OFF: {secondChartData.OFF}");
            //Console.WriteLine($"Total NC: {secondChartData.NC}");

            var viewModel = new ChartViewModel
            {
                FirstChart = firstChartData,
                SecondChart = secondChartData
            };

            return View(viewModel);
        }

        public async Task<IActionResult> GetChartCountData()
        {
            var dashboardData = await context.Dashboardinfo.FirstOrDefaultAsync();
            var result = new
            {
                Auto = await (from master in context.IMEI_Master
                              join live in context.VisLive
                              on master.IMEI_no equals live.IMEI_no
                              where live.P11 != 1 && (live.P10 == 1 || live.P10 == 0)
                              select live).CountAsync(),

                //Manual = await (from master in context.IMEI_Master
                //                join live in context.VisLive
                //                on master.IMEI_no equals live.IMEI_no
                //                where live.P11 == 1 && (live.P10 == 1 || live.P10 == 0)
                //                select live).CountAsync(),
                
                Manual = dashboardData.BYPASSCOUNT,

                MCBTrip = await (from master in context.IMEI_Master
                                 join live in context.VisLive
                                 on master.IMEI_no equals live.IMEI_no
                                 where live.P10 == 1 && live.P11 == 0 && live.P3 == 0
                                 select live).CountAsync(),

                MeterCommFail = await (from master in context.IMEI_Master
                                       join live in context.VisLive
                                       on master.IMEI_no equals live.IMEI_no
                                       where live.P20 == 1
                                       select live).CountAsync()
            };

            return Json(result);
        }
    }
}
