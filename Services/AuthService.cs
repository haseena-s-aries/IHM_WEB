using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class AuthService
{
    private readonly IHttpClientFactory _clientFactory;

    public AuthService(IHttpClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }

    // Sends login request and returns the JWT token or null if login fails
    public async Task<string> LoginAsync(string username, string password)
    {
        var loginData = new { username = username, password = password };
        var client = _clientFactory.CreateClient();
        var response = await client.PostAsJsonAsync("https://localhost:7069/auth/login", loginData);

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadAsStringAsync();
            var jwt = JsonConvert.DeserializeObject<dynamic>(result)?.accessToken?.ToString();
            return jwt;
        }

        return null;  // Return null if the login fails
    }
}
