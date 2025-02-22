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
using ClosedXML.Excel;
using OfficeOpenXml;
using System.IO;

namespace CCMS.Controllers
{
    public class Report : Controller
    {
        private readonly ReportsDbContext _reportsDbContext;

        public Report(ReportsDbContext reportsDbContext)
        {
            _reportsDbContext = reportsDbContext;
        }

        public async Task<IActionResult> GetLiveReportByUID(string UID)
        {
            if (string.IsNullOrEmpty(UID))
            {
                ViewBag.ErrorMessage = "UID number is required.";
                return View();
            }

            Console.WriteLine(UID);

            // Get today's date
            var today = DateTime.Today;
            Console.WriteLine($"{today}");

            // Fetch records based on UID for today, ordered by RecordID in descending order
            var liveReports = await _reportsDbContext.LiveReport
                .Where(r => r.UID == UID && r.DateTime >= today && r.DateTime < today.AddDays(1))
                .OrderByDescending(r => r.RecordID) // Order by RecordID in descending order
                .ToListAsync();

            if (!liveReports.Any())
            {
                ViewBag.ErrorMessage = "No records found for the provided UID number.";
            }

            // Pass UID to the view
            ViewBag.UID = UID;

            // Pass data to the view
            return View("GetLiveReportByUID", liveReports);
        }

        public IActionResult LiveReportFilter(string UID, DateTime? startDate)
        {
            // Set the default start and end dates to the current date if no date is provided
            if (!startDate.HasValue)
            {
                startDate = DateTime.Today; // Default to today's date
            }

            // Query the database for reports within the date range, ordered by RecordID in descending order
            var reports = _reportsDbContext.LiveReport
                .Where(r => r.UID == UID && r.DateTime >= startDate.Value && r.DateTime < startDate.Value.AddDays(1))
                .OrderByDescending(r => r.RecordID) // Order by RecordID in descending order
                .ToList();

            // Pass the data to the view
            ViewBag.UID = UID;
            ViewBag.StartDate = startDate.Value.ToString("yyyy-MM-dd");

            return View("GetLiveReportByUID", reports);
        }

