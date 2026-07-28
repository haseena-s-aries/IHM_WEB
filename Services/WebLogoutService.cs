using Microsoft.AspNetCore.Authentication;
using System.Net.Http.Headers;

namespace Survery_Certification_App_Consume.Services
{
    public sealed class WebLogoutService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<WebLogoutService> _logger;

        public WebLogoutService(IHttpClientFactory httpClientFactory, ILogger<WebLogoutService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task LogoutAsync(HttpContext context)
        {
            var jwt = context.Session.GetString("JWT");
            await TryBackendLogoutAsync(jwt);

            context.Session.Clear();
            await context.SignOutAsync("Cookies");

            foreach (var cookieName in context.Request.Cookies.Keys)
            {
                context.Response.Cookies.Delete(cookieName);
            }

            ApplyNoCacheHeaders(context.Response);
        }

        private async Task TryBackendLogoutAsync(string? jwt)
        {
            if (string.IsNullOrWhiteSpace(jwt))
                return;

            try
            {
                var client = _httpClientFactory.CreateClient("ApiClient");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                using var response = await client.PostAsync("User/logout", content: null, timeout.Token);
                if ((int)response.StatusCode == StatusCodes.Status404NotFound ||
                    (int)response.StatusCode == StatusCodes.Status405MethodNotAllowed)
                    return;

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogDebug("Backend logout returned {StatusCode}. Continuing local logout.", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Backend logout failed. Continuing local logout.");
            }
        }

        private static void ApplyNoCacheHeaders(HttpResponse response)
        {
            response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
            response.Headers["Pragma"] = "no-cache";
            response.Headers["Expires"] = "0";
            response.Headers["Surrogate-Control"] = "no-store";
        }
    }
}
