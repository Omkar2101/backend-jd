using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using backend_jd_api.Services;
using backend_jd_api.Models;
using backend_jd_api.Data;


namespace backend_jd_api.Tests.Services
{
    public class JobServiceTests
    {
        private readonly Mock<MongoDbContext> _mockDb;
        private readonly Mock<PythonService> _mockPythonService;
        private readonly Mock<IFileStorageService> _mockFileStorageService;
        private readonly Mock<ILogger<JobService>> _mockLogger;

        private readonly JobService _service;

        public JobServiceTests()
        {
            _mockDb = new Mock<MongoDbContext>();
            _mockPythonService = new Mock<PythonService>();
            _mockFileStorageService = new Mock<IFileStorageService>();
            _mockLogger = new Mock<ILogger<JobService>>();

            _service = new JobService(_mockDb.Object, _mockPythonService.Object, _mockLogger.Object, _mockFileStorageService.Object);
        }

        // Helper: create mock IFormFile from string content
        private static IFormFile CreateMockFile(string fileName, string content)
        {
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            writer.Write(content);
            writer.Flush();
            ms.Position = 0;

            return new FormFile(ms, 0, ms.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "text/plain"
            };
        }

        [Fact]
        public async Task AnalyzeFromFileAsync_ValidFile_ReturnsJobResponse()
        {
            var file = CreateMockFile("jobdesc.txt", "This is a valid job description text with more than 50 characters...");
            var userEmail = "user@example.com";
            var extractedText = "This is a valid job description text with more than 50 characters...";
            var analysis = new AnalysisResult
            {
                ImprovedText = "Improved text",
                suggestions = new List<Suggestion>()
            };

            _mockFileStorageService
                .Setup(s => s.SaveFileAsync(file, userEmail))
                .ReturnsAsync(new FileStorageResult
                {
                    IsSuccess = true,
                    StoredFileName = "storedname.txt",
                    FilePath = "/files/storedname.txt"
                });

            _mockPythonService
                .Setup(p => p.ExtractTextFromFileAsync(It.IsAny<byte[]>(), file.FileName))
                .ReturnsAsync(new TextExtractionResult
                {
                    IsSuccess = true,
                    ExtractedText = extractedText
                });

            _mockPythonService
                .Setup(p => p.AnalyzeTextAsync(extractedText))
                .ReturnsAsync(new AnalysisServiceResult
                {
                    IsSuccess = true,
                    AnalysisResult = analysis
                });

            _mockFileStorageService.Setup(s => s.GetFileUrl("storedname.txt"))
                .Returns("https://fileserver.com/storedname.txt");

            _mockDb.Setup(db => db.CreateJobAsync(It.IsAny<JobDescription>()))
                .ReturnsAsync((JobDescription jd) =>
                {
                    jd.Id = "job123";
                    return jd;
                });

            var result = await _service.AnalyzeFromFileAsync(file, userEmail);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.JobResponse);
            Assert.Equal("job123", result.JobResponse.Id);
            Assert.Equal(userEmail, result.JobResponse.UserEmail);
            Assert.Equal(extractedText, result.JobResponse.OriginalText);
            Assert.Equal("Improved text", result.JobResponse.ImprovedText);
            Assert.Equal("jobdesc.txt", result.JobResponse.FileName);
            Assert.Equal("https://fileserver.com/storedname.txt", result.JobResponse.FileUrl);
        }

        [Fact]
        public async Task AnalyzeFromFileAsync_EmptyExtractedText_ReturnsFailureAndDeletesFile()
        {
            var file = CreateMockFile("empty.txt", "some content");
            var userEmail = "user@example.com";

            _mockFileStorageService.Setup(s => s.SaveFileAsync(file, userEmail))
                .ReturnsAsync(new FileStorageResult
                {
                    IsSuccess = true,
                    StoredFileName = "storedname.txt",
                    FilePath = "/files/storedname.txt"
                });

            _mockPythonService.Setup(p => p.ExtractTextFromFileAsync(It.IsAny<byte[]>(), file.FileName))
                .ReturnsAsync(new TextExtractionResult
                {
                    IsSuccess = true,
                    ExtractedText = string.Empty
                });

            var result = await _service.AnalyzeFromFileAsync(file, userEmail);

            Assert.False(result.IsSuccess);
            Assert.Contains("No text could be extracted from the file", result.ErrorMessage);
            _mockFileStorageService.Verify(s => s.DeleteFileAsync("storedname.txt"), Times.Once);
        }

