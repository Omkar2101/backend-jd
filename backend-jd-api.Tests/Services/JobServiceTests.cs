using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using System.Text;
using backend_jd_api.Services;
using backend_jd_api.Models;
using backend_jd_api.Data;

namespace backend_jd_api.Tests.Services
{
    public class JobServiceTests
    {
        private readonly Mock<MongoDbContext> _mockDb;
        private readonly Mock<PythonService> _mockPythonService;
        private readonly Mock<ILogger<JobService>> _mockLogger;
        private readonly JobService _jobService;

        public JobServiceTests()
        {
            _mockDb = new Mock<MongoDbContext>();
            _mockPythonService = new Mock<PythonService>();
            _mockLogger = new Mock<ILogger<JobService>>();
            _jobService = new JobService(_mockDb.Object, _mockPythonService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task AnalyzeFromFileAsync_ValidFile_ReturnsJobResponse()
        {
            var file = CreateMockFile("test.pdf", "This is a valid job description with more than 50 characters to meet the minimum requirements.");
            var userEmail = "test@example.com";
            var extractedText = "This is a valid job description with more than 50 characters to meet the minimum requirements.";
            var analysisResult = new AnalysisResult
            {
                ImprovedText = "Improved job description",
                suggestions = new List<Suggestion>
                {
                    new Suggestion {  Original = "Suggestion 1" },
                    new Suggestion {  Original = "Suggestion 2" }
                }
            };
            var savedJob = new JobDescription
            {
                Id = "123",
                UserEmail = userEmail,
                OriginalText = extractedText,
                ImprovedText = analysisResult.ImprovedText,
                FileName = "test.pdf",
                Analysis = analysisResult,
                CreatedAt = DateTime.UtcNow
            };
            _mockPythonService.Setup(x => x.ExtractTextFromFileAsync(It.IsAny<byte[]>(), "test.pdf")).ReturnsAsync(extractedText);
            _mockPythonService.Setup(x => x.AnalyzeTextAsync(extractedText)).ReturnsAsync(analysisResult);
            _mockDb.Setup(x => x.CreateJobAsync(It.IsAny<JobDescription>())).ReturnsAsync(savedJob);
            var result = await _jobService.AnalyzeFromFileAsync(file, userEmail);
            //print the result
            Console.WriteLine("Job Response: bababaaaaaaaaaaaa" + System.Text.Json.JsonSerializer.Serialize(result));
            Assert.NotNull(result);
            Assert.Equal("123", result.Id);
            Assert.Equal(userEmail, result.UserEmail);
            Assert.Equal(extractedText, result.OriginalText);
            Assert.Equal(analysisResult.ImprovedText, result.ImprovedText);
            Assert.Equal("test.pdf", result.FileName);
            Assert.Equal(analysisResult, result.Analysis);
        }

        [Fact]
        public async Task AnalyzeFromFileAsync_EmptyExtractedText_ThrowsException()
        {
            var file = CreateMockFile("test.txt", "content");
            var userEmail = "test@example.com";
            _mockPythonService.Setup(x => x.ExtractTextFromFileAsync(It.IsAny<byte[]>(), "test.txt")).ReturnsAsync(string.Empty);
            var exception = await Assert.ThrowsAsync<Exception>(() => _jobService.AnalyzeFromFileAsync(file, userEmail));
            Assert.Contains("No text could be extracted from the file", exception.Message);
        }

        [Fact]
        public async Task AnalyzeFromFileAsync_WhitespaceOnlyExtractedText_ThrowsException()
        {
            var file = CreateMockFile("test.txt", "content");
            var userEmail = "test@example.com";
            _mockPythonService.Setup(x => x.ExtractTextFromFileAsync(It.IsAny<byte[]>(), "test.txt")).ReturnsAsync("   \n\t   ");
            var exception = await Assert.ThrowsAsync<Exception>(() => _jobService.AnalyzeFromFileAsync(file, userEmail));
            Assert.Contains("No text could be extracted from the file", exception.Message);
        }

        [Fact]
        public async Task AnalyzeFromFileAsync_TextTooShort_ThrowsException()
        {
            var file = CreateMockFile("test.txt", "content");
            var userEmail = "test@example.com";
            var shortText = "Short text";
            _mockPythonService.Setup(x => x.ExtractTextFromFileAsync(It.IsAny<byte[]>(), "test.txt")).ReturnsAsync(shortText);
            var exception = await Assert.ThrowsAsync<Exception>(() => _jobService.AnalyzeFromFileAsync(file, userEmail));
            Assert.Contains("The extracted text is too short", exception.Message);
            Assert.Contains($"({shortText.Trim().Length} characters)", exception.Message);
        }

        [Fact]
        public async Task AnalyzeFromFileAsync_ExtractTextThrowsException_LogsErrorAndRethrows()
        {
            var file = CreateMockFile("test.txt", "content");
            var userEmail = "test@example.com";
            var pythonServiceException = new Exception("Python service error");
            _mockPythonService.Setup(x => x.ExtractTextFromFileAsync(It.IsAny<byte[]>(), "test.txt")).ThrowsAsync(pythonServiceException);
            var exception = await Assert.ThrowsAsync<Exception>(() => _jobService.AnalyzeFromFileAsync(file, userEmail));
            Assert.Equal("Python service error", exception.Message);
            _mockLogger.Verify(x => x.Log(LogLevel.Error, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error analyzing file test.txt")), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }

        [Fact]
        public async Task AnalyzeFromFileAsync_AnalyzeTextThrowsException_LogsErrorAndRethrows()
        {
            var file = CreateMockFile("test.txt", "content");
            var userEmail = "test@example.com";
            var extractedText = "This is a valid job description with more than 50 characters to meet the minimum requirements.";
            var analysisException = new Exception("Analysis failed");
            _mockPythonService.Setup(x => x.ExtractTextFromFileAsync(It.IsAny<byte[]>(), "test.txt")).ReturnsAsync(extractedText);
            _mockPythonService.Setup(x => x.AnalyzeTextAsync(extractedText)).ThrowsAsync(analysisException);
            var exception = await Assert.ThrowsAsync<Exception>(() => _jobService.AnalyzeFromFileAsync(file, userEmail));
            Assert.Equal("Analysis failed", exception.Message);
        }

        [Fact]
        public async Task AnalyzeFromFileAsync_DatabaseSaveThrowsException_LogsErrorAndRethrows()
        {
            var file = CreateMockFile("test.txt", "content");
            var userEmail = "test@example.com";
            var extractedText = "This is a valid job description with more than 50 characters to meet the minimum requirements.";
            var analysisResult = new AnalysisResult { ImprovedText = "Improved text" };
            var dbException = new Exception("Database error");
            _mockPythonService.Setup(x => x.ExtractTextFromFileAsync(It.IsAny<byte[]>(), "test.txt")).ReturnsAsync(extractedText);
            _mockPythonService.Setup(x => x.AnalyzeTextAsync(extractedText)).ReturnsAsync(analysisResult);
            _mockDb.Setup(x => x.CreateJobAsync(It.IsAny<JobDescription>())).ThrowsAsync(dbException);
            var exception = await Assert.ThrowsAsync<Exception>(() => _jobService.AnalyzeFromFileAsync(file, userEmail));
            Assert.Equal("Database error", exception.Message);
        }

        [Fact]
        public async Task AnalyzeTextAsync_ValidText_ReturnsJobResponse()
        {
            var text = "This is a valid job description with more than 50 characters to meet the minimum requirements.";
            var userEmail = "test@example.com";
            var jobTitle = "Software Engineer";
            var analysisResult = new AnalysisResult
            {
                ImprovedText = "Improved job description",
                suggestions = new List<Suggestion>
                {
                    new Suggestion { Original = "Suggestion 1" }
                }
            };
            var savedJob = new JobDescription
            {
                Id = "123",
                UserEmail = userEmail,
                OriginalText = text,
                ImprovedText = analysisResult.ImprovedText,
                FileName = jobTitle,
                Analysis = analysisResult,
                CreatedAt = DateTime.UtcNow
            };
            _mockPythonService.Setup(x => x.AnalyzeTextAsync(text)).ReturnsAsync(analysisResult);
            _mockDb.Setup(x => x.CreateJobAsync(It.IsAny<JobDescription>())).ReturnsAsync(savedJob);
            var result = await _jobService.AnalyzeTextAsync(text, userEmail, jobTitle);
            Assert.NotNull(result);
            Assert.Equal("123", result.Id);
            Assert.Equal(userEmail, result.UserEmail);
            Assert.Equal(text, result.OriginalText);
            Assert.Equal(analysisResult.ImprovedText, result.ImprovedText);
            Assert.Equal(jobTitle, result.FileName);
            Assert.Equal(analysisResult, result.Analysis);
        }

        [Fact]
        public async Task AnalyzeTextAsync_EmptyText_ThrowsArgumentException()
        {
            var text = "";
            var userEmail = "test@example.com";
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => _jobService.AnalyzeTextAsync(text, userEmail));
            Assert.Equal("text", exception.ParamName);
            Assert.Contains("Text cannot be empty or whitespace", exception.Message);
        }

        [Fact]
        public async Task AnalyzeTextAsync_NullText_ThrowsArgumentException()
        {
            string text = null;
            var userEmail = "test@example.com";
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => _jobService.AnalyzeTextAsync(text, userEmail));
            Assert.Equal("text", exception.ParamName);
            Assert.Contains("Text cannot be empty or whitespace", exception.Message);
        }

        [Fact]
        public async Task AnalyzeTextAsync_WhitespaceOnlyText_ThrowsArgumentException()
        {
            var text = "   \n\t   ";
            var userEmail = "test@example.com";
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => _jobService.AnalyzeTextAsync(text, userEmail));
            Assert.Equal("text", exception.ParamName);
            Assert.Contains("Text cannot be empty or whitespace", exception.Message);
        }

        [Fact]
        public async Task AnalyzeTextAsync_TextTooShort_ThrowsArgumentException()
        {
            var text = "Short";
            var userEmail = "test@example.com";
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => _jobService.AnalyzeTextAsync(text, userEmail));
            Assert.Contains("Job description text must be at least 50 characters long", exception.Message);
            Assert.Contains($"Current length: {text.Trim().Length} characters", exception.Message);
        }

        [Fact]
        public async Task AnalyzeTextAsync_NoJobTitle_UsesDefaultFileName()
        {
            var text = "This is a valid job description with more than 50 characters to meet the minimum requirements.";
            var userEmail = "test@example.com";
            var analysisResult = new AnalysisResult { ImprovedText = "Improved text" };
            var savedJob = new JobDescription
            {
                Id = "123",
                FileName = "Direct Input",
                UserEmail = userEmail,
                OriginalText = text,
                ImprovedText = analysisResult.ImprovedText,
                Analysis = analysisResult,
                CreatedAt = DateTime.UtcNow
            };
            _mockPythonService.Setup(x => x.AnalyzeTextAsync(text)).ReturnsAsync(analysisResult);
            _mockDb.Setup(x => x.CreateJobAsync(It.IsAny<JobDescription>())).ReturnsAsync(savedJob);
            var result = await _jobService.AnalyzeTextAsync(text, userEmail);
            Assert.Equal("Direct Input", result.FileName);
        }

        [Fact]
        public async Task AnalyzeTextAsync_AnalysisThrowsException_LogsErrorAndRethrows()
        {
            var text = "This is a valid job description with more than 50 characters to meet the minimum requirements.";
            var userEmail = "test@example.com";
            var analysisException = new Exception("Analysis failed");
            _mockPythonService.Setup(x => x.AnalyzeTextAsync(text)).ThrowsAsync(analysisException);
            var exception = await Assert.ThrowsAsync<Exception>(() => _jobService.AnalyzeTextAsync(text, userEmail));
            Assert.Equal("Analysis failed", exception.Message);
            _mockLogger.Verify(x => x.Log(LogLevel.Error, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error analyzing text")), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }

        [Fact]
        public async Task GetJobAsync_JobExists_ReturnsJobResponse()
        {
            var jobId = "123";
            var job = new JobDescription
            {
                Id = jobId,
                UserEmail = "test@example.com",
                OriginalText = "Original text",
                ImprovedText = "Improved text",
                FileName = "test.txt",
                Analysis = new AnalysisResult(),
                CreatedAt = DateTime.UtcNow
            };
            _mockDb.Setup(x => x.GetJobAsync(jobId)).ReturnsAsync(job);
            var result = await _jobService.GetJobAsync(jobId);
            Assert.NotNull(result);
            Assert.Equal(jobId, result.Id);
            Assert.Equal("test@example.com", result.UserEmail);
            Assert.Equal("Original text", result.OriginalText);
            Assert.Equal("Improved text", result.ImprovedText);
            Assert.Equal("test.txt", result.FileName);
        }

        [Fact]
        public async Task GetJobAsync_JobDoesNotExist_ReturnsNull()
        {
            var jobId = "nonexistent";
            _mockDb.Setup(x => x.GetJobAsync(jobId)).ReturnsAsync((JobDescription)null);
            var result = await _jobService.GetJobAsync(jobId);
            Assert.Null(result);
        }

        [Fact]
        public async Task GetJobAsync_EmptyId_CallsDbWithEmptyId()
        {
            var jobId = "";
            _mockDb.Setup(x => x.GetJobAsync(jobId)).ReturnsAsync((JobDescription)null);
            var result = await _jobService.GetJobAsync(jobId);
            Assert.Null(result);
            _mockDb.Verify(x => x.GetJobAsync(jobId), Times.Once);
        }

        [Fact]
        public async Task GetAllJobsAsync_NoParameters_ReturnsJobListWithDefaultPagination()
        {
            var jobs = new List<JobDescription>
            {
                new JobDescription { Id = "1", UserEmail = "user1@example.com", OriginalText = "Text1", ImprovedText = "Improved1", FileName = "file1.txt", Analysis = new AnalysisResult(), CreatedAt = DateTime.UtcNow },
                new JobDescription { Id = "2", UserEmail = "user2@example.com", OriginalText = "Text2", ImprovedText = "Improved2", FileName = "file2.txt", Analysis = new AnalysisResult(), CreatedAt = DateTime.UtcNow }
            };
            _mockDb.Setup(x => x.GetAllJobsAsync(0, 20)).ReturnsAsync(jobs);
            var result = await _jobService.GetAllJobsAsync();
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("1", result[0].Id);
            Assert.Equal("2", result[1].Id);
        }

        [Fact]
        public async Task GetAllJobsAsync_WithCustomPagination_ReturnsCorrectPage()
        {
            var jobs = new List<JobDescription>
            {
                new JobDescription { Id = "3", UserEmail = "user3@example.com", OriginalText = "Text3", ImprovedText = "Improved3", FileName = "file3.txt", Analysis = new AnalysisResult(), CreatedAt = DateTime.UtcNow }
            };
            _mockDb.Setup(x => x.GetAllJobsAsync(10, 5)).ReturnsAsync(jobs);
            var result = await _jobService.GetAllJobsAsync(10, 5);
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("3", result[0].Id);
        }

        [Fact]
        public async Task GetAllJobsAsync_EmptyResult_ReturnsEmptyList()
        {
            var jobs = new List<JobDescription>();
            _mockDb.Setup(x => x.GetAllJobsAsync(0, 20)).ReturnsAsync(jobs);
            var result = await _jobService.GetAllJobsAsync();
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        // [Fact]
        // public async Task GetByUserEmailAsync_ValidEmail_ReturnsUserJobs()
        // {
        //     var userEmail = "test@example.com";
        //     var jobs = new List<JobDescription>
        //     {
        //         new JobDescription { Id = "1", UserEmail = userEmail, OriginalText = "Text1", ImprovedText = "Improved1", FileName = "file1.txt", Analysis = new AnalysisResult(), CreatedAt = DateTime.UtcNow },
        //         new JobDescription { Id = "2", UserEmail = userEmail, OriginalText = "Text2", ImprovedText = "Improved2", FileName = "file2.txt", Analysis = new AnalysisResult(), CreatedAt = DateTime.UtcNow }
        //     };
        //     SetupMongoCollection(jobs,null);
        //     var result = await _jobService.GetByUserEmailAsync(userEmail);
        //     Assert.NotNull(result);
        //     Assert.Equal(jobs.Count, result.Count);
        //     for (int i = 0; i < jobs.Count; i++)
        //     {
        //         Assert.Equal(jobs[i].Id, result[i].Id);
        //         Assert.Equal(jobs[i].UserEmail, result[i].UserEmail);
        //         Assert.Equal(jobs[i].OriginalText, result[i].OriginalText);
        //         Assert.Equal(jobs[i].ImprovedText, result[i].ImprovedText);
        //         Assert.Equal(jobs[i].FileName, result[i].FileName);
        //     }
        // }

        [Fact]
        public async Task GetByUserEmailAsync_EmptyEmail_ThrowsArgumentException()
        {
            var email = "";
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => _jobService.GetByUserEmailAsync(email));
            Assert.Equal("email", exception.ParamName);
            Assert.Contains("Email cannot be null or empty", exception.Message);
        }

        [Fact]
        public async Task GetByUserEmailAsync_NullEmail_ThrowsArgumentException()
        {
            string email = null;
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => _jobService.GetByUserEmailAsync(email));
            Assert.Equal("email", exception.ParamName);
            Assert.Contains("Email cannot be null or empty", exception.Message);
        }

        // [Fact]
        // public async Task GetByUserEmailAsync_NoJobsFound_ReturnsEmptyList()
        // {
        //     var userEmail = "nojobs@example.com";
        //     var jobs = new List<JobDescription>();
        //     SetupMongoCollection(jobs);
        //     var result = await _jobService.GetByUserEmailAsync(userEmail);
        //     Assert.NotNull(result);
        //     Assert.Empty(result);
        // }


        // [Fact]
        // public async Task GetByUserEmailAsync_DatabaseThrowsException_LogsErrorAndRethrows()
        // {
        //     var userEmail = "test@example.com";
        //     var dbException = new Exception("Database connection failed");

        //     // Use the unified helper method with exception
        //     SetupMongoCollection(null,exceptionToThrow: dbException);

        //     var exception = await Assert.ThrowsAsync<Exception>(() => _jobService.GetByUserEmailAsync(userEmail));
        //     Assert.Equal("Database connection failed", exception.Message);
        //     _mockLogger.Verify(x => x.Log(LogLevel.Error, It.IsAny<EventId>(),
        //         It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error retrieving jobs for user")),
        //         It.IsAny<Exception>(),
        //         It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        // }

        private IFormFile CreateMockFile(string fileName, string content)
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            var file = new Mock<IFormFile>();
            file.Setup(f => f.FileName).Returns(fileName);
            file.Setup(f => f.Length).Returns(bytes.Length);
            file.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>())).Returns((Stream stream, CancellationToken token) => { stream.Write(bytes, 0, bytes.Length); return Task.CompletedTask; });
            return file.Object;
        }
        
        private void SetupMongoCollection(List<JobDescription> jobs = null, Exception exceptionToThrow = null)
        {
            var mockCollection = new Mock<IMongoCollection<JobDescription>>();
            var mockFindFluent = new Mock<IFindFluent<JobDescription, JobDescription>>();
            var mockSortedFindFluent = new Mock<IFindFluent<JobDescription, JobDescription>>();
            
            // Explicitly specify both parameters to avoid optional parameter issue
            mockCollection.Setup(x => x.Find(It.IsAny<FilterDefinition<JobDescription>>(), It.IsAny<FindOptions>()))
                .Returns(mockFindFluent.Object);
            mockFindFluent.Setup(x => x.Sort(It.IsAny<SortDefinition<JobDescription>>()))
                .Returns(mockSortedFindFluent.Object);
            
            if (exceptionToThrow != null)
            {
                mockSortedFindFluent.Setup(x => x.ToListAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(exceptionToThrow);
            }
            else
            {
                mockSortedFindFluent.Setup(x => x.ToListAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(jobs ?? new List<JobDescription>());
            }
            
            _mockDb.Setup(x => x.Jobs).Returns(mockCollection.Object);
        }

        
    }
}