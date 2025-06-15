using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;
using backend_jd_api.Models;
using backend_jd_api.Config;

namespace backend_jd_api.Services
{
    public class PythonService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<PythonService> _logger;

        public PythonService(HttpClient httpClient, AppSettings settings, ILogger<PythonService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            _httpClient.BaseAddress = new Uri(settings.PythonApi.BaseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(settings.PythonApi.TimeoutSeconds);
        }

        public async Task<AnalysisResult> AnalyzeTextAsync(string text)
        {
            try
            {
                var request = new { text = text };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("Sending request to Python API: {RequestBody}", json);

                var response = await _httpClient.PostAsync("/analyze", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Python API returned {StatusCode}: {ErrorContent}",
                        response.StatusCode, errorContent);
                    throw new HttpRequestException($"Python API error ({response.StatusCode}): {errorContent}");
                }
                // response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Received response from Python API: {ResponseBody}", responseJson);
                
                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var result = JsonSerializer.Deserialize<AnalysisResult>(responseJson, options);

                return result ?? new AnalysisResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing text with Python API");
                throw new Exception("Failed to analyze text", ex);
            }
        }

        // public async Task<string> ExtractTextFromFileAsync(byte[] fileContent, string fileName, bool isImage = false)
        // {
        //     try
        //     {
        //         var content = new MultipartFormDataContent();
        //         var fileBytes = new ByteArrayContent(fileContent);
        //         fileBytes.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
        //         content.Add(fileBytes, "file", fileName);
        //         content.Add(new StringContent(isImage.ToString()), "is_image");

        //         var response = await _httpClient.PostAsync("/extract", content);
        //         response.EnsureSuccessStatusCode();

        //         var responseJson = await response.Content.ReadAsStringAsync();
        //         var result = JsonSerializer.Deserialize<TextExtractionResponse>(responseJson, 
        //             new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        //         return result?.Text ?? string.Empty;
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, "Error extracting text from file {FileName}", fileName);
        //         throw new Exception($"Failed to extract text from file {fileName}", ex);
        //     }
        // }
        public async Task<string> ExtractTextFromFileAsync(byte[] fileContent, string fileName)
        {
            try
            {
                using var form = new MultipartFormDataContent();
                using var fileStream = new ByteArrayContent(fileContent);
                form.Add(fileStream, "file", fileName);

                var response = await _httpClient.PostAsync("/extract", form);
                //add debug step for the response
                _logger.LogInformation("Response status code: {StatusCode}", response.StatusCode);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Python API returned {StatusCode}: {ErrorContent}",
                        response.StatusCode, errorContent);
                    throw new HttpRequestException($"Python API error ({response.StatusCode}): {errorContent}");
                }


                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                //add debug step
                _logger.LogInformation("Received response from Python API: {ResponseBody}", responseJson); //here also done no error

                // var result = JsonSerializer.Deserialize<TextExtractionResponse>(responseJson,
                //      new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

                return await response.Content.ReadAsStringAsync(); //this line is making the erreo why is ti there and why it will be problemantic
                // return result?.Text ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from file: {FileName}", fileName);
                throw new Exception($"Failed to extract text from {fileName}", ex);
            }
        }
    }
}