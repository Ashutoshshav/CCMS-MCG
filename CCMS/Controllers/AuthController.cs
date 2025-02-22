using CCMS.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CCMS.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext context;
        private readonly IConfiguration _configuration;

        public AuthController(ApplicationDbContext context, IConfiguration configuration)
        {
            this.context = context;
            _configuration = configuration;
        }

        public IActionResult Login()
        {
            return View("Index"); // This will now return the Views/Login/Index.cshtml view
        }

        [HttpPost]
        [Route("auth/login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // Validate user (you would replace this with actual user validation)
            var user = context.Login_Master.SingleOrDefault(u => u.User_Name == request.Username);
            Console.WriteLine(request.Username + "reigriu");
            Console.WriteLine(user + "reigriu");

            if (user != null)
            {
                if (request.Username == user.User_Name && request.Password == user.Password)
                {
                    Console.WriteLine(user.User_Name + "reigriu");
                    // Create claims
                    var claims = new[]
                    {
                        new Claim(ClaimTypes.Name, user.User_Name)
                    };

                    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
                    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                    var token = new JwtSecurityToken(
                        issuer: _configuration["Jwt:Issuer"],
                        audience: _configuration["Jwt:Audience"],
                        claims: claims,
                        expires: DateTime.UtcNow.AddHours(1),
                        signingCredentials: creds);

                    var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
                    Console.WriteLine(token);
                    Response.Cookies.Append("Token", tokenString, new CookieOptions
                    {
                        SameSite = SameSiteMode.Strict, // Prevent CSRF attacks
                        Secure = false, // Set to true if using HTTPS
                        Expires = DateTime.UtcNow.AddHours(1) // Cookie expiration time matches JWT expiration
                    });
                    return Json(new { token = new JwtSecurityTokenHandler().WriteToken(token), message = "ture" });
                } else
                {
                    return Json(new { message = "Wrong Password" });
                }
            }
            else
            {
                return Json(new { message = "User not exist" });
            }
            return Unauthorized();
        }
    }
    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
