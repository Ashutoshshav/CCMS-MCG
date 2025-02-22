using CCMS.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using CCMS.Models;
using CCMS.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlServer(connectionString);
});

builder.Services.AddDbContext<ReportsDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("ReportsDB");
    options.UseSqlServer(connectionString);
});

// Configure JWT Authentication
var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]); // Secret key from appsettings.json
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"], // Issuer from appsettings.json
        ValidAudience = builder.Configuration["Jwt:Audience"], // Audience from appsettings.json
        IssuerSigningKey = new SymmetricSecurityKey(key),
        // Ensure token expiration is checked
        ClockSkew = TimeSpan.Zero,  // Optional: to immediately expire tokens at expiration
        RequireExpirationTime = true,
    };
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"Authentication failed: {context.Exception.Message}");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Console.WriteLine($"Token validated for user: {context.Principal.Identity.Name}");
            return Task.CompletedTask;
        },
        OnMessageReceived = context =>
        {
            // Retrieve the token from cookies
            var token = context.Request.Cookies["Token"];
            Console.WriteLine($"Token received: {token}");
            if (!string.IsNullOrEmpty(token))
            {
                context.Token = token; // Set token in the context
                Console.WriteLine($"Token received: {token}");
            }

            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            // Redirect to login page when unauthorized
            if (!context.Response.HasStarted)
            {
                context.Response.Redirect("/auth/login");
            }
            context.HandleResponse(); // Prevent default behavior
            return Task.CompletedTask;
        }
    };

});

// Add Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SharedAccess", policy =>
        policy.RequireRole("Operator", "Officer", "Visio")); // Shared access policy
    options.AddPolicy("OfficerAccess", policy =>
        policy.RequireRole("Officer", "Visio")); 
});
// Register EmailService as a singleton (shared instance)
builder.Services.AddSingleton<EmailService>();

// Register ScheduledEmailService as a background service
builder.Services.AddHostedService<ScheduledEmailService>();

var app = builder.Build();

// Timer to call the function every 3 minutes
var timer = new Timer(async state =>
{
    try
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Call your function here
        await Update999(context);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}, null, TimeSpan.Zero, TimeSpan.FromMinutes(15));

// Timer to call the second function every 30 seconds
var timer30Sec = new Timer(async state =>
{
    try
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Call your 30-second function
        await UpdateIMEIStatus(context);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error in 30-second timer: {ex.Message}");
    }
}, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));

