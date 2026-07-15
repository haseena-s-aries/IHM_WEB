using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Survery_Certification_App_Consume.Filters;
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

            claims.TryGetValue("role", out var role);
            claims.TryGetValue("roleName", out var roleName);
            var effectiveRole = !string.IsNullOrWhiteSpace(roleName)
                ? roleName.Trim()
                : role?.Trim() ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(effectiveRole))
                HttpContext.Session.SetString("UserRole", effectiveRole);
            if (claims.TryGetValue("projectId", out var projectId))
                HttpContext.Session.SetString("ProjectId", projectId);

            if (claims.TryGetValue("projectName", out var projectName))
                HttpContext.Session.SetString("ProjectName", projectName);

            if (!string.IsNullOrWhiteSpace(roleName))
                HttpContext.Session.SetString("RoleName", roleName);
            // Decide where to go after login
            if (IsClientRole(effectiveRole))
            {
                returnUrl = Url.Action(nameof(ClientDashboard), "Home") ?? "/Home/ClientDashboard";
            }
            else if (string.IsNullOrEmpty(returnUrl))
            {
                returnUrl = Url.Action(nameof(AdminDashboard), "Home") ?? "/";
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

        private static bool IsClientRole(string? role) =>
            string.Equals(role?.Trim(), "Client", StringComparison.OrdinalIgnoreCase);

        [BlockClientAccess]
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
            ViewData["FullName"] =
                HttpContext.Session.GetString("UserFullName")
                ?? (claims.ContainsKey("fullName") ? claims["fullName"] : null)
                ?? (claims.ContainsKey("name") ? claims["name"] : null)
                ?? HttpContext.Session.GetString("Username")
                ?? "Surveyor";
            TotalCertificateCountDto model = new();

            var role = HttpContext.Session.GetString("UserRole") ?? "";
            ViewBag.ProjectId = HttpContext.Session.GetString("ProjectId") ?? "";
            ViewBag.ProjectName = HttpContext.Session.GetString("ProjectName") ?? "";
            ViewBag.RoleName = HttpContext.Session.GetString("RoleName") ?? role;
            var isSurveyor = role.Equals("Surveyor", StringComparison.OrdinalIgnoreCase);

            if (isSurveyor)
            {
                try
                {
                    var client = _clientFactory.CreateClient("ApiClient");
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
                    var response = await client.GetAsync("survey/dashboard/my-summary");
                    var json = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var summary = JsonConvert.DeserializeObject<SurveyorDashboardSummaryDto>(json)
                            ?? new SurveyorDashboardSummaryDto();
                        model.LiveJobs = summary.LiveJobs;
                        model.CompletedJobs = summary.CompletedJobs;
                    }
                    else
                    {
                        _logger.LogWarning("Surveyor dashboard summary API returned {StatusCode}: {Body}",
                            response.StatusCode,
                            json);
                        ViewBag.LoadError = "Survey dashboard summary could not be loaded.";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load surveyor dashboard summary.");
                    ViewBag.LoadError = "Survey dashboard summary could not be loaded.";
                }
            }

            return View(model); // Return empty model if API fails
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
        [BlockClientAccess]
        public async Task<IActionResult> AdminDashboard()
        {
            // Read JWT from session
            var jwt = HttpContext.Session.GetString("JWT");
            if (string.IsNullOrEmpty(jwt))
            {
                return RedirectToAction("Login", "Auth");
            }
            var sessionRole = HttpContext.Session.GetString("UserRole") ?? "";
            if (string.Equals(sessionRole, "Client", StringComparison.OrdinalIgnoreCase))
                return RedirectToAction(nameof(ClientDashboard));
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

            List<ProjectOption> manageableProjects = new();
            var projectsResponse = await client.GetAsync("AdminDashboard/manageable-projects");
            if (projectsResponse.IsSuccessStatusCode)
            {
                var projectsJson = await projectsResponse.Content.ReadAsStringAsync();
                manageableProjects = JsonConvert.DeserializeObject<List<ProjectOption>>(projectsJson)
                    ?? new List<ProjectOption>();
            }

            List<ASCENT.Models.VesselSummaryDto> fleetSummary = new();
            string? fleetErrorMessage = null;
            var vesselResponse = await client.GetAsync("vessels");
            var vesselBody = await vesselResponse.Content.ReadAsStringAsync();

            if (vesselResponse.IsSuccessStatusCode)
            {
                var vessels = JsonConvert.DeserializeObject<List<VesselDto>>(vesselBody)
                    ?? new List<VesselDto>();

                fleetSummary = vessels
                    .OrderByDescending(vessel => vessel.Id)
                    .Take(4)
                    .Select(vessel => new ASCENT.Models.VesselSummaryDto
                    {
                        Id = vessel.Id,
                        Name = vessel.VesselName,
                        Location = !string.IsNullOrWhiteSpace(vessel.PortOfRegistry)
                            ? vessel.PortOfRegistry
                            : !string.IsNullOrWhiteSpace(vessel.Flag)
                                ? vessel.Flag
                                : vessel.VesselType,
                        Status = string.IsNullOrWhiteSpace(vessel.Status) ? "Active" : vessel.Status
                    })
                    .ToList();
            }
            else
            {
                fleetErrorMessage = string.IsNullOrWhiteSpace(vesselBody)
                    ? $"Unable to load vessels ({(int)vesselResponse.StatusCode})."
                    : vesselBody;
                _logger.LogWarning(
                    "Vessel API returned {StatusCode} for dashboard fleet: {Body}",
                    vesselResponse.StatusCode,
                    vesselBody);
            }

            // Build ViewModel
            var viewModel = new AdminDashboardViewModel
            {
                PendingUsers = pendingUsers,
                Users = dashboardUsers,
                SavedReports = savedReports,
                CertificateCounts = certificateCounts,
                SurveySummary = surveySummary,
                ManageableProjects = manageableProjects,
                TotalUsers = totalUsers,
                FleetSummary = fleetSummary,
                FleetErrorMessage = fleetErrorMessage
            };

            return View(viewModel);
        }
        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        [BlockClientAccess]
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
        public Task<IActionResult> ClientDashboard()
        {
            if (!IsClientRole(HttpContext.Session.GetString("UserRole")) &&
                !IsClientRole(HttpContext.Session.GetString("RoleName")))
                return Task.FromResult<IActionResult>(RedirectToAction(nameof(AdminDashboard)));

            return LoadClientAssignedReportsView();
        }

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public Task<IActionResult> AssignedReports() => ClientDashboard();

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> SavedReports()
        {
            var jwt = HttpContext.Session.GetString("JWT");
            if (string.IsNullOrWhiteSpace(jwt))
                return RedirectToAction("Login", "Auth");

            if (IsClientRole(HttpContext.Session.GetString("UserRole")) ||
                IsClientRole(HttpContext.Session.GetString("RoleName")))
                return await LoadClientAssignedReportsView();

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

        private async Task<IActionResult> LoadClientAssignedReportsView()
        {
            var jwt = HttpContext.Session.GetString("JWT");
            if (string.IsNullOrWhiteSpace(jwt))
                return RedirectToAction("Login", "Auth");

            var client = _clientFactory.CreateClient("ApiClient");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
            var response = await client.GetAsync("report-assignments/my-reports");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var assignedReports = JsonConvert.DeserializeObject<List<AssignedReportViewModel>>(json)
                    ?? new List<AssignedReportViewModel>();

                var reports = assignedReports.Select(report => new SavedReportViewModel
                {
                    Id = report.ReportId,
                    ReportType = report.ReportType,
                    Title = report.Title,
                    VesselName = report.VesselName,
                    Reference = report.Reference,
                    Status = report.Status,
                    CreatedBy = report.CreatedBy,
                    SavedAt = report.AssignedAt
                }).ToList();

                return View("SavedReports", reports);
            }

            var primaryStatus = response.StatusCode;
            var primaryBody = await response.Content.ReadAsStringAsync();
            _logger.LogWarning(
                "Assigned reports API returned {StatusCode}: {Body}",
                primaryStatus,
                primaryBody);

            // Fallback for older/running API builds. This endpoint also returns Client-assigned reports only.
            var fallbackResponse = await client.GetAsync("AdminDashboard/saved-reports");
            if (fallbackResponse.IsSuccessStatusCode)
            {
                var fallbackJson = await fallbackResponse.Content.ReadAsStringAsync();
                var fallbackReports = JsonConvert.DeserializeObject<List<SavedReportViewModel>>(fallbackJson)
                    ?? new List<SavedReportViewModel>();

                return View("SavedReports", fallbackReports);
            }

            var fallbackBody = await fallbackResponse.Content.ReadAsStringAsync();
            _logger.LogWarning(
                "Assigned reports fallback API returned {StatusCode}: {Body}",
                fallbackResponse.StatusCode,
                fallbackBody);

            ViewBag.LoadError = primaryStatus == System.Net.HttpStatusCode.Forbidden
                ? "Your Client account is not authorized for assigned reports. Please confirm the user is approved as Client."
                : $"Assigned reports could not be loaded. API returned {(int)primaryStatus} from assigned reports and {(int)fallbackResponse.StatusCode} from saved reports.";

            return View("SavedReports", new List<SavedReportViewModel>());
        }

        [HttpGet]
        [BlockClientAccess]
        public async Task<IActionResult> ReportClients(string projectType, string? search = null)
        {
            var jwt = HttpContext.Session.GetString("JWT");
            if (string.IsNullOrWhiteSpace(jwt))
                return Unauthorized();

            var client = _clientFactory.CreateClient("ApiClient");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
            var url = $"report-assignments/clients?projectType={Uri.EscapeDataString(projectType ?? string.Empty)}";
            if (!string.IsNullOrWhiteSpace(search))
                url += $"&search={Uri.EscapeDataString(search)}";

            var response = await client.GetAsync(url);

            var body = await response.Content.ReadAsStringAsync();
            return JsonProxyResult(response, body);
        }

        [HttpGet]
        [BlockClientAccess]
        public async Task<IActionResult> ReportAssignments(string type, int id)
        {
            var jwt = HttpContext.Session.GetString("JWT");
            if (string.IsNullOrWhiteSpace(jwt))
                return Unauthorized();

            var client = _clientFactory.CreateClient("ApiClient");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
            var response = await client.GetAsync(
                $"report-assignments/{Uri.EscapeDataString(type ?? string.Empty)}/{id}");

            var body = await response.Content.ReadAsStringAsync();
            return JsonProxyResult(response, body);
        }

        [HttpPost]
        [BlockClientAccess]
        public async Task<IActionResult> AssignReportToClient([FromBody] AssignReportToClientRequest request)
        {
            var jwt = HttpContext.Session.GetString("JWT");
            if (string.IsNullOrWhiteSpace(jwt))
                return Unauthorized();

            var client = _clientFactory.CreateClient("ApiClient");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

            var payload = JsonConvert.SerializeObject(request);
            var response = await client.PostAsync(
                "report-assignments",
                new StringContent(payload, Encoding.UTF8, "application/json"));

            var body = await response.Content.ReadAsStringAsync();
            return JsonProxyResult(response, body);
        }

        [HttpPost]
        [BlockClientAccess]
        public async Task<IActionResult> RevokeReportAssignment(int assignmentId)
        {
            var jwt = HttpContext.Session.GetString("JWT");
            if (string.IsNullOrWhiteSpace(jwt))
                return Unauthorized();

            var client = _clientFactory.CreateClient("ApiClient");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
            var response = await client.PostAsync(
                $"report-assignments/{assignmentId}/revoke",
                new StringContent("{}", Encoding.UTF8, "application/json"));

            var body = await response.Content.ReadAsStringAsync();
            return JsonProxyResult(response, body);
        }

        private static ContentResult JsonProxyResult(HttpResponseMessage response, string body)
        {
            return new ContentResult
            {
                StatusCode = (int)response.StatusCode,
                ContentType = "application/json",
                Content = string.IsNullOrWhiteSpace(body) ? "{}" : body
            };
        }


        [HttpGet]
        public async Task<IActionResult> SavedReportFile(string type, int id, bool download = false)
        {
            var jwt = HttpContext.Session.GetString("JWT");
            if (string.IsNullOrWhiteSpace(jwt))
                return RedirectToAction("Login", "Auth");

            var normalizedType = (type ?? string.Empty).Trim().ToLowerInvariant();
            if (normalizedType is not ("ihm" or "exsurvey" or "towage" or "seafastening" or "loadout"))
                return BadRequest("Invalid report type.");

            if ((IsClientRole(HttpContext.Session.GetString("UserRole")) ||
                 IsClientRole(HttpContext.Session.GetString("RoleName"))) &&
                normalizedType is not ("ihm" or "exsurvey"))
                return Forbid();

            var path = normalizedType == "ihm"
                ? $"ihm-survey/jobs/{id}/report-pdf"
                : normalizedType == "exsurvey"
                    ? $"ex-survey/jobs/{id}/report"
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
        [BlockClientAccess]
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
            public string Alignment { get; set; } = "left";
            public bool AutoBullet { get; set; }
            public bool CoverOriginal { get; set; } = true;
        }

        [HttpGet]
        [BlockClientAccess]
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
                    Alignment = string.IsNullOrWhiteSpace(x.Alignment) ? "left" : x.Alignment,
                    AutoBullet = x.AutoBullet
                }).ToList()
            };

            return Json(document);
        }


        [HttpPost]
        [BlockClientAccess]
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
                    alignment = string.IsNullOrWhiteSpace(field.Alignment) ? "left" : field.Alignment,
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
        public async Task<IActionResult> RegenerateSavedPdf(string type, int id)
        {
            var normalizedType = NormalizeReportType(type);
            if (normalizedType == null || id <= 0)
                return BadRequest("Invalid report.");

            var jwt = HttpContext.Session.GetString("JWT");
            if (string.IsNullOrWhiteSpace(jwt))
                return Unauthorized();

            var client = _clientFactory.CreateClient("ApiClient");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);

            var response = await client.PostAsync(
                $"pdf-editor/{normalizedType}/{id}/regenerate",
                null);

            if (!response.IsSuccessStatusCode)
                return StatusCode(
                    (int)response.StatusCode,
                    await response.Content.ReadAsStringAsync());

            return Content(
                await response.Content.ReadAsStringAsync(),
                "application/json");
        }

        [HttpPost]
        [BlockClientAccess]
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
        [BlockClientAccess]
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
        [HttpGet]
        public async Task<IActionResult> RegisterProjects()
        {
            var client = _clientFactory.CreateClient("ApiClient");
            var response = await client.GetAsync("User/projects");
            var body = await response.Content.ReadAsStringAsync();
            return Content(body, "application/json");
        }

        [HttpGet]
        public async Task<IActionResult> RegisterRoles(int projectId)
        {
            var client = _clientFactory.CreateClient("ApiClient");
            var response = await client.GetAsync($"User/projects/{projectId}/roles");
            var body = await response.Content.ReadAsStringAsync();
            return Content(body, "application/json");
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

                    bool isPendingApproval =
                        apiMessage.Contains("Awaiting admin approval", StringComparison.OrdinalIgnoreCase) ||
                        apiMessage.Contains("Waiting for Admin Approval", StringComparison.OrdinalIgnoreCase) ||
                        apiMessage.Contains("pending admin approval", StringComparison.OrdinalIgnoreCase);

                    return Json(new
                    {
                        success = true,
                        message = isPendingApproval ? "Waiting for Admin Approval" : "Registration successful!",
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

        private async Task<List<SurveyJobDto>> GetSurveyJobsForDashboard(string moduleType, int userId)
        {
            var jwt = HttpContext.Session.GetString("JWT");
            var client = _clientFactory.CreateClient("ApiClient");

            if (!string.IsNullOrWhiteSpace(jwt))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", jwt);
            }

            var url =
                $"survey/jobs?moduleType={Uri.EscapeDataString(moduleType)}&assignedSurveyorUserId={userId}";

            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                return new List<SurveyJobDto>();
            }

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<SurveyJobDto>>(json)
                ?? new List<SurveyJobDto>();
        }



    }

    public sealed class AssignReportToClientRequest
    {
        public string ReportType { get; set; } = string.Empty;
        public int ReportId { get; set; }
        public int? SurveyJobId { get; set; }
        public int ClientUserId { get; set; }
    }

}
