// using Microsoft.AspNetCore.Http;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.AspNetCore.Mvc.Testing;
// using Microsoft.Extensions.Logging;
// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.AspNetCore.Hosting;
// using Moq;
// using System.Net;
// using System.Text;
// using Xunit;
// using backend_jd_api.Controllers;
// using backend_jd_api.Models;
// using backend_jd_api.Services;
// using backend_jd_api.Data;

// namespace backend_jd_api.Tests.Controllers
// {
//     public class JobControllerTests
//     {
//         private readonly Mock<JobService> _mockJobService;
//         private readonly Mock<ILogger<JobController>> _mockLogger;
//         private readonly JobController _controller;

//         public JobControllerTests()
//         {
//             _mockJobService = new Mock<JobService>();
//             _mockLogger = new Mock<ILogger<JobController>>();
//             _controller = new JobController(_mockJobService.Object, _mockLogger.Object);
//         }

//         private IFormFile CreateMockFormFile(string fileName, string content)
//         {
//             var bytes = Encoding.UTF8.GetBytes(content);
//             var file = new Mock<IFormFile>();
            
//             file.Setup(f => f.FileName).Returns(fileName);
//             file.Setup(f => f.Length).Returns(bytes.Length);
//             file.Setup(f => f.ContentType).Returns("text/plain");
//             file.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(bytes));
//             file.Setup(f => f.ContentDisposition).Returns($"form-data; name=\"file\"; filename=\"{fileName}\"");
            
//             return file.Object;
//         }



//         [Fact]
//         public async Task UploadFile_WithValidRequest_ReturnsOkResult()
//         {
//             // Arrange
//             var mockFile = CreateMockFormFile("test.pdf", "Test job description content");
//             var request = new UploadRequest
//             {
//                 File = mockFile,
//                 UserEmail = "test@example.com"
//             };

//             var expectedResponse = new JobResponse
//             {
//                 Id = "507f1f77bcf86cd799439011",
//                 OriginalText = "Test job description content",
//                 FileName = "test.pdf",
//                 CreatedAt = DateTime.UtcNow
//             };

//             _mockJobService
//                 .Setup(s => s.AnalyzeFromFileAsync(It.IsAny<IFormFile>(), It.IsAny<string>()))
//                 .ReturnsAsync(expectedResponse);

//             // Act
//             var result = await _controller.UploadFile(request);

//             // Assert
//             var okResult = Assert.IsType<OkObjectResult>(result);
//             var response = Assert.IsType<JobResponse>(okResult.Value);
//             Assert.Equal(expectedResponse.Id, response.Id);
//             Assert.Equal(expectedResponse.OriginalText, response.OriginalText);
//             Assert.Equal(expectedResponse.FileName, response.FileName);
//         }

//         // [Fact]
//         // public async Task UploadFile_WithNullFile_ReturnsBadRequest()
//         // {
//         //     // Arrange
//         //     var request = new UploadRequest
//         //     {
//         //         File = null,
//         //         UserEmail = "test@example.com"
//         //     };

//         //     // Act
//         //     var result = await _controller.UploadFile(request);

//         //     // Assert
//         //     var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
//         //     Assert.Equal("No file uploaded", badRequestResult.Value);
//         // }

//         // [Fact]
//         // public async Task UploadFile_WithEmptyFile_ReturnsBadRequest()
//         // {
//         //     // Arrange
//         //     var mockFile = CreateMockFormFile("test.txt", "");
//         //     var request = new UploadRequest
//         //     {
//         //         File = mockFile,
//         //         UserEmail = "test@example.com"
//         //     };

//         //     // Act
//         //     var result = await _controller.UploadFile(request);

//         //     // Assert
//         //     var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
//         //     Assert.Equal("No file uploaded", badRequestResult.Value);
//         // }

