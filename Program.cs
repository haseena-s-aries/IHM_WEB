using Survery_Certification_App_Consume.Services;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// HttpClient factory
builder.Services.AddHttpClient();                     // general pool
builder.Services.AddHttpClient<ProfileServices>();
builder.Services.AddSingleton<AuthService>();
//builder.Services.AddHttpClient<AuthController>();



builder.Services.AddHttpClient("ApiClient", (sp, client) =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var baseUrl = cfg["ApiBaseUrl"]; // e.g., "https://localhost:7048/api/"
    if (string.IsNullOrWhiteSpace(baseUrl))
        throw new InvalidOperationException("Missing configuration: ApiBaseUrl.");
    client.BaseAddress = new Uri(baseUrl.EndsWith("/") ? baseUrl : baseUrl + "/");
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

    // Increase the timeout to 10 minutes
    client.Timeout = TimeSpan.FromMinutes(10);
})
.ConfigurePrimaryHttpMessageHandler(sp =>
{
    var h = new HttpClientHandler();
    if (sp.GetRequiredService<IHostEnvironment>().IsDevelopment())
        h.ServerCertificateCustomValidationCallback = (_, __, ___, ____) => true;
    return h;
})
;

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader());
});




// Sessions
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o =>
{
    o.IdleTimeout = TimeSpan.FromMinutes(30);
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
});

// Cookies auth (if you use it)
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", o =>
    {
        o.LoginPath = "/Auth/Login";
        o.LogoutPath = "/Auth/Logout";
        o.AccessDeniedPath = "/Auth/AccessDenied";
    });
builder.Services.AddAuthorization();

var app = builder.Build();


// Verify env quickly (remove later if you want)
app.MapGet("/__env", (IHostEnvironment e) => Results.Text($"ENV={e.EnvironmentName}"));

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseStatusCodePages();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSession();
app.UseRouting();
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=landingpage}/{id?}");

app.Run();
