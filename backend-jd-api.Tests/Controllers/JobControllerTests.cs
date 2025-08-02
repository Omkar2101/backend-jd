
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
            var response = Assert.IsType<JobResponse>(okResult.Value); // Changed from JobAnalysisResult to JobResponse

            // Assert on JobResponse properties (not JobAnalysisResult)
            Assert.Equal(serviceResult.JobResponse.Id, response.Id);
            Assert.Equal(serviceResult.JobResponse.OriginalText, response.OriginalText);
            Assert.Equal(serviceResult.JobResponse.ImprovedText, response.ImprovedText);
            Assert.Equal(serviceResult.JobResponse.FileName, response.FileName);
            Assert.Equal(serviceResult.JobResponse.UserEmail, response.UserEmail);

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

            // Fixed: Removed the nested boolean check that was incorrect
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
            var response = Assert.IsType<JobResponse>(okResult.Value); // Changed from JobAnalysisResult to JobResponse

            // Assert on JobResponse properties
            Assert.Equal(serviceResult.JobResponse.Id, response.Id);
            Assert.Equal(serviceResult.JobResponse.FileName, response.FileName);

            _mockJobService.Verify(s => s.AnalyzeFromFileAsync(It.IsAny<IFormFile>(), "test@example.com"), Times.Once);
        }

        [Fact]
        public async Task UploadFile_ServiceReturnsNull_ReturnsUnprocessableEntity()
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
            Assert.Contains("Unable to process the file at this time", errorDict["message"].ToString());
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

            var serviceResult = new JobAnalysisResult
            {
                IsSuccess = true,
                JobResponse = new JobResponse
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
                }
            };

            _mockJobService
                .Setup(s => s.AnalyzeFromFileAsync(It.IsAny<IFormFile>(), "test@example.com"))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _controller.UploadFile(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<JobResponse>(okResult.Value); // Changed from JobAnalysisResult to JobResponse

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

            var expectedJobResponse = new JobResponse
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

            // Mock should return JobAnalysisResult, not JobResponse directly
            var expectedServiceResult = new JobAnalysisResult
            {
                IsSuccess = true,
                JobResponse = expectedJobResponse,
                ErrorMessage = null
            };

            _mockJobService
                .Setup(s => s.AnalyzeTextAsync(request.Text, request.UserEmail, request.JobTitle))
                .ReturnsAsync(expectedServiceResult); // Return JobAnalysisResult

            // Act
            var result = await _controller.AnalyzeText(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<JobResponse>(okResult.Value); // This should now work

            Assert.Equal(expectedJobResponse.Id, response.Id);
            Assert.Equal(expectedJobResponse.OriginalText, response.OriginalText);
            Assert.Equal(expectedJobResponse.ImprovedText, response.ImprovedText);
            Assert.Equal(expectedJobResponse.UserEmail, response.UserEmail);
            Assert.NotNull(response.Analysis);
            Assert.Equal(expectedJobResponse.Analysis.bias_score, response.Analysis.bias_score);
            Assert.Equal(expectedJobResponse.Analysis.inclusivity_score, response.Analysis.inclusivity_score);
            Assert.Equal(expectedJobResponse.Analysis.clarity_score, response.Analysis.clarity_score);
            Assert.Equal(expectedJobResponse.Analysis.role, response.Analysis.role);
            Assert.Equal(expectedJobResponse.Analysis.industry, response.Analysis.industry);
            Assert.Equal(expectedJobResponse.Analysis.overall_assessment, response.Analysis.overall_assessment);
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
            var errorDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
                JsonSerializer.Serialize(errorResponse)
            );

            Assert.Equal("Text is required", errorDict["message"].ToString());
            Assert.Equal("validation_error", errorDict["type"].ToString());
            Assert.Equal(400, JsonSerializer.Deserialize<int>(errorDict["status_code"].ToString()));

            // Verify service was never called for validation failure
            _mockJobService.Verify(s => s.AnalyzeTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
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
            var errorDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
                JsonSerializer.Serialize(errorResponse)
            );

            Assert.Equal("Text is required", errorDict["message"].ToString());
            Assert.Equal("validation_error", errorDict["type"].ToString());
            Assert.Equal(400, JsonSerializer.Deserialize<int>(errorDict["status_code"].ToString()));

            // Verify service was never called for validation failure
            _mockJobService.Verify(s => s.AnalyzeTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
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
            var errorDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
                JsonSerializer.Serialize(errorResponse)
            );

            var message = errorDict["message"].ToString();
            Assert.Contains($"Job description text must be at least 50 characters long. Current length: {whitespaceText.Trim().Length} characters", message);
            Assert.Equal("validation_error", errorDict["type"].ToString());
            Assert.Equal(400, JsonSerializer.Deserialize<int>(errorDict["status_code"].ToString()));

            // Verify service was never called for validation failure
            _mockJobService.Verify(s => s.AnalyzeTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
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
            var errorDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
                JsonSerializer.Serialize(errorResponse)
            );

            var message = errorDict["message"].ToString();
            Assert.Contains($"Job description text must be at least 50 characters long. Current length: {shortText.Length} characters", message);
            Assert.Equal("validation_error", errorDict["type"].ToString());
            Assert.Equal(400, JsonSerializer.Deserialize<int>(errorDict["status_code"].ToString()));

            // Verify service was never called for validation failure
            _mockJobService.Verify(s => s.AnalyzeTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
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
            var errorDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
                JsonSerializer.Serialize(errorResponse)
            );

            var message = errorDict["message"].ToString();
            Assert.Contains($"Job description text must be at least 50 characters long. Current length: {shortText.Trim().Length} characters", message);
            Assert.Equal("validation_error", errorDict["type"].ToString());
            Assert.Equal(400, JsonSerializer.Deserialize<int>(errorDict["status_code"].ToString()));

            // Verify service was never called for validation failure
            _mockJobService.Verify(s => s.AnalyzeTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
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
            var errorDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
                JsonSerializer.Serialize(errorResponse)
            );

            Assert.Equal("User email is required", errorDict["message"].ToString());
            Assert.Equal("validation_error", errorDict["type"].ToString());
            Assert.Equal(400, JsonSerializer.Deserialize<int>(errorDict["status_code"].ToString()));

            // Verify service was never called for validation failure
            _mockJobService.Verify(s => s.AnalyzeTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
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
            var errorDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
                JsonSerializer.Serialize(errorResponse)
            );

            Assert.Equal("User email is required", errorDict["message"].ToString());
            Assert.Equal("validation_error", errorDict["type"].ToString());
            Assert.Equal(400, JsonSerializer.Deserialize<int>(errorDict["status_code"].ToString()));

            // Verify service was never called for validation failure
            _mockJobService.Verify(s => s.AnalyzeTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
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

            var expectedJobResponse = new JobResponse
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

            // FIXED: Mock should return JobAnalysisResult, not JobResponse directly
            var expectedServiceResult = new JobAnalysisResult
            {
                IsSuccess = true,
                JobResponse = expectedJobResponse,
                ErrorMessage = null
            };

            _mockJobService
                .Setup(s => s.AnalyzeTextAsync(request.Text, request.UserEmail, request.JobTitle))
                .ReturnsAsync(expectedServiceResult); // Return JobAnalysisResult

            // Act
            var result = await _controller.AnalyzeText(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<JobResponse>(okResult.Value); // This should now work
            Assert.Equal(expectedJobResponse.Id, response.Id);

            // Verify service was called
            _mockJobService.Verify(s => s.AnalyzeTextAsync(request.Text, request.UserEmail, request.JobTitle), Times.Once);
        }

        [Fact]
        public async Task AnalyzeText_ServiceReturnsNull_ReturnsUnprocessableEntity()
        {
            // Arrange
            var request = new AnalyzeRequest
            {
                Text = "This is a test job description content that is longer than 50 characters to meet the minimum requirement.",
                UserEmail = "test@example.com"
            };

            _mockJobService
                .Setup(s => s.AnalyzeTextAsync(request.Text, request.UserEmail, request.JobTitle))
                .ReturnsAsync((JobAnalysisResult)null); // FIXED: Return JobAnalysisResult null, not JobResponse null

            // Act
            var result = await _controller.AnalyzeText(request);

            // Assert
            var unprocessableEntityResult = Assert.IsType<UnprocessableEntityObjectResult>(result);
            var errorResponse = unprocessableEntityResult.Value;
            var errorDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
                JsonSerializer.Serialize(errorResponse)
            );

            Assert.Equal(422, JsonSerializer.Deserialize<int>(errorDict["status_code"].ToString()));
            Assert.Equal("processing_error", errorDict["type"].ToString());
            Assert.Contains("Unable to analyze the text at this time", errorDict["message"].ToString());

            // Verify service was called
            _mockJobService.Verify(s => s.AnalyzeTextAsync(request.Text, request.UserEmail, request.JobTitle), Times.Once);
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
            var errorDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
                JsonSerializer.Serialize(errorResponse)
            );

            Assert.Equal("Text contains too many repetitive characters. Please provide a proper job description.", errorDict["message"].ToString());
            Assert.Equal("validation_error", errorDict["type"].ToString());
            Assert.Equal(400, JsonSerializer.Deserialize<int>(errorDict["status_code"].ToString()));

            // Verify service was never called for validation failure
            _mockJobService.Verify(s => s.AnalyzeTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task AnalyzeText_ServiceReturnsFailure_ReturnsBadRequest()
        {
            // Arrange
            var request = new AnalyzeRequest
            {
                Text = "This is a test job description content that is longer than 50 characters to meet the minimum requirement.",
                UserEmail = "test@example.com"
            };

            var failedServiceResult = new JobAnalysisResult
            {
                IsSuccess = false,
                ErrorMessage = "Analysis service failed to process the text",
                JobResponse = null
            };

            _mockJobService
                .Setup(s => s.AnalyzeTextAsync(request.Text, request.UserEmail, request.JobTitle))
                .ReturnsAsync(failedServiceResult);

            // Act
            var result = await _controller.AnalyzeText(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorResponse = badRequestResult.Value;
            var errorDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
                JsonSerializer.Serialize(errorResponse)
            );

            Assert.Equal("Analysis service failed to process the text", errorDict["message"].ToString());
            Assert.Equal("service_error", errorDict["type"].ToString());
            Assert.Equal(400, JsonSerializer.Deserialize<int>(errorDict["status_code"].ToString()));

            // Verify service was called
            _mockJobService.Verify(s => s.AnalyzeTextAsync(request.Text, request.UserEmail, request.JobTitle), Times.Once);
        }

        [Fact]
        public async Task AnalyzeText_ServiceReturnsSuccessButNullJobResponse_ReturnsUnprocessableEntity()
        {
            // Arrange
            var request = new AnalyzeRequest
            {
                Text = "This is a test job description content that is longer than 50 characters to meet the minimum requirement.",
                UserEmail = "test@example.com"
            };

            var serviceResultWithNullResponse = new JobAnalysisResult
            {
                IsSuccess = true,
                ErrorMessage = null,
                JobResponse = null // Success but null response
            };

            _mockJobService
                .Setup(s => s.AnalyzeTextAsync(request.Text, request.UserEmail, request.JobTitle))
                .ReturnsAsync(serviceResultWithNullResponse);

            // Act
            var result = await _controller.AnalyzeText(request);

            // Assert
            var unprocessableEntityResult = Assert.IsType<UnprocessableEntityObjectResult>(result);
            var errorResponse = unprocessableEntityResult.Value;
            var errorDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
                JsonSerializer.Serialize(errorResponse)
            );

            Assert.Equal(422, JsonSerializer.Deserialize<int>(errorDict["status_code"].ToString()));
            Assert.Equal("processing_error", errorDict["type"].ToString());
            Assert.Contains("Analysis completed but response data is unavailable", errorDict["message"].ToString());

            // Verify service was called
            _mockJobService.Verify(s => s.AnalyzeTextAsync(request.Text, request.UserEmail, request.JobTitle), Times.Once);
        }

        // Add these test methods to your existing JobControllerTests class

        #region GetJob Tests

        [Fact]
        public async Task GetJob_WithValidId_ReturnsOkResult()
        {
            // Arrange
            var jobId = "507f1f77bcf86cd799439011";
            var expectedJobResponse = new JobResponse
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

            // FIXED: Mock should return JobRetrievalResult, not JobResponse directly
            var expectedServiceResult = new JobRetrievalResult
            {
                IsSuccess = true,
                JobResponse = expectedJobResponse,
                ErrorMessage = null
            };

            _mockJobService
                .Setup(s => s.GetJobAsync(jobId))
                .ReturnsAsync(expectedServiceResult); // Return JobRetrievalResult

            // Act
            var result = await _controller.GetJob(jobId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<JobResponse>(okResult.Value); // This should now work
            Assert.Equal(expectedJobResponse.Id, response.Id);
            Assert.Equal(expectedJobResponse.OriginalText, response.OriginalText);
            Assert.Equal(expectedJobResponse.ImprovedText, response.ImprovedText);

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
            var errorDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
                JsonSerializer.Serialize(errorResponse)
            );

            Assert.Equal("Job ID is required", errorDict["message"].ToString());
            Assert.Equal("validation_error", errorDict["type"].ToString());
            Assert.Equal(400, JsonSerializer.Deserialize<int>(errorDict["status_code"].ToString()));

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
            var errorDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
                JsonSerializer.Serialize(errorResponse)
            );

            Assert.Equal("Job ID is required", errorDict["message"].ToString());
            Assert.Equal("validation_error", errorDict["type"].ToString());
            Assert.Equal(400, JsonSerializer.Deserialize<int>(errorDict["status_code"].ToString()));

            _mockJobService.Verify(s => s.GetJobAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetJob_WithNonExistentId_ReturnsNotFound()
        {
            // Arrange
            var jobId = "507f1f77bcf86cd799439011";

            // Service returns failure with "not found" message
            var failedServiceResult = new JobRetrievalResult
            {
                IsSuccess = false,
                ErrorMessage = "Job not found",
                JobResponse = null
            };

            _mockJobService
                .Setup(s => s.GetJobAsync(jobId))
                .ReturnsAsync(failedServiceResult);

            // Act
            var result = await _controller.GetJob(jobId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var errorResponse = notFoundResult.Value;
            var errorDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
                JsonSerializer.Serialize(errorResponse)
            );

            Assert.Equal("Job not found", errorDict["message"].ToString());
            Assert.Equal("not_found", errorDict["type"].ToString());
            Assert.Equal(404, JsonSerializer.Deserialize<int>(errorDict["status_code"].ToString()));

            _mockJobService.Verify(s => s.GetJobAsync(jobId), Times.Once);
        }

        [Fact]
        public async Task GetJob_ServiceReturnsNull_ReturnsUnprocessableEntity()
        {
            // Arrange
            var jobId = "507f1f77bcf86cd799439011";

            _mockJobService
                .Setup(s => s.GetJobAsync(jobId))
                .ReturnsAsync((JobRetrievalResult?)null);

            // Act
            var result = await _controller.GetJob(jobId);

            // Assert
            var unprocessableEntityResult = Assert.IsType<UnprocessableEntityObjectResult>(result);
            var errorResponse = unprocessableEntityResult.Value;
            var errorDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
                JsonSerializer.Serialize(errorResponse)
            );

            Assert.Equal(422, JsonSerializer.Deserialize<int>(errorDict["status_code"].ToString()));
            Assert.Equal("processing_error", errorDict["type"].ToString());
            Assert.Contains("Unable to retrieve job at this time", errorDict["message"].ToString());

            _mockJobService.Verify(s => s.GetJobAsync(jobId), Times.Once);
        }

        [Fact]
        public async Task GetJob_ServiceReturnsSuccessButNullJobResponse_ReturnsNotFound()
        {
            // Arrange
            var jobId = "507f1f77bcf86cd799439011";

            var serviceResultWithNullResponse = new JobRetrievalResult
            {
                IsSuccess = true,
                ErrorMessage = null,
                JobResponse = null // Success but null response
            };

            _mockJobService
                .Setup(s => s.GetJobAsync(jobId))
                .ReturnsAsync(serviceResultWithNullResponse);

            // Act
            var result = await _controller.GetJob(jobId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var errorResponse = notFoundResult.Value;
            var errorDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
                JsonSerializer.Serialize(errorResponse)
            );

            Assert.Equal("Job not found", errorDict["message"].ToString());
            Assert.Equal("not_found", errorDict["type"].ToString());
            Assert.Equal(404, JsonSerializer.Deserialize<int>(errorDict["status_code"].ToString()));

            _mockJobService.Verify(s => s.GetJobAsync(jobId), Times.Once);
        }

        [Fact]
        public async Task GetJob_ServiceReturnsGenericError_ReturnsBadRequest()
        {
            // Arrange
            var jobId = "507f1f77bcf86cd799439011";

            var failedServiceResult = new JobRetrievalResult
            {
                IsSuccess = false,
                ErrorMessage = "Database connection failed",
                JobResponse = null
            };

            _mockJobService
                .Setup(s => s.GetJobAsync(jobId))
                .ReturnsAsync(failedServiceResult);

            // Act
            var result = await _controller.GetJob(jobId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorResponse = badRequestResult.Value;
            var errorDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
                JsonSerializer.Serialize(errorResponse)
            );

            Assert.Equal("Database connection failed", errorDict["message"].ToString());
            Assert.Equal("service_error", errorDict["type"].ToString());
            Assert.Equal(400, JsonSerializer.Deserialize<int>(errorDict["status_code"].ToString()));

            _mockJobService.Verify(s => s.GetJobAsync(jobId), Times.Once);
        }

        #endregion

        #region GetAllJobs Tests

        [Fact]
        public async Task GetAllJobs_WithValidParameters_ReturnsOkResult()
        {
            // Arrange
            var skip = 0;
            var limit = 20;
            var expectedJobs = new List<JobResponse>
    {
        new JobResponse
        {
            Id = "507f1f77bcf86cd799439011",
            OriginalText = "Test job description 1",
            ImprovedText = "Improved test job description 1",
            FileName = "test1.txt",
            UserEmail = "test1@example.com",
            CreatedAt = DateTime.UtcNow
        },
        new JobResponse
        {
            Id = "507f1f77bcf86cd799439012",
            OriginalText = "Test job description 2",
            ImprovedText = "Improved test job description 2",
            FileName = "test2.txt",
            UserEmail = "test2@example.com",
            CreatedAt = DateTime.UtcNow
        }
    };

            var expectedServiceResult = new JobListResult
            {
                IsSuccess = true,
                Jobs = expectedJobs,
                ErrorMessage = null
            };

            _mockJobService
                .Setup(s => s.GetAllJobsAsync(skip, limit))
                .ReturnsAsync(expectedServiceResult);

            // Act
            var result = await _controller.GetAllJobs(skip, limit);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<List<JobResponse>>(okResult.Value);
            Assert.Equal(2, response.Count);
            Assert.Equal(expectedJobs[0].Id, response[0].Id);
            Assert.Equal(expectedJobs[1].Id, response[1].Id);

            _mockJobService.Verify(s => s.GetAllJobsAsync(skip, limit), Times.Once);
        }

        [Fact]
        public async Task GetAllJobs_WithNegativeSkip_ReturnsBadRequest()
        {
            // Arrange
            var skip = -1;
            var limit = 20;

            // Act
            var result = await _controller.GetAllJobs(skip, limit);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorResponse = badRequestResult.Value;
            var errorDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
                JsonSerializer.Serialize(errorResponse)
            );

            Assert.Equal("Skip parameter cannot be negative", errorDict["message"].ToString());
            Assert.Equal("validation_error", errorDict["type"].ToString());
            Assert.Equal(400, JsonSerializer.Deserialize<int>(errorDict["status_code"].ToString()));

            _mockJobService.Verify(s => s.GetAllJobsAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GetAllJobs_WithZeroLimit_ReturnsBadRequest()
        {
            // Arrange
            var skip = 0;
            var limit = 0;

            // Act
            var result = await _controller.GetAllJobs(skip, limit);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorResponse = badRequestResult.Value;
            var errorDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
                JsonSerializer.Serialize(errorResponse)
            );

            Assert.Equal("Limit must be between 1 and 100", errorDict["message"].ToString());
            Assert.Equal("validation_error", errorDict["type"].ToString());
            Assert.Equal(400, JsonSerializer.Deserialize<int>(errorDict["status_code"].ToString()));

            _mockJobService.Verify(s => s.GetAllJobsAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GetAllJobs_WithLimitOver100_ReturnsBadRequest()
        {
            // Arrange
            var skip = 0;
            var limit = 101;

            // Act
            var result = await _controller.GetAllJobs(skip, limit);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorResponse = badRequestResult.Value;
            var errorDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
                JsonSerializer.Serialize(errorResponse)
            );

            Assert.Equal("Limit must be between 1 and 100", errorDict["message"].ToString());
            Assert.Equal("validation_error", errorDict["type"].ToString());
            Assert.Equal(400, JsonSerializer.Deserialize<int>(errorDict["status_code"].ToString()));

            _mockJobService.Verify(s => s.GetAllJobsAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GetAllJobs_ServiceReturnsNull_ReturnsUnprocessableEntity()
        {
            // Arrange
            var skip = 0;
            var limit = 20;

            _mockJobService
                .Setup(s => s.GetAllJobsAsync(skip, limit))
                .ReturnsAsync((JobListResult?)null);

            // Act
            var result = await _controller.GetAllJobs(skip, limit);

            // Assert
            var unprocessableEntityResult = Assert.IsType<UnprocessableEntityObjectResult>(result);
            var errorResponse = unprocessableEntityResult.Value;
            var errorDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
                JsonSerializer.Serialize(errorResponse)
            );

            Assert.Equal(422, JsonSerializer.Deserialize<int>(errorDict["status_code"].ToString()));
            Assert.Equal("processing_error", errorDict["type"].ToString());
            Assert.Contains("Unable to retrieve jobs at this time", errorDict["message"].ToString());

            _mockJobService.Verify(s => s.GetAllJobsAsync(skip, limit), Times.Once);
        }

        [Fact]
        public async Task GetAllJobs_ServiceReturnsFailure_ReturnsBadRequest()
        {
            // Arrange
            var skip = 0;
            var limit = 20;

            var failedServiceResult = new JobListResult
            {
                IsSuccess = false,
                ErrorMessage = "Failed to retrieve jobs from database",
                Jobs = null
            };

            _mockJobService
                .Setup(s => s.GetAllJobsAsync(skip, limit))
                .ReturnsAsync(failedServiceResult);

            // Act
            var result = await _controller.GetAllJobs(skip, limit);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorResponse = badRequestResult.Value;
            var errorDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
                JsonSerializer.Serialize(errorResponse)
            );

            Assert.Equal("Failed to retrieve jobs from database", errorDict["message"].ToString());
            Assert.Equal("service_error", errorDict["type"].ToString());
            Assert.Equal(400, JsonSerializer.Deserialize<int>(errorDict["status_code"].ToString()));

            _mockJobService.Verify(s => s.GetAllJobsAsync(skip, limit), Times.Once);
        }

        [Fact]
        public async Task GetAllJobs_ServiceReturnsSuccessButNullJobsList_ReturnsUnprocessableEntity()
        {
            // Arrange
            var skip = 0;
            var limit = 20;

            var serviceResultWithNullJobs = new JobListResult
            {
                IsSuccess = true,
                ErrorMessage = null,
                Jobs = null // Success but null jobs list
            };

            _mockJobService
                .Setup(s => s.GetAllJobsAsync(skip, limit))
                .ReturnsAsync(serviceResultWithNullJobs);

            // Act
            var result = await _controller.GetAllJobs(skip, limit);

            // Assert
            var unprocessableEntityResult = Assert.IsType<UnprocessableEntityObjectResult>(result);
            var errorResponse = unprocessableEntityResult.Value;
            var errorDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
                JsonSerializer.Serialize(errorResponse)
            );

            Assert.Equal(422, JsonSerializer.Deserialize<int>(errorDict["status_code"].ToString()));
            Assert.Equal("processing_error", errorDict["type"].ToString());
            Assert.Contains("Jobs retrieval completed but data is unavailable", errorDict["message"].ToString());

            _mockJobService.Verify(s => s.GetAllJobsAsync(skip, limit), Times.Once);
        }

        [Fact]
        public async Task GetAllJobs_WithEmptyResultSet_ReturnsOkWithEmptyList()
        {
            // Arrange
            var skip = 0;
            var limit = 20;
            var emptyJobs = new List<JobResponse>();

            var expectedServiceResult = new JobListResult
            {
                IsSuccess = true,
                Jobs = emptyJobs,
                ErrorMessage = null
            };

            _mockJobService
                .Setup(s => s.GetAllJobsAsync(skip, limit))
                .ReturnsAsync(expectedServiceResult);

            // Act
            var result = await _controller.GetAllJobs(skip, limit);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<List<JobResponse>>(okResult.Value);
            Assert.Empty(response);

            _mockJobService.Verify(s => s.GetAllJobsAsync(skip, limit), Times.Once);
        }


        #endregion

        #region DeleteJob Tests

        [Fact]
        public async Task DeleteJob_WithValidId_ReturnsOkResult()
        {
            // Arrange
            var jobId = "507f1f77bcf86cd799439011";

            // FIXED: Use JobDeleteResult instead of anonymous object
            var deleteResult = new JobDeleteResult
            {
                IsSuccess = true,
                ErrorMessage = null
            };

            _mockJobService
                .Setup(s => s.DeleteJobAsync(jobId))
                .ReturnsAsync(deleteResult);

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
            var errorDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
                JsonSerializer.Serialize(errorResponse)
            );

            Assert.Equal("Job ID is required", errorDict["message"].ToString());
            Assert.Equal("validation_error", errorDict["type"].ToString());
            Assert.Equal(400, JsonSerializer.Deserialize<int>(errorDict["status_code"].ToString()));

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
            var errorDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
                JsonSerializer.Serialize(errorResponse)
            );

            Assert.Equal("Job ID is required", errorDict["message"].ToString());
            Assert.Equal("validation_error", errorDict["type"].ToString());
            Assert.Equal(400, JsonSerializer.Deserialize<int>(errorDict["status_code"].ToString()));

            _mockJobService.Verify(s => s.DeleteJobAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task DeleteJob_WithNonExistentId_ReturnsNotFound()
        {
            // Arrange
            var jobId = "507f1f77bcf86cd799439011";

            // FIXED: Use JobDeleteResult instead of anonymous object
            var deleteResult = new JobDeleteResult
            {
                IsSuccess = false,
                ErrorMessage = "Job not found"
            };

            _mockJobService
                .Setup(s => s.DeleteJobAsync(jobId))
                .ReturnsAsync(deleteResult);

            // Act
            var result = await _controller.DeleteJob(jobId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var errorResponse = notFoundResult.Value;
            var errorDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
                JsonSerializer.Serialize(errorResponse)
            );

            Assert.Equal("Job not found", errorDict["message"].ToString());
            Assert.Equal("not_found", errorDict["type"].ToString());
            Assert.Equal(404, JsonSerializer.Deserialize<int>(errorDict["status_code"].ToString()));

            _mockJobService.Verify(s => s.DeleteJobAsync(jobId), Times.Once);
        }

        [Fact]
        public async Task DeleteJob_ServiceError_ReturnsBadRequest()
        {
            // Arrange
            var jobId = "507f1f77bcf86cd799439011";

            // FIXED: Use JobDeleteResult instead of anonymous object
            var deleteResult = new JobDeleteResult
            {
                IsSuccess = false,
                ErrorMessage = "Database connection failed"
            };

            _mockJobService
                .Setup(s => s.DeleteJobAsync(jobId))
                .ReturnsAsync(deleteResult);

            // Act
            var result = await _controller.DeleteJob(jobId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorResponse = badRequestResult.Value;
            var errorDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
                JsonSerializer.Serialize(errorResponse)
            );

            Assert.Equal("Database connection failed", errorDict["message"].ToString());
            Assert.Equal("service_error", errorDict["type"].ToString());
            Assert.Equal(400, JsonSerializer.Deserialize<int>(errorDict["status_code"].ToString()));

            _mockJobService.Verify(s => s.DeleteJobAsync(jobId), Times.Once);
        }

        [Fact]
        public async Task DeleteJob_ServiceReturnsNull_ReturnsUnprocessableEntity()
        {
            // Arrange
            var jobId = "507f1f77bcf86cd799439011";

            _mockJobService
                .Setup(s => s.DeleteJobAsync(jobId))
                .ReturnsAsync((JobDeleteResult?)null);

            // Act
            var result = await _controller.DeleteJob(jobId);

            // Assert
            var unprocessableEntityResult = Assert.IsType<UnprocessableEntityObjectResult>(result);
            var errorResponse = unprocessableEntityResult.Value;
            var errorDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
                JsonSerializer.Serialize(errorResponse)
            );

            Assert.Equal(422, JsonSerializer.Deserialize<int>(errorDict["status_code"].ToString()));
            Assert.Equal("processing_error", errorDict["type"].ToString());
            Assert.Contains("Unable to delete job at this time", errorDict["message"].ToString());

            _mockJobService.Verify(s => s.DeleteJobAsync(jobId), Times.Once);
        }

        [Fact]
        public async Task DeleteJob_WithWhitespaceId_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.DeleteJob("   ");

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorResponse = badRequestResult.Value;
            var errorDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
                JsonSerializer.Serialize(errorResponse)
            );

            Assert.Equal("Job ID is required", errorDict["message"].ToString());
            Assert.Equal("validation_error", errorDict["type"].ToString());
            Assert.Equal(400, JsonSerializer.Deserialize<int>(errorDict["status_code"].ToString()));

            _mockJobService.Verify(s => s.DeleteJobAsync(It.IsAny<string>()), Times.Never);
        }

        #endregion

    }
}