        [Fact]
        public async Task AnalyzeTextAsync_ValidText_ReturnsJobResponse()
        {
            var text = "This job description text is definitely more than 50 characters long for testing.";
            var userEmail = "user@example.com";
            var analysis = new AnalysisResult { ImprovedText = "Improved text", suggestions = new List<Suggestion>() };

            _mockPythonService.Setup(p => p.AnalyzeTextAsync(text))
                .ReturnsAsync(new AnalysisServiceResult
                {
                    IsSuccess = true,
                    AnalysisResult = analysis
                });

            _mockDb.Setup(db => db.CreateJobAsync(It.IsAny<JobDescription>())).ReturnsAsync((JobDescription jd) =>
            {
                jd.Id = "jobTxt123";
                return jd;
            });

            var result = await _service.AnalyzeTextAsync(text, userEmail, "Job Title");

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.JobResponse);
            Assert.Equal("jobTxt123", result.JobResponse.Id);
            Assert.Equal(userEmail, result.JobResponse.UserEmail);
            Assert.Equal(text, result.JobResponse.OriginalText);
            Assert.Equal("Improved text", result.JobResponse.ImprovedText);
            Assert.Equal("Job Title", result.JobResponse.FileName);
        }

        [Fact]
        public async Task AnalyzeTextAsync_ShortText_ReturnsFailure()
        {
            var shortText = "Too short";
            var userEmail = "user@example.com";

            var result = await _service.AnalyzeTextAsync(shortText, userEmail);

            Assert.False(result.IsSuccess);
            Assert.Contains("at least 50 characters", result.ErrorMessage);
        }

        [Fact]
        public async Task GetJobAsync_ExistingJob_ReturnsJobResponse()
        {
            var jobId = "job123";
            var job = new JobDescription
            {
                Id = jobId,
                UserEmail = "user@example.com",
                OriginalText = "Original text",
                ImprovedText = "Improved text",
                FileName = "file.txt",
                Analysis = new AnalysisResult(),
                CreatedAt = DateTime.UtcNow,
                StoredFileName = "storedFile.txt"
            };

            _mockDb.Setup(db => db.GetJobAsync(jobId)).ReturnsAsync(job);
            _mockFileStorageService.Setup(s => s.GetFileUrl("storedFile.txt"))
                .Returns("https://fileserver.com/storedFile.txt");

            var result = await _service.GetJobAsync(jobId);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.JobResponse);
            Assert.Equal(jobId, result.JobResponse.Id);
            Assert.Equal("user@example.com", result.JobResponse.UserEmail);
            Assert.Equal("https://fileserver.com/storedFile.txt", result.JobResponse.FileUrl);
        }

        [Fact]
        public async Task GetJobAsync_NonExistingJob_ReturnsFailure()
        {
            var jobId = "nonexistent";
            _mockDb.Setup(db => db.GetJobAsync(jobId)).ReturnsAsync((JobDescription)null);

            var result = await _service.GetJobAsync(jobId);

            Assert.False(result.IsSuccess);
            Assert.Equal("Job not found", result.ErrorMessage);
        }

        [Fact]
        public async Task DeleteJobAsync_Success_ReturnsTrue()
        {
            _mockDb.Setup(db => db.DeleteJobAsync("job123")).ReturnsAsync(true);

            var result = await _service.DeleteJobAsync("job123");

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task DeleteJobAsync_NotFound_ReturnsFalse()
        {
            _mockDb.Setup(db => db.DeleteJobAsync("job123")).ReturnsAsync(false);

            var result = await _service.DeleteJobAsync("job123");

            Assert.False(result.IsSuccess);
            Assert.Equal("Job not found", result.ErrorMessage);
        }
    }
}
