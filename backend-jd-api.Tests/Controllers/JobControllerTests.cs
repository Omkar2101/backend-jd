
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

            var expectedResponse = new JobResponse
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
            var response = Assert.IsType<JobResponse>(okResult.Value);
            Assert.Equal(expectedResponse.Id, response.Id);
            Assert.Equal(expectedResponse.OriginalText, response.OriginalText);
            Assert.Equal(expectedResponse.ImprovedText, response.ImprovedText);
            Assert.Equal(expectedResponse.FileName, response.FileName);
            Assert.Equal(expectedResponse.UserEmail, response.UserEmail);

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
            Assert.Equal("No file uploaded", badRequestResult.Value);
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
            Assert.Equal("No file uploaded", badRequestResult.Value);
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
            Assert.Equal("User email is required", badRequestResult.Value);
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
            Assert.Equal("User email is required", badRequestResult.Value);
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
            Assert.Contains("Invalid file type", badRequestResult.Value.ToString());
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

            var expectedResponse = new JobResponse
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
        public async Task UploadFile_JobServiceThrowsException_ReturnsInternalServerError()
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
            Assert.Equal("An error occurred while processing your file. Please try again.", statusCodeResult.Value);
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
            Assert.Equal("Text is required", badRequestResult.Value);
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
            Assert.Equal("Text is required", badRequestResult.Value);
        }

        [Fact]
        public async Task AnalyzeText_WithWhitespaceOnlyText_ReturnsBadRequest()
        {
            // Arrange
            var whitespaceText = "   "; // 3 spaces
            var request = new AnalyzeRequest
            {
                Text = whitespaceText,
                UserEmail = "test@example.com"
            };

            // Act
            var result = await _controller.AnalyzeText(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains($"Job description text must be at least 50 characters long. Current length: {whitespaceText.Trim().Length} characters", badRequestResult.Value.ToString());
        }

        [Fact]
        public async Task AnalyzeText_WithShortText_ReturnsBadRequest()
        {
            // Arrange
            var shortText = "Short text"; // Less than 50 characters
            var request = new AnalyzeRequest
            {
                Text = shortText,
                UserEmail = "test@example.com"
            };

            // Act
            var result = await _controller.AnalyzeText(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains($"Job description text must be at least 50 characters long. Current length: {shortText.Length} characters", badRequestResult.Value.ToString());
        }

        [Fact]
        public async Task AnalyzeText_WithShortTextAfterTrim_ReturnsBadRequest()
        {
            // Arrange
            var shortText = "   Short text   "; // Less than 50 characters after trim
            var request = new AnalyzeRequest
            {
                Text = shortText,
                UserEmail = "test@example.com"
            };

            // Act
            var result = await _controller.AnalyzeText(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains($"Job description text must be at least 50 characters long. Current length: {shortText.Trim().Length} characters", badRequestResult.Value.ToString());
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
            Assert.Equal("User email is required", badRequestResult.Value);
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
            Assert.Equal("User email is required", badRequestResult.Value);
        }

        

        [Fact]
        public async Task AnalyzeText_WithoutJobTitle_ProcessesSuccessfully()
        {
            // Arrange
            var request = new AnalyzeRequest
            {
                Text = "This is a test job description content that is longer than 50 characters to meet the minimum requirement.",
                JobTitle = null, // No job title provided
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
        public async Task AnalyzeText_JobServiceThrowsException_ReturnsInternalServerError()
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

            // Act
            var result = await _controller.AnalyzeText(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal("An error occurred while analyzing the text. Please try again.", statusCodeResult.Value);
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
            Assert.Equal("Text contains too many repetitive characters. Please provide a proper job description.", badRequestResult.Value);
        }

        [Fact]
        public async Task AnalyzeText_WithExcessiveSpecialCharacters_ReturnsBadRequest()
        {
            // Arrange
            var textWithSpecialChars = "This job description has way too many special characters!@#$%^&*()_+{}|:<>?[]\\;',./~`±§¡™£¢∞§¶•ªº–≠";
            var request = new AnalyzeRequest
            {
                Text = textWithSpecialChars,
                UserEmail = "test@example.com"
            };

            // Act
            var result = await _controller.AnalyzeText(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Text contains too many special characters. Please provide a valid job description.", badRequestResult.Value);
        }

        [Fact]
        public async Task AnalyzeText_WithTooFewMeaningfulWords_ReturnsBadRequest()
        {
            // Arrange
            var textWithFewWords = "Job a b c d e f g h i developer position available now.";
            var request = new AnalyzeRequest
            {
                Text = textWithFewWords,
                UserEmail = "test@example.com"
            };

            // Act
            var result = await _controller.AnalyzeText(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Please provide a more detailed job description with proper words.", badRequestResult.Value);
        }

        [Fact]
        public async Task AnalyzeText_WithoutJobKeywords_ReturnsBadRequest()
        {
            // Arrange
            var textWithoutJobKeywords = "This is a text about mountain hiking, art exhibitions, and baking cakes. It contains no information related to cooking";
            var request = new AnalyzeRequest
            {
                Text = textWithoutJobKeywords,
                UserEmail = "test@example.com"
            };

            // Act
            var result = await _controller.AnalyzeText(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Text doesn't appear to be a job description. Please provide a valid job posting.", badRequestResult.Value);
        }

        [Fact]
        public async Task AnalyzeText_WithGibberishText_ReturnsBadRequest()
        {
            // Arrange
            var gibberishText = "Xbdfghjklmnpqrstvwxyz bcdfghjklmnpqrstvwxyz cdfghjklmnpqrstvwxyz dfghjklmnpqrstvwxyz fghjklmnpqrstvwxyz ghjklmnpqrstvwxyz hjklmnpqrstvwxyz jklmnpqrstvwxyz job position requirements";
            var request = new AnalyzeRequest
            {
                Text = gibberishText,
                UserEmail = "test@example.com"
            };

            // Act
            var result = await _controller.AnalyzeText(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Text appears to be invalid. Please provide a proper job description.", badRequestResult.Value);
        }
       


        #region GetJob Tests

        [Fact]
        public async Task GetJob_WithValidId_ReturnsOkResult()
        {
            // Arrange
            var jobId = "507f1f77bcf86cd799439011";
            var expectedResponse = new JobResponse
            {
                Id = jobId,
                OriginalText = "Test job description",
                ImprovedText = "Improved job description",
                UserEmail = "test@example.com",
                FileName = "test.txt",
                CreatedAt = DateTime.UtcNow,
                Analysis = new AnalysisResult
                {
                    bias_score = 0.3,
                    inclusivity_score = 0.7,
                    clarity_score = 0.8,
                    suggestions = new List<Suggestion>()
                }
            };

            _mockJobService
                .Setup(s => s.GetJobAsync(jobId))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetJob(jobId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedJob = Assert.IsType<JobResponse>(okResult.Value);
            Assert.Equal(expectedResponse.Id, returnedJob.Id);
            Assert.Equal(expectedResponse.OriginalText, returnedJob.OriginalText);
            Assert.Equal(expectedResponse.ImprovedText, returnedJob.ImprovedText);
            Assert.Equal(expectedResponse.UserEmail, returnedJob.UserEmail);

            _mockJobService.Verify(s => s.GetJobAsync(jobId), Times.Once);
        }

        [Fact]
        public async Task GetJob_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var jobId = "507f1f77bcf86cd799439011";

            _mockJobService
                .Setup(s => s.GetJobAsync(jobId))
                .ReturnsAsync((JobResponse)null);

            // Act
            var result = await _controller.GetJob(jobId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundResult>(result);
            _mockJobService.Verify(s => s.GetJobAsync(jobId), Times.Once);
        }

        [Fact]
        public async Task GetJob_JobServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var jobId = "507f1f77bcf86cd799439011";

            _mockJobService
                .Setup(s => s.GetJobAsync(jobId))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.GetJob(jobId);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal("Error retrieving job", statusCodeResult.Value);
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
            var returnedJobs = Assert.IsType<List<JobResponse>>(okResult.Value);
            Assert.Equal(2, returnedJobs.Count);
            Assert.Equal(expectedJobs[0].Id, returnedJobs[0].Id);
            Assert.Equal(expectedJobs[1].Id, returnedJobs[1].Id);

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
            var returnedJobs = Assert.IsType<List<JobResponse>>(okResult.Value);
            Assert.Single(returnedJobs);
            Assert.Equal(expectedJobs[0].Id, returnedJobs[0].Id);

            _mockJobService.Verify(s => s.GetAllJobsAsync(skip, limit), Times.Once);
        }

        [Fact]
        public async Task GetAllJobs_WithEmptyResult_ReturnsEmptyList()
        {
            // Arrange
            var expectedJobs = new List<JobResponse>();

            _mockJobService
                .Setup(s => s.GetAllJobsAsync(0, 20))
                .ReturnsAsync(expectedJobs);

            // Act
            var result = await _controller.GetAllJobs();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedJobs = Assert.IsType<List<JobResponse>>(okResult.Value);
            Assert.Empty(returnedJobs);

            _mockJobService.Verify(s => s.GetAllJobsAsync(0, 20), Times.Once);
        }

        [Fact]
        public async Task GetAllJobs_JobServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            _mockJobService
                .Setup(s => s.GetAllJobsAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.GetAllJobs();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal("Error retrieving jobs", statusCodeResult.Value);
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
                    OriginalText = "User Job 1",
                    ImprovedText = "Improved User Job 1",
                    UserEmail = userEmail,
                    CreatedAt = DateTime.UtcNow
                },
                new JobDescription
                {
                    Id = "507f1f77bcf86cd799439012",
                    OriginalText = "User Job 2",
                    ImprovedText = "Improved User Job 2",
                    UserEmail = userEmail,
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockJobService
                .Setup(service => service.GetByUserEmailAsync(userEmail))
                .ReturnsAsync(expectedJobs);

            // Act
            var result = await _controller.GetUserJobs(userEmail);

            // Assert
            var actionResult = Assert.IsType<ActionResult<List<JobDescription>>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var returnedJobs = Assert.IsType<List<JobDescription>>(okResult.Value);
            Assert.Equal(2, returnedJobs.Count);
            Assert.All(returnedJobs, job => Assert.Equal(userEmail, job.UserEmail));

            _mockJobService.Verify(service => service.GetByUserEmailAsync(userEmail), Times.Once);
        }

        [Fact]
        public async Task GetUserJobs_JobServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var userEmail = "test@example.com";

            _mockJobService
                .Setup(service => service.GetByUserEmailAsync(userEmail))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.GetUserJobs(userEmail);

            // Assert
            var actionResult = Assert.IsType<ActionResult<List<JobDescription>>>(result);
            var statusCodeResult = Assert.IsType<ObjectResult>(actionResult.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal("Internal server error", statusCodeResult.Value);
        }

        #endregion
    }
}