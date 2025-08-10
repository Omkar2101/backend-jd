using backend_jd_api.Config;
using backend_jd_api.Data;
using backend_jd_api.Services;

var builder = WebApplication.CreateBuilder(args);

// Environment variables will override appsettings.json values
builder.Configuration.AddEnvironmentVariables();

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure settings
var appSettings = new AppSettings();
builder.Configuration.Bind(appSettings);
builder.Services.AddSingleton(appSettings);

// Register services
builder.Services.AddSingleton<MongoDbContext>();
builder.Services.AddHttpClient<PythonService>();
builder.Services.AddScoped<IJobService, JobService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();

// Add CORS - Allow All Origins, Methods, and Headers
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
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
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }