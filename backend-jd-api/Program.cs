// using backend_jd_api.Config;
// using backend_jd_api.Data;
// using backend_jd_api.Services;

// var builder = WebApplication.CreateBuilder(args);

// // Add services
// builder.Services.AddControllers();
// builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen();

// // Configure settings
// var appSettings = new AppSettings();
// builder.Configuration.Bind(appSettings);
// builder.Services.AddSingleton(appSettings);

// // Register services
// builder.Services.AddSingleton<MongoDbContext>();
// builder.Services.AddHttpClient<PythonService>();
// builder.Services.AddScoped<IJobService, JobService>();
// builder.Services.AddScoped<IFileStorageService, FileStorageService>();

// // Add CORS for frontend
// builder.Services.AddCors(options =>
// {
//     options.AddPolicy("AllowFrontend", policy =>
//     {
//         policy.WithOrigins(
//                 "http://localhost:3000",     // React default
//                 "http://localhost:5173",     // Vite default
//                 "http://localhost:4200"      // Angular default
//             )
//             .AllowAnyMethod()
//             .AllowAnyHeader()
//             .WithExposedHeaders("Content-Disposition");  // For file downloads
//     });
// });

// var app = builder.Build();


// // Configure pipeline
// if (app.Environment.IsDevelopment())
// {
//     app.UseSwagger();
//     app.UseSwaggerUI();
// }



// app.UseHttpsRedirection();
// app.UseCors("AllowFrontend");
// app.UseCors("AllowAll");
// app.UseAuthorization();
// app.MapControllers();

// app.Run();

// // Make Program class accessible for integration tests
// public partial class Program { }



using backend_jd_api.Config;
using backend_jd_api.Data;
using backend_jd_api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure settings
var appSettings = new AppSettings();
builder.Configuration.Bind(appSettings);
builder.Services.AddSingleton(appSettings);

// Add HttpClientFactory
builder.Services.AddHttpClient();

// Configure named HttpClient for Python API
builder.Services.AddHttpClient("PythonAPI", (serviceProvider, client) =>
{
    var settings = serviceProvider.GetRequiredService<AppSettings>();
    client.BaseAddress = new Uri(settings.PythonApi.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(settings.PythonApi.TimeoutSeconds);
    
    // Optional: Add default headers
    client.DefaultRequestHeaders.Add("User-Agent", "JobDescriptionAPI/1.0");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// Register services
builder.Services.AddSingleton<MongoDbContext>();
builder.Services.AddScoped<PythonService>();
builder.Services.AddScoped<IJobService, JobService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();

// Add CORS for frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",     // React default
                "http://localhost:5173",     // Vite default
                "http://localhost:4200",     // Angular default
                "http://127.0.0.1:5173",     // Vite alternative
                "http://127.0.0.1:3000"      // React alternative
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .WithExposedHeaders("Content-Disposition");  // For file downloads
    });
});

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthorization();
app.MapControllers();

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }
