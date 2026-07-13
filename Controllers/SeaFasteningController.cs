using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Survery_Certification_App_Consume.Filters;
using Survery_Certification_App_Consume.Models;
using Survey_Certification_App.Models;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace Survery_Certification_App_Consume.Controllers
{
    [BlockClientAccess]
    public class SeaFasteningController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SeaFasteningController> _logger;
        private readonly IHostEnvironment _env;
        private readonly string _apiBaseUrl;

        public SeaFasteningController(
            IHttpClientFactory clientFactory,
            IConfiguration configuration,
            ILogger<SeaFasteningController> logger,
            IHostEnvironment env)
        {
            _clientFactory = clientFactory;
            _configuration = configuration;
            _logger = logger;
            _env = env;

            var raw = _configuration["ApiBaseUrl"];   // e.g., "https://localhost:7048/api/"
            if (string.IsNullOrWhiteSpace(raw))
                throw new InvalidOperationException("Missing configuration: ApiBaseUrl.");
            _apiBaseUrl = raw.EndsWith("/") ? raw : raw + "/";
        }

        public async Task<IActionResult> Create(int? id = null)
        {
            ViewBag.ApiBaseUrl = _apiBaseUrl;
            if (id > 0)
            {
                var client = _clientFactory.CreateClient("ApiClient");
                var jwt = HttpContext.Session.GetString("JWT");
                if (!string.IsNullOrWhiteSpace(jwt))
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
                var response = await client.GetAsync($"AdminDashboard/report-data/seafastening/{id.Value}");
                if (response.IsSuccessStatusCode)
                {
                    var saved = JsonConvert.DeserializeObject<SeaFastening>(await response.Content.ReadAsStringAsync());
                    if (saved != null)
                    {
                        saved.Recommendations ??= new List<Recommendation>();
                        saved.CargoDetails ??= new List<CargoDetails>();
                        return View(saved);
                    }
                }
                TempData["Error"] = "The saved sea fastening report could not be loaded.";
            }

            var model = new SeaFastening
            {
                IncludeAnnexure2 = false,
                Recommendations = new List<Recommendation>(),
                CargoDetails = new List<CargoDetails>
                {
                    new()
                    {
                        SrNo = "1",
                        Location = "",
                        Cargos = "",
                        IDMarkings = "",
                        Weight = "",
                        Dimensions = "",
                        Remarks = ""
                    }
                }
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> UploadStamp(IFormFile stampFile)
        {
            if (stampFile == null || stampFile.Length == 0)
                return Json(new { success = false, message = "No file selected." });

            try
            {
                // Use the same named client pattern as Towage (ensure it's registered with BaseAddress)
                var client = _clientFactory.CreateClient("ApiClient");
                using var content = new MultipartFormDataContent();

                using var fileStream = stampFile.OpenReadStream();
                var fileContent = new StreamContent(fileStream);
                fileContent.Headers.ContentType =
                    new MediaTypeHeaderValue(stampFile.ContentType ?? "application/octet-stream");
                content.Add(fileContent, "file", stampFile.FileName);

                var url = "SeaFastening/upload-stamp"; // relative to BaseAddress
                _logger.LogInformation("POST {Url}", client.BaseAddress + url);

                var response = await client.PostAsync(url, content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to upload stamp. {Code} {Reason}. Body: {Body}",
                        (int)response.StatusCode, response.ReasonPhrase, responseString);

                    if (_env.IsDevelopment())
                        throw new HttpRequestException($"Upload failed {(int)response.StatusCode}: {responseString}");

                    return Json(new { success = false, message = $"Upload failed ({(int)response.StatusCode})." });
                }

                var result = JsonConvert.DeserializeObject<UploadStampResponse>(responseString);
                if (result == null || string.IsNullOrWhiteSpace(result.filePath))
                {
                    var msg = $"Upload succeeded but filePath missing. Raw: {responseString}";
                    _logger.LogError(msg);
                    if (_env.IsDevelopment()) throw new InvalidOperationException(msg);
                    return Json(new { success = false, message = "Upload succeeded but filePath missing." });
                }

                return Json(new { success = true, filePath = result.filePath });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during SeaFastening stamp upload");
                if (_env.IsDevelopment()) throw;
                return Json(new { success = false, message = "Exception occurred during upload." });
            }
        }

        private sealed class UploadStampResponse
        {
            public string? filePath { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> GeneratePDF(SeaFastening model, string? preview, int currentstep)
        {
            try
            {
                // Normalize (mirror Towage pattern)
                model.IncludeCargoDetails = true;
                model.StampPath ??= "";
                model.SignaturePath ??= "";
                model.SignatureClient ??= "";
                model.CertificateTitle ??= "";
                model.HeaderImagePath ??= "";
                model.Recommendations ??= new List<Recommendation>();
                model.CargoDetails ??= new List<CargoDetails>();

                foreach (var item in model.CargoDetails)
                {
                    item.Location ??= "";
                    item.Cargos ??= "";
                    item.IDMarkings ??= "";
                    item.Weight ??= "";
                    item.Dimensions ??= "";
                    item.Remarks ??= "";
                }

                // Checkbox handling (safe)
                var checkboxVal = Request.Form["IncludeAnnexure2"].FirstOrDefault()?.ToLowerInvariant();
                model.IncludeAnnexure2 = checkboxVal == "true" || checkboxVal == "on";

                // Provide defaults if empty
                if (model.Recommendations.Count == 0)
                {
                    model.Recommendations = new List<Recommendation>
                    {
                        new() { RecommendationText = "Ensure all lashings and supports are inspected before sailing." },
                        new() { RecommendationText = "Voyage to commence within 24 hours from the time of our survey, subject to weather." }
                    };
                }

                var json = JsonConvert.SerializeObject(model);
                var client = _clientFactory.CreateClient("ApiClient");

                var url = "SeaFastening/create-seafastening"; // relative to BaseAddress
                _logger.LogInformation("POST {Url}", client.BaseAddress + url);

                using var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/pdf"));

                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.LogError("SeaFastening PDF API error: {Code} {Reason}\n{Body}",
                        (int)response.StatusCode, response.ReasonPhrase, body);

                    if (_env.IsDevelopment())
                        throw new HttpRequestException($"API {(int)response.StatusCode} {response.StatusCode}: {body}");

                    ViewBag.ApiError = $"API Error: {(int)response.StatusCode} {response.StatusCode}";
                    return View("Error");
                }

                // Read the raw PDF bytes once
                var pdfBytes = await response.Content.ReadAsByteArrayAsync();
                if (pdfBytes == null || pdfBytes.Length == 0)
                {
                    var msg = "PDF generation returned empty content.";
                    _logger.LogError(msg);
                    if (_env.IsDevelopment()) throw new InvalidOperationException(msg);
                    ViewBag.ApiError = msg;
                    return View("Error");
                }

                if (!string.IsNullOrEmpty(preview))
                {
                    TempData["LastModel"] = json;
                    TempData["LastStep"] = currentstep;
                    Response.Headers["Content-Disposition"] = "inline; filename=Preview.pdf";
                    return File(pdfBytes, "application/pdf");
                }

                return File(pdfBytes, "application/pdf", "SeaFastening.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in SeaFastening.GeneratePDF");
                if (_env.IsDevelopment()) throw;
                ViewBag.ApiError = $"Exception: {ex.Message}";
                return View("Error");
            }
        }
    }
}
