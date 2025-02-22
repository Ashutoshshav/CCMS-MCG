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

        public Home(ApplicationDbContext context, IConfiguration configuration)
        {
            this.context = context;
            _configuration = configuration;
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

            var model = new ZoneWardViewModel
            {
                Wards = new SelectList(wards, "RecordID", "Ward")
            };

            return View(model);
        }

        // Action to fetch wards based on selected zone
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetWardsByZone(string zoneIds)
        {
            var zoneIdList = zoneIds;

            // Fetch wards where Zone matches any of the zoneIds
            Console.WriteLine(zoneIdList);
            if(zoneIdList != null)
            {
                var wards = await context.Zone_master
                                          .Where(w => zoneIdList.Contains(w.Zone) && !string.IsNullOrEmpty(w.Ward))
                                          .Select(w => new { w.RecordID, w.Ward })
                                          .ToListAsync();
                return Json(wards); // Return the result as JSON for AJAX
            } else
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
        public async Task<IActionResult> DeviceData(string IMEI)
        {
            var Data = await context.VisLive
                                         .Where(w => w.IMEI_no == IMEI)
                                         .FirstOrDefaultAsync();

            var deviceData = await context.IMEI_Master
                                         .Where(w => w.IMEI_no == IMEI)
                                         .FirstOrDefaultAsync();

            var alldata = new DataModel
            {
                visLive_s = Data,
                IMEI_Master_s = deviceData
            };

            Console.WriteLine($"IMEI: {IMEI}");
            Console.WriteLine($"IMEI: {deviceData}");

            return View(alldata);
        }

        [Authorize]
        public async Task<IActionResult> IMEIs()
        {
            var data = await context.IMEI_Master.ToListAsync();
            return Json(data);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> filterDeveices([FromBody] JsonElement requestData)
        {
            try
            {
                var zones = requestData.TryGetProperty("zones", out var zonesElement) && zonesElement.ValueKind == JsonValueKind.Array
                    ? zonesElement.EnumerateArray().Select(x => x.ToString()).ToList()
                    : null;

                var wards = requestData.TryGetProperty("wards", out var wardsElement) && wardsElement.ValueKind == JsonValueKind.Array
                    ? wardsElement.EnumerateArray().Select(x => x.ToString()).ToList()
                    : null;

                var status = requestData.TryGetProperty("status", out var statusElement) && statusElement.ValueKind == JsonValueKind.Array
                    ? statusElement.EnumerateArray().Select(x => x.ToString()).ToList()
                    : null;

                Console.WriteLine(zones);
                Console.WriteLine(wards);
                Console.WriteLine(status);

                // Query the Devices table with conditional filtering
                var devicesQuery = context.IMEI_Master.AsQueryable();

                // If zones is not null, filter by zones
                if (zones != null && zones.Any())
                {
                    devicesQuery = devicesQuery.Where(d => zones.Contains(d.Zone));
                }

                // If wards is not null, filter by wards
                if (wards != null && wards.Any())
                {
                    devicesQuery = devicesQuery.Where(d => wards.Contains(d.Ward));
                }

                // If wards is not null, filter by wards
                if (status != null && status.Any())
                {
                    devicesQuery = devicesQuery.Where(d => status.Contains(d.Status));
                }

                // Execute the query
                var devices = await devicesQuery
                    .Select(d => new { d.IMEI_no, d.Location, d.Status, d.Zone, d.Ward })
                    .ToListAsync();

                Console.WriteLine(devices);
                return Json(devices);
            }
            catch (Exception ex) 
            {
                // Log the error for debugging
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

        [HttpGet]
        [Route("api/checkP10ByIMEI")]
        public async Task<IActionResult> CheckP7ByIMEI(string imei)
        {
            if (string.IsNullOrEmpty(imei))
            {
                return BadRequest("IMEI number is required.");
            }

            var value = await context.VisLive
                .Where(x => x.IMEI_no == imei)
                .Select(x => x.P10)
                .FirstOrDefaultAsync();

            if (value == null)
            {
                return NotFound($"No record found for IMEI: {imei}");
            }

            return Ok(new { value });
        }
    }
}
