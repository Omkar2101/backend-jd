
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
using Xunit;
using backend_jd_api.Controllers;
using backend_jd_api.Models;
using backend_jd_api.Services;
using backend_jd_api.Data;
using System.Text.Json;

namespace backend_jd_api.Tests.Controllers
{
    public class JobControllerTests
    {
        private readonly Mock<IJobService> _mockJobService;
        private readonly Mock<ILogger<JobController>> _mockControllerLogger;
        private readonly JobController _controller;

        public JobControllerTests()
        {
            _mockJobService = new Mock<IJobService>();
            _mockControllerLogger = new Mock<ILogger<JobController>>();
            _controller = new JobController(_mockJobService.Object, _mockControllerLogger.Object);
        }

        private IFormFile CreateMockFormFile(string fileName, string content)
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            var file = new Mock<IFormFile>();

            file.Setup(f => f.FileName).Returns(fileName);
            file.Setup(f => f.Length).Returns(bytes.Length);
            file.Setup(f => f.ContentType).Returns("text/plain");
            file.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(bytes));
            file.Setup(f => f.ContentDisposition).Returns($"form-data; name=\"file\"; filename=\"{fileName}\"");
            file.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns((Stream stream, CancellationToken token) =>
                {
                    var sourceStream = new MemoryStream(bytes);
                    return sourceStream.CopyToAsync(stream, token);
                });

