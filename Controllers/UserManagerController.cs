using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Survery_Certification_App_Consume.Filters;
using Survery_Certification_App_Consume.Models;
using Survey_Certification_App.Models;
using System.Net.Http.Headers;
using System.Text;


[BlockClientAccess]
public class UserManagerController : Controller
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;

    public UserManagerController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient("ApiClient");
        _apiBaseUrl = "AdminDashboard";
    }
    public IActionResult Index()
    {
        ViewBag.UserRole = HttpContext.Session.GetString("UserRole") ?? "";
        ViewBag.ProjectId = HttpContext.Session.GetString("ProjectId") ?? "";
        ViewBag.ProjectName = HttpContext.Session.GetString("ProjectName") ?? "";
        return View();
    }

    private bool IsAdmin()
    {
        return string.Equals(HttpContext.Session.GetString("UserRole"), "Admin", StringComparison.OrdinalIgnoreCase);
    }
    [HttpGet]
    public async Task<IActionResult> Projects()
    {
        var request = CreateApiRequest(HttpMethod.Get, "User/projects");
        var response = await _httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode, json);

        return Content(json, "application/json");
    }

    [HttpGet]
    public async Task<IActionResult> Roles(int projectId)
    {
        var request = CreateApiRequest(HttpMethod.Get, $"AdminDashboard/projects/{projectId}/roles");
        var response = await _httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode, json);

        return Content(json, "application/json");
    }
    private static bool IsAdminRole(string? role)
    {
        return string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);
    }

    private HttpRequestMessage CreateApiRequest(HttpMethod method, string url, HttpContent? content = null)
    {
        var request = new HttpRequestMessage(method, url) { Content = content };
        var token = HttpContext.Session.GetString("JWT");
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return request;
    }

    public async Task<IActionResult> GetAll()
    {
        var response = await _httpClient.SendAsync(
            CreateApiRequest(HttpMethod.Get, $"{_apiBaseUrl}/GetAll"));

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, string.IsNullOrWhiteSpace(error)
                ? "Unable to load users."
                : error);
        }

        var jsonData = await response.Content.ReadAsStringAsync();
        var userDtos = JsonConvert.DeserializeObject<List<UserDto>>(jsonData) ?? new List<UserDto>();

        if (!IsAdmin())
        {
            userDtos = userDtos
                .Where(u => !IsAdminRole(u.Role))
                .Where(u => !IsAdminRole(u.RequestedRole))
                .Where(u => u.IsApproved)
                .ToList();
        }

        return Json(userDtos);
    }

    // ✅ Get User by ID (AJAX)
    public async Task<IActionResult> GetById(int id)
    {
        var response = await _httpClient.SendAsync(
            CreateApiRequest(HttpMethod.Get, $"{_apiBaseUrl}/GetById/{id}"));
        if (!response.IsSuccessStatusCode)
            return NotFound();

        var jsonData = await response.Content.ReadAsStringAsync();
        var user = JsonConvert.DeserializeObject<UserDto>(jsonData);

        if (!IsAdmin() && (IsAdminRole(user?.Role) || IsAdminRole(user?.RequestedRole)))
            return Forbid();

        return Json(user);
    }

    // ✅ Create or Update User
    [HttpPost]
    public async Task<JsonResult> SaveUser([FromBody] UserDto user)
    {
        if (!ModelState.IsValid)
            return Json(new { success = false, message = "Invalid data" });
        if (!IsAdmin() && (IsAdminRole(user.Role) || IsAdminRole(user.RequestedRole)))
        {
            return Json(new
            {
                success = false,
                message = "Only Admin can manage Admin users."
            });
        }
        if (!IsAdmin())
        {
            var projectIdText = HttpContext.Session.GetString("ProjectId");
            if (int.TryParse(projectIdText, out var projectId))
                user.ProjectId = projectId;

            user.ProjectName = HttpContext.Session.GetString("ProjectName");
        }
        var jsonContent = new StringContent(JsonConvert.SerializeObject(user), Encoding.UTF8, "application/json");
        HttpResponseMessage response;

        if (user.Id == 0)
            response = await _httpClient.SendAsync(
                CreateApiRequest(HttpMethod.Post, $"{_apiBaseUrl}/Save", jsonContent));
        else
            response = await _httpClient.SendAsync(
                CreateApiRequest(HttpMethod.Put, $"{_apiBaseUrl}/Save", jsonContent));

        if (response.IsSuccessStatusCode)
            return Json(new { success = true });

        var errorMsg = await response.Content.ReadAsStringAsync();

        try
        {
            dynamic? errorObj = JsonConvert.DeserializeObject(errorMsg);
            if (errorObj?.message != null)
                errorMsg = errorObj.message;
        }
        catch
        {
        }

        return Json(new { success = false, message = string.IsNullOrWhiteSpace(errorMsg) ? "Save failed." : errorMsg });
    }


    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        if (!IsAdmin())
        {
            var getResponse = await _httpClient.SendAsync(
                CreateApiRequest(HttpMethod.Get, $"{_apiBaseUrl}/GetById/{id}"));

            if (getResponse.IsSuccessStatusCode)
            {
                var jsonData = await getResponse.Content.ReadAsStringAsync();
                var user = JsonConvert.DeserializeObject<UserDto>(jsonData);

                if (IsAdminRole(user?.Role))
                    return Forbid();
            }
        }
        var response = await _httpClient.SendAsync(
            CreateApiRequest(HttpMethod.Delete, $"User/delete-user/{id}"));
        if (response.IsSuccessStatusCode)
        {
            return Ok();
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync();
            return BadRequest(error);
        }
    }
}
