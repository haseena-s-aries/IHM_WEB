using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace Survey_Certification_App.Controllers
{
    public class AccountController : Controller
    {
        private readonly string jwtKey = "THIS_SHOULD_BE_A_LONG_RANDOM_SECRET_KEY";
        private readonly string issuer = "https://auth.mycompany.com";
        private readonly string audience = "my-apps";


        [HttpGet("/login-redirect")]
        public IActionResult LoginRedirect(string returnUrl = null)
        {
            var centralLogin = "https://auth.mycompany.com/login";
            var callback = Url.Action("ExternalLogin", "Account", null, Request.Scheme); // e.g. https://mvc-app/Account/ExternalLogin
            var redirectUrl = $"{centralLogin}?ReturnUrl={Uri.EscapeDataString(callback)}";
            if (!string.IsNullOrEmpty(returnUrl))
                redirectUrl += $"&returnUrl={Uri.EscapeDataString(returnUrl)}";

            return Redirect(redirectUrl);
        }

        [HttpGet]
        public IActionResult ExternalLogin(string token, string returnUrl = "/Home/AdminDashboard")
        {
            if (string.IsNullOrEmpty(token))
                return BadRequest("Missing token");

            // Validate JWT
            var handler = new JwtSecurityTokenHandler();
            try
            {
                var parameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                };

                var principal = handler.ValidateToken(token, parameters, out var validatedToken);

                // Extract claims
                var username = principal.Claims.FirstOrDefault(c => c.Type == "username")?.Value;
                var email = principal.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
                var role = principal.Claims.FirstOrDefault(c => c.Type == "role")?.Value;

                // Save to session
                HttpContext.Session.SetString("JWT", token);
                HttpContext.Session.SetString("Username", username ?? "");
                HttpContext.Session.SetString("Email", email ?? "");
                HttpContext.Session.SetString("Role", role ?? "");

                // Redirect to Home/AdminDashboard
                return Redirect(returnUrl);
            }
            catch (Exception ex)
            {
                return Unauthorized($"Invalid token: {ex.Message}");
            }
        }
    }
}
