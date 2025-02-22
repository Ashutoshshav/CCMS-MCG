using CCMS.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CCMS.Middleware
{
    public class CheckUserInDatabaseMiddleware
    {
        private readonly RequestDelegate _next;

        public CheckUserInDatabaseMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IServiceProvider serviceProvider, ILogger<CheckUserInDatabaseMiddleware> logger)
        {
            // Skip middleware for login page or other public routes
            if (context.Request.Path.StartsWithSegments("/auth/login"))
            {
                await _next(context);
                return;
            }

            logger.LogInformation("Checking user in database...");

            using (var scope = serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Get the username from the current user's identity
                var userId = context.User?.Identity?.Name;
                if (userId != null)
                {
                    // Check if user exists in the database
                    var userExists = UserExistsInDatabase(userId, dbContext);
                    if (!userExists)
                    {
                        logger.LogWarning($"User {userId} not found in database");
                        RedirectToLogin(context); // Redirect to login page
                        return; // Stop further processing
                    }
                }
            }

            // Continue with the next middleware in the pipeline
            await _next(context);
        }

        private bool UserExistsInDatabase(string userId, ApplicationDbContext dbContext)
        {
            var user = dbContext.Login_Master.FirstOrDefault(u => u.User_Name == userId);
            return user != null;
        }

        private void RedirectToLogin(HttpContext context)
        {
            context.Response.Redirect("/auth/login");
        }
    }
}
