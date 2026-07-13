using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
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
    public class TowageController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TowageController> _logger;
        private readonly IHostEnvironment _env;
        private readonly string _apiBaseUrl;

        public TowageController(
            IHttpClientFactory clientFactory,
            IConfiguration configuration,
            ILogger<TowageController> logger,
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
                var response = await client.GetAsync($"AdminDashboard/report-data/towage/{id.Value}");
                if (response.IsSuccessStatusCode)
                {
                    var saved = JsonConvert.DeserializeObject<TowageCertificate>(await response.Content.ReadAsStringAsync());
                    if (saved != null)
                    {
                        saved.defaultRecommendations ??= new List<DRecommendations>();
                        saved.CargoDetails ??= new List<CargoDetails>();
                        return View(saved);
                    }
                }
                TempData["Error"] = "The saved towage report could not be loaded.";
            }

            var model = new TowageCertificate
            {
                defaultRecommendations = new List<DRecommendations>
                {
                    new() { RecommendationText = "Voyage to commence within 24 hours from the time of our survey." },
                    new() { RecommendationText = "Tow to depart in favorable weather conditions and forecast, typically less than Beaufort Force 5." },
                    new() { RecommendationText = "Good seamanship to be followed, including but not limited to the following." },
                    new() { RecommendationText = "Frequent weather conditions to be obtained by the tug boat from meteorological authorities en-route." },
                    new() { RecommendationText = "Speed of tow to be controlled, commensurate with sea and weather conditions to prevent damage to vessel(s)." },
                    new() { RecommendationText = "Visual check to be kept at all times to the towed vessel with particular attention to lashing arrangements, towing gear, navigational lights and shapes and draft marks." },
                    new() { RecommendationText = "Tug boat’s log book to be kept updated, showing above supervisions as well as towing speed, weather conditions and forecasts, radio communication and other relevant particulars." },
                    new() { RecommendationText = "Vessel’s courses and positions to be plotted at regular intervals on navigational charts and same together with official log book to be available for review with relevant authorities." },
                    new() { RecommendationText = "Additional tug assistance to be provided for steering in restricted channels as necessary." },
                    new() { RecommendationText = "Pilots to be engaged on arrival in local waters, restricted channels and intermediate ports if any." },
                    new() { RecommendationText = "All flush scuttles on both units to be properly closed and sealed at all times during the passage." },
                    new() { RecommendationText = "All parts of towage arrangements are to be free without any obstacles." },
                    new() { RecommendationText = "Daylight towing signals to be created and navigations light to be lit during the hours of darkness on both units." },
                    new() { RecommendationText = "The Master/operator is to provide Aries Oil Field Services L.L.C office with a daily report on the vessel’s course, speed, heading, ROB figures, weather conditions, distance covered and ETA at the destination port. Any deviation for weather routing etc. is to be reported to ariesmarine@ariesgroup.ae" }
                },
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
        public async Task<IActionResult> UploadSignature(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return Json(new { success = false, message = "No file selected." });

            var client = _clientFactory.CreateClient(); // or CreateClient("ApiClient") if you set BaseAddress
            using var content = new MultipartFormDataContent();
            var fc = new StreamContent(file.OpenReadStream());
            fc.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
            content.Add(fc, "file", file.FileName);

            var resp = await client.PostAsync($"{_apiBaseUrl}Certificate/upload-signature", content);
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) return Json(new { success = false, message = "Upload failed." });

            dynamic result = JsonConvert.DeserializeObject(body);
            return Json(new { success = true, filePath = (string)result.filePath });
        }

        [HttpPost]
        public async Task<IActionResult> UploadClientSignature(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return Json(new { success = false, message = "No file selected." });

            var client = _clientFactory.CreateClient();
            using var content = new MultipartFormDataContent();
            var fc = new StreamContent(file.OpenReadStream());
            fc.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
            content.Add(fc, "file", file.FileName);

            var resp = await client.PostAsync($"{_apiBaseUrl}Certificate/upload-signature-Client", content);
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) return Json(new { success = false, message = "Upload failed." });

            dynamic result = JsonConvert.DeserializeObject(body);
            return Json(new { success = true, filePath = (string)result.filePath });
        }


        [HttpPost]
        public async Task<IActionResult> UploadStamp(IFormFile stampFile)
        {
            if (stampFile == null || stampFile.Length == 0)
                return Json(new { success = false, message = "No file selected." });

            try
            {
                var client = _clientFactory.CreateClient("ApiClient");
                using var content = new MultipartFormDataContent();

                using var fileStream = stampFile.OpenReadStream();
                var fileContent = new StreamContent(fileStream);
                fileContent.Headers.ContentType =
                    new MediaTypeHeaderValue(stampFile.ContentType ?? "application/octet-stream");
                content.Add(fileContent, "file", stampFile.FileName);

                var url = "Certificate/upload-stamp"; // base comes from named client
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

                var uploadResult = JsonConvert.DeserializeObject<UploadStampResponse>(responseString);
                if (uploadResult == null || string.IsNullOrWhiteSpace(uploadResult.filePath))
                {
                    var msg = $"Upload succeeded but filePath missing. Raw: {responseString}";
                    _logger.LogError(msg);
                    if (_env.IsDevelopment()) throw new InvalidOperationException(msg);
                    return Json(new { success = false, message = "Upload succeeded but filePath missing." });
                }

                return Json(new { success = true, filePath = uploadResult.filePath });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during stamp upload");
                if (_env.IsDevelopment()) throw;
                return Json(new { success = false, message = "Exception occurred during upload." });
            }
        }

        private sealed class UploadStampResponse
        {
            public string? filePath { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> GeneratePDF(TowageCertificate model, string? preview, int currentstep)
        {
            try
            {
                // Normalize
                model.IncludeCargoDetails = true;
                if (model.NoDateCheckbox) model.IssuedDate = null;

                model.StampPath ??= "";
                model.SignaturePath ??= "";
                model.SignatureClient ??= "";
                model.Location ??= "";
                model.IDMarkings ??= "";
                model.Weight ??= "";
                model.Dimensions ??= "";
                model.Remarks ??= "";
                model.CertificateType ??= "";
                model.HeaderImagePath ??= "";
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
                if (model.defaultRecommendations == null || model.defaultRecommendations.Count == 0)
                {
                    model.defaultRecommendations = new List<DRecommendations>
                    {
                        new() { RecommendationText = "Voyage to commence within 24 hours from the time of our survey." },
                        new() { RecommendationText = "Tow to depart in favorable weather conditions and forecast, typically less than Beaufort Force 5." },
                        new() { RecommendationText = "Good seamanship to be followed, including but not limited to the following." },
                        new() { RecommendationText = "Frequent weather conditions to be obtained by the tug boat from meteorological authorities en-route." },
                        new() { RecommendationText = "Speed of tow to be controlled, commensurate with sea and weather conditions to prevent damage to vessel(s)." },
                        new() { RecommendationText = "Visual check to be kept at all times to the towed vessel..." }
                    };
                }

                var json = JsonConvert.SerializeObject(model);
                var client = _clientFactory.CreateClient("ApiClient");

                var url = "Certificate/create";
                _logger.LogInformation("POST {Url}", client.BaseAddress + url);

                using var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/pdf"));

                var jwt = HttpContext.Session.GetString("JWT");
                if (!string.IsNullOrEmpty(jwt))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
                }

                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);


                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.LogError("PDF API error: {Code} {Reason}\n{Body}",
                        (int)response.StatusCode, response.ReasonPhrase, body);

                    if (_env.IsDevelopment())
                        throw new HttpRequestException($"API {(int)response.StatusCode} {response.StatusCode}: {body}");

                    ViewBag.ApiError = $"API Error: {(int)response.StatusCode} {response.StatusCode}";
                    return View("Error");
                }

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

                return File(pdfBytes, "application/pdf", "Towage.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in GeneratePDF");
                if (_env.IsDevelopment()) throw;                 // <-- key change
                ViewBag.ApiError = $"Exception: {ex.Message}";
                return View("Error");
            }
        }
    }
}