            return file.Object;
        }

        [Fact]
        public async Task UploadFile_WithValidRequest_ReturnsOkResult()
        {
            // Arrange
            var fileContent = "This is a test job description content that is longer than 50 characters to meet the minimum requirement.";
            var mockFile = CreateMockFormFile("test.txt", fileContent);
            var request = new UploadRequest
            {
                File = mockFile,
                UserEmail = "test@example.com"
            };

            var serviceResult = new JobAnalysisResult 
            { 
                IsSuccess = true,
                JobResponse = new JobResponse
                {
                    Id = "507f1f77bcf86cd799439011",
                    OriginalText = fileContent,
                    ImprovedText = "Improved job description content",
                    FileName = "test.txt",
                    UserEmail = "test@example.com",
                    CreatedAt = DateTime.UtcNow,
                    Analysis = new AnalysisResult
                    {
                        ImprovedText = "Improved job description content",
                        role = "Software Developer",
                        industry = "Technology",
                        overall_assessment = "Good job description with minor improvements needed",
                        Issues = new List<Issue>(),
                        suggestions = new List<Suggestion>()
                    }
                }
            };

            _mockJobService
                .Setup(s => s.AnalyzeFromFileAsync(It.IsAny<IFormFile>(), "test@example.com"))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _controller.UploadFile(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<JobAnalysisResult>(okResult.Value);
            Assert.True(response.IsSuccess);
            Assert.Equal(serviceResult.JobResponse.Id, response.JobResponse.Id);
            Assert.Equal(serviceResult.JobResponse.OriginalText, response.JobResponse.OriginalText);
            Assert.Equal(serviceResult.JobResponse.ImprovedText, response.JobResponse.ImprovedText);
            Assert.Equal(serviceResult.JobResponse.FileName, response.JobResponse.FileName);
            Assert.Equal(serviceResult.JobResponse.UserEmail, response.JobResponse.UserEmail);

            // Verify service calls
            _mockJobService.Verify(s => s.AnalyzeFromFileAsync(It.IsAny<IFormFile>(), "test@example.com"), Times.Once);
        }

        [Fact]
        public async Task UploadFile_WithNullFile_ReturnsBadRequest()
        {
            // Arrange
            var request = new UploadRequest
            {
                File = null,
                UserEmail = "test@example.com"
            };

            // Act
            var result = await _controller.UploadFile(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorResponse = Assert.IsAssignableFrom<object>(badRequestResult.Value);
            var errorDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
                JsonSerializer.Serialize(errorResponse)
            );

            Assert.True((bool)JsonSerializer.Deserialize<bool>(errorDict["error"].ToString()));
            Assert.Equal("No file uploaded", errorDict["message"].ToString());
            Assert.Equal("validation_error", errorDict["type"].ToString());
            Assert.Equal(400, JsonSerializer.Deserialize<int>(errorDict["status_code"].ToString()));
        }

        [Fact]
        public async Task UploadFile_WithEmptyFile_ReturnsBadRequest()
        {
            // Arrange
            var file = new Mock<IFormFile>();
            file.Setup(f => f.Length).Returns(0);
            file.Setup(f => f.FileName).Returns("test.txt");

            var request = new UploadRequest
            {
                File = file.Object,
                UserEmail = "test@example.com"
            };

            // Act
            var result = await _controller.UploadFile(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorResponse = Assert.IsAssignableFrom<object>(badRequestResult.Value);
            var errorDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
                JsonSerializer.Serialize(errorResponse)
            );

            Assert.Equal("No file uploaded", errorDict["message"].ToString());
            Assert.Equal("validation_error", errorDict["type"].ToString());
            Assert.Equal(400, JsonSerializer.Deserialize<int>(errorDict["status_code"].ToString()));
        }

        [Fact]
        public async Task UploadFile_WithNullUserEmail_ReturnsBadRequest()
        {
            // Arrange
            var mockFile = CreateMockFormFile("test.txt", "Test content");
            var request = new UploadRequest
            {
                File = mockFile,
                UserEmail = null
            };

            // Act
            var result = await _controller.UploadFile(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorResponse = Assert.IsAssignableFrom<object>(badRequestResult.Value);
            var errorDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
                JsonSerializer.Serialize(errorResponse)
            );

            Assert.Equal("User email is required", errorDict["message"].ToString());
            Assert.Equal("validation_error", errorDict["type"].ToString());
            Assert.Equal(400, JsonSerializer.Deserialize<int>(errorDict["status_code"].ToString()));
        }

        [Fact]
        public async Task UploadFile_WithEmptyUserEmail_ReturnsBadRequest()
        {
            // Arrange
            var mockFile = CreateMockFormFile("test.txt", "Test content");
            var request = new UploadRequest
            {
                File = mockFile,
                UserEmail = ""
            };

            // Act
            var result = await _controller.UploadFile(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorResponse = Assert.IsAssignableFrom<object>(badRequestResult.Value);
            var errorDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
                JsonSerializer.Serialize(errorResponse)
            );

            Assert.Equal("User email is required", errorDict["message"].ToString());
            Assert.Equal("validation_error", errorDict["type"].ToString());
            Assert.Equal(400, JsonSerializer.Deserialize<int>(errorDict["status_code"].ToString()));
        }

        [Theory]
        [InlineData("test.exe")]
        [InlineData("test.bat")]
        [InlineData("test.js")]
        [InlineData("test.xml")]
        public async Task UploadFile_WithInvalidFileType_ReturnsBadRequest(string fileName)
        {
            // Arrange
            var mockFile = CreateMockFormFile(fileName, "Test content");
            var request = new UploadRequest
            {
                File = mockFile,
                UserEmail = "test@example.com"
            };

            // Act
            var result = await _controller.UploadFile(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorResponse = Assert.IsAssignableFrom<object>(badRequestResult.Value);
            var errorDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
                JsonSerializer.Serialize(errorResponse)
            );

            Assert.Contains("Invalid file type", errorDict["message"].ToString());
            Assert.Equal("validation_error", errorDict["type"].ToString());
            Assert.Equal(400, JsonSerializer.Deserialize<int>(errorDict["status_code"].ToString()));
        }

        [Fact]
        public async Task UploadFile_WithLargeFile_ReturnsBadRequest()
        {
            // Arrange
            var largeContent = new string('a', 11 * 1024 * 1024); // 11MB content
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.txt");
            mockFile.Setup(f => f.Length).Returns(11 * 1024 * 1024); // 11MB
            mockFile.Setup(f => f.ContentType).Returns("text/plain");

            var request = new UploadRequest
            {
                File = mockFile.Object,
                UserEmail = "test@example.com"
            };

            // Act
            var result = await _controller.UploadFile(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorResponse = Assert.IsAssignableFrom<object>(badRequestResult.Value);
            var errorDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
                JsonSerializer.Serialize(errorResponse)
            );

            Assert.Equal("File size too large. Maximum allowed size is 10MB.", errorDict["message"].ToString());
            Assert.Equal("validation_error", errorDict["type"].ToString());
            Assert.Equal(400, JsonSerializer.Deserialize<int>(errorDict["status_code"].ToString()));
        }

        [Theory]
        [InlineData("test.txt")]
        [InlineData("test.doc")]
        [InlineData("test.docx")]
        [InlineData("test.pdf")]
        [InlineData("test.jpg")]
        [InlineData("test.jpeg")]
        [InlineData("test.png")]
        public async Task UploadFile_WithValidFileTypes_ProcessesSuccessfully(string fileName)
        {
            // Arrange
            var fileContent = "This is a test job description content that is longer than 50 characters to meet the minimum requirement.";
            var mockFile = CreateMockFormFile(fileName, fileContent);
            var request = new UploadRequest
            {
                File = mockFile,
                UserEmail = "test@example.com"
            };

            var serviceResult = new JobAnalysisResult
            {
                IsSuccess = true,
                JobResponse = new JobResponse
                {
                    Id = "507f1f77bcf86cd799439011",
                    OriginalText = fileContent,
                    ImprovedText = "Improved content",
                    FileName = fileName,
                    UserEmail = "test@example.com",
                    CreatedAt = DateTime.UtcNow,
                    Analysis = new AnalysisResult
                    {
                        ImprovedText = "Improved content",
                        role = "Software Developer",
                        industry = "Technology",
                        overall_assessment = "Good job description",
                        Issues = new List<Issue>(),
                        suggestions = new List<Suggestion>()
                    }
                }
            };

            _mockJobService
                .Setup(s => s.AnalyzeFromFileAsync(It.IsAny<IFormFile>(), "test@example.com"))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _controller.UploadFile(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<JobAnalysisResult>(okResult.Value);
            Assert.True(response.IsSuccess);
            Assert.Equal(serviceResult.JobResponse.Id, response.JobResponse.Id);
            Assert.Equal(serviceResult.JobResponse.FileName, response.JobResponse.FileName);
            _mockJobService.Verify(s => s.AnalyzeFromFileAsync(It.IsAny<IFormFile>(), "test@example.com"), Times.Once);
        }

        [Fact]
        public async Task UploadFile_ServiceError_ReturnsUnprocessableEntity()
        {
            // Arrange
            var fileContent = "This is a test job description content that is longer than 50 characters.";
            var mockFile = CreateMockFormFile("test.txt", fileContent);
            var request = new UploadRequest
            {
                File = mockFile,
                UserEmail = "test@example.com"
            };

            _mockJobService
                .Setup(s => s.AnalyzeFromFileAsync(It.IsAny<IFormFile>(), "test@example.com"))
                .ReturnsAsync((JobAnalysisResult)null);

            // Act
            var result = await _controller.UploadFile(request);

            // Assert
            var unprocessableEntityResult = Assert.IsType<UnprocessableEntityObjectResult>(result);
            var errorResponse = Assert.IsAssignableFrom<object>(unprocessableEntityResult.Value);
            var errorDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
                JsonSerializer.Serialize(errorResponse)
            );

            Assert.Equal(422, JsonSerializer.Deserialize<int>(errorDict["status_code"].ToString()));
            Assert.Equal("processing_error", errorDict["type"].ToString());
            Assert.Contains("Unable to process", errorDict["message"].ToString());
            Assert.Equal("Our AI analysis service is temporarily unavailable. Please try again in a few moments.", messageProperty.GetValue(errorResponse));
        }

        [Fact]
        public async Task UploadFile_TaskCanceledException_ReturnsTimeout()
        {
            // Arrange
            var fileContent = "This is a test job description content that is longer than 50 characters.";
            var mockFile = CreateMockFormFile("test.txt", fileContent);
            var request = new UploadRequest
            {
                File = mockFile,
                UserEmail = "test@example.com"
            };

            var timeoutException = new TimeoutException();
            var taskCanceledException = new TaskCanceledException("Task was canceled", timeoutException);

            _mockJobService
                .Setup(s => s.AnalyzeFromFileAsync(It.IsAny<IFormFile>(), "test@example.com"))
                .ThrowsAsync(taskCanceledException);

            // Act
            var result = await _controller.UploadFile(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(504, statusCodeResult.StatusCode);

            var errorResponse = statusCodeResult.Value;
            var messageProperty = errorResponse.GetType().GetProperty("message");
            Assert.Equal("The analysis is taking longer than expected. Please try again with a shorter job description.", messageProperty.GetValue(errorResponse));
        }

        [Fact]
        public async Task UploadFile_GenericException_ReturnsInternalServerError()
        {
            // Arrange
            var fileContent = "This is a test job description content that is longer than 50 characters.";
            var mockFile = CreateMockFormFile("test.txt", fileContent);
            var request = new UploadRequest
            {
                File = mockFile,
                UserEmail = "test@example.com"
            };

            _mockJobService
                .Setup(s => s.AnalyzeFromFileAsync(It.IsAny<IFormFile>(), "test@example.com"))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.UploadFile(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);

            var errorResponse = statusCodeResult.Value;
            var messageProperty = errorResponse.GetType().GetProperty("message");
            Assert.Equal("An unexpected error occurred while processing your file. Please try again.", messageProperty.GetValue(errorResponse));
        }

        [Fact]
        public async Task UploadFile_CaseInsensitiveFileExtension_AcceptsValidTypes()
        {
            // Arrange
            var fileContent = "This is a test job description content that is longer than 50 characters to meet the minimum requirement.";
            var mockFile = CreateMockFormFile("test.TXT", fileContent);
            var request = new UploadRequest
            {
                File = mockFile,
                UserEmail = "test@example.com"
            };

            var expectedResponse = new JobResponse
            {
                Id = "507f1f77bcf86cd799439011",
                OriginalText = fileContent,
                ImprovedText = "Improved content",
                FileName = "test.TXT",
                UserEmail = "test@example.com",
                CreatedAt = DateTime.UtcNow,
                Analysis = new AnalysisResult
                {
                    ImprovedText = "Improved content",
                    role = "Software Developer",
                    industry = "Technology",
                    overall_assessment = "Good job description",
                    Issues = new List<Issue>(),
                    suggestions = new List<Suggestion>()
                }
            };

            _mockJobService
                .Setup(s => s.AnalyzeFromFileAsync(It.IsAny<IFormFile>(), "test@example.com"))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.UploadFile(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            _mockJobService.Verify(s => s.AnalyzeFromFileAsync(It.IsAny<IFormFile>(), "test@example.com"), Times.Once);
        }

        [Fact]
        public async Task AnalyzeText_WithValidRequest_ReturnsOkResult()
        {
            // Arrange
            var request = new AnalyzeRequest
            {
                Text = "This is a test job description content that is longer than 50 characters to meet the minimum requirement.",
                JobTitle = "Software Developer",
                UserEmail = "test@example.com"
            };

            var expectedResponse = new JobResponse
            {
                Id = "507f1f77bcf86cd799439011",
                OriginalText = request.Text,
                ImprovedText = "Improved job description content",
                UserEmail = request.UserEmail,
                CreatedAt = DateTime.UtcNow,
                Analysis = new AnalysisResult
                {
                    ImprovedText = "Improved job description content",
                    bias_score = 0.2,
                    inclusivity_score = 0.8,
                    clarity_score = 0.9,
                    role = "Software Developer",
                    industry = "Technology",
                    overall_assessment = "Excellent job description with minimal bias",
                    Issues = new List<Issue>
                    {
                        new Issue
                        {
                            Type = "Gender",
                            Text = "guys",
                            Severity = "Medium",
                            Explanation = "This term may exclude non-male team members"
                        }
                    },
                    suggestions = new List<Suggestion>
                    {
                        new Suggestion
                        {
                            Original = "guys",
                            Improved = "team members",
                            rationale = "More inclusive language",
                            Category = "Bias"
                        }
                    },
                    seo_keywords = new List<string> { "software", "developer", "programming" }
                }
            };

            _mockJobService
                .Setup(s => s.AnalyzeTextAsync(request.Text, request.UserEmail, request.JobTitle))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.AnalyzeText(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<JobResponse>(okResult.Value);
            Assert.Equal(expectedResponse.Id, response.Id);
            Assert.Equal(expectedResponse.OriginalText, response.OriginalText);
            Assert.Equal(expectedResponse.ImprovedText, response.ImprovedText);
            Assert.Equal(expectedResponse.UserEmail, response.UserEmail);
            Assert.NotNull(response.Analysis);
            Assert.Equal(expectedResponse.Analysis.bias_score, response.Analysis.bias_score);
            Assert.Equal(expectedResponse.Analysis.inclusivity_score, response.Analysis.inclusivity_score);
            Assert.Equal(expectedResponse.Analysis.clarity_score, response.Analysis.clarity_score);
            Assert.Equal(expectedResponse.Analysis.role, response.Analysis.role);
            Assert.Equal(expectedResponse.Analysis.industry, response.Analysis.industry);
            Assert.Equal(expectedResponse.Analysis.overall_assessment, response.Analysis.overall_assessment);
            Assert.Single(response.Analysis.Issues);

            // Verify service calls
            _mockJobService.Verify(s => s.AnalyzeTextAsync(request.Text, request.UserEmail, request.JobTitle), Times.Once);
        }

        [Fact]
        public async Task AnalyzeText_WithNullText_ReturnsBadRequest()
        {
            // Arrange
            var request = new AnalyzeRequest
            {
                Text = null,
                UserEmail = "test@example.com"
            };

            // Act
            var result = await _controller.AnalyzeText(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorResponse = badRequestResult.Value;

            var messageProperty = errorResponse.GetType().GetProperty("message");
            Assert.Equal("Text is required", messageProperty.GetValue(errorResponse));
        }

        [Fact]
        public async Task AnalyzeText_WithEmptyText_ReturnsBadRequest()
        {
            // Arrange
            var request = new AnalyzeRequest
            {
                Text = "",
                UserEmail = "test@example.com"
            };

            // Act
            var result = await _controller.AnalyzeText(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorResponse = badRequestResult.Value;

            var messageProperty = errorResponse.GetType().GetProperty("message");
            Assert.Equal("Text is required", messageProperty.GetValue(errorResponse));
        }

        [Fact]
        public async Task AnalyzeText_WithWhitespaceOnlyText_ReturnsBadRequest()
        {
            // Arrange
            var whitespaceText = "   ";
            var request = new AnalyzeRequest
            {
                Text = whitespaceText,
                UserEmail = "test@example.com"
            };

            // Act
            var result = await _controller.AnalyzeText(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorResponse = badRequestResult.Value;

            var messageProperty = errorResponse.GetType().GetProperty("message");
            var message = messageProperty.GetValue(errorResponse).ToString();
            Assert.Contains($"Job description text must be at least 50 characters long. Current length: {whitespaceText.Trim().Length} characters", message);
        }

        [Fact]
        public async Task AnalyzeText_WithShortText_ReturnsBadRequest()
        {
            // Arrange
            var shortText = "Short text";
            var request = new AnalyzeRequest
            {
                Text = shortText,
                UserEmail = "test@example.com"
            };

            // Act
            var result = await _controller.AnalyzeText(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorResponse = badRequestResult.Value;

            var messageProperty = errorResponse.GetType().GetProperty("message");
            var message = messageProperty.GetValue(errorResponse).ToString();
            Assert.Contains($"Job description text must be at least 50 characters long. Current length: {shortText.Length} characters", message);
        }

        [Fact]
        public async Task AnalyzeText_WithShortTextAfterTrim_ReturnsBadRequest()
        {
            // Arrange
            var shortText = "   Short text   ";
            var request = new AnalyzeRequest
            {
                Text = shortText,
                UserEmail = "test@example.com"
            };

            // Act
            var result = await _controller.AnalyzeText(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorResponse = badRequestResult.Value;

            var messageProperty = errorResponse.GetType().GetProperty("message");
            var message = messageProperty.GetValue(errorResponse).ToString();
            Assert.Contains($"Job description text must be at least 50 characters long. Current length: {shortText.Trim().Length} characters", message);
        }

        [Fact]
        public async Task AnalyzeText_WithNullUserEmail_ReturnsBadRequest()
        {
            // Arrange
            var request = new AnalyzeRequest
            {
                Text = "This is a test job description content that is longer than 50 characters to meet the minimum requirement.",
                UserEmail = null
            };

            // Act
            var result = await _controller.AnalyzeText(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorResponse = badRequestResult.Value;

            var messageProperty = errorResponse.GetType().GetProperty("message");
            Assert.Equal("User email is required", messageProperty.GetValue(errorResponse));
        }

        [Fact]
        public async Task AnalyzeText_WithEmptyUserEmail_ReturnsBadRequest()
        {
            // Arrange
            var request = new AnalyzeRequest
            {
                Text = "This is a test job description content that is longer than 50 characters to meet the minimum requirement.",
                UserEmail = ""
            };

            // Act
            var result = await _controller.AnalyzeText(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorResponse = badRequestResult.Value;

            var messageProperty = errorResponse.GetType().GetProperty("message");
            Assert.Equal("User email is required", messageProperty.GetValue(errorResponse));
        }

        [Fact]
        public async Task AnalyzeText_WithoutJobTitle_ProcessesSuccessfully()
        {
            // Arrange
            var request = new AnalyzeRequest
            {
                Text = "This is a test job description content that is longer than 50 characters to meet the minimum requirement.",
                JobTitle = null,
                UserEmail = "test@example.com"
            };

            var expectedResponse = new JobResponse
            {
                Id = "507f1f77bcf86cd799439011",
                OriginalText = request.Text,
                ImprovedText = "Improved content",
                UserEmail = request.UserEmail,
                CreatedAt = DateTime.UtcNow,
                Analysis = new AnalysisResult
                {
                    ImprovedText = "Improved content",
                    role = "General",
                    industry = "Various",
                    overall_assessment = "Good job description",
                    Issues = new List<Issue>(),
                    suggestions = new List<Suggestion>()
                }
            };

            _mockJobService
                .Setup(s => s.AnalyzeTextAsync(request.Text, request.UserEmail, request.JobTitle))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.AnalyzeText(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<JobResponse>(okResult.Value);
            Assert.Equal(expectedResponse.Id, response.Id);
        }

        [Fact]
        public async Task AnalyzeText_HttpRequestException_ReturnsServiceUnavailable()
        {
            // Arrange
            var request = new AnalyzeRequest
            {
                Text = "This is a test job description content that is longer than 50 characters to meet the minimum requirement.",
                UserEmail = "test@example.com"
            };

            _mockJobService
                .Setup(s => s.AnalyzeTextAsync(request.Text, request.UserEmail, request.JobTitle))
                .ThrowsAsync(new HttpRequestException("Service error"));

            // Act
            var result = await _controller.AnalyzeText(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(503, statusCodeResult.StatusCode);

            var errorResponse = statusCodeResult.Value;
            var messageProperty = errorResponse.GetType().GetProperty("message");
            Assert.Equal("Our AI analysis service is temporarily unavailable. Please try again in a few moments.", messageProperty.GetValue(errorResponse));
        }

        [Fact]
        public async Task AnalyzeText_TaskCanceledException_ReturnsTimeout()
        {
            // Arrange
            var request = new AnalyzeRequest
            {
                Text = "This is a test job description content that is longer than 50 characters to meet the minimum requirement.",
                UserEmail = "test@example.com"
            };

            var timeoutException = new TimeoutException();
            var taskCanceledException = new TaskCanceledException("Task was canceled", timeoutException);

            _mockJobService
                .Setup(s => s.AnalyzeTextAsync(request.Text, request.UserEmail, request.JobTitle))
                .ThrowsAsync(taskCanceledException);

            // Act
            var result = await _controller.AnalyzeText(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(504, statusCodeResult.StatusCode);

            var errorResponse = statusCodeResult.Value;
            var messageProperty = errorResponse.GetType().GetProperty("message");
            Assert.Equal("The analysis is taking longer than expected. Please try again with a shorter job description.", messageProperty.GetValue(errorResponse));
        }

        [Fact]
        public async Task AnalyzeText_GenericException_ReturnsInternalServerError()
        {
            // Arrange
            var request = new AnalyzeRequest
            {
                Text = "This is a test job description content that is longer than 50 characters to meet the minimum requirement.",
                UserEmail = "test@example.com"
            };

            _mockJobService
                .Setup(s => s.AnalyzeTextAsync(request.Text, request.UserEmail, request.JobTitle))
                .ThrowsAsync(new Exception("Service error"));

            // Actb
            var result = await _controller.AnalyzeText(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);

            var errorResponse = statusCodeResult.Value;
            var messageProperty = errorResponse.GetType().GetProperty("message");
            Assert.Equal("An unexpected error occurred while analyzing the text. Please try again.", messageProperty.GetValue(errorResponse));
        }

        [Fact]
        public async Task AnalyzeText_WithRepetitiveCharacters_ReturnsBadRequest()
        {
            // Arrange
            var repetitiveText = "This is a job description with aaaaaaaaa repetitive characters that should be rejected because it contains too many consecutive same characters.";
            var request = new AnalyzeRequest
            {
                Text = repetitiveText,
                UserEmail = "test@example.com"
            };

            // Act
            var result = await _controller.AnalyzeText(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorResponse = badRequestResult.Value;

            var messageProperty = errorResponse.GetType().GetProperty("message");
            Assert.Equal("Text contains too many repetitive characters. Please provide a proper job description.", messageProperty.GetValue(errorResponse));
        }


        // Add these test methods to your existing JobControllerTests class

        #region GetJob Tests

        [Fact]
        public async Task GetJob_WithValidId_ReturnsOkResult()
        {
            // Arrange
            var jobId = "507f1f77bcf86cd799439011";
            var expectedJob = new JobResponse
            {
                Id = jobId,
                OriginalText = "Test job description",
                ImprovedText = "Improved test job description",
                FileName = "test.txt",
                UserEmail = "test@example.com",
                CreatedAt = DateTime.UtcNow,
                Analysis = new AnalysisResult
                {
                    ImprovedText = "Improved test job description",
                    role = "Software Developer",
                    industry = "Technology",
                    overall_assessment = "Good job description",
                    Issues = new List<Issue>(),
                    suggestions = new List<Suggestion>()
                }
            };

            _mockJobService
                .Setup(s => s.GetJobAsync(jobId))
                .ReturnsAsync(expectedJob);

            // Act
            var result = await _controller.GetJob(jobId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<JobResponse>(okResult.Value);
            Assert.Equal(expectedJob.Id, response.Id);
            Assert.Equal(expectedJob.OriginalText, response.OriginalText);
            Assert.Equal(expectedJob.ImprovedText, response.ImprovedText);

            _mockJobService.Verify(s => s.GetJobAsync(jobId), Times.Once);
        }

        [Fact]
        public async Task GetJob_WithNullId_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetJob(null);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorResponse = badRequestResult.Value;

            var messageProperty = errorResponse.GetType().GetProperty("message");
            Assert.Equal("Job ID is required", messageProperty.GetValue(errorResponse));

            _mockJobService.Verify(s => s.GetJobAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetJob_WithEmptyId_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetJob("");

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorResponse = badRequestResult.Value;

            var messageProperty = errorResponse.GetType().GetProperty("message");
            Assert.Equal("Job ID is required", messageProperty.GetValue(errorResponse));

            _mockJobService.Verify(s => s.GetJobAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetJob_WithNonExistentId_ReturnsNotFound()
        {
            // Arrange
            var jobId = "507f1f77bcf86cd799439011";

            _mockJobService
                .Setup(s => s.GetJobAsync(jobId))
                .ReturnsAsync((JobResponse?)null);

            // Act
            var result = await _controller.GetJob(jobId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var errorResponse = notFoundResult.Value;

            var messageProperty = errorResponse.GetType().GetProperty("message");
            Assert.Equal("Job not found", messageProperty.GetValue(errorResponse));

            _mockJobService.Verify(s => s.GetJobAsync(jobId), Times.Once);
        }

        [Fact]
        public async Task GetJob_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var jobId = "507f1f77bcf86cd799439011";

            _mockJobService
                .Setup(s => s.GetJobAsync(jobId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetJob(jobId);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);

            var errorResponse = statusCodeResult.Value;
            var messageProperty = errorResponse.GetType().GetProperty("message");
            Assert.Equal("Error retrieving job", messageProperty.GetValue(errorResponse));
        }

        #endregion

        #region GetAllJobs Tests

        [Fact]
        public async Task GetAllJobs_WithDefaultParameters_ReturnsOkResult()
        {
            // Arrange
            var expectedJobs = new List<JobResponse>
    {
        new JobResponse
        {
            Id = "507f1f77bcf86cd799439011",
            OriginalText = "Job 1",
            ImprovedText = "Improved Job 1",
            UserEmail = "user1@example.com",
            CreatedAt = DateTime.UtcNow
        },
        new JobResponse
        {
            Id = "507f1f77bcf86cd799439012",
            OriginalText = "Job 2",
            ImprovedText = "Improved Job 2",
            UserEmail = "user2@example.com",
            CreatedAt = DateTime.UtcNow
        }
    };

            _mockJobService
                .Setup(s => s.GetAllJobsAsync(0, 20))
                .ReturnsAsync(expectedJobs);

            // Act
            var result = await _controller.GetAllJobs();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<List<JobResponse>>(okResult.Value);
            Assert.Equal(2, response.Count);
            Assert.Equal(expectedJobs[0].Id, response[0].Id);
            Assert.Equal(expectedJobs[1].Id, response[1].Id);

            _mockJobService.Verify(s => s.GetAllJobsAsync(0, 20), Times.Once);
        }

        [Fact]
        public async Task GetAllJobs_WithCustomParameters_ReturnsOkResult()
        {
            // Arrange
            var skip = 10;
            var limit = 5;
            var expectedJobs = new List<JobResponse>
    {
        new JobResponse
        {
            Id = "507f1f77bcf86cd799439011",
            OriginalText = "Job 1",
            ImprovedText = "Improved Job 1",
            UserEmail = "user1@example.com",
            CreatedAt = DateTime.UtcNow
        }
    };

            _mockJobService
                .Setup(s => s.GetAllJobsAsync(skip, limit))
                .ReturnsAsync(expectedJobs);

            // Act
            var result = await _controller.GetAllJobs(skip, limit);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<List<JobResponse>>(okResult.Value);
            Assert.Single(response);

            _mockJobService.Verify(s => s.GetAllJobsAsync(skip, limit), Times.Once);
        }

        [Fact]
        public async Task GetAllJobs_WithNegativeSkip_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetAllJobs(-1, 20);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorResponse = badRequestResult.Value;

            var messageProperty = errorResponse.GetType().GetProperty("message");
            Assert.Equal("Skip parameter cannot be negative", messageProperty.GetValue(errorResponse));

            _mockJobService.Verify(s => s.GetAllJobsAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GetAllJobs_WithZeroLimit_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetAllJobs(0, 0);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorResponse = badRequestResult.Value;

            var messageProperty = errorResponse.GetType().GetProperty("message");
            Assert.Equal("Limit must be between 1 and 100", messageProperty.GetValue(errorResponse));

            _mockJobService.Verify(s => s.GetAllJobsAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GetAllJobs_WithLimitOver100_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetAllJobs(0, 101);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorResponse = badRequestResult.Value;

            var messageProperty = errorResponse.GetType().GetProperty("message");
            Assert.Equal("Limit must be between 1 and 100", messageProperty.GetValue(errorResponse));

            _mockJobService.Verify(s => s.GetAllJobsAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GetAllJobs_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            _mockJobService
                .Setup(s => s.GetAllJobsAsync(0, 20))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetAllJobs();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);

            var errorResponse = statusCodeResult.Value;
            var messageProperty = errorResponse.GetType().GetProperty("message");
            Assert.Equal("Error retrieving jobs", messageProperty.GetValue(errorResponse));
        }

        #endregion

        #region GetUserJobs Tests

        [Fact]
        public async Task GetUserJobs_WithValidEmail_ReturnsOkResult()
        {
            // Arrange
            var userEmail = "test@example.com";
            var expectedJobs = new List<JobDescription>
    {
        new JobDescription
        {
            Id = "507f1f77bcf86cd799439011",
            OriginalText = "User job 1",
            ImprovedText = "Improved user job 1",
            UserEmail = userEmail,
            CreatedAt = DateTime.UtcNow
        },
        new JobDescription
        {
            Id = "507f1f77bcf86cd799439012",
            OriginalText = "User job 2",
            ImprovedText = "Improved user job 2",
            UserEmail = userEmail,
            CreatedAt = DateTime.UtcNow
        }
    };

            _mockJobService
                .Setup(s => s.GetByUserEmailAsync(userEmail))
                .ReturnsAsync(expectedJobs);

            // Act
            var result = await _controller.GetUserJobs(userEmail);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<List<JobDescription>>(okResult.Value);
            Assert.Equal(2, response.Count);
            Assert.All(response, job => Assert.Equal(userEmail, job.UserEmail));

            _mockJobService.Verify(s => s.GetByUserEmailAsync(userEmail), Times.Once);
        }

        [Fact]
        public async Task GetUserJobs_WithEmptyList_ReturnsOkResultWithEmptyList()
        {
            // Arrange
            var userEmail = "test@example.com";
            var expectedJobs = new List<JobDescription>();

            _mockJobService
                .Setup(s => s.GetByUserEmailAsync(userEmail))
                .ReturnsAsync(expectedJobs);

            // Act
            var result = await _controller.GetUserJobs(userEmail);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<List<JobDescription>>(okResult.Value);
            Assert.Empty(response);

            _mockJobService.Verify(s => s.GetByUserEmailAsync(userEmail), Times.Once);
        }

        [Fact]
        public async Task GetUserJobs_WithNullEmail_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetUserJobs(null);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var errorResponse = badRequestResult.Value;

            var messageProperty = errorResponse.GetType().GetProperty("message");
            Assert.Equal("Email is required", messageProperty.GetValue(errorResponse));

            _mockJobService.Verify(s => s.GetByUserEmailAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetUserJobs_WithEmptyEmail_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetUserJobs("");

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var errorResponse = badRequestResult.Value;

            var messageProperty = errorResponse.GetType().GetProperty("message");
            Assert.Equal("Email is required", messageProperty.GetValue(errorResponse));

            _mockJobService.Verify(s => s.GetByUserEmailAsync(It.IsAny<string>()), Times.Never);
        }

        // [Theory]
        // [InlineData("invalid-email")]
        // [InlineData("test@")]
        // [InlineData("@example.com")]
        // [InlineData("testexample.com")]
        // public async Task GetUserJobs_WithInvalidEmailFormat_ReturnsBadRequest(string invalidEmail)
        // {
        //     // Act
        //     var result = await _controller.GetUserJobs(invalidEmail);

        //     // Assert
        //     var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        //     var errorResponse = badRequestResult.Value;

        //     var messageProperty = errorResponse.GetType().GetProperty("message");
        //     Assert.Equal("Invalid email format", messageProperty.GetValue(errorResponse));

        //     _mockJobService.Verify(s => s.GetByUserEmailAsync(It.IsAny<string>()), Times.Never);
        // }

        [Fact]
        public async Task GetUserJobs_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var userEmail = "test@example.com";

            _mockJobService
                .Setup(s => s.GetByUserEmailAsync(userEmail))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetUserJobs(userEmail);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);

            var errorResponse = statusCodeResult.Value;
            var messageProperty = errorResponse.GetType().GetProperty("message");
            Assert.Equal("Internal server error", messageProperty.GetValue(errorResponse));
        }

        #endregion

        #region DeleteJob Tests

        [Fact]
        public async Task DeleteJob_WithValidId_ReturnsOkResult()
        {
            // Arrange
            var jobId = "507f1f77bcf86cd799439011";

            _mockJobService
                .Setup(s => s.DeleteJobAsync(jobId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteJob(jobId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;

            // Use reflection to check the anonymous object properties
            var messageProperty = response.GetType().GetProperty("message");
            var idProperty = response.GetType().GetProperty("id");

            Assert.Equal("Job deleted successfully", messageProperty.GetValue(response));
            Assert.Equal(jobId, idProperty.GetValue(response));

            _mockJobService.Verify(s => s.DeleteJobAsync(jobId), Times.Once);
        }

        [Fact]
        public async Task DeleteJob_WithNullId_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.DeleteJob(null);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorResponse = badRequestResult.Value;

            var messageProperty = errorResponse.GetType().GetProperty("message");
            Assert.Equal("Job ID is required", messageProperty.GetValue(errorResponse));

            _mockJobService.Verify(s => s.DeleteJobAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task DeleteJob_WithEmptyId_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.DeleteJob("");

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorResponse = badRequestResult.Value;

            var messageProperty = errorResponse.GetType().GetProperty("message");
            Assert.Equal("Job ID is required", messageProperty.GetValue(errorResponse));

            _mockJobService.Verify(s => s.DeleteJobAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task DeleteJob_WithNonExistentId_ReturnsNotFound()
        {
            // Arrange
            var jobId = "507f1f77bcf86cd799439011";

            _mockJobService
                .Setup(s => s.DeleteJobAsync(jobId))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteJob(jobId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var errorResponse = notFoundResult.Value;

            var messageProperty = errorResponse.GetType().GetProperty("message");
            Assert.Equal($"Job with ID {jobId} not found", messageProperty.GetValue(errorResponse));

            _mockJobService.Verify(s => s.DeleteJobAsync(jobId), Times.Once);
        }

        [Fact]
        public async Task DeleteJob_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var jobId = "507f1f77bcf86cd799439011";

            _mockJobService
                .Setup(s => s.DeleteJobAsync(jobId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.DeleteJob(jobId);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);

            var errorResponse = statusCodeResult.Value;
            var messageProperty = errorResponse.GetType().GetProperty("message");
            Assert.Equal("An error occurred while deleting the job. Please try again.", messageProperty.GetValue(errorResponse));
        }

        #endregion


    }
}