//     //     [Fact]
//     //     public async Task UploadFile_WithoutUserEmail_ReturnsBadRequest()
//     //     {
//     //         // Arrange
//     //         var mockFile = CreateMockFormFile("test.txt", "Test content");
//     //         var request = new UploadRequest
//     //         {
//     //             File = mockFile,
//     //             UserEmail = null
//     //         };

//     //         // Act
//     //         var result = await _controller.UploadFile(request);

//     //         // Assert
//     //         var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
//     //         Assert.Equal("User email is required", badRequestResult.Value);
//     //     }

//     //     [Theory]
//     //     [InlineData("test.exe")]
//     //     [InlineData("test.bat")]
//     //     [InlineData("test.zip")]
//     //     [InlineData("test.html")]
//     //     public async Task UploadFile_WithInvalidFileType_ReturnsBadRequest(string fileName)
//     //     {
//     //         // Arrange
//     //         var mockFile = CreateMockFormFile(fileName, "Test content");
//     //         var request = new UploadRequest
//     //         {
//     //             File = mockFile,
//     //             UserEmail = "test@example.com"
//     //         };

//     //         // Act
//     //         var result = await _controller.UploadFile(request);

//     //         // Assert
//     //         var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
//     //         Assert.Contains("Invalid file type", badRequestResult.Value?.ToString());
//     //     }

//     //     [Theory]
//     //     [InlineData("test.txt")]
//     //     [InlineData("test.doc")]
//     //     [InlineData("test.docx")]
//     //     [InlineData("test.pdf")]
//     //     [InlineData("test.jpg")]
//     //     [InlineData("test.jpeg")]
//     //     [InlineData("test.png")]
//     //     public async Task UploadFile_WithValidFileTypes_CallsJobService(string fileName)
//     //     {
//     //         // Arrange
//     //         var mockFile = CreateMockFormFile(fileName, "Test job description content");
//     //         var request = new UploadRequest
//     //         {
//     //             File = mockFile,
//     //             UserEmail = "test@example.com"
//     //         };

//     //         var expectedResponse = new JobResponse
//     //         {
//     //             Id = "507f1f77bcf86cd799439011",
//     //             FileName = fileName
//     //         };

//     //         _mockJobService
//     //             .Setup(s => s.AnalyzeFromFileAsync(It.IsAny<IFormFile>(), It.IsAny<string>()))
//     //             .ReturnsAsync(expectedResponse);

//     //         // Act
//     //         var result = await _controller.UploadFile(request);

//     //         // Assert
//     //         Assert.IsType<OkObjectResult>(result);
//     //         _mockJobService.Verify(s => s.AnalyzeFromFileAsync(mockFile, "test@example.com"), Times.Once);
//     //     }

//     //     [Fact]
//     //     public async Task UploadFile_WhenServiceThrowsException_ReturnsInternalServerError()
//     //     {
//     //         // Arrange
//     //         var mockFile = CreateMockFormFile("test.txt", "Test content");
//     //         var request = new UploadRequest
//     //         {
//     //             File = mockFile,
//     //             UserEmail = "test@example.com"
//     //         };

//     //         _mockJobService
//     //             .Setup(s => s.AnalyzeFromFileAsync(It.IsAny<IFormFile>(), It.IsAny<string>()))
//     //             .ThrowsAsync(new Exception("Service error"));

//     //         // Act
//     //         var result = await _controller.UploadFile(request);

//     //         // Assert
//     //         var statusCodeResult = Assert.IsType<ObjectResult>(result);
//     //         Assert.Equal(500, statusCodeResult.StatusCode);
//     //         Assert.Equal("An error occurred while processing your file. Please try again.", statusCodeResult.Value);
//     //     }

//     //     #endregion

//     //     #region AnalyzeText Tests

//     //     [Fact]
//     //     public async Task AnalyzeText_WithValidRequest_ReturnsOkResult()
//     //     {
//     //         // Arrange
//     //         var request = new AnalyzeRequest
//     //         {
//     //             Text = "This is a valid job description with more than fifty characters for testing purposes.",
//     //             UserEmail = "test@example.com",
//     //             JobTitle = "Software Developer"
//     //         };

//     //         var expectedResponse = new JobResponse
//     //         {
//     //             Id = "507f1f77bcf86cd799439011",
//     //             OriginalText = request.Text,
//     //             CreatedAt = DateTime.UtcNow
//     //         };

//     //         _mockJobService
//     //             .Setup(s => s.AnalyzeTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
//     //             .ReturnsAsync(expectedResponse);

//     //         // Act
//     //         var result = await _controller.AnalyzeText(request);

//     //         // Assert
//     //         var okResult = Assert.IsType<OkObjectResult>(result);
//     //         var response = Assert.IsType<JobResponse>(okResult.Value);
//     //         Assert.Equal(expectedResponse.Id, response.Id);
//     //         Assert.Equal(expectedResponse.OriginalText, response.OriginalText);
//     //     }

//     //     [Fact]
//     //     public async Task AnalyzeText_WithEmptyText_ReturnsBadRequest()
//     //     {
//     //         // Arrange
//     //         var request = new AnalyzeRequest
//     //         {
//     //             Text = "",
//     //             UserEmail = "test@example.com"
//     //         };

//     //         // Act
//     //         var result = await _controller.AnalyzeText(request);

//     //         // Assert
//     //         var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
//     //         Assert.Equal("Text is required", badRequestResult.Value);
//     //     }

//     //     [Fact]
//     //     public async Task AnalyzeText_WithNullText_ReturnsBadRequest()
//     //     {
//     //         // Arrange
//     //         var request = new AnalyzeRequest
//     //         {
//     //             Text = null,
//     //             UserEmail = "test@example.com"
//     //         };

//     //         // Act
//     //         var result = await _controller.AnalyzeText(request);

//     //         // Assert
//     //         var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
//     //         Assert.Equal("Text is required", badRequestResult.Value);
//     //     }

//     //     [Fact]
//     //     public async Task AnalyzeText_WithShortText_ReturnsBadRequest()
//     //     {
//     //         // Arrange
//     //         var shortText = "Short text"; // Less than 50 characters
//     //         var request = new AnalyzeRequest
//     //         {
//     //             Text = shortText,
//     //             UserEmail = "test@example.com"
//     //         };

//     //         // Act
//     //         var result = await _controller.AnalyzeText(request);

//     //         // Assert
//     //         var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
//     //         Assert.Contains("Job description text must be at least 50 characters", badRequestResult.Value?.ToString());
//     //     }

//     //     [Fact]
//     //     public async Task AnalyzeText_WithoutUserEmail_ReturnsBadRequest()
//     //     {
//     //         // Arrange
//     //         var request = new AnalyzeRequest
//     //         {
//     //             Text = "This is a valid job description with more than fifty characters for testing purposes.",
//     //             UserEmail = null
//     //         };

//     //         // Act
//     //         var result = await _controller.AnalyzeText(request);

//     //         // Assert
//     //         var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
//     //         Assert.Equal("User email is required", badRequestResult.Value);
//     //     }

//     //     [Fact]
//     //     public async Task AnalyzeText_WithExactly50Characters_CallsJobService()
//     //     {
//     //         // Arrange
//     //         var text = "This text has exactly fifty characters in total!"; // Exactly 50 characters
//     //         var request = new AnalyzeRequest
//     //         {
//     //             Text = text,
//     //             UserEmail = "test@example.com",
//     //             JobTitle = "Developer"
//     //         };

//     //         var expectedResponse = new JobResponse { Id = "507f1f77bcf86cd799439011" };
//     //         _mockJobService
//     //             .Setup(s => s.AnalyzeTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
//     //             .ReturnsAsync(expectedResponse);

//     //         // Act
//     //         var result = await _controller.AnalyzeText(request);

//     //         // Assert
//     //         Assert.IsType<OkObjectResult>(result);
//     //         _mockJobService.Verify(s => s.AnalyzeTextAsync(text, "test@example.com", "Developer"), Times.Once);
//     //     }

//     //     [Fact]
//     //     public async Task AnalyzeText_WhenServiceThrowsException_ReturnsInternalServerError()
//     //     {
//     //         // Arrange
//     //         var request = new AnalyzeRequest
//     //         {
//     //             Text = "This is a valid job description with more than fifty characters for testing purposes.",
//     //             UserEmail = "test@example.com"
//     //         };

//     //         _mockJobService
//     //             .Setup(s => s.AnalyzeTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
//     //             .ThrowsAsync(new Exception("Service error"));

//     //         // Act
//     //         var result = await _controller.AnalyzeText(request);

//     //         // Assert
//     //         var statusCodeResult = Assert.IsType<ObjectResult>(result);
//     //         Assert.Equal(500, statusCodeResult.StatusCode);
//     //         Assert.Equal("An error occurred while analyzing the text. Please try again.", statusCodeResult.Value);
//     //     }

//     //     #endregion

//     //     #region GetJob Tests

//     //     [Fact]
//     //     public async Task GetJob_WithValidId_ReturnsOkResult()
//     //     {
//     //         // Arrange
//     //         var jobId = "507f1f77bcf86cd799439011";
//     //         var expectedJob = new JobResponse
//     //         {
//     //             Id = jobId,
//     //             OriginalText = "Test job description",
//     //             CreatedAt = DateTime.UtcNow
//     //         };

//     //         _mockJobService
//     //             .Setup(s => s.GetJobAsync(jobId))
//     //             .ReturnsAsync(expectedJob);

//     //         // Act
//     //         var result = await _controller.GetJob(jobId);

//     //         // Assert
//     //         var okResult = Assert.IsType<OkObjectResult>(result);
//     //         var job = Assert.IsType<JobResponse>(okResult.Value);
//     //         Assert.Equal(expectedJob.Id, job.Id);
//     //     }

//     //     [Fact]
//     //     public async Task GetJob_WithNonExistentId_ReturnsNotFound()
//     //     {
//     //         // Arrange
//     //         var jobId = "507f1f77bcf86cd799439011";
//     //         _mockJobService
//     //             .Setup(s => s.GetJobAsync(jobId))
//     //             .ReturnsAsync((JobResponse?)null);

//     //         // Act
//     //         var result = await _controller.GetJob(jobId);

//     //         // Assert
//     //         Assert.IsType<NotFoundResult>(result);
//     //     }

//     //     [Fact]
//     //     public async Task GetJob_WhenServiceThrowsException_ReturnsInternalServerError()
//     //     {
//     //         // Arrange
//     //         var jobId = "507f1f77bcf86cd799439011";
//     //         _mockJobService
//     //             .Setup(s => s.GetJobAsync(jobId))
//     //             .ThrowsAsync(new Exception("Database error"));

//     //         // Act
//     //         var result = await _controller.GetJob(jobId);

//     //         // Assert
//     //         var statusCodeResult = Assert.IsType<ObjectResult>(result);
//     //         Assert.Equal(500, statusCodeResult.StatusCode);
//     //         Assert.Equal("Error retrieving job", statusCodeResult.Value);
//     //     }

//     //     #endregion

//     //     #region GetAllJobs Tests

//     //     [Fact]
//     //     public async Task GetAllJobs_WithDefaultParameters_ReturnsOkResult()
//     //     {
//     //         // Arrange
//     //         var expectedJobs = new List<JobResponse>
//     //         {
//     //             new() { Id = "1", OriginalText = "Job 1" },
//     //             new() { Id = "2", OriginalText = "Job 2" }
//     //         };

//     //         _mockJobService
//     //             .Setup(s => s.GetAllJobsAsync(0, 20))
//     //             .ReturnsAsync(expectedJobs);

//     //         // Act
//     //         var result = await _controller.GetAllJobs();

