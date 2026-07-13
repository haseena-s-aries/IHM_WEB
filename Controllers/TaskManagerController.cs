using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Survery_Certification_App_Consume.Filters;
using Survery_Certification_App_Consume.Models;
using Survey_Certification_App.Models;
using System.Net.Http;
using System.Threading.Tasks;


[BlockClientAccess]
public class TaskManagerController : Controller
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;

    public TaskManagerController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        // Creating a named HttpClient to keep the API base URL and client configurations separate
        _httpClient = httpClientFactory.CreateClient("YourApiClient");
        _apiBaseUrl = configuration["ApiBaseUrl"].TrimEnd('/') + "/AdminDashboard";

    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ViewBag.ApiBaseUrl = _apiBaseUrl; // Pass base URL to the view
        return View();
    }

    // Index action for listing the certificates based on type and search query
    public async Task<IActionResult> Index(string type, string search = "", int page = 1, int pageSize = 10)
    {
        if (string.IsNullOrEmpty(type))
        {
            ViewBag.Error = "Type is required";
            return View(new PagedResult<CertificateDto> { Data = new List<CertificateDto>(), TotalCount = 0 });
        }

        // Forming the API URL dynamically based on search, type, and pagination
        string apiUrl = $"{_apiBaseUrl}/GetCertificates?type={type}&search={search}&page={page}&pageSize={pageSize}";

        try
        {
            var response = await _httpClient.GetAsync(apiUrl);

            if (response.IsSuccessStatusCode)
            {
                // Deserialize the response to PagedResult<cerificatedto> model
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<PagedResult<CertificateDto>>(json);
                ViewBag.Type = type;
                ViewBag.Search = search;
                return View(result);
            }
            else
            {
                // Handle any errors from the API
                ViewBag.Error = "Failed to load data from API.";
                return View(new PagedResult<CertificateDto> { Data = new List<CertificateDto>(), TotalCount = 0 });
            }
        }
        catch (Exception ex)
        {
            // Catch exceptions during API calls
            ViewBag.Error = $"Exception occurred: {ex.Message}";
            return View(new PagedResult<CertificateDto> { Data = new List<CertificateDto>(), TotalCount = 0 });
        }
    }

    // New action to view certificate PDF
    [HttpGet]
    public async Task<IActionResult> ViewCertificate(string type, int id)
    {
        if (string.IsNullOrEmpty(type))
            return BadRequest("Certificate type is required");

        // Build API URL to get PDF bytes - you need an endpoint that returns PDF bytes
        var apiUrl = $"{_apiBaseUrl}/GetCertificatePdf?type={type}&id={id}";

        try
        {
            var response = await _httpClient.GetAsync(apiUrl);

            if (response.IsSuccessStatusCode)
            {
                var pdfBytes = await response.Content.ReadAsByteArrayAsync();
                
                // Return PDF inline so browser can show it
                return File(pdfBytes, "application/pdf", $"Certificate_{id}.pdf");
            }
            else
            {
                return NotFound("Certificate PDF not found");
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error retrieving certificate PDF: {ex.Message}");
        }
    }
}


