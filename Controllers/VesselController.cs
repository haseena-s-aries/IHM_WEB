using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Survery_Certification_App_Consume.Filters;
using Survery_Certification_App_Consume.Models;
using System.Net.Http.Headers;
using System.Text;

namespace Survery_Certification_App_Consume.Controllers
{
    public class VesselController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;

        public VesselController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public async Task<IActionResult> Index()
        {
            var model = new VesselMasterViewModel();

            try
            {
                var client = CreateApiClient();
                var response = await client.GetAsync("vessels");
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    model.ErrorMessage = body;
                    return View(model);
                }

                model.Vessels = JsonConvert.DeserializeObject<List<VesselDto>>(body) ?? new();
            }
            catch (Exception ex)
            {
                model.ErrorMessage = ex.Message;
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(SaveVesselRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.VesselName))
            {
                TempData["ErrorMessage"] = "Vessel name is required.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var client = CreateApiClient();
                var json = JsonConvert.SerializeObject(request);
                var response = await client.PostAsync(
                    "vessels",
                    new StringContent(json, Encoding.UTF8, "application/json")
                );
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    TempData["ErrorMessage"] = "Create vessel failed: " + ExtractApiErrorMessage(body);
                    return RedirectToAction(nameof(Index));
                }

                TempData["SuccessMessage"] = "Vessel saved successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Create vessel failed: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        public async Task<IActionResult> Update(int id, SaveVesselRequest request)
        {
            if (id <= 0)
            {
                TempData["ErrorMessage"] = "Invalid vessel selected.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(request.VesselName))
            {
                TempData["ErrorMessage"] = "Vessel name is required.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var client = CreateApiClient();
                var json = JsonConvert.SerializeObject(request);
                var response = await client.PutAsync(
                    $"vessels/{id}",
                    new StringContent(json, Encoding.UTF8, "application/json")
                );
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    TempData["ErrorMessage"] = "Update vessel failed: " + ExtractApiErrorMessage(body);
                    return RedirectToAction(nameof(Index));
                }

                TempData["SuccessMessage"] = "Vessel updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Update vessel failed: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        public async Task<IActionResult> Deactivate(int id)
        {
            try
            {
                var client = CreateApiClient();
                var response = await client.PostAsync($"vessels/{id}/deactivate", null);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    TempData["ErrorMessage"] = "Delete vessel failed: " + ExtractApiErrorMessage(body);
                    return RedirectToAction(nameof(Index));
                }

                TempData["SuccessMessage"] = "Vessel deleted successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Deactivate vessel failed: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        private HttpClient CreateApiClient()
        {
            var client = _clientFactory.CreateClient("ApiClient");
            var jwt = HttpContext.Session.GetString("JWT");

            if (!string.IsNullOrWhiteSpace(jwt))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

            return client;
        }

        private static string ExtractApiErrorMessage(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
                return "No error details returned by API.";

            try
            {
                var token = Newtonsoft.Json.Linq.JToken.Parse(body);
                var message = token["message"]?.ToString()
                    ?? token["Message"]?.ToString()
                    ?? token["title"]?.ToString()
                    ?? token["Title"]?.ToString();

                if (!string.IsNullOrWhiteSpace(message))
                    return message;
            }
            catch
            {
                // The API may return plain text.
            }

            return body;
        }
    }
}
