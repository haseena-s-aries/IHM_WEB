using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Survery_Certification_App_Consume.Models;
using System.Net.Http.Headers;
using System.Text;
using Survery_Certification_App_Consume.Filters;
namespace Survery_Certification_App_Consume.Controllers
{
    [BlockClientAccess]
    public class SurveyJobsController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly ILogger<SurveyJobsController> _logger;

        public SurveyJobsController(
            IHttpClientFactory clientFactory,
            ILogger<SurveyJobsController> logger)
        {
            _clientFactory = clientFactory;
            _logger = logger;
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
                    var ihmLoadTasks = model.Jobs.Select(async job =>
                    {
                        var areasTask = GetAsync<List<IhmAreaOptionDto>>(
                            client,
                            $"ihm-survey/jobs/{job.Id}/areas-with-checkpoints");

                        var attachmentsTask = GetAsync<List<IhmLayoutAttachmentDto>>(
                            client,
                            $"ihm-survey/jobs/{job.Id}/layout-attachments");

                        await Task.WhenAll(areasTask, attachmentsTask);

                        return new
                        {
                            JobId = job.Id,
                            Areas = areasTask.Result ?? new List<IhmAreaOptionDto>(),
                            Attachments = attachmentsTask.Result ?? new List<IhmLayoutAttachmentDto>()
                        };
                    });

                    var ihmLoadResults = await Task.WhenAll(ihmLoadTasks);

                    foreach (var result in ihmLoadResults)
                    {
                        model.AreasByJob[result.JobId] = result.Areas;
                        model.LayoutAttachmentsByJob[result.JobId] = result.Attachments
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

                var attachments = await GetAsync<List<IhmLayoutAttachmentDto>>(
                    client,
                    $"ihm-survey/jobs/{jobId}/layout-attachments"
                ) ?? new List<IhmLayoutAttachmentDto>();

                var areaLayouts = attachments
                    .Where(x =>
                        string.Equals(x.Target, "common", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(x.Target, areaId.ToString(), StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(x.Target, area.AreaName, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var areaLayout = areaLayouts.FirstOrDefault(x =>
                    string.Equals(x.Target, areaId.ToString(), StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(x.Target, area.AreaName, StringComparison.OrdinalIgnoreCase));

                var commonLayout = areaLayouts.FirstOrDefault(x =>
                    string.Equals(x.Target, "common", StringComparison.OrdinalIgnoreCase));

                area.LayoutFilePath = areaLayout?.FilePath ?? commonLayout?.FilePath;

                ViewBag.Job = job;
                ViewBag.JobId = jobId;
                ViewBag.Layouts = areaLayouts;
                return View(area);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to load checkpoints: " + ex.Message;
                return RedirectToAction("SurveyAreas", new { jobId });
            }
        }
        [HttpPost]
        public async Task<IActionResult> AutoMapLayoutMarkers([FromBody] AutoMapLayoutMarkersRequest request)
        {
            try
            {
                var client = CreateApiClient();
                var userId = HttpContext.Session.GetInt32("UserId") ?? 0;

                var payload = new
                {
                    areaId = request.AreaId,
                    imagePath = request.ImagePath,
                    userId
                };

                var response = await client.PostAsync(
                    $"ihm-survey/jobs/{request.JobId}/layouts/auto-map-markers",
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

        [HttpGet]
        public async Task<IActionResult> GetMarkedImageStatus(int jobId)
        {
            try
            {
                var client = CreateApiClient();
                var response = await client.GetAsync($"ihm-survey/jobs/{jobId}/marked-images/status");
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Unable to check marked image status for job {JobId}. API returned {StatusCode}: {Body}",
                        jobId,
                        (int)response.StatusCode,
                        body);

                    return Json(new
                    {
                        jobId,
                        items = Array.Empty<object>(),
                        statusUnavailable = true,
                        message = "Marked image status is not available yet."
                    });
                }

                return Content(body, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to check marked image status for job {JobId}.", jobId);
                return Json(new
                {
                    jobId,
                    items = Array.Empty<object>(),
                    statusUnavailable = true,
                    message = "Marked image status is not available yet."
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteIhmMarker(int markerId)
        {
            try
            {
                var client = CreateApiClient();
                var response = await client.DeleteAsync($"ihm-survey/markers/{markerId}");
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Response.StatusCode = (int)response.StatusCode;
                    return Content(body);
                }

                return Ok();
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
                    source = request.Source,
                    suppressRender = request.SuppressRender,
                    isManualForExistingImage = request.IsManualForExistingImage,
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
                ViewBag.ApiRoot = (client.BaseAddress?.ToString() ?? "")
                    .Replace("/api/", "/")
                    .Replace("/api", "")
                    .TrimEnd('/');
                ViewBag.CheckpointLabels = areas
    .SelectMany(x => x.Checkpoints ?? new List<IhmCheckpointOptionDto>())
    .Where(x => x.Id > 0)
    .Where(x => x.SurveyJobId == jobId)
    .Where(x => !string.IsNullOrWhiteSpace(x.SampleNo))
    .Where(x => string.Equals(
        (x.CheckResult ?? "").Trim(),
        "YES",
        StringComparison.OrdinalIgnoreCase))
    .ToDictionary(
        x => x.Id,
        x => x.SampleNo!.Trim()
    );
                ViewBag.Layouts = attachments
                    .Where(x =>
                        string.Equals(x.Target, "common", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(x.Target, areaId.ToString(), StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(x.Target, area.AreaName, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                return View(checkpoint);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to load checkpoint: " + ex.Message;
                return RedirectToAction("AreaCheckpoints", new { jobId, areaId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveCheckpointAudit(SaveCheckpointAuditRequest request)
        {
            try
            {
                var client = CreateApiClient();
                var payload = new
                {
                    sampleNo = request.SampleNo,
                    description = request.Description ?? "",
                    tableType = request.TableType,
                    location = request.Location,
                    equipmentName = request.EquipmentName,
                    component = request.Component,
                    materialCategory = request.MaterialCategory,
                    hazardousMaterial = request.HazardousMaterial,
                    quantityUnitKg = CleanTextValue(request.QuantityUnitKg),
                    quantityNo = CleanTextValue(request.QuantityNo),
                    quantityTotalKg = CleanTextValue(request.QuantityTotalKg),
                    manufacturerBrandName = request.ManufacturerBrandName,
                    documentAnalysisResult = request.DocumentAnalysisResult,
                    samplingCheck = request.SamplingCheck,
                    visualCheck = request.VisualCheck,
                    checkResult = request.CheckResult,
                    result = ToCheckpointResultValue(request.AuditResult),
                    isPchm = request.IsPchm,
                    remarks = request.Remarks,
                    locationRemarks = request.LocationRemarks,
                    updatedByUserId = HttpContext.Session.GetInt32("UserId") ?? 0
                };

                var response = await client.PutAsync(
                    $"ihm-survey/checkpoints/{request.CheckpointId}/details",
                    new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json"));
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    TempData["ErrorMessage"] = string.IsNullOrWhiteSpace(body)
                        ? "Unable to save checkpoint."
                        : body;
                    return RedirectToAction("CheckpointDetail", new
                    {
                        jobId = request.JobId,
                        areaId = request.AreaId,
                        checkpointId = request.CheckpointId
                    });
                }

                TempData["SuccessMessage"] = "Checkpoint saved successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Unable to save checkpoint: " + ex.Message;
            }

            return RedirectToAction("CheckpointDetail", new
            {
                jobId = request.JobId,
                areaId = request.AreaId,
                checkpointId = request.CheckpointId
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetCheckpointPhotos(int checkpointId)
        {
            try
            {
                var client = CreateApiClient();
                var photos = await GetAsync<List<IhmSamplingPhotoDto>>(
                    client,
                    $"ihm-survey/checkpoints/{checkpointId}/photos"
                ) ?? new List<IhmSamplingPhotoDto>();

                return Json(photos);
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UploadCheckpointPhotos(int jobId, int checkpointId, List<IFormFile> files)
        {
            if (files == null || files.Count == 0 || files.All(x => x.Length == 0))
                return BadRequest("Please select one or more images.");

            try
            {
                var client = CreateApiClient();
                var uploaded = new List<IhmSamplingPhotoDto>();

                foreach (var file in files.Where(x => x.Length > 0))
                {
                    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (extension is not (".jpg" or ".jpeg" or ".png" or ".webp"))
                        return BadRequest("Only JPG, JPEG, PNG and WEBP images are supported.");

                    using var content = new MultipartFormDataContent();
                    await using var stream = file.OpenReadStream();
                    content.Add(new StreamContent(stream), "file", file.FileName);

                    var response = await client.PostAsync(
                        $"ihm-survey/jobs/{jobId}/checkpoints/{checkpointId}/photos",
                        content);
                    var body = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        Response.StatusCode = (int)response.StatusCode;
                        return Content(body);
                    }

                    var photo = JsonConvert.DeserializeObject<IhmSamplingPhotoDto>(body);
                    if (photo != null)
                        uploaded.Add(photo);
                }

                return Json(uploaded);
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCheckpointPhoto(int photoId)
        {
            try
            {
                var client = CreateApiClient();
                var response = await client.DeleteAsync($"ihm-survey/sampling-photos/{photoId}");
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Response.StatusCode = (int)response.StatusCode;
                    return Content(body);
                }

                return Ok();
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { message = ex.Message });
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
                projectId = request.ProjectId,
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
        public async Task<IActionResult> UpdateSurveyDate(int jobId, string surveyDate, string surveyType)
        {
            var returnAction = surveyType == "IHM Survey" ? "Ihm" : "Ex";

            if (string.IsNullOrWhiteSpace(surveyDate))
            {
                TempData["ErrorMessage"] = "Survey date is required.";
                return RedirectToAction(returnAction);
            }

            try
            {
                var client = CreateApiClient();
                var payload = new { surveyDate };
                var response = await client.PostAsync(
                    $"survey/jobs/{jobId}/survey-date",
                    new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json")
                );
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    TempData["ErrorMessage"] = "Survey date update failed: " + body;
                    return RedirectToAction(returnAction);
                }

                TempData["SuccessMessage"] = "Survey date saved successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Survey date update failed: " + ex.Message;
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

                var client = CreateApiClient();

                var existingAreas = await GetAsync<List<IhmAreaOptionDto>>(
                    client,
                    $"ihm-survey/jobs/{jobId}/areas-with-checkpoints");

                var isReupload = existingAreas != null && existingAreas.Any();

                var message = await UploadFile(jobId, file, $"ihm-survey/jobs/{jobId}/import-excel", "file");

                TempData["SuccessMessage"] = isReupload
                    ? "Excel checklist uploaded successfully. Previous Excel data was replaced."
                    : "Excel checklist uploaded successfully.";
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
                TempData["ErrorMessage"] = "Please select one layout image or PDF file.";
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
                    if (extension is not (".jpg" or ".jpeg" or ".png" or ".webp" or ".pdf"))
                        throw new InvalidOperationException("Only JPG, PNG, WEBP images and PDF files are supported.");

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
            {
                var message = ExtractUploadErrorMessage(body);
                throw new InvalidOperationException(
                    string.IsNullOrWhiteSpace(message)
                        ? "Upload failed. Please check the Excel format."
                        : message);
            }

            return body;
        }

        private static string ExtractUploadErrorMessage(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
                return string.Empty;

            try
            {
                var token = Newtonsoft.Json.Linq.JToken.Parse(body);
                var message = token["message"]?.ToString()
                    ?? token["Message"]?.ToString();
                var inner = token["innerException"]?.ToString()
                    ?? token["InnerException"]?.ToString();

                if (!string.IsNullOrWhiteSpace(inner) &&
                    !string.Equals(message, inner, StringComparison.OrdinalIgnoreCase))
                    return $"{message} {inner}".Trim();

                if (!string.IsNullOrWhiteSpace(message))
                    return message;
            }
            catch
            {
            }

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
        private static string? CleanTextValue(string? value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Trim() == "-")
                return null;

            return value.Trim();
        }
        private static decimal? ToNullableDecimal(string? value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Trim() == "-")
                return null;

            return decimal.TryParse(value, out var result) ? result : null;
        }
        private static int ToCheckpointResultValue(string? result)
        {
            return (result ?? "").Trim().ToLowerInvariant().Replace(" ", "").Replace("_", "").Replace("-", "") switch
            {
                "present" or "2" => 2,
                "notpresent" or "3" => 3,
                "notapplicable" or "4" => 4,
                "samplerequired" or "5" => 5,
                _ => 1
            };
        }
    }
    public class AutoMapLayoutMarkersRequest
    {
        public int JobId { get; set; }
        public int AreaId { get; set; }
        public string ImagePath { get; set; } = string.Empty;
    }
    public class SurveyJobCreateRequest
    {
        public string SurveyType { get; set; } = "IHM Survey";
        public int? ProjectId { get; set; }
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

    public class SaveCheckpointAuditRequest
    {
        public int JobId { get; set; }
        public int AreaId { get; set; }
        public int CheckpointId { get; set; }
        public string? SampleNo { get; set; }
        public string? Description { get; set; }
        public string? TableType { get; set; }
        public string? Location { get; set; }
        public string? EquipmentName { get; set; }
        public string? Component { get; set; }
        public string? MaterialCategory { get; set; }
        public string? HazardousMaterial { get; set; }
        public string? QuantityUnitKg { get; set; }
        public string? QuantityNo { get; set; }
        public string? QuantityTotalKg { get; set; }
        public string? ManufacturerBrandName { get; set; }
        public string? DocumentAnalysisResult { get; set; }
        public string? SamplingCheck { get; set; }
        public string? VisualCheck { get; set; }
        public string? CheckResult { get; set; }
        public string? AuditResult { get; set; }
        public bool IsPchm { get; set; }
        public string? Remarks { get; set; }
        public string? LocationRemarks { get; set; }
    }
}
