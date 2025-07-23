// Tests/Services/FileStorageServiceTests.cs
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using System.Text;

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
            var (storedFileName, filePath) = await _service.SaveFileAsync(mockFile.Object, userEmail);

            // Assert
            Assert.NotEmpty(storedFileName);
            Assert.True(storedFileName.EndsWith(".txt"));
            Assert.True(File.Exists(filePath));
            
            var savedContent = await File.ReadAllTextAsync(filePath);
            Assert.Equal(fileContent, savedContent);
        }

        [Fact]
        public async Task SaveFileAsync_CreatesUserDirectory()
        {
            // Arrange
            var userEmail = "newuser@example.com";
            var mockFile = CreateMockFile("test.txt", "content");

            // Act
            await _service.SaveFileAsync(mockFile.Object, userEmail);

            // Assert
            var expectedUserDir = Path.Combine(_tempDirectory, "uploads", "newuser_example_com");
            Assert.True(Directory.Exists(expectedUserDir));
        }

        [Fact]
        public async Task GetFileAsync_WithExistingFile_ReturnsFileData()
        {
            // Arrange
            var userEmail = "test@example.com";
            var fileContent = "Test file content";
            var mockFile = CreateMockFile("document.pdf", fileContent);
            
            var (storedFileName, _) = await _service.SaveFileAsync(mockFile.Object, userEmail);

            // Act
            var (fileData, contentType, fileName) = await _service.GetFileAsync(storedFileName);

            // Assert
            Assert.Equal(Encoding.UTF8.GetBytes(fileContent), fileData);
            Assert.Equal("application/pdf", contentType);
            Assert.Equal(storedFileName, fileName);
        }

        [Fact]
        public async Task GetFileAsync_WithNonExistentFile_ThrowsFileNotFoundException()
        {
            // Arrange
            var nonExistentFileName = "non-existent-file.pdf";

            // Act & Assert
            await Assert.ThrowsAsync<FileNotFoundException>(
                () => _service.GetFileAsync(nonExistentFileName)
            );
        }

        [Fact]
        public async Task DeleteFileAsync_WithExistingFile_DeletesSuccessfully()
        {
            // Arrange
            var userEmail = "test@example.com";
            var mockFile = CreateMockFile("to-delete.txt", "content");
            var (storedFileName, filePath) = await _service.SaveFileAsync(mockFile.Object, userEmail);

            // Verify file exists before deletion
            Assert.True(File.Exists(filePath));

            // Act
            var result = await _service.DeleteFileAsync(storedFileName);

            // Assert
            Assert.True(result);
            Assert.False(File.Exists(filePath));
        }

        [Fact]
        public async Task DeleteFileAsync_WithNonExistentFile_ReturnsFalse()
        {
            // Arrange
            var nonExistentFileName = "non-existent-file.txt";

            // Act
            var result = await _service.DeleteFileAsync(nonExistentFileName);

            // Assert
            Assert.False(result);
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
            var (storedFileName, _) = await _service.SaveFileAsync(mockFile.Object, userEmail);

            // Act
            var (_, contentType, _) = await _service.GetFileAsync(storedFileName);

            // Assert
            Assert.Equal(expectedContentType, contentType);
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