//     //         // Assert
//     //         var okResult = Assert.IsType<OkObjectResult>(result);
//     //         var jobs = Assert.IsType<List<JobResponse>>(okResult.Value);
//     //         Assert.Equal(2, jobs.Count);
//     //     }

//     //     [Fact]
//     //     public async Task GetAllJobs_WithCustomParameters_CallsServiceWithCorrectParameters()
//     //     {
//     //         // Arrange
//     //         var skip = 10;
//     //         var limit = 5;
//     //         var expectedJobs = new List<JobResponse>();

//     //         _mockJobService
//     //             .Setup(s => s.GetAllJobsAsync(skip, limit))
//     //             .ReturnsAsync(expectedJobs);

//     //         // Act
//     //         var result = await _controller.GetAllJobs(skip, limit);

//     //         // Assert
//     //         Assert.IsType<OkObjectResult>(result);
//     //         _mockJobService.Verify(s => s.GetAllJobsAsync(skip, limit), Times.Once);
//     //     }

//     //     [Fact]
//     //     public async Task GetAllJobs_WhenServiceThrowsException_ReturnsInternalServerError()
//     //     {
//     //         // Arrange
//     //         _mockJobService
//     //             .Setup(s => s.GetAllJobsAsync(It.IsAny<int>(), It.IsAny<int>()))
//     //             .ThrowsAsync(new Exception("Database error"));

//     //         // Act
//     //         var result = await _controller.GetAllJobs();

//     //         // Assert
//     //         var statusCodeResult = Assert.IsType<ObjectResult>(result);
//     //         Assert.Equal(500, statusCodeResult.StatusCode);
//     //         Assert.Equal("Error retrieving jobs", statusCodeResult.Value);
//     //     }

//     //     #endregion

//     //     #region GetUserJobs Tests

//     //     [Fact]
//     //     public async Task GetUserJobs_WithValidEmail_ReturnsOkResult()
//     //     {
//     //         // Arrange
//     //         var email = "test@example.com";
//     //         var expectedJobs = new List<JobDescription>
//     //         {
//     //             new() { Id = "1", OriginalText = "Job 1", UserEmail = email },
//     //             new() { Id = "2", OriginalText = "Job 2", UserEmail = email }
//     //         };

//     //         _mockJobService
//     //             .Setup(s => s.GetByUserEmailAsync(email))
//     //             .ReturnsAsync(expectedJobs);

//     //         // Act
//     //         var result = await _controller.GetUserJobs(email);

//     //         // Assert
//     //         var okResult = Assert.IsType<OkObjectResult>(result.Result);
//     //         var jobs = Assert.IsType<List<JobDescription>>(okResult.Value);
//     //         Assert.Equal(2, jobs.Count);
//     //         Assert.All(jobs, job => Assert.Equal(email, job.UserEmail));
//     //     }

//     //     [Fact]
//     //     public async Task GetUserJobs_WithNonExistentEmail_ReturnsEmptyList()
//     //     {
//     //         // Arrange
//     //         var email = "nonexistent@example.com";
//     //         var expectedJobs = new List<JobDescription>();

//     //         _mockJobService
//     //             .Setup(s => s.GetByUserEmailAsync(email))
//     //             .ReturnsAsync(expectedJobs);

//     //         // Act
//     //         var result = await _controller.GetUserJobs(email);

//     //         // Assert
//     //         var okResult = Assert.IsType<OkObjectResult>(result.Result);
//     //         var jobs = Assert.IsType<List<JobDescription>>(okResult.Value);
//     //         Assert.Empty(jobs);
//     //     }

//     //     [Fact]
//     //     public async Task GetUserJobs_WhenServiceThrowsException_ReturnsInternalServerError()
//     //     {
//     //         // Arrange
//     //         var email = "test@example.com";
//     //         _mockJobService
//     //             .Setup(s => s.GetByUserEmailAsync(email))
//     //             .ThrowsAsync(new Exception("Database error"));