// Function to update data on 10 min
static async Task Update999(ApplicationDbContext context)
{
    var networkData = await context.Network
        .Select(n => new { n.IMEI_no, n.DUR })
        .ToListAsync();

    if (!networkData.Any())
    {
        Console.WriteLine("No network data found.");
        return;
    }

    var networkStsData = await context.NetworkSts
        .Select(ns => ns.Device_NA)
        .FirstOrDefaultAsync();

    if (networkStsData == null)
    {
        Console.WriteLine("No network status data found.");
        return;
    }

    // Fetch only necessary IMEI_Master and VisLive records
    //var imeiRecords = await context.IMEI_Master
    //    .Where(im => im.Response != "9999")
    //    .ToDictionaryAsync(im => im.IMEI_no);

    var visLiveRecords = await context.VisLive
        .Where(vl => vl.P10 != 2)
        .ToDictionaryAsync(vl => vl.IMEI_no);

    var updatedIMEIs = new List<IMEI_Master>();
    var updatedVisLives = new List<VisLive>();
    var notFoundCount = 0;

    foreach (var device in networkData)
    {
        //if (imeiRecords.TryGetValue(device.IMEI_no, out var imeiRecord))
        //{
        //    imeiRecord.Response = device.ResDur > networkStsData ? "9999" : imeiRecord.Response;
        //    updatedIMEIs.Add(imeiRecord);
        //}

        if (visLiveRecords.TryGetValue(device.IMEI_no, out var visLiveRecord))
        {
            visLiveRecord.P10 = device.DUR > networkStsData ? 2 : visLiveRecord.P10;
            updatedVisLives.Add(visLiveRecord);
        }
        else
        {
            notFoundCount++;
        }
    }

    if (notFoundCount > 0)
    {
        Console.WriteLine($"{notFoundCount} IMEI records not found in IMEI_Master or VisLive.");
    }

    using var transaction = await context.Database.BeginTransactionAsync();
    try
    {
        if (updatedIMEIs.Any())
        {
            context.IMEI_Master.UpdateRange(updatedIMEIs);
        }
        if (updatedVisLives.Any())
        {
            context.VisLive.UpdateRange(updatedVisLives);
        }

        if (updatedIMEIs.Any() || updatedVisLives.Any())
        {
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            Console.WriteLine("Statuses and P10 values updated successfully. 3-Min function executed.");
        }
        else
        {
            Console.WriteLine("No records were updated.");
            await transaction.RollbackAsync();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred: {ex.Message}");
        await transaction.RollbackAsync();
    }
}

// Function to update data every 30 seconds
static async Task UpdateIMEIStatus(ApplicationDbContext context)
{
    // Fetch necessary data upfront
    var networkData = await context.Network.ToListAsync();
    var networkStsData = await context.NetworkSts.FirstOrDefaultAsync();
    var imeiData = (await context.IMEI_Master.ToListAsync()).ToLookup(im => im.IMEI_no); // Use ToListAsync and then ToLookup

    if (networkStsData == null)
    {
        Console.WriteLine("NetworkSts data not found.");
        return;
    }

    var deviceOfflineThreshold = networkStsData.DeviceOffline;
    var deviceNCThreshold = networkStsData.Device_NA;

    // Create a list to hold updated IMEI records
    var updatedIMEIs = new List<IMEI_Master>();

    foreach (var device in networkData)
    {
        // Get all IMEI records matching the IMEI_no
        if (imeiData.Contains(device.IMEI_no))
        {
            var imeiRecords = imeiData[device.IMEI_no]; // Get all records for the IMEI_no
            foreach (var imeiRecord in imeiRecords)
            {
                // Apply the same logic to all records with the same IMEI_no
                if (device.DUR > deviceNCThreshold)
                {
                    imeiRecord.Status = "3";
                }
                else
                {
                    imeiRecord.Status = device.DUR > deviceOfflineThreshold ? "2" : "1";
                }
                updatedIMEIs.Add(imeiRecord);
            }
        }
        else
        {
            Console.WriteLine($"IMEI record not found for IMEI_no: {device.IMEI_no}");
        }
    }

    // Bulk update IMEI records
    context.IMEI_Master.UpdateRange(updatedIMEIs);
    await context.SaveChangesAsync();

    Console.WriteLine("30-second function executed.");
}

//static async Task UpdateIMEIStatus(ApplicationDbContext context)
//{
//    // Fetch necessary data upfront
//    var networkData = await context.Network.ToListAsync();
//    var networkStsData = await context.NetworkSts.FirstOrDefaultAsync();
//    var imeiData = await context.IMEI_Master.ToDictionaryAsync(im => im.IMEI_no); // Preload all IMEI records

//    if (networkStsData == null)
//    {
//        Console.WriteLine("NetworkSts data not found.");
//        return;
//    }

//    var deviceOfflineThreshold = networkStsData.DeviceOffline;
//    var deviceNCThreshold = networkStsData.Device_NA;

//    // Create a list to hold updated IMEI records
//    var updatedIMEIs = new List<IMEI_Master>();

//    foreach (var device in networkData)
//    {
//        if (imeiData.TryGetValue(device.IMEI_no, out var imeiRecord))
//        {
//            if(device.DUR > deviceNCThreshold)
//            {
//                imeiRecord.Status = "3";
//                updatedIMEIs.Add(imeiRecord);
//            }
//            else
//            {
//                // Update status based on condition
//                imeiRecord.Status = device.DUR > deviceOfflineThreshold ? "2" : "1";
//                updatedIMEIs.Add(imeiRecord);
//            }
//        }
//        else
//        {
//            Console.WriteLine($"IMEI record not found for IMEI_no: {device.IMEI_no}");
//        }
//    }

//    // Bulk update IMEI records
//    context.IMEI_Master.UpdateRange(updatedIMEIs);
//    await context.SaveChangesAsync();

//    Console.WriteLine("30-second function executed.");
//}

Boolean insertFlag = true;
Boolean updateFlag = true;

var istTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
var fixedScheduleTimer = new Timer(async state =>
{
    try
    {
        var currentTimeInIST = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istTimeZone);

        // Specific call for 5:00 PM
        if (currentTimeInIST.Hour == 00 && currentTimeInIST.Minute == 2 && insertFlag)
        {
            Console.WriteLine("Call First");
            using var scope = app.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var _reportsDbContext = scope.ServiceProvider.GetRequiredService<ReportsDbContext>();

            Console.WriteLine($"[{currentTimeInIST}] Executing specific 5:00 PM task.");
            insertFlag = false;
            updateFlag = true;
            await FivePMTask(context, _reportsDbContext);
            //await SixAMTask(context);
        }

        // Schedule for 6:15 AM
        if (currentTimeInIST.Hour == 23 && currentTimeInIST.Minute == 55 && updateFlag)
        {
            Console.WriteLine("Call Second");
            using var scope = app.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var _reportsDbContext = scope.ServiceProvider.GetRequiredService<ReportsDbContext>();

            Console.WriteLine($"[{currentTimeInIST}] Executing 6 AM task.");
            insertFlag = true;
            updateFlag = false;
            await SixAMTask(context, _reportsDbContext);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error in fixed-schedule task: {ex.Message}");
    }
}, null, TimeSpan.Zero, TimeSpan.FromMinutes(1)); // Check every minute

// Function for the 5 PM task
static async Task FivePMTask(ApplicationDbContext context, ReportsDbContext _reportsDbContext)
{
    try
    {
        // Fetch all records from IMEI_Master (using ApplicationDbContext)
        var imeiData = await context.IMEI_Master
            .GroupBy(im => im.IMEI_no)
            .ToDictionaryAsync(g => g.Key, g => g.ToList()); // Store a list of records for each IMEI_no

        // Fetch all records from VisLive (using ApplicationDbContext)
        var visLiveData = await context.VisLive.ToListAsync();

        // Create a list to hold DayReport entries
        var dayReports = new List<DayReport>();

        foreach (var visLiveRecord in visLiveData)
        {
            // Check if IMEI_Master contains a matching record
            if (imeiData.TryGetValue(visLiveRecord.IMEI_no, out var imeiRecords))
            {
                foreach (var imeiRecord in imeiRecords)
                {
                    // Create a new DayReport entry based on the matching records
                    var dayReport = new DayReport
                    {
                        UID = imeiRecord.UID, // Use the UID of the current record
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
        }

        // Add and save all DayReport entries in ReportsDbContext
        if (dayReports.Any())
        {
            await _reportsDbContext.DayReport.AddRangeAsync(dayReports);
            await _reportsDbContext.SaveChangesAsync();
            Console.WriteLine("DayReport data has been successfully updated.");
        }
        else
        {
            Console.WriteLine("No matching data found between IMEI_Master and VisLive.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred: {ex.Message}");
    }
}

//static async Task FivePMTask(ApplicationDbContext context, ReportsDbContext _reportsDbContext)
//{
//    try
//    {
//        // Fetch all records from IMEI_Master (using ApplicationDbContext)
//        var imeiData = await context.IMEI_Master.ToDictionaryAsync(im => im.IMEI_no);

//        // Fetch all records from VisLive (using ApplicationDbContext)
//        var visLiveData = await context.VisLive.ToListAsync();

//        // Create a list to hold DayReport entries
//        var dayReports = new List<DayReport>();

//        foreach (var visLiveRecord in visLiveData)
//        {
//            // Check if IMEI_Master contains a matching record
//            if (imeiData.TryGetValue(visLiveRecord.IMEI_no, out var imeiRecord))
//            {
//                // Create a new DayReport entry based on the matching records
//                var dayReport = new DayReport
//                {
//                    UID = imeiRecord.UID,
//                    IMEI_no = visLiveRecord.IMEI_no,
//                    RDateTime = DateTime.Now,
//                    Zone = imeiRecord.Zone,
//                    Ward = imeiRecord.Ward,
//                    PhoneNo = imeiRecord.SIM_No,
//                    Phase = imeiRecord.Phase,
//                    OpenReading = visLiveRecord.P7,
//                };

//                dayReports.Add(dayReport);
//            }
//        }

//        // Add and save all DayReport entries in ReportsDbContext
//        if (dayReports.Any())
//        {
//            await _reportsDbContext.DayReport.AddRangeAsync(dayReports);
//            await _reportsDbContext.SaveChangesAsync();
//            Console.WriteLine("DayReport data has been successfully updated.");
//        }
//        else
//        {
//            Console.WriteLine("No matching data found between IMEI_Master and VisLive.");
//        }
//    }
//    catch (Exception ex)
//    {
//        Console.WriteLine($"An error occurred: {ex.Message}");
//    }
//}

// Function for the 6 AM task
//static async Task SixAMTask(ApplicationDbContext context, ReportsDbContext _reportsDbContext)
//{
//    try
//    {
//        var today = DateTime.UtcNow.Date;

//        // Use AsNoTracking to optimize read-only queries
//        var imeiData = await context.IMEI_Master.AsNoTracking().ToDictionaryAsync(im => im.IMEI_no);
//        var visLiveData = await context.VisLive.AsNoTracking().ToListAsync();

//        // Fetch all DayReport records for the given date in a single query
//        var existingReports = await _reportsDbContext.DayReport
//            .Where(dr => EF.Functions.DateDiffDay(dr.RDateTime, today) == 0)
//            .ToDictionaryAsync(dr => dr.IMEI_no);

//        // Fetch all the required average RKW in one batch query
//        var avgRKWs = await _reportsDbContext.LiveReport
//            .Where(lr => EF.Functions.DateDiffDay(lr.DateTime, today) == 0)
//            .GroupBy(lr => lr.IMEI_no)
//            .Select(g => new
//            {
//                IMEI_no = g.Key,
//                AvgRKW = g.Average(lr => (double?)lr.RKW) ?? 0
//            })
//            .ToDictionaryAsync(g => g.IMEI_no, g => g.AvgRKW);

//        var dayReports = new List<DayReport>();

//        foreach (var visLiveRecord in visLiveData)
//        {
//            if (imeiData.TryGetValue(visLiveRecord.IMEI_no, out var imeiRecord))
//            {
//                existingReports.TryGetValue(visLiveRecord.IMEI_no, out var existingDayReport);

//                if (!avgRKWs.ContainsKey(visLiveRecord.IMEI_no) || avgRKWs[visLiveRecord.IMEI_no] == 0)
//                {
//                    Console.WriteLine($"No valid average RKW for IMEI: {visLiveRecord.IMEI_no}");
//                    continue;
//                }

//                var avgPower = avgRKWs[visLiveRecord.IMEI_no];
//                var ontimeDecimal = (decimal)(visLiveRecord.P7 - (existingDayReport?.OpenReading ?? 0)) / (decimal)avgPower;
//                if (ontimeDecimal < 0) ontimeDecimal = 0;

//                var ontimeHours = Math.Floor((double)ontimeDecimal);
//                var ontimeMinutes = (ontimeDecimal - (decimal)ontimeHours) * 60;

//                if (existingDayReport != null)
//                {
//                    existingDayReport.OnTime = $"{ontimeHours:00}:{ontimeMinutes:00}";
//                    existingDayReport.CloseReading = visLiveRecord.P7;
//                    existingDayReport.DDateTime = DateTime.Now;
//                    existingDayReport.Mode = visLiveRecord.P11 == 1 ? "MANUAL" : "AUTO";
//                    existingDayReport.RVolt = visLiveRecord.P1;
//                    existingDayReport.YVolt = visLiveRecord.P4;
//                    existingDayReport.BVolt = visLiveRecord.P6;
//                    existingDayReport.RKW = visLiveRecord.P3;
//                    existingDayReport.YKW = visLiveRecord.P13;
//                    existingDayReport.BKW = visLiveRecord.P14;
//                    existingDayReport.RPF = visLiveRecord.P17;
//                    existingDayReport.YPF = visLiveRecord.P18;
//                    existingDayReport.BPF = visLiveRecord.P19;
//                    existingDayReport.UnitConsumed = visLiveRecord.P7 - existingDayReport.OpenReading;
//                }
//                else
//                {
//                    var dayReport = new DayReport
//                    {
//                        UID = imeiRecord.UID,
//                        IMEI_no = visLiveRecord.IMEI_no,
//                        RDateTime = DateTime.Now,
//                        Zone = imeiRecord.Zone,
//                        Ward = imeiRecord.Ward,
//                        PhoneNo = imeiRecord.SIM_No,
//                        Phase = imeiRecord.Phase,
//                        OpenReading = visLiveRecord.P7,
//                        OnTime = "00:00",
//                        CloseReading = visLiveRecord.P7,
//                        DDateTime = DateTime.Now,
//                        Mode = visLiveRecord.P11 == 1 ? "MANUAL" : "AUTO",
//                        RVolt = visLiveRecord.P1,
//                        YVolt = visLiveRecord.P4,
//                        BVolt = visLiveRecord.P6,
//                        RKW = visLiveRecord.P3,
//                        YKW = visLiveRecord.P13,
//                        BKW = visLiveRecord.P14,
//                        RPF = visLiveRecord.P17,
//                        YPF = visLiveRecord.P18,
//                        BPF = visLiveRecord.P19,
//                    };

//                    dayReports.Add(dayReport);
//                }
//            }
//        }

//        if (dayReports.Any())
//        {
//            await _reportsDbContext.DayReport.AddRangeAsync(dayReports);
//        }

//        await _reportsDbContext.SaveChangesAsync();
//        Console.WriteLine("DayReport data has been successfully updated.");
//    }
//    catch (Exception ex)
//    {
//        Console.WriteLine($"An error occurred: {ex.Message}");
//    }
//}

static async Task SixAMTask(ApplicationDbContext context, ReportsDbContext _reportsDbContext)
{
    try
    {
        var today = DateTime.UtcNow.Date;

        // Fetch IMEI data, including UID for each IMEI_no
        var imeiData = await context.IMEI_Master
            .GroupBy(im => im.IMEI_no)
            .Select(g => new
            {
                IMEI_no = g.Key,
                UIDs = g.ToList() // Collect all UIDs for each IMEI_no
            })
            .ToListAsync();

        // Fetch VisLive data
        var visLiveData = await context.VisLive.AsNoTracking().ToListAsync();

        // Fetch existing reports for today
        var existingReports = await _reportsDbContext.DayReport
            .Where(dr => EF.Functions.DateDiffDay(dr.RDateTime, today) == 0)
            .ToDictionaryAsync(dr => new { dr.IMEI_no, dr.UID });

        // Fetch average RKW for each IMEI_no for today
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
            if (imeiData.Any(im => im.IMEI_no == visLiveRecord.IMEI_no))
            {
                var imeiRecordGroup = imeiData.First(im => im.IMEI_no == visLiveRecord.IMEI_no);

                foreach (var imeiRecord in imeiRecordGroup.UIDs)
                {
                    // Get any existing report for the current IMEI_no and UID
                    existingReports.TryGetValue(new { IMEI_no = visLiveRecord.IMEI_no, UID = imeiRecord.UID }, out var existingDayReport);

                    // Skip if no valid average RKW for this IMEI_no
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
                        // Update the existing day report
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

                        // Mark for update
                        _reportsDbContext.DayReport.Update(existingDayReport);
                    }
                    else
                    {
                        // Create a new day report if no existing record is found
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
        }

        // Add new reports and update existing ones
        if (dayReports.Any())
        {
            await _reportsDbContext.DayReport.AddRangeAsync(dayReports);
        }

        // Save the changes to the database
        await _reportsDbContext.SaveChangesAsync();
        Console.WriteLine("DayReport data has been successfully updated.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred: {ex.Message}");
    }
}

// Configure the HTTP request pipeline.</div></div>
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<CheckUserInDatabaseMiddleware>();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Chart}/{action=Index}");

app.Run();
