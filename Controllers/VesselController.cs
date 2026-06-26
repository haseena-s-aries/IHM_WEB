using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
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
                    TempData["ErrorMessage"] = "Create vessel failed: " + body;
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
        public async Task<IActionResult> Deactivate(int id)
        {
            try
            {
                var client = CreateApiClient();
                var response = await client.PostAsync($"vessels/{id}/deactivate", null);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    TempData["ErrorMessage"] = "Deactivate vessel failed: " + body;
                    return RedirectToAction(nameof(Index));
                }

                TempData["SuccessMessage"] = "Vessel deactivated successfully.";
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
    }
}
