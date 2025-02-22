using System.Linq;
using CCMS.Models;
using CCMS.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;
using static System.Runtime.InteropServices.JavaScript.JSType;
using CCMS.CustomModel;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace CCMS.Controllers
{
    public class Home : Controller
    {
        private readonly ApplicationDbContext context;
        private readonly IConfiguration _configuration;
        private readonly ReportsDbContext _reportsDbContext;
        private readonly ILogger<Home> _logger;

        public Home(ApplicationDbContext context, IConfiguration configuration, ReportsDbContext reportsDbContext, ILogger<Home> logger)
        {
            this.context = context;
            _configuration = configuration;
            _reportsDbContext = reportsDbContext;
            _logger = logger;
        }

        // Action to display the view
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var wards = await context.Zone_master
                                     .Where(w => !string.IsNullOrEmpty(w.Ward))
                                     .Select(w => new { w.RecordID, w.Ward })
                                     .Distinct()
                                     .ToListAsync();

            //var result = (from master in context.IMEI_Master
            //              join live in context.VisLive
            //              on master.IMEI_no equals live.IMEI_no
            //              let dur = EF.Functions.DateDiffMinute(live.DTIME_SYS, DateTime.Now)
            //              group new { master.UID, live.P10, dur } by master.UID into g
            //              select new
            //              {
            //                  UID = g.Key,
            //                  ON_Count = g.Count(x => x.P10 == 1 && x.dur < 1500),
            //                  OFF_Count = g.Count(x => x.P10 == 0 && x.dur < 1500),
            //                  NC_Count = g.Count(x => x.dur > 1500)
            //              }).ToList();

            //// Transform result into chart data
            //var totalCounts = result.Select(r => new ChartModel
            //{
            //    ON = r.ON_Count,
            //    OFF = r.OFF_Count,
            //    NC = r.NC_Count
            //}).ToList();

            //var data = new List<ChartModel>
            //{
            //    new ChartModel
            //    {
            //        ON = result.Sum(r => r.ON_Count),
            //        OFF = result.Sum(r => r.OFF_Count),
            //        NC = result.Sum(r => r.NC_Count)
            //    }
            //};

            //var data = await context.db_Live_info
            //     .GroupBy(x => 1) // Group all rows into a single set
            //     .Select(group => new ChartModel
            //     {
            //         ON = group.Count(x => x.P10 == 1 && x.DUR < 1500),
            //         OFF = group.Count(x => x.P10 == 0 && x.DUR < 1500),
            //         NC = group.Count(x => x.DUR > 1500),
            //     })
            //     .ToListAsync();


            //var data = await context.IMEI_Master
            //    .GroupBy(x => 1) // Group all rows into a single set for chart 1
            //    .Select(group => new ChartModel
            //    {
            //        ON = group.Count(x => x.Status == "1"),
            //        OFF = group.Count(x => x.Status == "2"),
            //        NC = group.Count(x => x.Status == "3"),
            //    })
            //    .ToListAsync();

            //var data = await context.IMEI_Master
            //    .GroupBy(x => 1) // Group all rows into a single set
            //    .Select(group => new ChartModel
            //    {
            //        ON = group.Where(x => x.Status == "1").Sum(x => string.IsNullOrEmpty(x.NoOfStreetlight) ? 0 : Convert.ToInt32(x.NoOfStreetlight)),
            //        OFF = group.Where(x => x.Status == "2").Sum(x => string.IsNullOrEmpty(x.NoOfStreetlight) ? 0 : Convert.ToInt32(x.NoOfStreetlight)),
            //        NC = group.Where(x => x.Status == "3").Sum(x => string.IsNullOrEmpty(x.NoOfStreetlight) ? 0 : Convert.ToInt32(x.NoOfStreetlight)),
            //        TotalNoOfStreetlight = group.Sum(x => string.IsNullOrEmpty(x.NoOfStreetlight) ? 0 : Convert.ToInt32(x.NoOfStreetlight)) // Sum of NoOfStreetlight
            //    })
            //    .ToListAsync();

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

            //Console.WriteLine($"firstChartData: {firstChartData}");

            var dashboardData = await context.Dashboardinfo.FirstOrDefaultAsync();

            var data = new List<ChartModel>
{
    new ChartModel
    {
        ON = dashboardData.ONCOUNT,
        OFF = dashboardData.OFFCOUNT,
        NC = dashboardData.NCCOUNT
    }
};

            ViewData["data"] = data;

            var model = new ZoneWardViewModel
            {
                Wards = new SelectList(wards, "RecordID", "Ward")
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> TriggerScheduledTask()
        {
            try
            {
                // Fetch all records from IMEI_Master (using ApplicationDbContext)
                var imeiData = await context.IMEI_Master.ToDictionaryAsync(im => im.IMEI_no);

                // Fetch all records from VisLive (using ApplicationDbContext)
                var visLiveData = await context.VisLive.ToListAsync();

                // Create a list to hold DayReport entries
                var dayReports = new List<DayReport>();

                foreach (var visLiveRecord in visLiveData)
                {
                    // Check if IMEI_Master contains a matching record
                    if (imeiData.TryGetValue(visLiveRecord.IMEI_no, out var imeiRecord))
                    {
                        // Create a new DayReport entry based on the matching records
                        var dayReport = new DayReport
                        {
                            UID = imeiRecord.UID,
                            IMEI_no = visLiveRecord.IMEI_no,
                            RDateTime = DateTime.Now,
                            Zone = imeiRecord.Zone,
                            Ward = imeiRecord.Ward,
                            PhoneNo = imeiRecord.SIM_No,
                            Phase = imeiRecord.Phase,
                            OpenReading = visLiveRecord.P7,
                        };

                        dayReports.Add(dayReport);
                    }
                }

                // Add and save all DayReport entries in ReportsDbContext
                if (dayReports.Any())
                {
                    await _reportsDbContext.DayReport.AddRangeAsync(dayReports);
                    await _reportsDbContext.SaveChangesAsync();
                    Console.WriteLine("DayReport data has been successfully updated.");
                    return Ok("DayReport data has been successfully updated.");
                }
                else
                {
                    Console.WriteLine("No matching data found between IMEI_Master and VisLive.");
                    return Ok("No matching data found between IMEI_Master and VisLive.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return BadRequest($"An error occurred: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateDayReports()
        {
            try
            {
                var today = DateTime.UtcNow.Date;

                // Use AsNoTracking to optimize read-only queries
                var imeiData = await context.IMEI_Master.AsNoTracking().ToDictionaryAsync(im => im.IMEI_no);
                var visLiveData = await context.VisLive.AsNoTracking().ToListAsync();

                // Fetch all DayReport records for the given date in a single query
                var existingReports = await _reportsDbContext.DayReport
                    .Where(dr => EF.Functions.DateDiffDay(dr.RDateTime, today) == 0)
                    .ToDictionaryAsync(dr => dr.IMEI_no);

                // Fetch all the required average RKW in one batch query
                var avgRKWs = await _reportsDbContext.LiveReport
                    .Where(lr => EF.Functions.DateDiffDay(lr.DateTime, today) == 0)
                    .GroupBy(lr => lr.IMEI_no)
                    .Select(g => new
                    {
                        IMEI_no = g.Key,
                        AvgRKW = g.Average(lr => (double?)lr.RKW) ?? 0
                    })
                    .ToDictionaryAsync(g => g.IMEI_no, g => g.AvgRKW);

                var dayReports = new List<DayReport>();

                foreach (var visLiveRecord in visLiveData)
                {
                    if (imeiData.TryGetValue(visLiveRecord.IMEI_no, out var imeiRecord))
                    {
                        existingReports.TryGetValue(visLiveRecord.IMEI_no, out var existingDayReport);

                        if (!avgRKWs.ContainsKey(visLiveRecord.IMEI_no) || avgRKWs[visLiveRecord.IMEI_no] == 0)
                        {
                            Console.WriteLine($"No valid average RKW for IMEI: {visLiveRecord.IMEI_no}");
                            continue;
                        }

                        var avgPower = avgRKWs[visLiveRecord.IMEI_no];
                        var ontimeDecimal = (decimal)(visLiveRecord.P7 - (existingDayReport?.OpenReading ?? 0)) / (decimal)avgPower;
                        if (ontimeDecimal < 0) ontimeDecimal = 0;

                        var ontimeHours = Math.Floor((double)ontimeDecimal);
                        var ontimeMinutes = (ontimeDecimal - (decimal)ontimeHours) * 60;

                        if (existingDayReport != null)
                        {
                            existingDayReport.OnTime = $"{ontimeHours:00}:{ontimeMinutes:00}";
                            existingDayReport.CloseReading = visLiveRecord.P7;
                            existingDayReport.DDateTime = DateTime.Now;
                            existingDayReport.Mode = visLiveRecord.P11 == 1 ? "MANUAL" : "AUTO";
                            existingDayReport.RVolt = visLiveRecord.P1;
                            existingDayReport.YVolt = visLiveRecord.P4;
                            existingDayReport.BVolt = visLiveRecord.P6;
                            existingDayReport.RKW = visLiveRecord.P3;
                            existingDayReport.YKW = visLiveRecord.P13;
                            existingDayReport.BKW = visLiveRecord.P14;
                            existingDayReport.RPF = visLiveRecord.P17;
                            existingDayReport.YPF = visLiveRecord.P18;
                            existingDayReport.BPF = visLiveRecord.P19;
                            existingDayReport.UnitConsumed = visLiveRecord.P7 - existingDayReport.OpenReading;
                        }
                        else
                        {
                            var dayReport = new DayReport
                            {
                                UID = imeiRecord.UID,
                                IMEI_no = visLiveRecord.IMEI_no,
                                RDateTime = DateTime.Now,
                                Zone = imeiRecord.Zone,
                                Ward = imeiRecord.Ward,
                                PhoneNo = imeiRecord.SIM_No,
                                Phase = imeiRecord.Phase,
                                OpenReading = visLiveRecord.P7,
                                OnTime = "00:00",
                                CloseReading = visLiveRecord.P7,
                                DDateTime = DateTime.Now,
                                Mode = visLiveRecord.P11 == 1 ? "MANUAL" : "AUTO",
                                RVolt = visLiveRecord.P1,
                                YVolt = visLiveRecord.P4,
                                BVolt = visLiveRecord.P6,
                                RKW = visLiveRecord.P3,
                                YKW = visLiveRecord.P13,
                                BKW = visLiveRecord.P14,
                                RPF = visLiveRecord.P17,
                                YPF = visLiveRecord.P18,
                                BPF = visLiveRecord.P19,
                            };

                            dayReports.Add(dayReport);
                        }
                    }
                }

                if (dayReports.Any())
                {
                    await _reportsDbContext.DayReport.AddRangeAsync(dayReports);
                }

                await _reportsDbContext.SaveChangesAsync();

                // Use logging instead of Console.WriteLine
                _logger.LogInformation("DayReport data has been successfully updated.");
                return Ok("DayReport data has been successfully updated.");
            }
            catch (Exception ex)
            {
                // Use logging for errors
                _logger.LogError($"An error occurred: {ex.Message}");
                return BadRequest($"An error occurred: {ex.Message}");
            }
        }

        // Action to fetch wards based on selected zone
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetWardsByZone(string zoneIds)
        {
            var zoneIdList = zoneIds;

            // Fetch wards where Zone matches any of the zoneIds
            Console.WriteLine(zoneIdList);
            if (zoneIdList != null)
            {
                var wards = await context.Zone_master
                                          .Where(w => zoneIdList.Contains(w.Zone) && !string.IsNullOrEmpty(w.Ward))
                                          .Select(w => new { w.RecordID, w.Ward })
                                          .ToListAsync();
                return Json(wards); // Return the result as JSON for AJAX
            }
            else
            {
                var wards = await context.Zone_master
                                     .Where(w => !string.IsNullOrEmpty(w.Ward))
                                     .Select(w => new { w.RecordID, w.Ward })
                                     .Distinct()
                                     .ToListAsync();
                return Json(wards); // Return the result as JSON for AJAX
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> DeviceData(string uid)
        {
            // Fetch device data
            var deviceData = await context.IMEI_Master
                                         .Where(w => w.UID == uid)
                                         .FirstOrDefaultAsync();

            if (deviceData == null)
            {
                Console.WriteLine($"No data found for UID: {uid}");
                return NotFound("Device data not found.");
            }

            // Fetch VisLive data
            var Data = await context.VisLive
                                    .Where(w => w.IMEI_no == deviceData.IMEI_no)
                                    .FirstOrDefaultAsync();

            if (Data == null)
            {
                Console.WriteLine($"No data found in VisLive for IMEI_no: {deviceData.IMEI_no}");
            }

            // Fetch loader value
            var loaderValue = context.NetworkSts
                                    .Select(n => n.Loader)
                                    .FirstOrDefault();

            if (loaderValue == null)
            {
                Console.WriteLine("No loader value found in NetworkSts.");
                ViewBag.loader = "Default Loader Value";
            }
            else
            {
                ViewBag.loader = loaderValue;
            }

            // Log data
            Console.WriteLine($"DeviceData IMEI: {deviceData.IMEI_no}");
            Console.WriteLine($"Loader Value: {ViewBag.loader}");

            // Create view model
            var alldata = new DataModel
            {
                visLive_s = Data,
                IMEI_Master_s = deviceData
            };

            return View(alldata);
        }

        //public async Task<IActionResult> DeviceData(string uid)
        //{
        //    var deviceData = await context.IMEI_Master
        //                                 .Where(w => w.UID == uid)
        //                                 .FirstOrDefaultAsync();

        //    //var Data = await context.VisLive
        //    //                             .Where(w => w.IMEI_no == IMEI)
        //    //                             .FirstOrDefaultAsync();

        //    var Data = await context.VisLive
        //                    .Where(w => w.IMEI_no == deviceData.IMEI_no)
        //                    .FirstOrDefaultAsync();

        //    var loaderValue = context.NetworkSts
        //                      .Select(n => n.Loader)
        //                      .FirstOrDefault();
        //    Console.WriteLine($"loaderValue : {loaderValue}");

        //    ViewBag.loader = loaderValue;

        //    var alldata = new DataModel
        //    {
        //        visLive_s = Data,
        //        IMEI_Master_s = deviceData
        //    };

        //    Console.WriteLine($"IMEI: {deviceData.IMEI_no}");
        //    Console.WriteLine($"IMEI: {deviceData}");

        //    return View(alldata);
        //}

        [Authorize]
        public async Task<IActionResult> IMEIs()
        {
            var data = await context.SiteSummary.OrderBy(s => s.SNO).ToListAsync();
            //var data2 = await (from imei in context.IMEI_Master
            //                  join SiteSummary in context.SiteSummary on imei.UID equals SiteSummary.UID
            //                  orderby imei.SNO
            //                  select new
            //                  {
            //                      imei.UID,
            //                      imei.IMEI_no,
            //                      imei.Location,
            //                      imei.Status,
            //                      imei.Zone,
            //                      imei.Ward,
            //                      SiteSummary.LIGHTSTS
            //                  }).ToListAsync();

            //var data = await (from imei in context.IMEI_Master
            //                  join vislive in context.VisLive on imei.IMEI_no equals vislive.IMEI_no
            //                  orderby imei.UID descending // Use 'descending' for descending order
            //                  select new
            //                  {
            //                      imei.UID,
            //                      imei.IMEI_no,
            //                      imei.Location,
            //                      imei.Status,
            //                      imei.Zone,
            //                      imei.Ward,
            //                      vislive.P10,
            //                      vislive.P11,
            //                      vislive.P3,
            //                      vislive.P20,
            //                  }).ToListAsync();

            //// Count P10 where it is 0
            //var countP10Zero = data.Count(d => d.P10 == 0);

            //// Count P10 where it is 1
            //var countP10One = data.Count(d => d.P10 == 1);

            //// Total count of filtered P10 values
            //var totalP10Count = data.Count;


            // Return filtered data with counts
            return Json(new
            {
                Records = data,
                //CountP10Zero = countP10Zero,
                //CountP10One = countP10One,
                //TotalP10Count = totalP10Count
            });
            //return Json(data);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> FilterDevices([FromBody] JsonElement requestData)
        {
            try
            {
                // Parse filters from the request
                var zones = requestData.TryGetProperty("zones", out var zonesElement) && zonesElement.ValueKind == JsonValueKind.Array
                    ? zonesElement.EnumerateArray().Select(x => x.ToString()).ToList()
                    : null;

                var wards = requestData.TryGetProperty("wards", out var wardsElement) && wardsElement.ValueKind == JsonValueKind.Array
                    ? wardsElement.EnumerateArray().Select(x => x.ToString()).ToList()
                    : null;

                var status = requestData.TryGetProperty("status", out var statusElement) && statusElement.ValueKind == JsonValueKind.Array
                    ? statusElement.EnumerateArray().Select(x => x.ToString()).ToList()
                    : null;

                // Convert status values to double for comparison
                List<double> statusAsDoubles = status?.Select(double.Parse).ToList();

                // Log the received filters
                //Console.WriteLine($"Zones: {string.Join(", ", zones ?? new List<string>())}");
                //Console.WriteLine($"Wards: {string.Join(", ", wards ?? new List<string>())}");
                //Console.WriteLine($"Status: {string.Join(", ", status ?? new List<string>())}");
                //Console.WriteLine($"StatusAsDoubles: {string.Join(", ", statusAsDoubles ?? new List<double>())}");

                // Start building the query from IMEI_Master
                var imeiMasterQuery = context.IMEI_Master.AsQueryable();

                // Apply filters for zones and wards from IMEI_Master
                if (zones != null && zones.Any())
                {
                    imeiMasterQuery = imeiMasterQuery.Where(d => zones.Contains(d.Zone));
                }

                if (wards != null && wards.Any())
                {
                    imeiMasterQuery = imeiMasterQuery.Where(d => wards.Contains(d.Ward));
                }

                // Fetch filtered IMEI numbers
                var filteredIMEINumbers = await imeiMasterQuery
                    .Select(d => d.IMEI_no)
                    .ToListAsync();

                // Log filtered IMEI numbers
                Console.WriteLine($"Filtered IMEI Numbers: {string.Join(", ", filteredIMEINumbers)}");

                // Now join with VisLive to get P10 (status) and apply status filter
                var imeiAndStatusQuery = context.VisLive
                    .Where(v => filteredIMEINumbers.Contains(v.IMEI_no) && v.P10.HasValue) // Ensure P10 is not null
                    .Join(
                        context.IMEI_Master,
                        visLive => visLive.IMEI_no,
                        imeiMaster => imeiMaster.IMEI_no,
                        (visLive, imeiMaster) => new
                        {
                            imeiMaster.IMEI_no,
                            imeiMaster.UID,
                            imeiMaster.Location,
                            imeiMaster.Zone,
                            imeiMaster.Ward,
                            visLive.P10
                        });

                // Apply the status filter on P10 if provided
                if (statusAsDoubles != null && statusAsDoubles.Any())
                {
                    imeiAndStatusQuery = imeiAndStatusQuery.Where(v => statusAsDoubles.Contains(v.P10.Value));
                }

                // Fetch the final data with IMEI_no, Location, Zone, Ward, and P10
                var devices = await imeiAndStatusQuery
                    .ToListAsync();

                return Json(devices);
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error: {ex.Message}");

                // Return a simplified error response
                return StatusCode(500, new
                {
                    Message = "An error occurred while processing the request.",
                    Details = ex.Message
                });
            }
        }

        [HttpPost]
        [Authorize]
        [Authorize(Policy = "OfficerAccess")]
        public async Task<IActionResult> UpdateIMEIStatus([FromBody] JsonElement requestData)
        {
            try
            {
                // Parse input
                var imeiNo = requestData.TryGetProperty("IMEI_no", out var IMEI_noElement) ? IMEI_noElement.GetString() : null;
                var response = requestData.TryGetProperty("ResponseSts", out var ResponseStsElement) ? ResponseStsElement.GetString() : null;

                if (imeiNo == null || response == null)
                    return Json(new { success = false, message = "Invalid input." });

                // Check in cache first (if applicable)
                var imeiRecord = await context.IMEI_Master.FirstOrDefaultAsync(i => i.IMEI_no == imeiNo);

                if (imeiRecord != null)
                {
                    // Convert UTC to IST
                    DateTime utcNow = DateTime.UtcNow;
                    TimeZoneInfo istTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                    DateTime istDateTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, istTimeZone);

                    imeiRecord.Response = response;
                    imeiRecord.ResponseDTime = istDateTime;
                    await context.SaveChangesAsync();

                    // Return minimal response
                    return Json(new
                    {
                        success = true,
                        message = "Status updated successfully.",
                        data = new { imeiRecord.IMEI_no, imeiRecord.Status, imeiRecord.Response }
                    });
                }
                else
                {
                    return Json(new { success = false, message = "IMEI not found." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                ViewBag.Message = "Please enter a search query.";
                return View("Index");
            }

            // Perform the search across all columns
            var results = context.IMEI_Master
                .Where(row => EF.Functions.Like(row.IMEI_no, $"%{query}%") ||
                              EF.Functions.Like(row.Location, $"%{query}%") ||
                              EF.Functions.Like(row.Zone, $"%{query}%"))
                .ToList();

            return View("SearchResults", results);
        }

        [HttpGet]
        public async Task<IActionResult> UpdateStatus()
        {
            // Fetch necessary data upfront
            var networkData = await context.Network.ToListAsync();
            var networkStsData = await context.NetworkSts.FirstOrDefaultAsync(); // Assuming only one row is relevant
            var imeiData = await context.IMEI_Master.ToDictionaryAsync(im => im.IMEI_no); // Preload all IMEI records

            if (networkStsData == null)
            {
                return NotFound("NetworkSts data not found.");
            }

            var deviceOfflineThreshold = networkStsData.DeviceOffline;

            // Create a list to hold updated IMEI records
            var updatedIMEIs = new List<IMEI_Master>();

            foreach (var device in networkData)
            {
                if (imeiData.TryGetValue(device.IMEI_no, out var imeiRecord))
                {
                    // Update status based on condition
                    imeiRecord.Status = device.DUR > deviceOfflineThreshold ? "2" : "1";
                    updatedIMEIs.Add(imeiRecord);
                }
                else
                {
                    Console.WriteLine($"IMEI record not found for IMEI_no: {device.IMEI_no}");
                }
            }

            // Bulk update IMEI records
            context.IMEI_Master.UpdateRange(updatedIMEIs);
            await context.SaveChangesAsync();

            return Ok("Statuses updated successfully.");
        }

        [HttpPost]
        public async Task<IActionResult> CheckP10ByIMEI([FromBody] JsonElement requestData)
        {
            // Parse input
            var imeiNo = requestData.TryGetProperty("IMEI_no", out var IMEI_noElement) ? IMEI_noElement.GetString() : null;

            if (string.IsNullOrEmpty(imeiNo))
            {
                return BadRequest("IMEI number is required.");
            }

            var value = await context.VisLive
                .Where(x => x.IMEI_no == imeiNo)
                .Select(x => x.P10)
                .FirstOrDefaultAsync();

            if (value == null)
            {
                return NotFound($"No record found for IMEI: {imeiNo}");
            }

            return Json(new { success = true, P10 = value });
        }
    }
}
