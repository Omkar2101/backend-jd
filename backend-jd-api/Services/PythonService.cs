// using System.Text;
// using System.Text.Json;
// using System.Net.Http.Headers;
// using backend_jd_api.Models;
// using backend_jd_api.Config;

// namespace backend_jd_api.Services
// {
//     public class PythonService
//     {
//         private readonly HttpClient _httpClient;
//         private readonly ILogger<PythonService> _logger;

//         // Add this constructor for Moq
//         protected PythonService() { }

//         public PythonService(HttpClient httpClient, AppSettings settings, ILogger<PythonService> logger)
//         {
//             _httpClient = httpClient;
//             _logger = logger;

//             _httpClient.BaseAddress = new Uri(settings.PythonApi.BaseUrl);
//             _httpClient.Timeout = TimeSpan.FromSeconds(settings.PythonApi.TimeoutSeconds);
//         }

//         public virtual async Task<AnalysisResult> AnalyzeTextAsync(string text)
//         {
//             try
//             {
//                 var request = new { text = text };
//                 var json = JsonSerializer.Serialize(request);
//                 var content = new StringContent(json, Encoding.UTF8, "application/json");

//                 _logger.LogInformation("Sending request to Python API: {RequestBody}", json);

//                 var response = await _httpClient.PostAsync("/analyze", content);

//                 if (!response.IsSuccessStatusCode)
//                 {
//                     var errorContent = await response.Content.ReadAsStringAsync();
//                     _logger.LogError("Python API returned {StatusCode}: {ErrorContent}",
//                         response.StatusCode, errorContent);
//                     throw new HttpRequestException($"Python API error ({response.StatusCode}): {errorContent}");
//                 }
//                 // response.EnsureSuccessStatusCode();

//                 var responseJson = await response.Content.ReadAsStringAsync();
//                 _logger.LogInformation("Received response from Python API: {ResponseBody}", responseJson);
                
//                 var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase,PropertyNameCaseInsensitive = true };
//                 var result = JsonSerializer.Deserialize<AnalysisResult>(responseJson, options);

//                 return result ?? new AnalysisResult();
//             }
//             catch (HttpRequestException ex)
//             {
//                 throw; // preserves original type
//             }
//             catch (JsonException ex)
//             {
//                 throw; // preserves original type
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Error analyzing text with Python API");
//                 throw new Exception("Failed to analyze text", ex);
//             }
//         }

//         public virtual async Task<string> ExtractTextFromFileAsync(byte[] fileContent, string fileName)
//         {
//             try
//             {
//                 using var form = new MultipartFormDataContent();
//                 using var fileStream = new ByteArrayContent(fileContent);
//                 form.Add(fileStream, "file", fileName);

//                 var response = await _httpClient.PostAsync("/extract", form);
//                 //add debug step for the response
//                 _logger.LogInformation("Response status code: {StatusCode}", response.StatusCode);
//                 if (!response.IsSuccessStatusCode)
//                 {
//                     var errorContent = await response.Content.ReadAsStringAsync();
//                     _logger.LogError("Python API returned {StatusCode}: {ErrorContent}",
//                         response.StatusCode, errorContent);
//                     throw new HttpRequestException($"Python API error ({response.StatusCode}): {errorContent}");
//                 }


//                 response.EnsureSuccessStatusCode();

//                 var responseJson = await response.Content.ReadAsStringAsync();
//                 //add debug step
//                 _logger.LogInformation("Received response from Python API: {ResponseBody}", responseJson); //here also done no error

//                 // var result = JsonSerializer.Deserialize<TextExtractionResponse>(responseJson,
//                 //      new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

//                 // return await response.Content.ReadAsStringAsync(); //this line is making the error why is it there and why it will be problemantic
//                 return responseJson; //used
//                 // return result?.Text ?? string.Empty;
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Error extracting text from file: {FileName}", fileName);
//                 throw new Exception($"Failed to extract text from {fileName}", ex);
//             }
//         }
//     }
// }


// 2. Updated PythonService using IHttpClientFactory
using System.Text;
using System.Text.Json;
using backend_jd_api.Models;
using backend_jd_api.Config;

