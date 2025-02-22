using CCMS.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using MimeKit;
using MailKit.Net.Smtp;

public class ScheduledEmailService : BackgroundService
{
    private readonly EmailService _emailService;
    private readonly IServiceScopeFactory _scopeFactory;

    public ScheduledEmailService(EmailService emailService, IServiceScopeFactory scopeFactory)
    {
        _emailService = emailService;
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.Now;
            var scheduledTime1 = new DateTime(now.Year, now.Month, now.Day, 7, 0, 0);
            var scheduledTime2 = new DateTime(now.Year, now.Month, now.Day, 19, 0, 0);

            if ((now >= scheduledTime1 && now < scheduledTime1.AddMinutes(1)) ||
                (now >= scheduledTime2 && now < scheduledTime2.AddMinutes(1)))
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var _context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    var devicesStsCount = await _context.Dashboardinfo.FirstOrDefaultAsync();

                    using var package = new ExcelPackage();
                    var worksheet1 = package.Workbook.Worksheets.Add("Not Connected Devices Report");

                    // Merge cells for the title in the first row
                    worksheet1.Cells["A1:E1"].Merge = true;
                    worksheet1.Cells["A1:E1"].Value = $"NOT CONNECTED DEVICES REPORT OF DATE {now.Date:dd MMM yyyy}";
                    worksheet1.Cells["A1:E1"].Style.Font.Bold = true;
                    worksheet1.Cells["A1:E1"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    worksheet1.Cells["A1:E1"].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;

                    // Add headers to the worksheet1
                    worksheet1.Cells[2, 1].Value = "UID";
                    worksheet1.Cells[2, 2].Value = "ZONE";
                    worksheet1.Cells[2, 3].Value = "WARD";
                    worksheet1.Cells[2, 4].Value = "LOCATION";
                    worksheet1.Cells[2, 5].Value = "STATUS";
                    
                    // Style of headers of the worksheet1
                    worksheet1.Cells[2, 1].Style.Font.Bold = true;
                    worksheet1.Cells[2, 2].Style.Font.Bold = true;
                    worksheet1.Cells[2, 3].Style.Font.Bold = true;
                    worksheet1.Cells[2, 4].Style.Font.Bold = true;
                    worksheet1.Cells[2, 5].Style.Font.Bold = true;

                    var mailExcelDataNC = await _context.SiteSummary
                        .Where(d => d.LIGHTSTS == 3)
                        .OrderBy(d => d.SNO)
                        .ToListAsync();

                    int row = 3;

                    foreach(var item in mailExcelDataNC)
                    {
                        worksheet1.Cells[row, 1].Value = item.UID;
                        worksheet1.Cells[row, 2].Value = item.Zone;
                        worksheet1.Cells[row, 3].Value = item.Ward;
                        worksheet1.Cells[row, 4].Value = item.Location;
                        worksheet1.Cells[row, 5].Value = item.LIGHTSTS == 3 ? "NOT CONNECTED" : "";

                        row++;
                    }

                    // AutoFit columns for better visibility
                    worksheet1.Cells[worksheet1.Dimension.Address].AutoFitColumns();
                    
                    var worksheet2 = package.Workbook.Worksheets.Add("ON Devices Report");

                    // Merge cells for the title in the first row
                    worksheet2.Cells["A1:E1"].Merge = true;
                    worksheet2.Cells["A1:E1"].Value = $"ON DEVICES REPORT OF DATE {now.Date:dd MMM yyyy}";
                    worksheet2.Cells["A1:E1"].Style.Font.Bold = true;
                    worksheet2.Cells["A1:E1"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    worksheet2.Cells["A1:E1"].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;

                    // Add headers to the worksheet2
                    worksheet2.Cells[2, 1].Value = "UID";
                    worksheet2.Cells[2, 2].Value = "ZONE";
                    worksheet2.Cells[2, 3].Value = "WARD";
                    worksheet2.Cells[2, 4].Value = "LOCATION";
                    worksheet2.Cells[2, 5].Value = "STATUS";
                    
                    // Style of headers of the worksheet2
                    worksheet2.Cells[2, 1].Style.Font.Bold = true;
                    worksheet2.Cells[2, 2].Style.Font.Bold = true;
                    worksheet2.Cells[2, 3].Style.Font.Bold = true;
                    worksheet2.Cells[2, 4].Style.Font.Bold = true;
                    worksheet2.Cells[2, 5].Style.Font.Bold = true;

                    var mailExcelDataON = await _context.SiteSummary
                        .Where(d => d.LIGHTSTS == 1)
                        .OrderBy(d => d.SNO)
                        .ToListAsync();

                    row = 3;

                    foreach(var item in mailExcelDataON)
                    {
                        worksheet2.Cells[row, 1].Value = item.UID;
                        worksheet2.Cells[row, 2].Value = item.Zone;
                        worksheet2.Cells[row, 3].Value = item.Ward;
                        worksheet2.Cells[row, 4].Value = item.Location;
                        worksheet2.Cells[row, 5].Value = item.LIGHTSTS == 1 ? "ON" : "";

                        row++;
                    }

                    // AutoFit columns for better visibility
                    worksheet2.Cells[worksheet2.Dimension.Address].AutoFitColumns();
                    
                    var worksheet3 = package.Workbook.Worksheets.Add("OFF Devices Report");

                    // Merge cells for the title in the first row
                    worksheet3.Cells["A1:E1"].Merge = true;
                    worksheet3.Cells["A1:E1"].Value = $"OFF DEVICES REPORT OF DATE {now.Date:dd MMM yyyy}";
                    worksheet3.Cells["A1:E1"].Style.Font.Bold = true;
                    worksheet3.Cells["A1:E1"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    worksheet3.Cells["A1:E1"].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;

                    // Add headers to the worksheet3
                    worksheet3.Cells[2, 1].Value = "UID";
                    worksheet3.Cells[2, 2].Value = "ZONE";
                    worksheet3.Cells[2, 3].Value = "WARD";
                    worksheet3.Cells[2, 4].Value = "LOCATION";
                    worksheet3.Cells[2, 5].Value = "STATUS";
                    
                    // Style of headers of the worksheet3
                    worksheet3.Cells[2, 1].Style.Font.Bold = true;
                    worksheet3.Cells[2, 2].Style.Font.Bold = true;
                    worksheet3.Cells[2, 3].Style.Font.Bold = true;
                    worksheet3.Cells[2, 4].Style.Font.Bold = true;
                    worksheet3.Cells[2, 5].Style.Font.Bold = true;

                    var mailExcelDataOFF = await _context.SiteSummary
                        .Where(d => d.LIGHTSTS == 0)
                        .OrderBy(d => d.SNO)
                        .ToListAsync();

                    row = 3;

                    foreach(var item in mailExcelDataOFF)
                    {
                        worksheet3.Cells[row, 1].Value = item.UID;
                        worksheet3.Cells[row, 2].Value = item.Zone;
                        worksheet3.Cells[row, 3].Value = item.Ward;
                        worksheet3.Cells[row, 4].Value = item.Location;
                        worksheet3.Cells[row, 5].Value = item.LIGHTSTS == 0 ? "OFF" : "";

                        row++;
                    }

                    // AutoFit columns for better visibility
                    worksheet3.Cells[worksheet3.Dimension.Address].AutoFitColumns();

                    var worksheet4 = package.Workbook.Worksheets.Add("BYPASS Devices Report");

                    // Merge cells for the title in the first row
                    worksheet4.Cells["A1:E1"].Merge = true;
                    worksheet4.Cells["A1:E1"].Value = $"BYPASS DEVICES REPORT OF DATE {now.Date:dd MMM yyyy}";
                    worksheet4.Cells["A1:E1"].Style.Font.Bold = true;
                    worksheet4.Cells["A1:E1"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    worksheet4.Cells["A1:E1"].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;

                    // Add headers to the worksheet4
                    worksheet4.Cells[2, 1].Value = "UID";
                    worksheet4.Cells[2, 2].Value = "ZONE";
                    worksheet4.Cells[2, 3].Value = "WARD";
                    worksheet4.Cells[2, 4].Value = "LOCATION";
                    worksheet4.Cells[2, 5].Value = "STATUS";
                    
                    // Style of headers of the worksheet4
                    worksheet4.Cells[2, 1].Style.Font.Bold = true;
                    worksheet4.Cells[2, 2].Style.Font.Bold = true;
                    worksheet4.Cells[2, 3].Style.Font.Bold = true;
                    worksheet4.Cells[2, 4].Style.Font.Bold = true;
                    worksheet4.Cells[2, 5].Style.Font.Bold = true;

                    var mailExcelDataBypass = await _context.SiteSummary
                        .Where(d => d.LIGHTSTS == 2)
                        .OrderBy(d => d.SNO)
                        .ToListAsync();

                    row = 3;

                    foreach(var item in mailExcelDataBypass)
                    {
                        worksheet4.Cells[row, 1].Value = item.UID;
                        worksheet4.Cells[row, 2].Value = item.Zone;
                        worksheet4.Cells[row, 3].Value = item.Ward;
                        worksheet4.Cells[row, 4].Value = item.Location;
                        worksheet4.Cells[row, 5].Value = item.LIGHTSTS == 2 ? "BYPASS" : "";

                        row++;
                    }

                    // AutoFit columns for better visibility
                    worksheet4.Cells[worksheet4.Dimension.Address].AutoFitColumns();

                    // Create a memory stream to store the file content
                    var stream = new MemoryStream();
                    package.SaveAs(stream);

                    stream.Position = 0; // Reset stream position

                    var emails = await _context.ReportingEmails.ToListAsync();

                    foreach (var recipient in emails)
                    {
                        await _emailService.SendEmailAsync(
                            recipient.EmailID,
                            "CCMS Report",
                            GenerateEmailBody((now.Date).ToString("dd MMM yyyy"), devicesStsCount.ONCOUNT, devicesStsCount.OFFCOUNT, devicesStsCount.NCCOUNT, devicesStsCount.BYPASSCOUNT, (devicesStsCount.ONCOUNT + devicesStsCount.OFFCOUNT + devicesStsCount.NCCOUNT)),
                            //$"<h3>CCMS Report - {now.Date:dd MMM yyyy} Devices Status</h3>" +
                            //$"<h4>ON_Devices : {devicesStsCount.ONCOUNT},   OFF_Devices:{devicesStsCount.OFFCOUNT},   NC_Devices : {devicesStsCount.NCCOUNT},   BYPASS_Devices : {devicesStsCount.BYPASSCOUNT}</h4>",
                            stream.ToArray(), // Pass Excel file as byte array
                            $"CCMS_Report_OF_{now.Date:dd MMM yyyy}.xlsx"
                        );
                    }
                }
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Check every 1 minute
        }
    }

    public string GenerateEmailBody(string date, int onDevices, int offDevices, int ncDevices, int bypassDevices, int total)
    {
        return $@"
    <html>
    <head>
        <style>
            body {{ font-family: Arial, sans-serif; }}
            .container {{ padding: 20px; border: 1px solid #ddd; border-radius: 8px; width: 80%; margin: auto; }}
            .header {{ font-size: 18px; font-weight: bold; color: #333; }}
            .table {{ width: 100%; border-collapse: collapse; margin-top: 10px; }}
            .table th, .table td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
            .table th {{ background-color: #007bff; color: white; }}
            .footer {{ margin-top: 20px; font-size: 12px; color: #555; }}
        </style>
    </head>
    <body>
        <div class='container'>
            <p class='header'>📅 Date: {date}</p>
            <p><strong>Devices Status</strong></p>
            <p><strong>Total Devices {total}</strong></p>
            <table class='table'>
                <tr>
                    <th>Device Type</th>
                    <th>No Of Devices</th>
                </tr>
                <tr>
                    <td>ON Devices</td>
                    <td><strong>{onDevices}</strong></td>
                </tr>
                <tr>
                    <td>OFF Devices</td>
                    <td><strong>{offDevices}</strong></td>
                </tr>
                <tr>
                    <td>NC Devices</td>
                    <td><strong>{ncDevices}</strong></td>
                </tr>
                <tr>
                    <td>Bypass Devices</td>
                    <td><strong>{bypassDevices}</strong></td>
                </tr>
            </table>
            <p class='footer'>📎 Attachment: CCMS_Report_{date.Replace(" ", "_")}.xlsx</p>
        </div>
    </body>
    </html>";
    }
}
