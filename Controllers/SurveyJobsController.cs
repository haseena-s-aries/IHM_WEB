using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Survery_Certification_App_Consume.Models;
using System.Net.Http.Headers;
using System.Text;

namespace Survery_Certification_App_Consume.Controllers
{
    public class SurveyJobsController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;

        public SurveyJobsController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        private async Task<SurveyJobsPageViewModel> BuildSurveyJobsModel(string surveyType)
        {
            var model = new SurveyJobsPageViewModel { SurveyType = surveyType };

            var role = HttpContext.Session.GetString("UserRole") ?? "";
            var userId = HttpContext.Session.GetInt32("UserId") ?? 0;

            model.IsAdmin = role.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                            role.Equals("Manager", StringComparison.OrdinalIgnoreCase);

            try
            {
                var client = CreateApiClient();

                var apiModuleType = surveyType == "IHM Survey" ? "IhmSurvey" : "ExSurvey";

                var jobsUrl = model.IsAdmin
                    ? $"survey/jobs?moduleType={Uri.EscapeDataString(apiModuleType)}"
                    : $"survey/jobs?moduleType={Uri.EscapeDataString(apiModuleType)}&assignedSurveyorUserId={userId}";

                model.Jobs = await GetAsync<List<SurveyJobDto>>(client, jobsUrl) ?? new();

                if (surveyType == "IHM Survey")
                {
                    foreach (var job in model.Jobs)
                    {
                        model.AreasByJob[job.Id] =
                            await GetAsync<List<IhmAreaOptionDto>>(
                                client, $"ihm-survey/jobs/{job.Id}/areas-with-checkpoints") ?? new();
                        var attachments = await GetAsync<List<IhmLayoutAttachmentDto>>(
                            client, $"ihm-survey/jobs/{job.Id}/layout-attachments") ?? new();
                        model.LayoutAttachmentsByJob[job.Id] = attachments
                            .GroupBy(x => x.Target)
                            .ToDictionary(x => x.Key, x => x.First());
                    }
                }

                if (model.IsAdmin)
                {
                    model.Vessels = await GetAsync<List<VesselDto>>(client, "vessels") ?? new();
                    model.Surveyors = await GetAsync<List<SurveyorOptionDto>>(client, "User/surveyors") ?? new();
                }
            }
            catch (Exception ex)
            {
                model.ErrorMessage = ex.Message;
            }

            return model;
        }


        public async Task<IActionResult> Ihm()
        {
            var model = await BuildSurveyJobsModel("IHM Survey");
            return View("Index", model);
        }


        public async Task<IActionResult> Ex()
        {
            var model = await BuildSurveyJobsModel("EX Survey");
            return View("Index", model);
        }

