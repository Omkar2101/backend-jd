

// // Controllers/FileController.cs
// using Microsoft.AspNetCore.Mvc;
// using backend_jd_api.Services;

// namespace backend_jd_api.Controllers
// {
//     [ApiController]
//     [Route("api/files")]
//     public class FileController : ControllerBase
//     {
//         private readonly IFileStorageService _fileStorageService;
//         private readonly ILogger<FileController> _logger;

//         public FileController(IFileStorageService fileStorageService, ILogger<FileController> logger)
//         {
//             _fileStorageService = fileStorageService;
//             _logger = logger;
//         }

//         /// <summary>
//         /// Download a file by its stored filename
//         /// </summary>
//         [HttpGet("{storedFileName}")]
//         public async Task<IActionResult> DownloadFile(string storedFileName)
//         {
//             try
//             {
//                 var (fileData, contentType, fileName) = await _fileStorageService.GetFileAsync(storedFileName);
                
//                 // Force download for all files
//                 return File(fileData, contentType, fileName);
//             }
//             catch (FileNotFoundException)
//             {
//                 return NotFound("File not found");
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Error downloading file {StoredFileName}", storedFileName);
//                 return StatusCode(500, "Error retrieving file");
//             }
//         }

//         /// <summary>
//         /// View file inline (for display in browsers/viewers)
//         /// </summary>
//         [HttpGet("{storedFileName}/view")]
//         public async Task<IActionResult> ViewFile(string storedFileName)
//         {
//             try
//             {
//                 var (fileData, contentType, fileName) = await _fileStorageService.GetFileAsync(storedFileName);
                
//                 // Set headers for inline viewing
//                 Response.Headers.Add("Content-Disposition", "inline");
//                 Response.Headers.Add("X-Frame-Options", "SAMEORIGIN");
//                 Response.Headers.Add("Cache-Control", "public, max-age=3600");
                
//                 return File(fileData, contentType);
//             }
//             catch (FileNotFoundException)
//             {
//                 return NotFound("File not found");
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Error viewing file {StoredFileName}", storedFileName);
//                 return StatusCode(500, "Error retrieving file");
//             }
//         }
//     }
// }

// Controllers/FileController.cs
using Microsoft.AspNetCore.Mvc;
using backend_jd_api.Services;
using backend_jd_api.Models;

namespace backend_jd_api.Controllers
{
    [ApiController]
    [Route("api/files")]
    public class FileController : ControllerBase
    {
        private readonly IFileStorageService _fileStorageService;
        private readonly ILogger<FileController> _logger;

        public FileController(IFileStorageService fileStorageService, ILogger<FileController> logger)
        {
            _fileStorageService = fileStorageService;
            _logger = logger;
        }

        /// <summary>
        /// Creates a standardized error response
        /// </summary>
        private ErrorResponse CreateErrorResponse(string message, string type = "error", int statusCode = 400)
        {
            return new ErrorResponse
            {
                error = true,
                message = message,
                type = type,
                status_code = statusCode,
                timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Download a file by its stored filename
        /// </summary>
        [HttpGet("{storedFileName}")]
        // FIXED: Issue #3 - Added ProducesResponseType attributes
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)] // File download success
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)] // Validation errors
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)] // File not found
        [ProducesResponseType(typeof(object), StatusCodes.Status422UnprocessableEntity)] // Service errors
        public async Task<IActionResult> DownloadFile(string storedFileName)
        {
            // FIXED: Issue #1 - Removed try-catch, handle errors properly with BadRequest
            // Input validation
            if (string.IsNullOrWhiteSpace(storedFileName))
            {
                return BadRequest(CreateErrorResponse("Stored filename is required", "validation_error", 400));
            }

            // FIXED: Issue #4 - No more 500 errors, handle service calls gracefully
            var result = await _fileStorageService.GetFileAsync(storedFileName);
            
            if (!result.IsSuccess)
            {
                // Handle different types of errors appropriately
                if (result.ErrorMessage.Contains("not found", StringComparison.OrdinalIgnoreCase))
                {
                    return NotFound(CreateErrorResponse(result.ErrorMessage, "file_not_found", 404));
                }
                
                if (result.ErrorMessage.Contains("access denied", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Access denied for file: {StoredFileName}", storedFileName);
                    return UnprocessableEntity(CreateErrorResponse("Access to file is currently unavailable", "access_error", 422));
                }
                
                // Generic service error
                _logger.LogWarning("File service error for {StoredFileName}: {Error}", storedFileName, result.ErrorMessage);
                return UnprocessableEntity(CreateErrorResponse("Unable to retrieve file at this time", "service_error", 422));
            }

            // Force download for all files
            return File(result.FileData, result.ContentType, result.FileName);

        }

        /// <summary>
        /// View file inline (for display in browsers/viewers)
        /// </summary>
        [HttpGet("{storedFileName}/view")]
        // FIXED: Issue #3 - Added ProducesResponseType attributes
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)] // File view success
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)] // Validation errors
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)] // File not found
        [ProducesResponseType(typeof(object), StatusCodes.Status422UnprocessableEntity)] // Service errors
        public async Task<IActionResult> ViewFile(string storedFileName)
        {
            // FIXED: Issue #1 - Removed try-catch, handle errors properly with BadRequest
            // Input validation
            if (string.IsNullOrWhiteSpace(storedFileName))
            {
                return BadRequest(CreateErrorResponse("Stored filename is required", "validation_error", 400));
            }

            // FIXED: Issue #4 - No more 500 errors, handle service calls gracefully
            var result = await _fileStorageService.GetFileAsync(storedFileName);
            
            if (!result.IsSuccess)
            {
                // Handle different types of errors appropriately
                if (result.ErrorMessage.Contains("not found", StringComparison.OrdinalIgnoreCase))
                {
                    return NotFound(CreateErrorResponse(result.ErrorMessage, "file_not_found", 404));
                }
                
                if (result.ErrorMessage.Contains("access denied", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Access denied for file view: {StoredFileName}", storedFileName);
                    return UnprocessableEntity(CreateErrorResponse("Access to file is currently unavailable", "access_error", 422));
                }
                
                // Generic service error
                _logger.LogWarning("File service error for view {StoredFileName}: {Error}", storedFileName, result.ErrorMessage);
                return UnprocessableEntity(CreateErrorResponse("Unable to view file at this time", "service_error", 422));
            }

            // Validate that the file type is viewable inline
            var viewableTypes = new[] { "image/jpeg", "image/png", "application/pdf", "text/plain" };
            if (!viewableTypes.Contains(result.ContentType))
            {
                return BadRequest(CreateErrorResponse(
                    $"File type {result.ContentType} is not supported for inline viewing. Please download the file instead.",
                    "unsupported_type",
                    400));
            }

            // Set headers for inline viewing
            Response.Headers.Add("Content-Disposition", "inline");
            Response.Headers.Add("X-Frame-Options", "SAMEORIGIN");
            Response.Headers.Add("Cache-Control", "public, max-age=3600");
            
            return File(result.FileData, result.ContentType);

           
        }

        

       
    }
}