//     //         // Act
//     //         var result = await _controller.GetUserJobs(email);

//     //         // Assert
//     //         var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
//     //         Assert.Equal(500, statusCodeResult.StatusCode);
//     //         Assert.Equal("Internal server error", statusCodeResult.Value);
//     //     }

//     //     #endregion

//     //     #region Helper Methods

//     //     private static IFormFile CreateMockFormFile(string fileName, string content)
//     //     {
//     //         var mockFile = new Mock<IFormFile>();
//     //         var contentBytes = Encoding.UTF8.GetBytes(content);
//     //         var stream = new MemoryStream(contentBytes);

//     //         mockFile.Setup(f => f.FileName).Returns(fileName);
//     //         mockFile.Setup(f => f.Length).Returns(contentBytes.Length);
//     //         mockFile.Setup(f => f.OpenReadStream()).Returns(stream);
//     //         mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
//     //                .Returns((Stream target, CancellationToken token) =>
//     //                {
//     //                    stream.Position = 0;
//     //                    return stream.CopyToAsync(target, token);
//     //                });

//     //         return mockFile.Object;
//     //     }

//     //     #endregion
//     // }

//     // #region Integration Tests

//     // /// <summary>
//     // /// Integration tests using TestServer for end-to-end testing
//     // /// </summary>
//     // public class JobControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
//     // {
//     //     private readonly WebApplicationFactory<Program> _factory;
//     //     private readonly HttpClient _client;

//     //     // public JobControllerIntegrationTests(WebApplicationFactory<Program> factory)
//     //     // {
//     //     //     _factory = factory;
//     //     //     _client = _factory.CreateClient();
//     //     // }
//     //     public JobControllerIntegrationTests(WebApplicationFactory<Program> factory)
//     //         {
//     //             _factory = factory.WithWebHostBuilder(builder =>
//     //             {
//     //                 builder.ConfigureServices(services =>
//     //                 {
//     //                     // Remove the real MongoDB connection
//     //                     var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(MongoDbContext));
//     //                     if (descriptor != null)
//     //                     {
//     //                         services.Remove(descriptor);
//     //                     }
                        
//     //                     // Add a mock MongoDB context
//     //                     var mockDb = new Mock<MongoDbContext>();
//     //                     mockDb.Setup(x => x.GetJobAsync("invalid-id")).ReturnsAsync((JobDescription)null);
//     //                     mockDb.Setup(x => x.GetJobAsync(It.IsAny<string>())).ReturnsAsync((JobDescription)null);
                        
//     //                     services.AddSingleton(mockDb.Object);
//     //                 });
                    
//     //                 builder.UseEnvironment("Testing");
//     //             });
                
//     //             _client = _factory.CreateClient();
//     //         }

//     //     [Fact]
//     //     public async Task GetAllJobs_ReturnsSuccessStatusCode()
//     //     {
//     //         // Act
//     //         var response = await _client.GetAsync("/api/jobs");

//     //         // Assert
//     //         response.EnsureSuccessStatusCode();
//     //         Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType?.ToString());
//     //     }

//     //     [Fact]
//     //     public async Task GetJob_WithInvalidId_ReturnsNotFound()
//     //     {
//     //         // Act
//     //         var response = await _client.GetAsync("/api/jobs/invalid-id");

//     //         // Assert
//     //         Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
//     //     }

//     //     [Fact]
//     //     public async Task GetUserJobs_WithValidEmail_ReturnsSuccessStatusCode()
//     //     {
//     //         // Arrange
//     //         var email = "test@example.com";

//     //         // Act
//     //         var response = await _client.GetAsync($"/api/jobs/user/{email}");

//     //         // Assert
//     //         response.EnsureSuccessStatusCode();
//     //         Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType?.ToString());
//     //     }

//     //     [Fact]
//     //     public async Task HealthCheck_ReturnsSuccessStatusCode()
//     //     {
//     //         // Act
//     //         var response = await _client.GetAsync("/health");

