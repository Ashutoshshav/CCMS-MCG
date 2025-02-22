using CCMS.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using CCMS.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
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
        IssuerSigningKey = new SymmetricSecurityKey(key)
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
}, null, TimeSpan.Zero, TimeSpan.FromMinutes(3));

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

// Function to update data
static async Task Update999(ApplicationDbContext context)
{
    var imeiRecordsToUpdate = await context.IMEI_Master
            .Where(im => im.Status == "2")
            .ToListAsync();

    if (!imeiRecordsToUpdate.Any())
    {
        Console.WriteLine("No records found with Response = 9993.");
        return;
    }

    // Update the Response field to 9999
    foreach (var record in imeiRecordsToUpdate)
    {
        record.Response = "9999";
    }

    context.SaveChangesAsync();

    Console.WriteLine("Statuses updated successfully. 3-Min function executed.");
}

// Function to update data every 30 seconds
static async Task UpdateIMEIStatus(ApplicationDbContext context)
{
    // Fetch necessary data upfront
    var networkData = await context.Network.ToListAsync();
    var networkStsData = await context.NetworkSts.FirstOrDefaultAsync(); // Assuming only one row is relevant
    var imeiData = await context.IMEI_Master.ToDictionaryAsync(im => im.IMEI_no); // Preload all IMEI records

    if (networkStsData == null)
    {
        Console.WriteLine("NetworkSts data not found.");
        return;
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

    Console.WriteLine("30-second function executed.");
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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{IMEI?}");

app.Run();
