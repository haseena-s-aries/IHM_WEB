using Survery_Certification_App_Consume.Models;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Survery_Certification_App_Consume.Services
{
    public class ProfileServices
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl;

        public ProfileServices(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiBaseUrl = configuration["ApiBaseUrl"] + "User";
        }

        public async Task<UserDto> GetProfileAsync(int userId)
        {
            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/GetProfile/{userId}");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<UserDto>();
            }
            return null;
        }

        public async Task<bool> UpdateProfileAsync(UserDto user)
        {
            var content = new MultipartFormDataContent
            {
                { new StringContent(user.Id.ToString()), "UserId" },
                { new StringContent(user.FullName ?? ""), "FullName" },
                { new StringContent(user.Email ?? ""), "Email" }
            };

            var response = await _httpClient.PostAsync($"{_apiBaseUrl}/UpdateProfile", content);
            return response.IsSuccessStatusCode;
        }
    }
}