        public IActionResult DownloadLiveReportExcel(string UID, DateTime? startDate)
        {
            // Set default date range if not provided
            if (!startDate.HasValue)
            {
                startDate = DateTime.Today;
            }

            // Set the license context for EPPlus
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // Query the database for reports within the date range, ordered by RecordID in descending order
            var reports = _reportsDbContext.LiveReport
                .Where(r => r.UID == UID && r.DateTime >= startDate.Value && r.DateTime < startDate.Value.AddDays(1))
                .OrderByDescending(r => r.RecordID)
                .ToList();

            // Create a new Excel package using EPPlus
            using (var package = new ExcelPackage())
            {
                // Create a worksheet for the merged data
                var worksheet = package.Workbook.Worksheets.Add("reports");

                // Merge cells for the title in the first row
                worksheet.Cells["A1:H1"].Merge = true;
                worksheet.Cells["A1:H1"].Value = $"LIVE REPORT OF DATE {startDate.Value:dd MMM yyyy} OF UID {UID}";
                worksheet.Cells["A1:H1"].Style.Font.Bold = true;
                worksheet.Cells["A1:H1"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                worksheet.Cells["A1:H1"].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;

                // Add headers to the worksheet
                worksheet.Cells[2, 1].Value = "DATE TIME";
                worksheet.Cells[2, 2].Value = "RVolt";
                worksheet.Cells[2, 3].Value = "RCur";
                worksheet.Cells[2, 4].Value = "RKW";
                worksheet.Cells[2, 5].Value = "RPF";
                worksheet.Cells[2, 6].Value = "Error";
                worksheet.Cells[2, 7].Value = "RELAY";
                worksheet.Cells[2, 8].Value = "UNITS";

                // Add the merged data to the worksheet
                int row = 3; // Start from the second row (after headers)
                foreach (var item in reports)
                {
                    worksheet.Cells[row, 1].Value = item.DateTime;
                    worksheet.Cells[row, 2].Value = item.RVolt;
                    worksheet.Cells[row, 3].Value = item.RCurr;
                    worksheet.Cells[row, 4].Value = item.RKW;
                    worksheet.Cells[row, 5].Value = item.RPF;
                    worksheet.Cells[row, 6].Value = item.Error;
                    worksheet.Cells[row, 7].Value = item.RelayStatus;
                    worksheet.Cells[row, 8].Value = item.Energy;

                    row++;
                }

                // AutoFit columns for better visibility
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                // Create a memory stream to store the file content
                var stream = new MemoryStream();
                package.SaveAs(stream);

                // Set UID back to ViewBag
                ViewBag.UID = UID;
                ViewBag.StartDate = startDate;

                // Return the file as a download
                stream.Position = 0;
                string fileName = $"LiveReport_{startDate.Value:dd-MM-yyyy}.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        public async Task<IActionResult> GetDayReportByUID(string UID)
        {
            if (string.IsNullOrEmpty(UID))
            {
                ViewBag.ErrorMessage = "UID number is required.";
                return View();
            }

            Console.WriteLine(UID);

            // Get today's date
            var today = DateTime.Today;

            // Fetch records based on UID for today, ordered by RecordID in descending order
            var dayReports = await _reportsDbContext.DayReport
                .Where(r => r.UID == UID && r.RDateTime >= today && r.RDateTime < today.AddDays(1))
                .OrderByDescending(r => r.RecordID) // Order by RecordID in descending order
                .ToListAsync();

            if (!dayReports.Any())
            {
                ViewBag.ErrorMessage = "No records found for the provided UID number.";
            }

            // Pass UID to the view
            ViewBag.UID = UID;

            // Pass data to the view
            return View("GetDayReportByUID", dayReports);
        }

        public IActionResult DayReportFilter(string UID, DateTime? startDate, DateTime? endDate)
        {
            // Set the default start and end dates to the current date if no date is provided
            if (!startDate.HasValue)
            {
                startDate = DateTime.Today; // Default to today's date
            }
            if (!endDate.HasValue)
            {
                endDate = DateTime.Today; // Default to today's date
            }

            // Query the database for reports within the date range, ordered by RecordID in descending order
            var reports = _reportsDbContext.DayReport
                .Where(r => r.UID == UID && r.RDateTime >= startDate.Value && r.RDateTime <= endDate.Value)
                .OrderByDescending(r => r.RecordID) // Order by RecordID in descending order
                .ToList();

            // Pass the data to the view
            ViewBag.UID = UID;
            ViewBag.StartDate = startDate.Value.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate.Value.ToString("yyyy-MM-dd");

            return View("GetDayReportByUID", reports);
        }

        public IActionResult DownloadDayReportExcel(string UID, DateTime? startDate, DateTime? endDate)
        {
            // Set default date range if not provided
            if (!startDate.HasValue)
            {
                startDate = DateTime.Today;
            }
            if (!endDate.HasValue)
            {
                endDate = DateTime.Today;
            }

            // Set the license context for EPPlus
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // Query the database for reports within the date range, ordered by RecordID in descending order
            var reports = _reportsDbContext.DayReport
                .Where(r => r.UID == UID && r.RDateTime >= startDate.Value && r.RDateTime <= endDate.Value)
                .OrderByDescending(r => r.RecordID)
                .ToList();

            // Create a new Excel package using EPPlus
            using (var package = new ExcelPackage())
            {
                // Create a worksheet for the merged data
                var worksheet = package.Workbook.Worksheets.Add("reports");

                // Merge cells for the title in the first row
                worksheet.Cells["A1:V1"].Merge = true;
                worksheet.Cells["A1:V1"].Value = $"DAY REPORT FROM {startDate.Value:dd MMM yyyy} TO {endDate.Value:dd MMM yyyy} OF UID {UID}";
                worksheet.Cells["A1:V1"].Style.Font.Bold = true;
                worksheet.Cells["A1:V1"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                worksheet.Cells["A1:V1"].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;

                // Add headers to the worksheet
                worksheet.Cells[2, 1].Value = "UID";
                worksheet.Cells[2, 2].Value = "IMEI NO";
                worksheet.Cells[2, 3].Value = "ZONE";
                worksheet.Cells[2, 4].Value = "WARD";
                worksheet.Cells[2, 5].Value = "R-TIME";
                worksheet.Cells[2, 6].Value = "D-TIME";
                worksheet.Cells[2, 7].Value = "R-VOLT";
                worksheet.Cells[2, 8].Value = "Y-VOLT";
                worksheet.Cells[2, 9].Value = "B-VOLT";
                worksheet.Cells[2, 10].Value = "R-KW";
                worksheet.Cells[2, 11].Value = "Y-KW";
                worksheet.Cells[2, 12].Value = "B-KW";
                worksheet.Cells[2, 13].Value = "R-PF";
                worksheet.Cells[2, 14].Value = "Y-PF";
                worksheet.Cells[2, 15].Value = "B-PF";
                worksheet.Cells[2, 16].Value = "ON TIME";
                worksheet.Cells[2, 17].Value = "MODE";
                worksheet.Cells[2, 18].Value = "OPENING READING";
                worksheet.Cells[2, 19].Value = "CLOSING READING";
                worksheet.Cells[2, 20].Value = "UNITS CONSUMED";
                worksheet.Cells[2, 21].Value = "PHONE NO";
                worksheet.Cells[2, 22].Value = "PHASE";

                // Add the merged data to the worksheet
                int row = 3; // Start from the second row (after headers)
                foreach (var item in reports)
                {
                    worksheet.Cells[row, 1].Value = item.UID;
                    worksheet.Cells[row, 2].Value = item.IMEI_no;
                    worksheet.Cells[row, 3].Value = item.Zone;
                    worksheet.Cells[row, 4].Value = item.Ward;
                    worksheet.Cells[row, 5].Value = item.RDateTime;
                    worksheet.Cells[row, 6].Value = item.DDateTime;
                    worksheet.Cells[row, 7].Value = item.RVolt;
                    worksheet.Cells[row, 8].Value = item.YVolt;
                    worksheet.Cells[row, 9].Value = item.BVolt;
                    worksheet.Cells[row, 10].Value = item.RKW;
                    worksheet.Cells[row, 11].Value = item.YKW;
                    worksheet.Cells[row, 12].Value = item.BKW;
                    worksheet.Cells[row, 13].Value = item.RPF;
                    worksheet.Cells[row, 14].Value = item.YPF;
                    worksheet.Cells[row, 15].Value = item.BPF;
                    worksheet.Cells[row, 16].Value = item.OnTime;
                    worksheet.Cells[row, 17].Value = item.Mode;
                    worksheet.Cells[row, 18].Value = item.OpenReading;
                    worksheet.Cells[row, 19].Value = item.CloseReading;
                    worksheet.Cells[row, 20].Value = item.UnitConsumed;
                    worksheet.Cells[row, 21].Value = item.PhoneNo;
                    worksheet.Cells[row, 22].Value = item.Phase;

                    row++;
                }

                // AutoFit columns for better visibility
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                // Create a memory stream to store the file content
                var stream = new MemoryStream();
                package.SaveAs(stream);

                // Set UID back to ViewBag
                ViewBag.UID = UID;
                ViewBag.StartDate = startDate;
                ViewBag.EndDate = endDate;

                // Return the file as a download
                stream.Position = 0;
                string fileName = $"DayReport_{startDate.Value:dd-MM-yyyy}_{endDate.Value:dd-MM-yyyy}_Of_UID{UID}.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }
    }
}
