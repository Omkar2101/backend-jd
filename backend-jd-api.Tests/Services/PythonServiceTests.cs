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
        private readonly Mock<ILogger<PythonService>> _mockLogger;
        private readonly PythonService _service;
        private readonly AppSettings _settings;

        public PythonServiceTests()
        {
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _mockLogger = new Mock<ILogger<PythonService>>();
            
            _settings = new AppSettings
            {
                PythonApi = new backend_jd_api.Config.PythonApiConfig
                {
                    BaseUrl = "http://localhost:8000",
                    TimeoutSeconds = 300
                }
            };

            var client = new HttpClient(_mockHttpMessageHandler.Object);
            _service = new PythonService(client, _settings, _mockLogger.Object);
        }

        [Fact]
        public async Task AnalyzeTextAsync_WithValidText_ReturnsAnalysisResult()
        {
            // Arrange
            var text = "Test job description";
            var expectedResult = new AnalysisResult
            {
                ImprovedText = "Improved job description",
                suggestions = new List<Suggestion>
                {
                    new Suggestion { Original = "Suggestion 1" },
                    new Suggestion { Original = "Suggestion 2" }
                }
            };

            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(expectedResult))
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
            Assert.NotNull(result);
            Assert.Equal("Improved job description", result.ImprovedText);
            Assert.NotNull(result.suggestions);
            Assert.Equal(2, result.suggestions.Count);
            Assert.Equal("Suggestion 1", result.suggestions[0].Original);
            Assert.Equal("Suggestion 2", result.suggestions[1].Original);
            _mockHttpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == HttpMethod.Post && 
                    req.RequestUri.ToString().Contains(_settings.PythonApi.BaseUrl)),
                ItExpr.IsAny<CancellationToken>()
            );
        }

        [Fact]
        public async Task AnalyzeTextAsync_WithFailedRequest_ThrowsException()
        {
            // Arrange
            var text = "Test job description";

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

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() =>
                _service.AnalyzeTextAsync(text));
        }

        [Fact]
        public async Task AnalyzeTextAsync_WithTimeout_ThrowsException()
        {
            // Arrange
            var text = "Test job description";

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new TaskCanceledException());

            // Act & Assert
            await Assert.ThrowsAsync<TaskCanceledException>(() =>
                _service.AnalyzeTextAsync(text));
        }

        [Fact]
        public async Task AnalyzeTextAsync_WithInvalidResponse_ThrowsException()
        {
            // Arrange
            var text = "Test job description";
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

            // Act & Assert
            await Assert.ThrowsAsync<JsonException>(() =>
                _service.AnalyzeTextAsync(text));
        }
    }
}
