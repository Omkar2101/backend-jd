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
//         /// Download/view a file by its stored filename
//         /// </summary>
//         [HttpGet("{storedFileName}")]
//         public async Task<IActionResult> GetFile(string storedFileName)
//         {
//             try
//             {
//                 var (fileData, contentType, fileName) = await _fileStorageService.GetFileAsync(storedFileName);
                
//                 // For images, return inline for viewing
//                 if (contentType.StartsWith("image/"))
//                 {
//                     return File(fileData, contentType);
//                 }
                
//                 // For other files, suggest download
//                 return File(fileData, contentType, fileName);
//             }
//             catch (FileNotFoundException)
//             {
//                 return NotFound("File not found");
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Error serving file {StoredFileName}", storedFileName);
//                 return StatusCode(500, "Error retrieving file");
//             }
//         }

//         /// <summary>
//         /// View file inline (useful for images and PDFs)
//         /// </summary>
//         [HttpGet("{storedFileName}/view")]
//         public async Task<IActionResult> ViewFile(string storedFileName)
//         {
//             try
//             {
//                 var (fileData, contentType, fileName) = await _fileStorageService.GetFileAsync(storedFileName);
                
//                 // Enhanced headers for better compatibility
//                 Response.Headers.Clear();
//                 Response.Headers.Add("Access-Control-Allow-Origin", "*");
//                 Response.Headers.Add("Access-Control-Allow-Methods", "GET, OPTIONS");
//                 Response.Headers.Add("Access-Control-Allow-Headers", "*");
//                 Response.Headers.Add("Access-Control-Expose-Headers", "Content-Disposition, Content-Type, Content-Length");
                
//                 // Specific headers for Word documents
//                 if (contentType.Contains("word") || contentType.Contains("officedocument"))
//                 {
//                     Response.Headers.Add("Content-Disposition", $"inline; filename=\"{fileName}\"");
//                     Response.Headers.Add("X-Frame-Options", "ALLOWALL");
//                     Response.Headers.Add("X-Content-Type-Options", "nosniff");
//                 }
//                 else
//                 {
//                     Response.Headers.Add("Content-Disposition", $"inline; filename=\"{fileName}\"");
//                 }
                
//                 return File(fileData, contentType, fileName);
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
// }



        
//     }
// }

// Controllers/FileController.cs
using Microsoft.AspNetCore.Mvc;
using backend_jd_api.Services;

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
        /// Download a file by its stored filename
        /// </summary>
        [HttpGet("{storedFileName}")]
        public async Task<IActionResult> DownloadFile(string storedFileName)
        {
            try
            {
                var (fileData, contentType, fileName) = await _fileStorageService.GetFileAsync(storedFileName);
                
                // Force download for all files
                return File(fileData, contentType, fileName);
            }
            catch (FileNotFoundException)
            {
                return NotFound("File not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file {StoredFileName}", storedFileName);
                return StatusCode(500, "Error retrieving file");
            }
        }

        /// <summary>
        /// View file inline (for display in browsers/viewers)
        /// </summary>
        [HttpGet("{storedFileName}/view")]
        public async Task<IActionResult> ViewFile(string storedFileName)
        {
            try
            {
                var (fileData, contentType, fileName) = await _fileStorageService.GetFileAsync(storedFileName);
                
                // Set headers for inline viewing
                Response.Headers.Add("Content-Disposition", "inline");
                Response.Headers.Add("X-Frame-Options", "SAMEORIGIN");
                Response.Headers.Add("Cache-Control", "public, max-age=3600");
                
                return File(fileData, contentType);
            }
            catch (FileNotFoundException)
            {
                return NotFound("File not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error viewing file {StoredFileName}", storedFileName);
                return StatusCode(500, "Error retrieving file");
            }
        }
    }
}