        public async Task<IActionResult> Index(string surveyType = "IHM Survey")
        {
            var model = new SurveyJobsPageViewModel { SurveyType = surveyType };
            var role = HttpContext.Session.GetString("UserRole") ?? "";
            var userId = HttpContext.Session.GetInt32("UserId") ?? 0;

            model.IsAdmin = role.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                            role.Equals("Manager", StringComparison.OrdinalIgnoreCase);

            try
            {
                var client = CreateApiClient();

                var apiModuleType = surveyType == "IHM Survey" ? "IhmSurvey" : "ExSurvey";

                var jobsUrl = model.IsAdmin
                    ? $"survey/jobs?moduleType={Uri.EscapeDataString(apiModuleType)}"
                    : $"survey/jobs?moduleType={Uri.EscapeDataString(apiModuleType)}&assignedSurveyorUserId={userId}";

                model.Jobs = await GetAsync<List<SurveyJobDto>>(client, jobsUrl) ?? new();

                if (model.IsAdmin)
                {
                    model.Vessels = await GetAsync<List<VesselDto>>(client, "vessels") ?? new();
                    model.Surveyors = await GetAsync<List<SurveyorOptionDto>>(client, "User/surveyors") ?? new();
                }
            }
            catch (Exception ex)
            {
                model.ErrorMessage = ex.Message;
            }

            return View(model);
        }
        public async Task<IActionResult> Work(int jobId, string surveyType = "IHM Survey")
        {
            try
            {
                var client = CreateApiClient();
                var response = await client.GetAsync($"survey/jobs/{jobId}");
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    TempData["ErrorMessage"] = "Failed to load survey job: " + body;
                    return RedirectToAction(surveyType == "IHM Survey" ? "Ihm" : "Ex");
                }

                var job = JsonConvert.DeserializeObject<SurveyJobDto>(body);
                if (job == null)
                {
                    TempData["ErrorMessage"] = "Survey job not found.";
                    return RedirectToAction(surveyType == "IHM Survey" ? "Ihm" : "Ex");
                }

                ViewBag.SurveyType = surveyType;

                if (surveyType == "IHM Survey")
                {
                    var areas = await GetAsync<List<IhmAreaOptionDto>>(
                        client,
                        $"ihm-survey/jobs/{jobId}/areas-with-checkpoints"
                    ) ?? new List<IhmAreaOptionDto>();

                    var checkpoints = areas.SelectMany(x => x.Checkpoints ?? new List<IhmCheckpointOptionDto>()).ToList();

                    var total = checkpoints.Count;
                    var totalVisuals = checkpoints.Count(x => x.IsVisual);
                    var totalSamples = checkpoints.Count(x => x.IsSample);
                    var audited = checkpoints.Count(x => x.IsAudited);

                    ViewBag.Total = total;
                    ViewBag.TotalVisuals = totalVisuals;
                    ViewBag.TotalSamples = totalSamples;
                    ViewBag.Audited = audited;
                    ViewBag.AreaCount = areas.Count;
                }
                else
                {
                    var summary = await GetAsync<ExSurveySummaryDto>(
                        client,
                        $"ex-survey/jobs/{jobId}/summary"
                    );

                    ViewBag.TotalVisuals = summary?.EquipmentCount ?? 0;
                    ViewBag.TotalSamples = summary?.DefectCount ?? 0;
                    ViewBag.Audited = summary?.CompletedChecklistCount ?? 0;
                    ViewBag.AreaCount = 0;
                    ViewBag.ExSummary = summary;
                }

                if (job.VesselId.HasValue)
                {
                    ViewBag.Vessel = await GetAsync<VesselDto>(
                        client,
                        $"vessels/{job.VesselId.Value}"
                    );
                }

                return View(job);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(surveyType == "IHM Survey" ? "Ihm" : "Ex");
            }
        }
        public async Task<IActionResult> VesselInfo(int jobId)
        {
            try
            {
                var client = CreateApiClient();
                var job = await GetAsync<SurveyJobDto>(client, $"survey/jobs/{jobId}");

                if (job == null || !job.VesselId.HasValue)
                {
                    TempData["ErrorMessage"] = "Vessel information not found.";
                    return RedirectToAction("Work", new { jobId, surveyType = "IHM Survey" });
                }

                var vessel = await GetAsync<VesselDto>(client, $"vessels/{job.VesselId.Value}");
                if (vessel == null)
                {
                    TempData["ErrorMessage"] = "Vessel information not found.";
                    return RedirectToAction("Work", new { jobId, surveyType = "IHM Survey" });
                }

                ViewBag.JobId = jobId;
                return View(vessel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to load vessel information: " + ex.Message;
                return RedirectToAction("Work", new { jobId, surveyType = "IHM Survey" });
            }
        }

        public async Task<IActionResult> SurveyAreas(int jobId)
        {
            try
            {
                var client = CreateApiClient();

                var job = await GetAsync<SurveyJobDto>(client, $"survey/jobs/{jobId}");
                var areas = await GetAsync<List<IhmAreaOptionDto>>(
                    client,
                    $"ihm-survey/jobs/{jobId}/areas-with-checkpoints"
                ) ?? new List<IhmAreaOptionDto>();

                ViewBag.Job = job;
                ViewBag.JobId = jobId;
                return View(areas);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to load survey areas: " + ex.Message;
                return RedirectToAction("Work", new { jobId, surveyType = "IHM Survey" });
            }
        }

        public async Task<IActionResult> AreaCheckpoints(int jobId, int areaId)
        {
            try
            {
                var client = CreateApiClient();

                var job = await GetAsync<SurveyJobDto>(client, $"survey/jobs/{jobId}");
                var areas = await GetAsync<List<IhmAreaOptionDto>>(
                    client,
                    $"ihm-survey/jobs/{jobId}/areas-with-checkpoints"
                ) ?? new List<IhmAreaOptionDto>();

                var area = areas.FirstOrDefault(x => x.Id == areaId);
                if (area == null)
                {
                    TempData["ErrorMessage"] = "Survey area not found.";
                    return RedirectToAction("SurveyAreas", new { jobId });
                }

                ViewBag.Job = job;
                ViewBag.JobId = jobId;
                return View(area);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to load checkpoints: " + ex.Message;
                return RedirectToAction("SurveyAreas", new { jobId });
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetIhmMarkers(int jobId)
        {
            try
            {
                var client = CreateApiClient();
                var markers = await GetAsync<List<IhmImageMarkerDto>>(
                    client,
                    $"ihm-survey/jobs/{jobId}/markers"
                ) ?? new List<IhmImageMarkerDto>();

                return Json(markers);
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveIhmMarker([FromBody] SaveIhmMarkerRequest request)
        {
            try
            {
                var client = CreateApiClient();
                var userId = HttpContext.Session.GetInt32("UserId") ?? 0;

                var payload = new
                {
                    surveyJobId = request.SurveyJobId,
                    ihmAreaId = request.IhmAreaId,
                    ihmCheckpointId = request.IhmCheckpointId,
                    imagePath = request.ImagePath,
                    xPercent = request.XPercent,
                    yPercent = request.YPercent,
                    notes = request.Notes,
                    createdByUserId = userId
                };

                var response = await client.PostAsync(
                    "ihm-survey/markers",
                    new StringContent(
                        JsonConvert.SerializeObject(payload),
                        Encoding.UTF8,
                        "application/json"
                    )
                );

                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Response.StatusCode = (int)response.StatusCode;
                    return Content(body, "application/json");
                }

                return Content(body, "application/json");
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { message = ex.Message });
            }
        }
        public async Task<IActionResult> CheckpointDetail(int jobId, int areaId, int checkpointId)
        {
            try
            {
                var client = CreateApiClient();

                var areas = await GetAsync<List<IhmAreaOptionDto>>(
                    client,
                    $"ihm-survey/jobs/{jobId}/areas-with-checkpoints"
                ) ?? new List<IhmAreaOptionDto>();

                var area = areas.FirstOrDefault(x => x.Id == areaId);
                if (area == null)
                {
                    TempData["ErrorMessage"] = "Survey area not found.";
                    return RedirectToAction("SurveyAreas", new { jobId });
                }
                var attachments = await GetAsync<List<IhmLayoutAttachmentDto>>(
                    client,
                    $"ihm-survey/jobs/{jobId}/layout-attachments"
                ) ?? new List<IhmLayoutAttachmentDto>();

                var areaLayout = attachments.FirstOrDefault(x =>
                    string.Equals(x.Target, areaId.ToString(), StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(x.Target, area.AreaName, StringComparison.OrdinalIgnoreCase));

                area.LayoutFilePath = areaLayout?.FilePath;
                var checkpoint = area.Checkpoints.FirstOrDefault(x => x.Id == checkpointId);
                if (checkpoint == null)
                {
                    TempData["ErrorMessage"] = "Checkpoint not found.";
                    return RedirectToAction("AreaCheckpoints", new { jobId, areaId });
                }

                ViewBag.JobId = jobId;
                ViewBag.AreaId = areaId;
                ViewBag.AreaName = area.AreaName;

                return View(checkpoint);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to load checkpoint: " + ex.Message;
                return RedirectToAction("AreaCheckpoints", new { jobId, areaId });
            }
        }
        public async Task<IActionResult> ProjectInfo(int jobId)
        {
            try
            {
                var client = CreateApiClient();
                var response = await client.GetAsync($"survey/jobs/{jobId}");
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    TempData["ErrorMessage"] = "Failed to load project information: " + body;
                    return RedirectToAction("Ihm");
                }

                var job = JsonConvert.DeserializeObject<SurveyJobDto>(body);
                if (job == null)
                {
                    TempData["ErrorMessage"] = "Project information not found.";
                    return RedirectToAction("Ihm");
                }

                return View(job);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to load project information: " + ex.Message;
                return RedirectToAction("Ihm");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create(SurveyJobCreateRequest request)
        {
            var returnAction = request.SurveyType == "IHM Survey" ? "Ihm" : "Ex";

            if (string.IsNullOrWhiteSpace(request.JobNumber) ||
                !request.VesselId.HasValue ||
                string.IsNullOrWhiteSpace(request.AuditPort) ||
                string.IsNullOrWhiteSpace(request.StartDate) ||
                string.IsNullOrWhiteSpace(request.EndDate))
            {
                TempData["ErrorMessage"] = "Job number, vessel, audit port and dates are required.";
                return RedirectToAction(returnAction);
            }

            if (request.PrimarySurveyorUserId.HasValue &&
                request.SecondarySurveyorUserId.HasValue &&
                request.PrimarySurveyorUserId.Value == request.SecondarySurveyorUserId.Value)
            {
                TempData["ErrorMessage"] = "Primary and secondary surveyor cannot be the same user.";
                return RedirectToAction(returnAction);
            }

            var client = CreateApiClient();

            var payload = new
            {
                moduleType = request.SurveyType == "IHM Survey" ? 2 : 1,
                jobNumber = request.JobNumber.Trim(),
                clientName = request.ClientName?.Trim(),
                projectName = request.ProjectName?.Trim(),
                vesselId = request.VesselId,
                siteName = request.AuditPort?.Trim(),
                auditPort = request.AuditPort?.Trim(),
                auditAddress = request.AuditAddress?.Trim(),
                startDate = request.StartDate,
                endDate = request.EndDate,
                createdByUserId = HttpContext.Session.GetInt32("UserId") ?? 0
            };

            var response = await client.PostAsync(
                "survey/jobs",
                new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json")
            );

            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                TempData["ErrorMessage"] = "Create job failed: " + body;
                return RedirectToAction(returnAction);
            }

            var createdJob = JsonConvert.DeserializeObject<SurveyJobDto>(body);

            if (createdJob != null)
            {
                await AssignSurveyorsInternal(client, createdJob.Id, request.PrimarySurveyorUserId, request.SecondarySurveyorUserId);
            }

            TempData["SuccessMessage"] = "Survey job created successfully.";
            return RedirectToAction(returnAction);
        }

        [HttpPost]
        public async Task<IActionResult> AssignSurveyors(int jobId, int? primarySurveyorUserId, int? secondarySurveyorUserId, string surveyType)
        {
            var returnAction = surveyType == "IHM Survey" ? "Ihm" : "Ex";

            if (primarySurveyorUserId.HasValue &&
                secondarySurveyorUserId.HasValue &&
                primarySurveyorUserId.Value == secondarySurveyorUserId.Value)
            {
                TempData["ErrorMessage"] = "Primary and secondary surveyor cannot be the same user.";
                return RedirectToAction(returnAction);
            }

            try
            {
                var client = CreateApiClient();
                await AssignSurveyorsInternal(client, jobId, primarySurveyorUserId, secondarySurveyorUserId);
                TempData["SuccessMessage"] = "Surveyors assigned successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Assign surveyors failed: " + ex.Message;
            }

            return RedirectToAction(returnAction);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int jobId, string surveyType)
        {
            var returnAction = surveyType == "IHM Survey" ? "Ihm" : "Ex";

            try
            {
                var client = CreateApiClient();
                var response = await client.DeleteAsync($"survey/jobs/{jobId}");
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    TempData["ErrorMessage"] = "Delete failed: " + body;
                    return RedirectToAction(returnAction);
                }

                TempData["SuccessMessage"] = "Survey job deleted successfully.";
                return RedirectToAction(returnAction);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Delete failed: " + ex.Message;
                return RedirectToAction(returnAction);
            }
        }

        [HttpPost]
        public async Task<IActionResult> UploadIhmExcel(int jobId, IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    TempData["ErrorMessage"] = "Please select an Excel file.";
                    return RedirectToAction("Ihm");
                }

                var message = await UploadFile(jobId, file, $"ihm-survey/jobs/{jobId}/import-excel", "file");

                TempData["SuccessMessage"] = "Excel checklist uploaded successfully. Previous Excel data was replaced.";
                return RedirectToAction("Ihm");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Excel upload failed: " + ex.Message;
                return RedirectToAction("Ihm");
            }
        }



        [HttpPost]
        public async Task<IActionResult> UploadIhmLayout(int jobId, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select a layout file.";
                return RedirectToAction("Ihm");
            }

            var client = CreateApiClient();
            var uploadedByUserId = HttpContext.Session.GetInt32("UserId") ?? 0;

            using var content = new MultipartFormDataContent();
            content.Add(new StringContent("IhmLayout"), "relatedModule");
            content.Add(new StringContent(uploadedByUserId.ToString()), "uploadedByUserId");

            await using var stream = file.OpenReadStream();
            content.Add(new StreamContent(stream), "file", file.FileName);

            var response = await client.PostAsync($"survey/jobs/{jobId}/photos", content);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                TempData["ErrorMessage"] = "Failed to upload layout: " + body;
                return RedirectToAction("Ihm");
            }

            TempData["SuccessMessage"] = "Layout uploaded successfully.";
            return RedirectToAction("Ihm");
        }

        [HttpPost]
        public async Task<IActionResult> UploadIhmAreaLayout(
            int jobId, string layoutTarget, string? newAreaName, List<IFormFile> files)
        {
            if (files == null || files.Count == 0 || files.All(x => x.Length == 0))
            {
                TempData["ErrorMessage"] = "Please select one or more layout images.";
                return RedirectToAction("Ihm");
            }

            try
            {
                var client = CreateApiClient();
                int? areaId = null;
                if (layoutTarget == "new")
                {
                    if (string.IsNullOrWhiteSpace(newAreaName))
                        throw new InvalidOperationException("Enter the new area name.");

                    var response = await client.PostAsync(
                        $"ihm-survey/jobs/{jobId}/areas",
                        new StringContent(JsonConvert.SerializeObject(new
                        {
                            areaName = newAreaName.Trim(),
                            description = "Created while uploading layouts",
                            sortOrder = 999
                        }), Encoding.UTF8, "application/json"));
                    var body = await response.Content.ReadAsStringAsync();
                    if (!response.IsSuccessStatusCode) throw new InvalidOperationException(body);
                    areaId = JsonConvert.DeserializeObject<IhmAreaOptionDto>(body)?.Id;
                }
                else if (layoutTarget != "common")
                {
                    areaId = int.Parse(layoutTarget);
                }

                var file = files.First(x => x.Length > 0);
                {
                    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (extension is not (".jpg" or ".jpeg" or ".png" or ".webp"))
                        throw new InvalidOperationException("Only JPG, PNG, and WEBP images are supported.");

                    using var content = new MultipartFormDataContent();
                    content.Add(new StringContent(
                        (HttpContext.Session.GetInt32("UserId") ?? 0).ToString()),
                        "uploadedByUserId");
                    await using var stream = file.OpenReadStream();
                    content.Add(new StreamContent(stream), "file", file.FileName);

                    var endpoint = areaId.HasValue
                        ? $"ihm-survey/jobs/{jobId}/areas/{areaId}/layout"
                        : $"ihm-survey/jobs/{jobId}/ga-layout";
                    var uploadResponse = await client.PostAsync(endpoint, content);
                    if (!uploadResponse.IsSuccessStatusCode)
                        throw new InvalidOperationException(
                            await uploadResponse.Content.ReadAsStringAsync());
                }

                TempData["SuccessMessage"] = "Layout attachment uploaded successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to upload layouts: " + ex.Message;
            }

            return RedirectToAction("Ihm");
        }

        private async Task AssignSurveyorsInternal(HttpClient client, int jobId, int? primaryId, int? secondaryId)
        {
            var payload = new
            {
                primarySurveyorUserId = primaryId,
                secondarySurveyorUserId = secondaryId,
                assignedByUserId = HttpContext.Session.GetInt32("UserId") ?? 0
            };

            var response = await client.PostAsync(
                $"survey/jobs/{jobId}/assign-surveyors",
                new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json")
            );

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException(
                    string.IsNullOrWhiteSpace(body)
                        ? "Unable to assign surveyors."
                        : body);
            }
        }

        private async Task<string> UploadFile(int jobId, IFormFile file, string url, string fieldName)
        {
            if (file == null || file.Length == 0)
                throw new InvalidOperationException("Please select a file.");

            var client = CreateApiClient();
            using var content = new MultipartFormDataContent();
            using var stream = file.OpenReadStream();
            content.Add(new StreamContent(stream), fieldName, file.FileName);

            var response = await client.PostAsync(url, content);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException(
                    string.IsNullOrWhiteSpace(body)
                        ? "Upload failed. Please check the Excel format."
                        : body);

            return body;
        }

        private HttpClient CreateApiClient()
        {
            var client = _clientFactory.CreateClient("ApiClient");
            var jwt = HttpContext.Session.GetString("JWT");

            if (!string.IsNullOrWhiteSpace(jwt))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

            return client;
        }

        private static async Task<T?> GetAsync<T>(HttpClient client, string url)
        {
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(json);
        }
    }

    public class SurveyJobCreateRequest
    {
        public string SurveyType { get; set; } = "IHM Survey";
        public string JobNumber { get; set; } = "";
        public string? ClientName { get; set; }
        public string? ProjectName { get; set; }
        public int? VesselId { get; set; }
        public string? AuditPort { get; set; }
        public string? AuditAddress { get; set; }
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
        public int? PrimarySurveyorUserId { get; set; }
        public int? SecondarySurveyorUserId { get; set; }
    }
}