//     //         // Assert
//     //         if (response.StatusCode == HttpStatusCode.NotFound)
//     //         {
//     //             // If health endpoint doesn't exist, that's fine for this test
//     //             Assert.True(true);
//     //         }
//     //         else
//     //         {
//     //             response.EnsureSuccessStatusCode();
//     //         }
//     //     }

//         // protected virtual void Dispose(bool disposing)
//         // {
//         //     if (disposing)
//         //     {
//         //         _client?.Dispose();
//         //     }
//         // }

//         // public void Dispose()
//         // {
//         //     Dispose(true);
//         //     GC.SuppressFinalize(this);
//         // }
//     }

    
// }

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
        private readonly Mock<MongoDbContext> _mockDbContext;
        private readonly Mock<PythonService> _mockPythonService;
        private readonly Mock<ILogger<JobService>> _mockJobServiceLogger;
        private readonly Mock<ILogger<JobController>> _mockControllerLogger;
        private readonly JobService _jobService;
        private readonly JobController _controller;

        public JobControllerTests()
        {
            // Mock all JobService dependencies
            _mockDbContext = new Mock<MongoDbContext>();
            _mockPythonService = new Mock<PythonService>();
            _mockJobServiceLogger = new Mock<ILogger<JobService>>();
            _mockControllerLogger = new Mock<ILogger<JobController>>();

            // Create actual JobService with mocked dependencies
            _jobService = new JobService(_mockDbContext.Object, _mockPythonService.Object, _mockJobServiceLogger.Object);
            
            // Create controller with the JobService
            _controller = new JobController(_jobService, _mockControllerLogger.Object);
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

            // Mock PythonService.ExtractTextFromFileAsync
            _mockPythonService
                .Setup(s => s.ExtractTextFromFileAsync(It.IsAny<byte[]>(), It.IsAny<string>()))
                .ReturnsAsync(fileContent);

            // Mock PythonService.AnalyzeTextAsync
            var mockAnalysis = new AnalysisResult
            {
                ImprovedText = "Improved job description content",
                suggestions = new List<Suggestion>()
            };
            _mockPythonService
                .Setup(s => s.AnalyzeTextAsync(It.IsAny<string>()))
                .ReturnsAsync(mockAnalysis);

            // Mock database save
            var savedJob = new JobDescription
            {
                Id = "507f1f77bcf86cd799439011",
                OriginalText = fileContent,
                ImprovedText = mockAnalysis.ImprovedText,
                FileName = "test.txt",
                UserEmail = "test@example.com",
                CreatedAt = DateTime.UtcNow,
                Analysis = mockAnalysis
            };

            _mockDbContext
                .Setup(db => db.CreateJobAsync(It.IsAny<JobDescription>()))
                .ReturnsAsync(savedJob);

            // Act
            var result = await _controller.UploadFile(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<JobResponse>(okResult.Value);
            Assert.Equal(savedJob.Id, response.Id);
            Assert.Equal(savedJob.OriginalText, response.OriginalText);
            Assert.Equal(savedJob.ImprovedText, response.ImprovedText);
            Assert.Equal(savedJob.FileName, response.FileName);
            Assert.Equal(savedJob.UserEmail, response.UserEmail);

            // Verify service calls
            _mockPythonService.Verify(s => s.ExtractTextFromFileAsync(It.IsAny<byte[]>(), "test.txt"), Times.Once);
            _mockPythonService.Verify(s => s.AnalyzeTextAsync(fileContent), Times.Once);
            _mockDbContext.Verify(db => db.CreateJobAsync(It.IsAny<JobDescription>()), Times.Once);
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

            // Setup mocks
            _mockPythonService
                .Setup(s => s.ExtractTextFromFileAsync(It.IsAny<byte[]>(), It.IsAny<string>()))
                .ReturnsAsync(fileContent);

            var mockAnalysis = new AnalysisResult
            {
                ImprovedText = "Improved content",
                suggestions = new List<Suggestion>()
            };
            _mockPythonService
                .Setup(s => s.AnalyzeTextAsync(It.IsAny<string>()))
                .ReturnsAsync(mockAnalysis);

            var savedJob = new JobDescription
            {
                Id = "507f1f77bcf86cd799439011",
                OriginalText = fileContent,
                ImprovedText = mockAnalysis.ImprovedText,
                FileName = fileName,
                UserEmail = "test@example.com",
                CreatedAt = DateTime.UtcNow,
                Analysis = mockAnalysis
            };

            _mockDbContext
                .Setup(db => db.CreateJobAsync(It.IsAny<JobDescription>()))
                .ReturnsAsync(savedJob);

            // Act
            var result = await _controller.UploadFile(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            _mockPythonService.Verify(s => s.ExtractTextFromFileAsync(It.IsAny<byte[]>(), fileName), Times.Once);
        }

        [Fact]
        public async Task UploadFile_PythonServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var fileContent = "This is a test job description content that is longer than 50 characters.";
            var mockFile = CreateMockFormFile("test.txt", fileContent);
            var request = new UploadRequest
            {
                File = mockFile,
                UserEmail = "test@example.com"
            };

            _mockPythonService
                .Setup(s => s.ExtractTextFromFileAsync(It.IsAny<byte[]>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Python service error"));

            // Act
            var result = await _controller.UploadFile(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal("An error occurred while processing your file. Please try again.", statusCodeResult.Value);
        }

        [Fact]
        public async Task UploadFile_ExtractedTextTooShort_ReturnsInternalServerError()
        {
            // Arrange
            var shortContent = "Short"; // Less than 50 characters
            var mockFile = CreateMockFormFile("test.txt", "Some file content");
            var request = new UploadRequest
            {
                File = mockFile,
                UserEmail = "test@example.com"
            };

            _mockPythonService
                .Setup(s => s.ExtractTextFromFileAsync(It.IsAny<byte[]>(), It.IsAny<string>()))
                .ReturnsAsync(shortContent);

            // Act
            var result = await _controller.UploadFile(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal("An error occurred while processing your file. Please try again.", statusCodeResult.Value);
        }

        [Fact]
        public async Task UploadFile_NoTextExtracted_ReturnsInternalServerError()
        {
            // Arrange
            var mockFile = CreateMockFormFile("test.txt", "Some file content");
            var request = new UploadRequest
            {
                File = mockFile,
                UserEmail = "test@example.com"
            };

            _mockPythonService
                .Setup(s => s.ExtractTextFromFileAsync(It.IsAny<byte[]>(), It.IsAny<string>()))
                .ReturnsAsync(string.Empty);

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

            // Setup mocks
            _mockPythonService
                .Setup(s => s.ExtractTextFromFileAsync(It.IsAny<byte[]>(), It.IsAny<string>()))
                .ReturnsAsync(fileContent);

            var mockAnalysis = new AnalysisResult
            {
                ImprovedText = "Improved content",
                suggestions = new List<Suggestion>()
            };
            _mockPythonService
                .Setup(s => s.AnalyzeTextAsync(It.IsAny<string>()))
                .ReturnsAsync(mockAnalysis);

            var savedJob = new JobDescription
            {
                Id = "507f1f77bcf86cd799439011",
                OriginalText = fileContent,
                ImprovedText = mockAnalysis.ImprovedText,
                FileName = "test.TXT",
                UserEmail = "test@example.com",
                CreatedAt = DateTime.UtcNow,
                Analysis = mockAnalysis
            };

            _mockDbContext
                .Setup(db => db.CreateJobAsync(It.IsAny<JobDescription>()))
                .ReturnsAsync(savedJob);

            // Act
            var result = await _controller.UploadFile(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            _mockPythonService.Verify(s => s.ExtractTextFromFileAsync(It.IsAny<byte[]>(), "test.TXT"), Times.Once);
        }
    }
}