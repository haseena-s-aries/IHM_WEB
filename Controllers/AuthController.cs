using Newtonsoft.Json;
using Survery_Certification_App_Consume.Models;
using Survey_Certification_App.DTOS;
using System.Text;

namespace YourMvcApp.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using System.Net.Http;
    using System.Net.Http.Json;
    using System.Threading.Tasks;

    public class AuthController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl;

        public AuthController(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _apiBaseUrl = config["ApiBaseUrl"] + "User"; // e.g., "https://localhost:7048/api/User"
            _httpClient = httpClientFactory.CreateClient("ApiClient");
        }

        [HttpGet]
        public async Task<IActionResult> GetNotificationCount()
        {
            var response = await _httpClient.GetAsync("User/pending-users");

            if (!response.IsSuccessStatusCode)
                return Json(new { count = 0 });

            var users = await response.Content.ReadFromJsonAsync<List<PendingUser>>();
            int pendingCount = users?.Count ?? 0;

            return Json(new { count = pendingCount });
        }


        [HttpGet]
        public async Task<IActionResult> PendingUsers()
        {
            var response = await _httpClient.GetAsync("User/pending-users");

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.PendingCount = 0;
                return View(new List<PendingUser>());
            }

            var users = await response.Content.ReadFromJsonAsync<List<PendingUser>>();
            ViewBag.PendingCount = users?.Count ?? 0;
            return View(users);
        }



        // POST: /Auth/ApproveUser
        // POST: /Auth/ApproveUser
        [HttpPost]
        public async Task<IActionResult> ApproveUser(int id, string? returnUrl = null)
        {
            try
            {
                var response = await _httpClient.PostAsync($"User/approve-user/{id}", null);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                    TempData["Success"] = "User approved successfully!";
                else
                    TempData["Error"] = string.IsNullOrWhiteSpace(responseBody)
                        ? "Approval failed. Try again."
                        : responseBody;
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Approval failed: {ex.Message}";
            }

            return RedirectAfterApproval(returnUrl);
        }

        // POST: /Auth/RejectUser
        [HttpPost]
        public async Task<IActionResult> RejectUser(int id, string? returnUrl = null)
        {
            try
            {
                var response = await _httpClient.PostAsync($"User/reject-user/{id}", null);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                    TempData["Success"] = "User rejected successfully.";
                else
                    TempData["Error"] = string.IsNullOrWhiteSpace(responseBody)
                        ? "Failed to reject user."
                        : responseBody;
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to reject user: {ex.Message}";
            }

            return RedirectAfterApproval(returnUrl);
        }

        private IActionResult RedirectAfterApproval(string? returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToAction(nameof(PendingUsers));
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null || userId == 0)
            {
                TempData["ErrorMessage"] = "User not logged in.";
                return RedirectToAction("Login", "Auth");
            }

            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/GetProfile/{userId}");
            if (response.IsSuccessStatusCode)
            {
                var jsonData = await response.Content.ReadAsStringAsync();
                var userProfile = JsonConvert.DeserializeObject<ProfileDto>(jsonData);


                // ✅ Step 1: Pass role to view
                ViewBag.UserRole = HttpContext.Session.GetString("UserRole");

                return View(userProfile);
            }

            TempData["ErrorMessage"] = "Failed to load profile.";
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null || userId == 0)
                return Json(new { success = false, message = "User not logged in." });

            var response = await _httpClient.GetAsync($"User/GetProfile/{userId}");

            if (response.IsSuccessStatusCode)
            {
                var jsonData = await response.Content.ReadAsStringAsync();
                var user = JsonConvert.DeserializeObject<ProfileDto>(jsonData);
                return Json(new { success = true, data = user });
            }

            return Json(new { success = false, message = "Failed to load profile." });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile([FromBody] ProfileDto model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Validation failed" });

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null || userId == 0)
                return Json(new { success = false, message = "User not logged in." });

            model.UserId = userId.Value;

            try
            {
                var jsonContent = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("User/UpdateProfile", jsonContent);

                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                    return Json(new { success = true, message = "Profile updated successfully!" });
                else
                    return Json(new { success = false, message = "Failed to update profile.", details = responseContent });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Exception occurred: " + ex.Message });
            }
        }

        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Login()
        {
            HttpContext.Session.Clear();

            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginUserDto model)
        {
            if (!ModelState.IsValid)
                return View(model);

            model.Email = model.Email?.Trim();

            var jsonContent = new StringContent(
                JsonConvert.SerializeObject(model),
                Encoding.UTF8,
                "application/json"
            );

            HttpResponseMessage response;

            try
            {
                response = await _httpClient.PostAsync("User/login", jsonContent);
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError("", $"Login API is not reachable: {ex.Message}");
                return View(model);
            }

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var userResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);

                int userId = userResponse.user.id;
                string fullName = userResponse.user.fullName;
                string email = userResponse.user.email;
                string role = userResponse.user.role;
                string token = userResponse.token ?? "";

                HttpContext.Session.SetInt32("UserId", userId);
                HttpContext.Session.SetString("UserFullName", fullName);
                HttpContext.Session.SetString("UserEmail", email);
                HttpContext.Session.SetString("UserRole", role);
                HttpContext.Session.SetString("JWT", token);

                if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(role, "Manager", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToAction("AdminDashboard", "Home");
                }

                if (string.Equals(role, "User", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToAction("Dashboard", "Home");
                }

                if (string.Equals(role, "Surveyor", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToAction("Index", "Home");
                }

                return RedirectToAction("Index", "Home");
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            var errorMessage = GetApiErrorMessage(errorContent);

            ModelState.AddModelError("", string.IsNullOrWhiteSpace(errorMessage)
                ? $"Login failed ({(int)response.StatusCode} {response.ReasonPhrase})."
                : errorMessage);

            return View(model);
        }

        private static string GetApiErrorMessage(string responseContent)
        {
            if (string.IsNullOrWhiteSpace(responseContent))
                return "";

            try
            {
                var token = Newtonsoft.Json.Linq.JToken.Parse(responseContent);

                var nestedErrors = token["errors"]?
                    .SelectMany(e => e["errors"] ?? new Newtonsoft.Json.Linq.JArray())
                    .Select(e => e.ToString())
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .ToList();

                if (nestedErrors != null && nestedErrors.Any())
                    return string.Join(" ", nestedErrors);

                var dictionaryErrors = token["errors"]?
                    .Children<Newtonsoft.Json.Linq.JProperty>()
                    .SelectMany(x => x.Value.Select(v => v.ToString()))
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList();

                if (dictionaryErrors != null && dictionaryErrors.Any())
                    return string.Join(" ", dictionaryErrors);

                var message = token["message"]?.ToString();
                if (!string.IsNullOrWhiteSpace(message))
                {
                    var marker = message.IndexOf("{", StringComparison.Ordinal);
                    if (marker >= 0)
                    {
                        var nestedJson = message.Substring(marker);
                        try
                        {
                            var nested = Newtonsoft.Json.Linq.JToken.Parse(nestedJson);
                            var nestedMessage = nested["message"]?.ToString();
                            if (!string.IsNullOrWhiteSpace(nestedMessage))
                                return nestedMessage;
                        }
                        catch
                        {
                        }
                    }

                    return message;
                }

                return responseContent;
            }
            catch
            {
                return responseContent;
            }
        }


        [HttpGet]
        public IActionResult ChangePassword()
        {
            var role = HttpContext.Session.GetString("UserRole"); // ✅ Get from session
            ViewBag.UserRole = role; // ✅ Pass to view
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangepassDto model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.UserRole = HttpContext.Session.GetString("UserRole");
                return View(model);
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null || userId == 0)
            {
                TempData["ErrorMessage"] = "User not logged in.";
                return RedirectToAction("Login");
            }

            var requestModel = new ChangepassDto
            {
                UserId = userId.Value,
                CurrentPassword = model.CurrentPassword,
                NewPassword = model.NewPassword,
                ConfirmPassword = model.ConfirmPassword
            };

            var jsonContent = new StringContent(JsonConvert.SerializeObject(requestModel), Encoding.UTF8, "application/json");

            // ✅ _apiBaseUrl already includes "User"
            var response = await _httpClient.PostAsync($"{_apiBaseUrl}/ChangePassword", jsonContent);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Password changed successfully!";
                return RedirectToAction("ChangePassword");
            }

            var errorMessage = await response.Content.ReadAsStringAsync();
            TempData["ErrorMessage"] = errorMessage;
            ViewBag.UserRole = HttpContext.Session.GetString("UserRole");

            return View(model);
        }

        public IActionResult ForgotPassword() => View();

        [HttpPost]
        public async Task<IActionResult> SendOtp([FromBody] ForgetPassDto model)
        {
            if (string.IsNullOrEmpty(model.Email))
                return Json(new { success = false, message = "Email is required." });

            var jsonContent = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync("User/ForgotPassword", jsonContent);
                var responseText = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                    return Json(new { success = true, message = "OTP sent successfully." });

                return Json(new { success = false, message = GetApiErrorMessage(responseText) });
            }
            catch (HttpRequestException ex)
            {
                return Json(new { success = false, message = $"Forgot password API is not reachable: {ex.Message}" });
            }
        }

        // ✅ 2️⃣ Verify OTP
        [HttpPost]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto model)
        {
            if (string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.OTP))
                return Json(new { success = false, message = "Email and OTP are required." });

            var jsonContent = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync("User/VerifyOtp", jsonContent);
                var responseText = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                    return Json(new { success = true, message = "OTP verified successfully." });

                return Json(new { success = false, message = GetApiErrorMessage(responseText) });
            }
            catch (HttpRequestException ex)
            {
                return Json(new { success = false, message = $"OTP verification API is not reachable: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ResendOtp([FromBody] ResendDto model)
        {
            if (string.IsNullOrEmpty(model.Email))
                return Json(new { success = false, message = "Email is required." });

            var jsonContent = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync("User/ResendOtp", jsonContent);
                var responseText = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                    return Json(new { success = true, message = "New OTP sent successfully." });

                return Json(new { success = false, message = GetApiErrorMessage(responseText) });
            }
            catch (HttpRequestException ex)
            {
                return Json(new { success = false, message = $"Resend OTP API is not reachable: {ex.Message}" });
            }
        }


        [HttpPost]
        public async Task<IActionResult> SendRegistrationOtp([FromBody] ForgetPassDto model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Email))
                return Json(new { success = false, message = "Email is required." });

            model.Email = model.Email.Trim().ToLowerInvariant();

            var jsonContent = new StringContent(
                JsonConvert.SerializeObject(model),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync("User/send-otp", jsonContent);
            var responseText = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
                return Json(new { success = true, message = "OTP sent successfully." });

            return Json(new { success = false, message = GetApiErrorMessage(responseText) });
        }


        [HttpPost]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto model)
        {
            if (string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.NewPassword))
                return Json(new { success = false, message = "Email and new password are required." });

            var jsonContent = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync("User/ResetPassword", jsonContent);
                var responseText = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                    return Json(new { success = true, message = "Password reset successfully." });

                return Json(new { success = false, message = GetApiErrorMessage(responseText) });
            }
            catch (HttpRequestException ex)
            {
                return Json(new { success = false, message = $"Reset password API is not reachable: {ex.Message}" });
            }
        }
    }
}
