// Tests/Controllers/FileControllerTests.cs
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using backend_jd_api.Controllers;
using backend_jd_api.Services;

namespace backend_jd_api.Tests.Controllers
{
    public class FileControllerTests
    {
        private readonly Mock<IFileStorageService> _mockFileService;
        private readonly Mock<ILogger<FileController>> _mockLogger;
        private readonly FileController _controller;

        public FileControllerTests()
        {
            _mockFileService = new Mock<IFileStorageService>();
            _mockLogger = new Mock<ILogger<FileController>>();
            _controller = new FileController(_mockFileService.Object, _mockLogger.Object);
            
            // Initialize the controller's HTTP context for header tests
            SetupControllerContext();
        }

        private void SetupControllerContext()
        {
            var httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = httpContext
            };
        }

        [Fact]
        public async Task DownloadFile_WithValidFile_ReturnsFileResult()
        {
            // Arrange
            var fileName = "test-file.pdf";
            var fileData = new byte[] { 1, 2, 3, 4, 5 };
            var contentType = "application/pdf";
            var originalFileName = "original-file.pdf";

            _mockFileService
                .Setup(x => x.GetFileAsync(fileName))
                .ReturnsAsync(new FileRetrievalResult { 
                    IsSuccess = true,
                    FileData = fileData,
                    ContentType = contentType,
                    FileName = originalFileName
                });

            // Act
            var result = await _controller.DownloadFile(fileName);

            // Assert
            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal(fileData, fileResult.FileContents);
            Assert.Equal(contentType, fileResult.ContentType);
            Assert.Equal(originalFileName, fileResult.FileDownloadName);
        }

        [Fact]
        public async Task DownloadFile_WithNonExistentFile_ReturnsNotFound()
        {
            // Arrange
            var fileName = "non-existent-file.pdf";
            
            _mockFileService
                .Setup(x => x.GetFileAsync(fileName))
                .ReturnsAsync(new FileRetrievalResult { 
                    IsSuccess = false,
                    ErrorMessage = "File not found"
                });

            // Act
            var result = await _controller.DownloadFile(fileName);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var errorResponse = Assert.IsAssignableFrom<object>(notFoundResult.Value);
            Assert.Equal(404, Assert.IsAssignableFrom<dynamic>(errorResponse).status_code);
        }

        [Fact]
        public async Task DownloadFile_WithServiceError_ReturnsInternalServerError()
        {
            // Arrange
            var fileName = "error-file.pdf";
            
            _mockFileService
                .Setup(x => x.GetFileAsync(fileName))
                .ReturnsAsync(new FileRetrievalResult { 
                    IsSuccess = false,
                    ErrorMessage = "Service error occurred"
                });

            // Act
            var result = await _controller.DownloadFile(fileName);

            // Assert
            var errorResult = Assert.IsType<UnprocessableEntityObjectResult>(result);
            var errorResponse = Assert.IsAssignableFrom<object>(errorResult.Value);
            Assert.Equal(422, Assert.IsAssignableFrom<dynamic>(errorResponse).status_code);
        }

        [Fact]
        public async Task ViewFile_WithValidFile_ReturnsFileForInlineViewing()
        {
            // Arrange
            var fileName = "test-document.pdf";
            var fileData = new byte[] { 1, 2, 3, 4, 5 };
            var contentType = "application/pdf";
            var originalFileName = "document.pdf";

            _mockFileService
                .Setup(x => x.GetFileAsync(fileName))
                .ReturnsAsync(new FileRetrievalResult { 
                    IsSuccess = true,
                    FileData = fileData,
                    ContentType = contentType,
                    FileName = originalFileName
                });

            // Act
            var result = await _controller.ViewFile(fileName);

            // Assert
            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal(fileData, fileResult.FileContents);
            Assert.Equal(contentType, fileResult.ContentType);
            
        }

        [Fact]
        public async Task ViewFile_SetsCorrectHeaders()
        {
            // Arrange
            var fileName = "test-file.pdf";
            var fileData = new byte[] { 1, 2, 3 };
            var contentType = "application/pdf";
            var originalFileName = "test.pdf";

            _mockFileService
                .Setup(x => x.GetFileAsync(fileName))
                .ReturnsAsync(new FileRetrievalResult { 
                    IsSuccess = true,
                    FileData = fileData,
                    ContentType = contentType,
                    FileName = originalFileName
                });

            // Act
            await _controller.ViewFile(fileName);

            // Assert
            // Check that headers were set correctly
            Assert.True(_controller.Response.Headers.ContainsKey("Content-Disposition"));
            Assert.Equal("inline", _controller.Response.Headers["Content-Disposition"].ToString());
            
            Assert.True(_controller.Response.Headers.ContainsKey("X-Frame-Options"));
            Assert.Equal("SAMEORIGIN", _controller.Response.Headers["X-Frame-Options"].ToString());
            
            Assert.True(_controller.Response.Headers.ContainsKey("Cache-Control"));
            Assert.Equal("public, max-age=3600", _controller.Response.Headers["Cache-Control"].ToString());
        }

        [Fact]
        public async Task ViewFile_WithNonExistentFile_ReturnsNotFound()
        {
            // Arrange
            var fileName = "missing-file.pdf";
            
            _mockFileService
                .Setup(x => x.GetFileAsync(fileName))
                .ReturnsAsync(new FileRetrievalResult { 
                    IsSuccess = false,
                    ErrorMessage = "File not found"
                });

            // Act
            var result = await _controller.ViewFile(fileName);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var errorResponse = Assert.IsAssignableFrom<object>(notFoundResult.Value);
            Assert.Equal(404, Assert.IsAssignableFrom<dynamic>(errorResponse).status_code);
        }

        [Fact]
        public async Task ViewFile_WithServiceError_ReturnsInternalServerError()
        {
            // Arrange
            var fileName = "error-file.pdf";
            
            _mockFileService
                .Setup(x => x.GetFileAsync(fileName))
                .ReturnsAsync(new FileRetrievalResult { 
                    IsSuccess = false,
                    ErrorMessage = "Service error occurred"
                });

            // Act
            var result = await _controller.ViewFile(fileName);

            // Assert
            var errorResult = Assert.IsType<UnprocessableEntityObjectResult>(result);
            var errorResponse = Assert.IsAssignableFrom<object>(errorResult.Value);
            Assert.Equal(422, Assert.IsAssignableFrom<dynamic>(errorResponse).status_code);
        }
    }
}