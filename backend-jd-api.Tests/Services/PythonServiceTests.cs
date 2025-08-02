using Xunit;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using System.Text.Json;
using backend_jd_api.Services;
using backend_jd_api.Models;
using backend_jd_api.Config;
using Microsoft.Extensions.Logging;

namespace backend_jd_api.Tests.Services
{
    public class PythonServiceTests
    {
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
        private readonly Mock<ILogger<PythonService>> _mockLogger;
        private readonly PythonService _service;
        private readonly AppSettings _settings;
        private readonly HttpClient _httpClient;

        public PythonServiceTests()
        {
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
            _mockLogger = new Mock<ILogger<PythonService>>();
            
            _settings = new AppSettings
            {
                PythonApi = new backend_jd_api.Config.PythonApiConfig
                {
                    BaseUrl = "http://localhost:8000",
                    TimeoutSeconds = 300
                }
            };

            _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri(_settings.PythonApi.BaseUrl)
            };

            _mockHttpClientFactory.Setup(x => x.CreateClient("PythonAPI")).Returns(_httpClient);
            _service = new PythonService(_mockHttpClientFactory.Object, _settings, _mockLogger.Object);
        }

        [Fact]
        public async Task AnalyzeTextAsync_WithValidText_ReturnsAnalysisResult()
        {
            // Arrange
            var text = "This is a valid job description text with more than 50 characters for testing purposes.";
            var expectedResult = new AnalysisResult
            {
                ImprovedText = "Improved job description",
                bias_score = 0.2,
                inclusivity_score = 0.8,
                clarity_score = 0.9,
                role = "Software Engineer",
                industry = "Technology",
                overall_assessment = "Good job description with minor improvements needed",
                Issues = new List<Issue>
                {
                    new Issue
                    {
                        Type = "Gender",
                        Text = "Use of gendered language",
                        Severity = "Medium",
                        Explanation = "Consider using more inclusive language"
                    }
                },
                seo_keywords = new List<string> { "software", "engineer", "development", "technology" },
                suggestions = new List<Suggestion>
                {
                    new Suggestion 
                    { 
                        Original = "Suggestion 1",
                        Improved = "Improved 1",
                        rationale = "Rationale 1",
                        Category = "Category 1"
                    },
                    new Suggestion 
                    { 
                        Original = "Suggestion 2",
                        Improved = "Improved 2",
                        rationale = "Rationale 2",
                        Category = "Category 2"
                    }
                }
            };

            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(expectedResult), Encoding.UTF8, "application/json")
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            // Act
            var result = await _service.AnalyzeTextAsync(text);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.AnalysisResult);
            Assert.Equal("Improved job description", result.AnalysisResult.ImprovedText);
            Assert.Equal(0.2, result.AnalysisResult.bias_score);
            Assert.Equal(0.8, result.AnalysisResult.inclusivity_score);
            Assert.Equal(0.9, result.AnalysisResult.clarity_score);
            Assert.Equal("Software Engineer", result.AnalysisResult.role);
            Assert.Equal("Technology", result.AnalysisResult.industry);
            Assert.Equal("Good job description with minor improvements needed", result.AnalysisResult.overall_assessment);
            Assert.NotNull(result.AnalysisResult.Issues);
            Assert.Single(result.AnalysisResult.Issues);
            Assert.Equal("Gender", result.AnalysisResult.Issues[0].Type);
            Assert.NotNull(result.AnalysisResult.seo_keywords);
            Assert.Equal(4, result.AnalysisResult.seo_keywords.Count);
            Assert.Contains("software", result.AnalysisResult.seo_keywords);
            Assert.NotNull(result.AnalysisResult.suggestions);
            Assert.Equal(2, result.AnalysisResult.suggestions.Count);
            Assert.Equal("Suggestion 1", result.AnalysisResult.suggestions[0].Original);
            Assert.Equal("Suggestion 2", result.AnalysisResult.suggestions[1].Original);

            _mockHttpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == HttpMethod.Post && 
                    req.RequestUri.ToString().Contains("/analyze")),
                ItExpr.IsAny<CancellationToken>()
            );
        }

        [Fact]
        public async Task AnalyzeTextAsync_WithFailedRequest_ReturnsFailureResult()
        {
            // Arrange
            var text = "This is a valid job description text with more than 50 characters for testing purposes.";

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    Content = new StringContent("Server error")
                });

            // Act
            var result = await _service.AnalyzeTextAsync(text);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Analysis service error", result.ErrorMessage);
        }

        [Fact]
        public async Task AnalyzeTextAsync_WithInvalidResponse_ReturnsFailureResult()
        {
            // Arrange
            var text = "This is a valid job description text with more than 50 characters for testing purposes.";
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("Invalid JSON")
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            // Act
            var result = await _service.AnalyzeTextAsync(text);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Failed to parse analysis results", result.ErrorMessage);
        }

        [Fact]
        public async Task AnalyzeTextAsync_WithShortText_ReturnsFailureResult()
        {
            // Arrange
            var shortText = "Too short";

            // Act
            var result = await _service.AnalyzeTextAsync(shortText);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("at least 50 characters", result.ErrorMessage);
        }

        [Fact]
        public async Task ExtractTextFromFileAsync_WithValidFile_ReturnsExtractedText()
        {
            // Arrange
            var fileContent = Encoding.UTF8.GetBytes("This is test file content");
            var fileName = "test.txt";
            var expectedText = "Extracted text from file";

            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(expectedText)
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            // Act
            var result = await _service.ExtractTextFromFileAsync(fileContent, fileName);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(expectedText, result.ExtractedText);

            _mockHttpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == HttpMethod.Post && 
                    req.RequestUri.ToString().Contains("/extract")),
                ItExpr.IsAny<CancellationToken>()
            );
        }

        [Fact]
        public async Task ExtractTextFromFileAsync_WithEmptyFile_ReturnsFailureResult()
        {
            // Arrange
            var emptyFileContent = Array.Empty<byte>();
            var fileName = "empty.txt";

            // Act
            var result = await _service.ExtractTextFromFileAsync(emptyFileContent, fileName);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("File content cannot be empty", result.ErrorMessage);
        }

       

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _httpClient?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}