namespace backend_jd_api.Services
{
    // Result classes remain the same
    public class AnalysisServiceResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public AnalysisResult? AnalysisResult { get; set; }
    }

    public class TextExtractionResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string ExtractedText { get; set; } = string.Empty;
    }

    public class PythonService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<PythonService> _logger;
        private readonly AppSettings _settings;

        // Constructor for Moq testing
        protected PythonService() { }

        // Updated constructor using IHttpClientFactory
        public PythonService(IHttpClientFactory httpClientFactory, AppSettings settings, ILogger<PythonService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _settings = settings;
        }

        /// <summary>
        /// Analyzes text using the Python API
        /// </summary>
        /// <param name="text">The text to analyze</param>
        /// <returns>AnalysisServiceResult with analysis results or error information</returns>
        public virtual async Task<AnalysisServiceResult> AnalyzeTextAsync(string text)
        {
            // Input validation
            if (string.IsNullOrWhiteSpace(text))
            {
                return new AnalysisServiceResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Text cannot be empty or whitespace"
                };
            }

            // Check minimum length requirement
            if (text.Trim().Length < 50)
            {
                return new AnalysisServiceResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Text must be at least 50 characters long. Current length: {text.Trim().Length} characters"
                };
            }

            // Create HttpClient from factory - this reuses connections efficiently
            using var httpClient = _httpClientFactory.CreateClient("PythonAPI");

            // Prepare request
            var request = new { text = text };
            string json;
            
            try
            {
                json = JsonSerializer.Serialize(request);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to serialize request for text analysis");
                return new AnalysisServiceResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Failed to prepare request data"
                };
            }

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Sending request to Python API: {RequestBody}", json);

            // Send request to Python API
            HttpResponseMessage response;
            try
            {
                response = await httpClient.PostAsync("/analyze", content);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error when calling Python API for text analysis");
                return new AnalysisServiceResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Unable to connect to analysis service. Please try again later."
                };
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogError(ex, "Timeout when calling Python API for text analysis");
                return new AnalysisServiceResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Analysis service request timed out. Please try again."
                };
            }

            // Handle HTTP error responses
            if (!response.IsSuccessStatusCode)
            {
                string errorContent;
                try
                {
                    errorContent = await response.Content.ReadAsStringAsync();
                }
                catch
                {
                    errorContent = "Unable to read error response";
                }

                _logger.LogError("Python API returned {StatusCode}: {ErrorContent}",
                    response.StatusCode, errorContent);

                return new AnalysisServiceResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Analysis service error: {response.StatusCode}"
                };
            }

            // Read and parse successful response
            string responseJson;
            try
            {
                responseJson = await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read response from Python API");
                return new AnalysisServiceResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Failed to read analysis results"
                };
            }

            _logger.LogInformation("Received response from Python API: {ResponseBody}", responseJson);

            // Deserialize response
            AnalysisResult? result;
            try
            {
                var options = new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true 
                };
                result = JsonSerializer.Deserialize<AnalysisResult>(responseJson, options);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize response from Python API");
                return new AnalysisServiceResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Failed to parse analysis results"
                };
            }

            if (result == null)
            {
                _logger.LogWarning("Python API returned null result for text analysis");
                return new AnalysisServiceResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Analysis service returned invalid results"
                };
            }

            return new AnalysisServiceResult
            {
                IsSuccess = true,
                AnalysisResult = result
            };
        }

        /// <summary>
        /// Extracts text from a file using the Python API
        /// </summary>
        /// <param name="fileContent">The file content as byte array</param>
        /// <param name="fileName">The original filename</param>
        /// <returns>TextExtractionResult with extracted text or error information</returns>
        public virtual async Task<TextExtractionResult> ExtractTextFromFileAsync(byte[] fileContent, string fileName)
        {
            // Input validation
            if (fileContent == null || fileContent.Length == 0)
            {
                return new TextExtractionResult
                {
                    IsSuccess = false,
                    ErrorMessage = "File content cannot be empty"
                };
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                return new TextExtractionResult
                {
                    IsSuccess = false,
                    ErrorMessage = "File name is required"
                };
            }

            // Check file size (e.g., max 10MB)
            const int maxFileSize = 10 * 1024 * 1024; // 10MB
            if (fileContent.Length > maxFileSize)
            {
                return new TextExtractionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"File size exceeds maximum limit of {maxFileSize / (1024 * 1024)}MB"
                };
            }

            // Create HttpClient from factory
            using var httpClient = _httpClientFactory.CreateClient("PythonAPI");

            // Prepare multipart form data
            MultipartFormDataContent form;
            ByteArrayContent fileStream;
            
            try
            {
                form = new MultipartFormDataContent();
                fileStream = new ByteArrayContent(fileContent);
                form.Add(fileStream, "file", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to prepare file upload for text extraction");
                return new TextExtractionResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Failed to prepare file for processing"
                };
            }

            // Send request to Python API
            HttpResponseMessage response;
            try
            {
                response = await httpClient.PostAsync("/extract", form);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error when calling Python API for text extraction from file: {FileName}", fileName);
                return new TextExtractionResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Unable to connect to text extraction service. Please try again later."
                };
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogError(ex, "Timeout when calling Python API for text extraction from file: {FileName}", fileName);
                return new TextExtractionResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Text extraction service request timed out. Please try again."
                };
            }
            finally
            {
                // Clean up resources
                form?.Dispose();
                fileStream?.Dispose();
            }

            _logger.LogInformation("Response status code: {StatusCode}", response.StatusCode);

            // Handle HTTP error responses
            if (!response.IsSuccessStatusCode)
            {
                string errorContent;
                try
                {
                    errorContent = await response.Content.ReadAsStringAsync();
                }
                catch
                {
                    errorContent = "Unable to read error response";
                }

                _logger.LogError("Python API returned {StatusCode}: {ErrorContent}",
                    response.StatusCode, errorContent);

                // Provide more specific error messages based on status code
                string userFriendlyMessage = response.StatusCode.ToString() switch
                {
                    "400" => "Invalid file format or corrupted file",
                    "413" => "File size too large",
                    "415" => "Unsupported file type",
                    "500" => "Text extraction service temporarily unavailable",
                    _ => $"Text extraction failed with error: {response.StatusCode}"
                };

                return new TextExtractionResult
                {
                    IsSuccess = false,
                    ErrorMessage = userFriendlyMessage
                };
            }

            // Read successful response
            string responseJson;
            try
            {
                responseJson = await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read response from Python API for file: {FileName}", fileName);
                return new TextExtractionResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Failed to read extraction results"
                };
            }

            _logger.LogInformation("Received response from Python API: {ResponseBody}", responseJson);

            // Validate response content
            if (string.IsNullOrWhiteSpace(responseJson))
            {
                _logger.LogWarning("Python API returned empty response for file: {FileName}", fileName);
                return new TextExtractionResult
                {
                    IsSuccess = false,
                    ErrorMessage = "No text could be extracted from the file"
                };
            }

            return new TextExtractionResult
            {
                IsSuccess = true,
                ExtractedText = responseJson
            };
        }
    }
}