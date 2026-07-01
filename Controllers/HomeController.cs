using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Survery_Certification_App_Consume.Models;
using Survey_Certification_App.DTOS;
using Survey_Certification_App.Models;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Text;

namespace Health_Matrix_Consuming_Side.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly HttpClient _client;
        private readonly IHttpClientFactory _clientFactory; // Declare the IHttpClientFactory

        private readonly string _apiBaseUrl;

        public HomeController(ILogger<HomeController> logger, IConfiguration config, IHttpClientFactory clientFactory)
        {
            _logger = logger;
            _clientFactory = clientFactory;

            _apiBaseUrl = config["ApiBaseUrl"] + "User";

            var clientHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };

            _client = new HttpClient(clientHandler)
            {
                BaseAddress = new Uri(config["ApiBaseUrl"])
            };
        }

        [HttpGet]
        public IActionResult ExternalLogin(string token, string? returnUrl = null)
        {
            // No token → go to normal login
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Login", "Auth");
            }

            // Decode the JWT (you already have DecodeJwt(token) in this controller)
            var claims = DecodeJwt(token);

            // Store JWT in session
            HttpContext.Session.SetString("JWT", token);
            if (claims.TryGetValue("sub", out var sub))
            {
                if (int.TryParse(sub, out var userId))
                    HttpContext.Session.SetInt32("UserId", userId);
            }

            if (claims.TryGetValue("email", out var email))
                HttpContext.Session.SetString("UserEmail", email);

            if (claims.TryGetValue("role", out var role))
                HttpContext.Session.SetString("UserRole", role);
            // Decide where to go after login
            if (string.IsNullOrEmpty(returnUrl))
            {
                returnUrl = Url.Action("Dashboard", "Home") ?? "/";
            }

            return Redirect(returnUrl);
        }

        public IDictionary<string, string> DecodeJwt(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token); // Decode the JWT token
            var claims = jwtToken?.Claims.ToDictionary(c => c.Type, c => c.Value);
            return claims ?? new Dictionary<string, string>();
        }

        public async Task<IActionResult> Index()
        {
            var jwt = HttpContext.Session.GetString("JWT");

            if (string.IsNullOrEmpty(jwt))
            {
                return RedirectToAction("Login", "Auth"); // Redirect to login if no JWT
            }

            // Optionally, decode the JWT to access claims like username, role, etc.
            var claims = DecodeJwt(jwt); // A method to decode the JWT and extract claims
            ViewData["Username"] = claims.ContainsKey("username") ? claims["username"] : "Unknown"; // Example usage of claims

            // Make the GET request using _client
            var response = await _client.GetAsync("AdminDashboard/certificate-report"); // ✅ Relative to https://localhost:7048/api/

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var model = JsonConvert.DeserializeObject<TotalCertificateCountDto>(json);
                return View(model); // Pass data to the view
            }

            return View(new TotalCertificateCountDto()); // Return empty model if API fails
        }

        public async Task<IActionResult> GetProtectedData()
        {
            var jwt = HttpContext.Session.GetString("JWT");

            if (string.IsNullOrEmpty(jwt))
            {
                return RedirectToAction("Login", "Auth"); // Redirect if no JWT token
            }

            // Create the HttpClient from _clientFactory
            var client = _clientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt); // Add JWT to Authorization header

            var response = await client.GetAsync("https://localhost:5000/api/secure-data");

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                return View("SecureData", data); // Pass the data to the view
            }

            return View("Error"); // Handle error if the request fails
        }



        public IActionResult Login()
        {
            return View();
        }


        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> AdminDashboard()
        {
            // Read JWT from session
            var jwt = HttpContext.Session.GetString("JWT");
            if (string.IsNullOrEmpty(jwt))
            {
                return RedirectToAction("Login", "Auth");
            }
            var sessionRole = HttpContext.Session.GetString("UserRole") ?? "";
            var isAdmin = string.Equals(sessionRole, "Admin", StringComparison.OrdinalIgnoreCase);

            // Create client and attach JWT
            var client = _clientFactory.CreateClient("ApiClient");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);

            // ---------- FETCH CERTIFICATE REPORT ----------
            TotalCertificateCountDto certificateCounts = new();
            var certResponse = await client.GetAsync("AdminDashboard/certificate-report");

            if (certResponse.IsSuccessStatusCode)
            {
                var certJson = await certResponse.Content.ReadAsStringAsync();
                certificateCounts = JsonConvert.DeserializeObject<TotalCertificateCountDto>(certJson);
            }

            SurveySummaryDto surveySummary = new();
            var surveyResponse = await client.GetAsync("survey/summary");

            if (surveyResponse.IsSuccessStatusCode)
            {
                var surveyJson = await surveyResponse.Content.ReadAsStringAsync();
                surveySummary = JsonConvert.DeserializeObject<SurveySummaryDto>(surveyJson) ?? new SurveySummaryDto();
            }

            // ---------- FETCH USER COUNT ----------
            int totalUsers = 0;
            List<UserDto> dashboardUsers = new();
            var userResponse = await client.GetAsync("AdminDashboard/GetAll");

            if (userResponse.IsSuccessStatusCode)
            {
                var userJson = await userResponse.Content.ReadAsStringAsync();
                var users = JsonConvert.DeserializeObject<List<UserDto>>(userJson);
                if (!isAdmin)
                {
                    users = users?
                        .Where(user => !string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                if (!isAdmin)
                {
                    users = users?
                        .Where(user => !string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                totalUsers = users?.Count ?? 0;
                dashboardUsers = users?
                    .OrderBy(user => user.FullName)
                    .ToList() ?? new List<UserDto>();
            }

            List<PendingUser> pendingUsers = new();
            var pendingResponse = await client.GetAsync("User/pending-users");
            if (pendingResponse.IsSuccessStatusCode)
            {
                var pendingJson = await pendingResponse.Content.ReadAsStringAsync();
                pendingUsers = JsonConvert.DeserializeObject<List<PendingUser>>(pendingJson)
                    ?? new List<PendingUser>();
                if (!isAdmin)
                {
                    pendingUsers = pendingUsers
                        .Where(user => !string.Equals(user.RequestedRole, "Admin", StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }
            }

            List<SavedReportViewModel> savedReports = new();
            var reportsResponse = await client.GetAsync("AdminDashboard/saved-reports");
            if (reportsResponse.IsSuccessStatusCode)
            {
                var reportsJson = await reportsResponse.Content.ReadAsStringAsync();
                savedReports = JsonConvert.DeserializeObject<List<SavedReportViewModel>>(reportsJson)
                    ?? new List<SavedReportViewModel>();
            }
            else
            {
                _logger.LogWarning(
                    "Saved reports API returned {StatusCode}. Ensure the API is running with the latest build.",
                    reportsResponse.StatusCode);
            }

            // Build ViewModel
            var viewModel = new AdminDashboardViewModel
            {
                PendingUsers = pendingUsers,
                Users = dashboardUsers,
                SavedReports = savedReports,
                CertificateCounts = certificateCounts,
                SurveySummary = surveySummary,
                TotalUsers = totalUsers
            };

            return View(viewModel);
        }
        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> UsageAnalytics(
            DateTime? from,
            DateTime? to,
            string? role,
            string? department,
            int? userId,
            string? module,
            string? activityStatus)
        {
            var jwt = HttpContext.Session.GetString("JWT");
            if (string.IsNullOrWhiteSpace(jwt))
                return RedirectToAction("Login", "Auth");

            var sessionRole = HttpContext.Session.GetString("UserRole") ?? string.Empty;
            if (!string.Equals(sessionRole, "Admin", StringComparison.OrdinalIgnoreCase))
                return Forbid();

            var client = _clientFactory.CreateClient("ApiClient");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

            var url = "usage-analytics" + BuildUsageAnalyticsQuery(
                from,
                to,
                role,
                department,
                userId,
                module,
                activityStatus);

            var model = new UsageAnalyticsViewModel
            {
                Filters = new UsageAnalyticsFilters
                {
                    From = from,
                    To = to,
                    Role = role,
                    Department = department,
                    UserId = userId,
                    Module = module,
                    ActivityStatus = activityStatus
                }
            };

            try
            {
                var response = await client.GetAsync(url);
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    model = JsonConvert.DeserializeObject<UsageAnalyticsViewModel>(json)
                        ?? model;
                    return View(model);
                }

                _logger.LogWarning("Usage analytics API returned {StatusCode}: {Body}", response.StatusCode, json);
                model.LoadError = response.StatusCode == System.Net.HttpStatusCode.Forbidden
                    ? "Usage analytics is available only for Admin users."
                    : "Usage analytics could not be loaded. Please confirm the API is running with the latest build.";
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Usage analytics load failed.");
                model.LoadError = "Usage analytics could not be loaded. Please confirm the API is running and try again.";
                return View(model);
            }
        }

        private static string BuildUsageAnalyticsQuery(
            DateTime? from,
            DateTime? to,
            string? role,
            string? department,
            int? userId,
            string? module,
            string? activityStatus)
        {
            var query = new List<string>();

            void Add(string key, string? value)
            {
                if (string.IsNullOrWhiteSpace(value) || value == "All") return;
                query.Add($"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}");
            }

            if (from.HasValue) Add("from", from.Value.ToString("O"));
            if (to.HasValue) Add("to", to.Value.ToString("O"));
            Add("role", role);
            Add("department", department);
            if (userId.HasValue && userId.Value > 0) Add("userId", userId.Value.ToString());
            Add("module", module);
            Add("activityStatus", activityStatus);

            return query.Count == 0 ? string.Empty : "?" + string.Join("&", query);
        }
        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> SavedReports()
        {
            var jwt = HttpContext.Session.GetString("JWT");
            if (string.IsNullOrWhiteSpace(jwt))
                return RedirectToAction("Login", "Auth");

            var client = _clientFactory.CreateClient("ApiClient");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
            var response = await client.GetAsync("AdminDashboard/saved-reports");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Saved reports API returned {StatusCode}.", response.StatusCode);
                ViewBag.LoadError = "Reports could not be loaded. Please confirm the API is running and try again.";
                return View(new List<SavedReportViewModel>());
            }

            var json = await response.Content.ReadAsStringAsync();
            return View(JsonConvert.DeserializeObject<List<SavedReportViewModel>>(json)
                ?? new List<SavedReportViewModel>());
        }


        [HttpGet]
        public async Task<IActionResult> SavedReportFile(string type, int id, bool download = false)
        {
            var jwt = HttpContext.Session.GetString("JWT");
            if (string.IsNullOrWhiteSpace(jwt))
                return RedirectToAction("Login", "Auth");

            var normalizedType = (type ?? string.Empty).Trim().ToLowerInvariant();
            if (normalizedType is not ("ihm" or "towage" or "seafastening" or "loadout"))
                return BadRequest("Invalid report type.");

            var path = normalizedType == "ihm"
                ? $"ihm-survey/jobs/{id}/report-pdf"
                : $"AdminDashboard/view-pdf/{Uri.EscapeDataString(normalizedType)}/{id}";

            var client = _clientFactory.CreateClient("ApiClient");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
            var response = await client.GetAsync(path);

            // Reports created before PDF persistence was added have no stored file.
            // Generate them from their saved data so they can still be viewed/downloaded.
            if (!response.IsSuccessStatusCode && normalizedType != "ihm")
            {
                response.Dispose();
                var fallbackPath =
                    $"AdminDashboard/generate-pdf?type={Uri.EscapeDataString(normalizedType)}&id={id}";
                response = await client.GetAsync(fallbackPath);
            }

            if (!response.IsSuccessStatusCode)
                return NotFound("Saved report PDF was not found.");

            var bytes = await response.Content.ReadAsByteArrayAsync();
            var fileName = $"{normalizedType}-report-{id}.pdf";
            return download
                ? File(bytes, "application/pdf", fileName)
                : File(bytes, "application/pdf");
        }

        [HttpGet]
        public IActionResult EditSavedPdf(string type, int id, string? title = null)
        {
            var normalizedType = NormalizeReportType(type);
            if (normalizedType == null)
                return BadRequest("Invalid report type.");

            return View(new PdfEditorViewModel
            {
                ReportType = normalizedType,
                ReportId = id,
                Title = string.IsNullOrWhiteSpace(title) ? "PDF Editor" : title
            });
        }

        private sealed class ApiPdfDynamicEdit
        {
            public int Id { get; set; }
            public string ReportType { get; set; } = "";
            public int ReportId { get; set; }
            public string EditType { get; set; } = "text";
            public int PageNumber { get; set; }

            public double X { get; set; }
            public double Y { get; set; }
            public double Width { get; set; }
            public double Height { get; set; }

            public string? Content { get; set; }
            public float FontSize { get; set; } = 12;
            public string FontWeight { get; set; } = "regular";
            public bool AutoBullet { get; set; }
            public bool CoverOriginal { get; set; } = true;
        }

        [HttpGet]
        public async Task<IActionResult> GetPdfEdits(string type, int id)
        {
            var normalizedType = NormalizeReportType(type);
            if (normalizedType == null)
                return BadRequest("Invalid report type.");

            var jwt = HttpContext.Session.GetString("JWT");
            if (string.IsNullOrWhiteSpace(jwt))
                return Unauthorized();

            var client = _clientFactory.CreateClient("ApiClient");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);

            var response = await client.GetAsync(
                $"pdf-editor/{normalizedType}/{id}/edits");

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode);

            var apiEdits = await response.Content
                .ReadFromJsonAsync<List<ApiPdfDynamicEdit>>() ?? new();

            var document = new PdfEditorDocument
            {
                ReportType = normalizedType,
                ReportId = id,
                Fields = apiEdits.Select(x => new PdfEditorField
                {
                    Id = x.Id.ToString(),
                    Page = x.PageNumber,
                    X = x.X,
                    Y = x.Y,
                    Width = x.Width,
                    Height = x.Height,
                    Text = x.Content ?? "",
                    FontSize = x.FontSize,
                    CoverOriginal = x.CoverOriginal,
                    EditType = x.EditType,
                    FontWeight = x.FontWeight,
                    AutoBullet = x.AutoBullet
                }).ToList()
            };

            return Json(document);
        }


        [HttpPost]
        public async Task<IActionResult> SavePdfEdits(
    [FromBody] PdfEditorDocument document)
        {
            var type = NormalizeReportType(document.ReportType);
            if (type == null || document.ReportId <= 0)
                return BadRequest("Invalid report.");

            var jwt = HttpContext.Session.GetString("JWT");
            if (string.IsNullOrWhiteSpace(jwt))
                return Unauthorized();

            var client = _clientFactory.CreateClient("ApiClient");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);

            // Replace previous saved editor fields.
            var deleteResponse = await client.DeleteAsync(
                $"pdf-editor/{type}/{document.ReportId}/edits");

            if (!deleteResponse.IsSuccessStatusCode)
                return StatusCode(
                    (int)deleteResponse.StatusCode,
                    await deleteResponse.Content.ReadAsStringAsync());

            foreach (var field in document.Fields.Where(x => x.Page > 0))
            {
                var payload = new
                {
                    editType = field.EditType,
                    pageNumber = field.Page,
                    x = Math.Clamp(field.X, 0, 1),
                    y = Math.Clamp(field.Y, 0, 1),
                    width = Math.Clamp(field.Width, 0.01, 1),
                    height = Math.Clamp(field.Height, 0.01, 1),
                    content = field.Text ?? "",
                    tableDataJson = "{}",
                    fontSize = Math.Clamp(field.FontSize, 6, 72),
                    fontWeight = field.FontWeight,
                    alignment = "left",
                    autoBullet = field.AutoBullet,
                    coverOriginal = field.CoverOriginal
                };

                var response = await client.PostAsJsonAsync(
                    $"pdf-editor/{type}/{document.ReportId}/edits",
                    payload);

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode(
                        (int)response.StatusCode,
                        await response.Content.ReadAsStringAsync());
                }
            }

            return Ok(new { message = "PDF edits saved." });
        }


        [HttpPost]
        public async Task<IActionResult> GenerateEditedPdf([FromBody] PdfEditorDocument document)
        {
            try
            {
                var normalizedType = NormalizeReportType(document.ReportType);
                if (normalizedType == null || document.ReportId <= 0)
                    return BadRequest("Invalid report.");

                var original = await FetchSavedReportPdf(normalizedType, document.ReportId);
                if (original == null)
                    return NotFound("Original PDF was not found.");

                var scriptPath = Path.Combine(
                    Directory.GetCurrentDirectory(), "PythonServices", "edit_pdf.py");
                if (!System.IO.File.Exists(scriptPath))
                    return StatusCode(500, "PDF editor Python script was not found.");

                var workFolder = Path.Combine(Path.GetTempPath(), "ihm-pdf-editor", Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(workFolder);
                var inputPath = Path.Combine(workFolder, "input.pdf");
                var editsPath = Path.Combine(workFolder, "edits.json");
                var outputPath = Path.Combine(workFolder, "output.pdf");

                try
                {
                    await System.IO.File.WriteAllBytesAsync(inputPath, original);
                    await System.IO.File.WriteAllTextAsync(
                        editsPath, JsonConvert.SerializeObject(document.Fields));

                    var apiVenvPython = Path.GetFullPath(Path.Combine(
                        Directory.GetCurrentDirectory(), "..", "..", "AurasApp_Apis",
                        "AurasApp_Apis", ".venv", "Scripts", "python.exe"));
                    var localVenvPython = Path.Combine(
                        Directory.GetCurrentDirectory(), ".venv", "Scripts", "python.exe");
                    var pythonExe = System.IO.File.Exists(localVenvPython)
                        ? localVenvPython
                        : System.IO.File.Exists(apiVenvPython) ? apiVenvPython : "python";

                    var startInfo = new ProcessStartInfo
                    {
                        FileName = pythonExe,
                        Arguments = $"\"{scriptPath}\" \"{inputPath}\" \"{editsPath}\" \"{outputPath}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var process = Process.Start(startInfo);
                    if (process == null)
                        return StatusCode(500, "Unable to start the IHM PDF editor.");

                    var standardOutput = await process.StandardOutput.ReadToEndAsync();
                    var standardError = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    if (process.ExitCode != 0 || !System.IO.File.Exists(outputPath))
                    {
                        var error = string.IsNullOrWhiteSpace(standardError)
                            ? standardOutput
                            : standardError;
                        return StatusCode(500, $"PDF generation failed: {error.Trim()}");
                    }

                    var editedBytes = await System.IO.File.ReadAllBytesAsync(outputPath);
                    document.ReportType = normalizedType;
                    await SavePdfEdits(document);
                    return File(editedBytes, "application/pdf",
                        $"{normalizedType}-report-{document.ReportId}-edited.pdf");
                }
                finally
                {
                    try
                    {
                        Directory.Delete(workFolder, recursive: true);
                    }
                    catch (IOException)
                    {
                        _logger.LogWarning("Could not remove PDF editor temporary folder {Folder}.", workFolder);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate edited PDF for {Type} report {Id}.",
                    document.ReportType, document.ReportId);
                return StatusCode(500, $"PDF generation failed: {ex.GetBaseException().Message}");
            }
        }

        private static string? NormalizeReportType(string? type)
        {
            var value = (type ?? "").Trim().ToLowerInvariant();
            return value is "ihm" or "towage" or "seafastening" or "loadout" ? value : null;
        }

        private string GetPdfEditorDataPath(string type, int id) =>
            Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "pdf-edits", $"{type}-{id}.json");

        private async Task<byte[]?> FetchSavedReportPdf(string type, int id)
        {
            var jwt = HttpContext.Session.GetString("JWT");
            if (string.IsNullOrWhiteSpace(jwt))
                return null;

            var client = _clientFactory.CreateClient("ApiClient");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
            var path = type == "ihm"
                ? $"ihm-survey/jobs/{id}/report-pdf"
                : $"AdminDashboard/view-pdf/{Uri.EscapeDataString(type)}/{id}";
            var response = await client.GetAsync(path);

            if (!response.IsSuccessStatusCode && type != "ihm")
            {
                response.Dispose();
                response = await client.GetAsync(
                    $"AdminDashboard/generate-pdf?type={Uri.EscapeDataString(type)}&id={id}");
            }

            return response.IsSuccessStatusCode
                ? await response.Content.ReadAsByteArrayAsync()
                : null;
        }



        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> Dashboard()
        {
            var jwt = HttpContext.Session.GetString("JWT");
            var email = HttpContext.Session.GetString("UserEmail");

            if (string.IsNullOrWhiteSpace(jwt) || string.IsNullOrWhiteSpace(email))
            {
                return RedirectToAction("Login", "Auth");
            }

            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            var client = _clientFactory.CreateClient("ApiClient");
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);

            TotalCertificateCountDto certificateCounts = new();

            var response = await client.GetAsync("AdminDashboard/certificate-report");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                certificateCounts = JsonConvert.DeserializeObject<TotalCertificateCountDto>(json)
                    ?? new TotalCertificateCountDto();
            }

            return View(certificateCounts);
        }

        [HttpPost]
        public async Task<IActionResult> SendOtp([FromBody] EmailDto model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.Email))
                {
                    return BadRequest(new { message = "Email is required." });
                }

                var email = model.Email.ToLower();
                var requestData = new { Email = email };

                // If _client.BaseAddress is https://localhost:7048/api/
                // And "User/send-otp" is the correct path, then do this:
                var response = await _client.PostAsJsonAsync("User/send-otp", requestData);

                if (response.IsSuccessStatusCode)
                {
                    return Ok(new { message = "OTP sent successfully." });
                }
                else
                {
                    var responseMessage = await response.Content.ReadAsStringAsync();
                    return BadRequest(new { message = "Failed to send OTP: " + responseMessage });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Server error: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> VerifyOtp([FromBody] OtpDto model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Otp))
                {
                    return BadRequest(new { message = "Email and OTP are required." });
                }

                var requestData = new { Email = model.Email.ToLower(), Otp = model.Otp };

                // ✅ Clean relative path (since BaseAddress is https://localhost:7048/api/)
                var response = await _client.PostAsJsonAsync("User/verify-otp", requestData);

                if (response.IsSuccessStatusCode)
                {
                    return Ok(new { message = "OTP verified successfully." });
                }

                var responseMessage = await response.Content.ReadAsStringAsync();
                return BadRequest(new { message = "OTP verification failed: " + responseMessage });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Server error: " + ex.Message });
            }
        }




        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register([FromBody] CreateUserDto model)
        {
            if (model == null)
                return BadRequest(new { message = "No data received" });

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Any())
                    .Select(x => new
                    {
                        Field = x.Key,
                        Errors = x.Value.Errors.Select(e => e.ErrorMessage).ToList()
                    });

                return BadRequest(new { message = "Validation failed", errors });
            }

            try
            {
                model.Email = model.Email?.ToLower();

                var json = JsonConvert.SerializeObject(model);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Use relative path (since _client.BaseAddress is already set)
                var response = await _client.PostAsync("User/register", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    // Try to parse response properly
                    dynamic apiResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);
                    string apiMessage = apiResponse?.message ?? "";

                    bool isPendingApproval = apiMessage.Contains("Awaiting admin approval", StringComparison.OrdinalIgnoreCase);

                    return Json(new
                    {
                        success = true,
                        message = isPendingApproval ? "Account created and pending admin approval." : "Registration successful!",
                        redirectUrl = Url.Action("Login", "Auth")
                    });
                }
                else
                {
                    try
                    {
                        // Try parsing error object from API
                        dynamic errorResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);
                        string errorMessage = errorResponse?.message ?? "Registration failed due to unknown error.";

                        return BadRequest(new { message = errorMessage });
                    }
                    catch
                    {
                        // Fallback plain text error
                        return BadRequest(new { message = responseContent });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal Server Error: " + ex.Message });
            }
        }



        public IActionResult LandingPage()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            return RedirectToAction("Login", "Auth");
        }

        public IActionResult TowageDemo()
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Samples", "TOWAGE.pdf");
            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            return File(fileBytes, "application/pdf");
        }

        public IActionResult SeaFasteningDemo()
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Samples", "SEAFASTENING.pdf");
            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            return File(fileBytes, "application/pdf");
        }

        public IActionResult LoadoutDemo()
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Samples", "LOADOUT.pdf");
            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            return File(fileBytes, "application/pdf");
        }





    }


}
