// Tests/Services/FileStorageServiceTests.cs
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using System.Text;
using backend_jd_api.Services;

namespace backend_jd_api.Tests.Services
{
    public class FileStorageServiceTests : IDisposable
    {
        private readonly Mock<IWebHostEnvironment> _mockEnvironment;
        private readonly Mock<ILogger<FileStorageService>> _mockLogger;
        private readonly FileStorageService _service;
        private readonly string _tempDirectory;

        public FileStorageServiceTests()
        {
            // Create a temporary directory for testing
            _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDirectory);

            _mockEnvironment = new Mock<IWebHostEnvironment>();
            _mockEnvironment.Setup(x => x.ContentRootPath).Returns(_tempDirectory);

            _mockLogger = new Mock<ILogger<FileStorageService>>();
            
            _service = new FileStorageService(_mockEnvironment.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task SaveFileAsync_WithValidFile_SavesSuccessfully()
        {
            // Arrange
            var userEmail = "test@example.com";
            var fileName = "test.txt";
            var fileContent = "Hello, World!";
            var mockFile = CreateMockFile(fileName, fileContent);

            // Act
            var result = await _service.SaveFileAsync(mockFile.Object, userEmail);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Empty(result.ErrorMessage);
            Assert.NotEmpty(result.StoredFileName);
            Assert.True(result.StoredFileName.EndsWith(".txt"));
            Assert.True(File.Exists(result.FilePath));
            
            var savedContent = await File.ReadAllTextAsync(result.FilePath);
            Assert.Equal(fileContent, savedContent);
        }

        [Fact]
        public async Task SaveFileAsync_CreatesUserDirectory()
        {
            // Arrange
            var userEmail = "newuser@example.com";
            var mockFile = CreateMockFile("test.txt", "content");

            // Act
            var result = await _service.SaveFileAsync(mockFile.Object, userEmail);

            // Assert
            Assert.True(result.IsSuccess);
            var expectedUserDir = Path.Combine(_tempDirectory, "uploads", "newuser_example_com");
            Assert.True(Directory.Exists(expectedUserDir));
        }

        [Fact]
        public async Task SaveFileAsync_WithNullFile_ReturnsFailure()
        {
            // Arrange
            var userEmail = "test@example.com";

            // Act
            var result = await _service.SaveFileAsync(null, userEmail);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("File cannot be null", result.ErrorMessage);
            Assert.Empty(result.StoredFileName);
            Assert.Empty(result.FilePath);
        }

        [Fact]
        public async Task SaveFileAsync_WithEmptyFile_ReturnsFailure()
        {
            // Arrange
            var userEmail = "test@example.com";
            var mockFile = CreateMockFile("empty.txt", "");

            // Act
            var result = await _service.SaveFileAsync(mockFile.Object, userEmail);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("File cannot be empty", result.ErrorMessage);
        }

        [Fact]
        public async Task SaveFileAsync_WithNullUserEmail_ReturnsFailure()
        {
            // Arrange
            var mockFile = CreateMockFile("test.txt", "content");

            // Act
            var result = await _service.SaveFileAsync(mockFile.Object, null);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("User email is required", result.ErrorMessage);
        }

        [Fact]
        public async Task SaveFileAsync_WithFileWithoutExtension_ReturnsFailure()
        {
            // Arrange
            var userEmail = "test@example.com";
            var mockFile = CreateMockFile("filenoext", "content");

            // Act
            var result = await _service.SaveFileAsync(mockFile.Object, userEmail);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("File must have a valid extension", result.ErrorMessage);
        }

        [Fact]
        public async Task GetFileAsync_WithExistingFile_ReturnsFileData()
        {
            // Arrange
            var userEmail = "test@example.com";
            var fileContent = "Test file content";
            var mockFile = CreateMockFile("document.pdf", fileContent);
            
            var saveResult = await _service.SaveFileAsync(mockFile.Object, userEmail);
            Assert.True(saveResult.IsSuccess);

            // Act
            var getResult = await _service.GetFileAsync(saveResult.StoredFileName);

            // Assert
            Assert.True(getResult.IsSuccess);
            Assert.Empty(getResult.ErrorMessage);
            Assert.Equal(Encoding.UTF8.GetBytes(fileContent), getResult.FileData);
            Assert.Equal("application/pdf", getResult.ContentType);
            Assert.Equal(saveResult.StoredFileName, getResult.FileName);
        }

        [Fact]
        public async Task GetFileAsync_WithNonExistentFile_ReturnsFailure()
        {
            // Arrange
            var nonExistentFileName = "non-existent-file.pdf";

            // Act
            var result = await _service.GetFileAsync(nonExistentFileName);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("not found", result.ErrorMessage);
            Assert.Empty(result.FileData);
            Assert.Empty(result.ContentType);
            Assert.Empty(result.FileName);
        }

        [Fact]
        public async Task GetFileAsync_WithNullFileName_ReturnsFailure()
        {
            // Act
            var result = await _service.GetFileAsync(null);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("Stored filename is required", result.ErrorMessage);
        }

        [Fact]
        public async Task DeleteFileAsync_WithExistingFile_DeletesSuccessfully()
        {
            // Arrange
            var userEmail = "test@example.com";
            var mockFile = CreateMockFile("to-delete.txt", "content");
            var saveResult = await _service.SaveFileAsync(mockFile.Object, userEmail);
            Assert.True(saveResult.IsSuccess);

            // Verify file exists before deletion
            Assert.True(File.Exists(saveResult.FilePath));

            // Act
            var deleteResult = await _service.DeleteFileAsync(saveResult.StoredFileName);

            // Assert
            Assert.True(deleteResult.IsSuccess);
            Assert.Empty(deleteResult.ErrorMessage);
            Assert.False(File.Exists(saveResult.FilePath));
        }

        [Fact]
        public async Task DeleteFileAsync_WithNonExistentFile_ReturnsFailure()
        {
            // Arrange
            var nonExistentFileName = "non-existent-file.txt";

            // Act
            var result = await _service.DeleteFileAsync(nonExistentFileName);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("File not found", result.ErrorMessage);
        }

        [Fact]
        public async Task DeleteFileAsync_WithNullFileName_ReturnsFailure()
        {
            // Act
            var result = await _service.DeleteFileAsync(null);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("Stored filename is required", result.ErrorMessage);
        }

        [Fact]
        public void GetFileUrl_ReturnsCorrectUrl()
        {
            // Arrange
            var storedFileName = "test-file.pdf";

            // Act
            var url = _service.GetFileUrl(storedFileName);

            // Assert
            Assert.Equal("/api/files/test-file.pdf", url);
        }

        [Theory]
        [InlineData("document.pdf", "application/pdf")]
        [InlineData("document.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
        [InlineData("document.doc", "application/msword")]
        [InlineData("notes.txt", "text/plain")]
        [InlineData("image.jpg", "image/jpeg")]
        [InlineData("image.jpeg", "image/jpeg")]
        [InlineData("image.png", "image/png")]
        [InlineData("unknown.xyz", "application/octet-stream")]
        public async Task GetFileAsync_ReturnsCorrectContentType(string fileName, string expectedContentType)
        {
            // Arrange
            var userEmail = "test@example.com";
            var mockFile = CreateMockFile(fileName, "content");
            var saveResult = await _service.SaveFileAsync(mockFile.Object, userEmail);
            Assert.True(saveResult.IsSuccess);

            // Act
            var getResult = await _service.GetFileAsync(saveResult.StoredFileName);

            // Assert
            Assert.True(getResult.IsSuccess);
            Assert.Equal(expectedContentType, getResult.ContentType);
        }

    

        private Mock<IFormFile> CreateMockFile(string fileName, string content)
        {
            var mockFile = new Mock<IFormFile>();
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            
            mockFile.Setup(f => f.FileName).Returns(fileName);
            mockFile.Setup(f => f.Length).Returns(stream.Length);
            mockFile.Setup(f => f.OpenReadStream()).Returns(stream);
            mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                   .Returns((Stream target, CancellationToken token) => 
                   {
                       stream.Position = 0;
                       return stream.CopyToAsync(target, token);
                   });

            return mockFile;
        }

        public void Dispose()
        {
            // Clean up test directory
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }
    }
}