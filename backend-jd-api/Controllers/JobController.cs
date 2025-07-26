// using Microsoft.AspNetCore.Mvc;
// using backend_jd_api.Models;
// using backend_jd_api.Services;
// using System.Text.RegularExpressions;

// namespace backend_jd_api.Controllers
// {
//     [ApiController]
//     [Route("api/jobs")]
//     public class JobController : ControllerBase
//     {
//         // private readonly JobService _jobService;
//         private readonly IJobService _jobService;
//         private readonly ILogger<JobController> _logger;

//         public JobController(IJobService jobService, ILogger<JobController> logger)
//         {
//             _jobService = jobService;
//             _logger = logger;
//         }

//         /// <summary>
//         /// Validates if the provided text is a valid job description
//         /// </summary>
//         private (bool isValid, string errorMessage) ValidateJobDescriptionText(string text)
//         {
//             var trimmedText = text.Trim();

//             // Check minimum length
//             if (trimmedText.Length < 50)
//             {
//                 return (false, $"Job description text must be at least 50 characters long. Current length: {trimmedText.Length} characters");
//             }

//             // Check for repetitive characters (more than 5 consecutive same characters)
//             var repetitivePattern = new Regex(@"(.)\1{5,}");
//             if (repetitivePattern.IsMatch(trimmedText))
//             {
//                 return (false, "Text contains too many repetitive characters. Please provide a proper job description.");
//             }

//             // Check for excessive special characters
//             var specialCharPattern = new Regex(@"[^\w\s.,!?;:()\-'""/]");
//             var specialCharMatches = specialCharPattern.Matches(trimmedText);
//             var specialCharRatio = (double)specialCharMatches.Count / trimmedText.Length;
//             if (specialCharRatio > 0.3)
//             {
//                 return (false, "Text contains too many special characters. Please provide a valid job description.");
//             }

//             // Check for meaningful words (at least 10 words with 3+ characters)
//             var words = trimmedText.Split(new char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
//             var meaningfulWords = words.Where(word =>
//                 Regex.Replace(word, @"[^\w]", "").Length >= 3
//             ).ToList();

//             if (meaningfulWords.Count < 10)
//             {
//                 return (false, "Please provide a more detailed job description with proper words.");
//             }

//             // Check for common job-related keywords
//             var jobKeywords = new[]
//             {
//                 "job", "position", "role", "responsibilities", "requirements", "experience",
//                 "skills", "qualifications", "candidate", "work", "team", "company",
//                 "duties", "tasks", "developer", "manager", "analyst", "engineer", "coordinator",
//                 "employment", "hiring", "recruit", "apply", "application", "resume", "cv",
//                 "salary", "benefits", "location", "remote", "office", "department"
//             };

//             var textLower = trimmedText.ToLower();
//             var hasJobKeywords = jobKeywords.Any(keyword => textLower.Contains(keyword));

//             if (!hasJobKeywords)
//             {
//                 return (false, "Text doesn't appear to be a job description. Please provide a valid job posting.");
//             }

//             // Check if text is mostly gibberish (low vowel-to-consonant ratio)
//             var vowels = "aeiouAEIOU";
//             var vowelCount = trimmedText.Count(c => vowels.Contains(c));
//             var consonantCount = trimmedText.Count(c => char.IsLetter(c) && !vowels.Contains(c));

//             if (consonantCount > 0)
//             {
//                 var vowelRatio = (double)vowelCount / consonantCount;
//                 if (vowelRatio < 0.2) // Very low vowel ratio indicates gibberish
//                 {
//                     return (false, "Text appears to be invalid. Please provide a proper job description.");
//                 }
//             }

//             return (true, string.Empty);
//         }


//         /// <summary>
//         /// Creates a standardized error response
//         /// </summary>
//         private object CreateErrorResponse(string message, string type = "error", int statusCode = 500)
//         {
//             return new
//             {
//                 error = true,
//                 message = message,
//                 type = type,
//                 status_code = statusCode,
//                 timestamp = DateTime.UtcNow
//             };
//         }

//         // /// <summary>
//         // /// Upload a file and analyze it for bias
//         // /// </summary>
//         // [HttpPost("upload")]
//         // public async Task<IActionResult> UploadFile([FromForm] UploadRequest request)
//         // {
//         //     try
//         //     {
//         //         if (request.File == null || request.File.Length == 0)
//         //             return BadRequest("No file uploaded");

//         //         if (string.IsNullOrEmpty(request.UserEmail))
//         //             return BadRequest("User email is required");

//         //         // Validate file type
//         //         var allowedTypes = new[] { ".txt", ".doc", ".docx", ".pdf", ".jpg", ".jpeg", ".png" };
//         //         var fileExtension = Path.GetExtension(request.File.FileName).ToLowerInvariant();
//         //         if (!allowedTypes.Contains(fileExtension))
//         //             return BadRequest($"Invalid file type. Allowed types are: {string.Join(", ", allowedTypes)}");

//         //         // // Check if it's an image file
//         //         // var imageTypes = new[] { ".jpg", ".jpeg", ".png" };
//         //         // bool isImage = imageTypes.Contains(fileExtension);

//         //         var response = await _jobService.AnalyzeFromFileAsync(request.File, request.UserEmail);

//         //         return Ok(response);
//         //     }
//         //     catch (Exception ex)
//         //     {
//         //         _logger.LogError(ex, "Error processing file upload");
//         //         return StatusCode(500, "An error occurred while processing your file. Please try again.");
//         //     }
//         // }

//         /// <summary>
//         /// Analyze text directly for bias
//         /// </summary>
//         /// 
        
//         // <summary>
//         /// Upload a file and analyze it for bias
//         /// </summary>
//         [HttpPost("upload")]
//         public async Task<IActionResult> UploadFile([FromForm] UploadRequest request)
//         {
//             try
//             {
//                 if (request.File == null || request.File.Length == 0)
//                     return BadRequest(CreateErrorResponse("No file uploaded", "validation_error", 400));

//                 if (string.IsNullOrEmpty(request.UserEmail))
//                     return BadRequest(CreateErrorResponse("User email is required", "validation_error", 400));

//                 // Validate file type
//                 var allowedTypes = new[] { ".txt", ".doc", ".docx", ".pdf", ".jpg", ".jpeg", ".png" };
//                 var fileExtension = Path.GetExtension(request.File.FileName).ToLowerInvariant();
//                 if (!allowedTypes.Contains(fileExtension))
//                     return BadRequest(CreateErrorResponse($"Invalid file type. Allowed types are: {string.Join(", ", allowedTypes)}", "validation_error", 400));

//                 // Validate file size (e.g., max 10MB)
//                 const long maxFileSize = 10 * 1024 * 1024; // 10MB
//                 if (request.File.Length > maxFileSize)
//                     return BadRequest(CreateErrorResponse("File size too large. Maximum allowed size is 10MB.", "validation_error", 400));

//                 var response = await _jobService.AnalyzeFromFileAsync(request.File, request.UserEmail);
//                 return Ok(response);
//             }
//             catch (HttpRequestException httpEx)
//             {
//                 _logger.LogError(httpEx, "HTTP error during file analysis for file: {FileName}", request?.File?.FileName);
                
//                 // Try to parse the error message from Python API
//                 if (httpEx.Message.Contains("Python API error"))
//                 {
//                     try
//                     {
//                         var errorStart = httpEx.Message.IndexOf("{");
//                         if (errorStart >= 0)
//                         {
//                             var jsonError = httpEx.Message.Substring(errorStart);
//                             var errorObj = JsonSerializer.Deserialize<JsonElement>(jsonError);
                            
//                             if (errorObj.TryGetProperty("message", out var messageElement))
//                             {
//                                 var errorMessage = messageElement.GetString();
//                                 return StatusCode(503, CreateErrorResponse(
//                                     "AI service is currently experiencing issues. Please try again in a few moments or rephrase your job description.",
//                                     "ai_service_error",
//                                     503
//                                 ));
//                             }
//                         }
//                     }
//                     catch (Exception parseEx)
//                     {
//                         _logger.LogWarning(parseEx, "Could not parse Python API error response");
//                     }
//                 }
                
//                 return StatusCode(503, CreateErrorResponse(
//                     "Our AI analysis service is temporarily unavailable. Please try again in a few moments.",
//                     "service_unavailable",
//                     503
//                 ));
//             }
//             catch (TaskCanceledException tcEx) when (tcEx.InnerException is TimeoutException)
//             {
//                 _logger.LogWarning(tcEx, "Timeout during file analysis for file: {FileName}", request?.File?.FileName);
//                 return StatusCode(504, CreateErrorResponse(
//                     "The analysis is taking longer than expected. Please try again with a shorter job description.",
//                     "timeout_error",
//                     504
//                 ));
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Unexpected error processing file upload: {FileName}", request?.File?.FileName);
//                 return StatusCode(500, CreateErrorResponse(
//                     "An unexpected error occurred while processing your file. Please try again.",
//                     "internal_server_error",
//                     500
//                 ));
//             }
//         }
//         [HttpPost("analyze")]
//         public async Task<IActionResult> AnalyzeText([FromBody] AnalyzeRequest request)
//         {
//             try
//             {
//                 if (string.IsNullOrEmpty(request.Text))
//                     return BadRequest("Text is required");



//                 if (string.IsNullOrEmpty(request.UserEmail))
//                     return BadRequest("User email is required");


//                 // Enhanced text validation
//                 var validation = ValidateJobDescriptionText(request.Text);
//                 if (!validation.isValid)
//                 {
//                     return BadRequest(validation.errorMessage);
//                 }

//                 var response = await _jobService.AnalyzeTextAsync(request.Text, request.UserEmail, request.JobTitle);
//                 return Ok(response);
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Error analyzing text");
//                 return StatusCode(500, "An error occurred while analyzing the text. Please try again.");
//             }
//         }

//         /// <summary>
//         /// Get a specific job analysis by ID
//         /// </summary>
//         [HttpGet("{id}")]
//         public async Task<IActionResult> GetJob(string id)
//         {
//             try
//             {
//                 var job = await _jobService.GetJobAsync(id);
//                 if (job == null)
//                     return NotFound();

//                 return Ok(job);
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Error getting job {Id}", id);
//                 return StatusCode(500, "Error retrieving job");
//             }
//         }

//         /// <summary>
//         /// Get all job analyses with pagination
//         /// </summary>
//         [HttpGet]
//         public async Task<IActionResult> GetAllJobs([FromQuery] int skip = 0, [FromQuery] int limit = 20)
//         {
//             try
//             {
//                 var jobs = await _jobService.GetAllJobsAsync(skip, limit);
//                 return Ok(jobs);
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Error getting jobs");
//                 return StatusCode(500, "Error retrieving jobs");
//             }
//         }

//         /// <summary>
//         /// Get job analyses by user email
//         /// </summary>
//         [HttpGet("user/{email}")]
//         public async Task<ActionResult<List<JobDescription>>> GetUserJobs(string email)
//         {
//             try
//             {
//                 var jobs = await _jobService.GetByUserEmailAsync(email);
//                 // if (jobs == null || !jobs.Any())
//                 // return NotFound("No jobs found for the provided email.");

//                 return Ok(jobs);
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Error retrieving jobs for user {Email}", email);
//                 return StatusCode(500, "Internal server error");
//             }
//         }


//         /// <summary>
//         /// Delete a specific job description by ID
//         /// </summary>
//         [HttpDelete("{id}")]
//         public async Task<IActionResult> DeleteJob(string id)
//         {
//             try
//             {
//                 if (string.IsNullOrEmpty(id))
//                     return BadRequest("Job ID is required");

//                 var deleted = await _jobService.DeleteJobAsync(id);

//                 if (!deleted)
//                     return NotFound($"Job with ID {id} not found");

//                 return Ok(new { message = "Job deleted successfully", id });
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Error deleting job {Id}", id);
//                 return StatusCode(500, "An error occurred while deleting the job. Please try again.");
//             }
//         }


//     }
// }

using Microsoft.AspNetCore.Mvc;
using backend_jd_api.Models;
using backend_jd_api.Services;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace backend_jd_api.Controllers
{
    [ApiController]
    [Route("api/jobs")]
    public class JobController : ControllerBase
    {
        private readonly IJobService _jobService;
        private readonly ILogger<JobController> _logger;

        public JobController(IJobService jobService, ILogger<JobController> logger)
        {
            _jobService = jobService;
            _logger = logger;
        }

        /// <summary>
        /// Validates if the provided text is a valid job description
        /// </summary>
        private (bool isValid, string errorMessage) ValidateJobDescriptionText(string text)
        {
            var trimmedText = text.Trim();

            // Check minimum length
            if (trimmedText.Length < 50)
            {
                return (false, $"Job description text must be at least 50 characters long. Current length: {trimmedText.Length} characters");
            }

            // Check for repetitive characters (more than 5 consecutive same characters)
            var repetitivePattern = new Regex(@"(.)\1{5,}");
            if (repetitivePattern.IsMatch(trimmedText))
            {
                return (false, "Text contains too many repetitive characters. Please provide a proper job description.");
            }

            // Check for excessive special characters
            var specialCharPattern = new Regex(@"[^\w\s.,!?;:()\-'""/]");
            var specialCharMatches = specialCharPattern.Matches(trimmedText);
            var specialCharRatio = (double)specialCharMatches.Count / trimmedText.Length;
            if (specialCharRatio > 0.3)
            {
                return (false, "Text contains too many special characters. Please provide a valid job description.");
            }

            // Check for meaningful words (at least 10 words with 3+ characters)
            var words = trimmedText.Split(new char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var meaningfulWords = words.Where(word =>
                Regex.Replace(word, @"[^\w]", "").Length >= 3
            ).ToList();

            if (meaningfulWords.Count < 10)
            {
                return (false, "Please provide a more detailed job description with proper words.");
            }

            // Check for common job-related keywords
            var jobKeywords = new[]
            {
                "job", "position", "role", "responsibilities", "requirements", "experience",
                "skills", "qualifications", "candidate", "work", "team", "company",
                "duties", "tasks", "developer", "manager", "analyst", "engineer", "coordinator",
                "employment", "hiring", "recruit", "apply", "application", "resume", "cv",
                "salary", "benefits", "location", "remote", "office", "department"
            };

            var textLower = trimmedText.ToLower();
            var hasJobKeywords = jobKeywords.Any(keyword => textLower.Contains(keyword));

            if (!hasJobKeywords)
            {
                return (false, "Text doesn't appear to be a job description. Please provide a valid job posting.");
            }

            // Check if text is mostly gibberish (low vowel-to-consonant ratio)
            var vowels = "aeiouAEIOU";
            var vowelCount = trimmedText.Count(c => vowels.Contains(c));
            var consonantCount = trimmedText.Count(c => char.IsLetter(c) && !vowels.Contains(c));

            if (consonantCount > 0)
            {
                var vowelRatio = (double)vowelCount / consonantCount;
                if (vowelRatio < 0.2) // Very low vowel ratio indicates gibberish
                {
                    return (false, "Text appears to be invalid. Please provide a proper job description.");
                }
            }

            return (true, string.Empty);
        }

        /// <summary>
        /// Creates a standardized error response
        /// </summary>
        private object CreateErrorResponse(string message, string type = "error", int statusCode = 500)
        {
            return new
            {
                error = true,
                message = message,
                type = type,
                status_code = statusCode,
                timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Upload a file and analyze it for bias
        /// </summary>
        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile([FromForm] UploadRequest request)
        {
            try
            {
                if (request.File == null || request.File.Length == 0)
                    return BadRequest(CreateErrorResponse("No file uploaded", "validation_error", 400));

                if (string.IsNullOrEmpty(request.UserEmail))
                    return BadRequest(CreateErrorResponse("User email is required", "validation_error", 400));

                // Validate file type
                var allowedTypes = new[] { ".txt", ".doc", ".docx", ".pdf", ".jpg", ".jpeg", ".png" };
                var fileExtension = Path.GetExtension(request.File.FileName).ToLowerInvariant();
                if (!allowedTypes.Contains(fileExtension))
                    return BadRequest(CreateErrorResponse($"Invalid file type. Allowed types are: {string.Join(", ", allowedTypes)}", "validation_error", 400));

                // Validate file size (e.g., max 10MB)
                const long maxFileSize = 10 * 1024 * 1024; // 10MB
                if (request.File.Length > maxFileSize)
                    return BadRequest(CreateErrorResponse("File size too large. Maximum allowed size is 10MB.", "validation_error", 400));

                var response = await _jobService.AnalyzeFromFileAsync(request.File, request.UserEmail);
                return Ok(response);
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP error during file analysis for file: {FileName}", request?.File?.FileName);
                
                // Try to parse the error message from Python API
                if (httpEx.Message.Contains("Python API error"))
                {
                    try
                    {
                        var errorStart = httpEx.Message.IndexOf("{");
                        if (errorStart >= 0)
                        {
                            var jsonError = httpEx.Message.Substring(errorStart);
                            var errorObj = JsonSerializer.Deserialize<JsonElement>(jsonError);
                            
                            if (errorObj.TryGetProperty("message", out var messageElement))
                            {
                                var errorMessage = messageElement.GetString();
                                return StatusCode(503, CreateErrorResponse(
                                    "AI service is currently experiencing issues. Please try again in a few moments or rephrase your job description.",
                                    "ai_service_error",
                                    503
                                ));
                            }
                        }
                    }
                    catch (Exception parseEx)
                    {
                        _logger.LogWarning(parseEx, "Could not parse Python API error response");
                    }
                }
                
                return StatusCode(503, CreateErrorResponse(
                    "Our AI analysis service is temporarily unavailable. Please try again in a few moments.",
                    "service_unavailable",
                    503
                ));
            }
            catch (TaskCanceledException tcEx) when (tcEx.InnerException is TimeoutException)
            {
                _logger.LogWarning(tcEx, "Timeout during file analysis for file: {FileName}", request?.File?.FileName);
                return StatusCode(504, CreateErrorResponse(
                    "The analysis is taking longer than expected. Please try again with a shorter job description.",
                    "timeout_error",
                    504
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error processing file upload: {FileName}", request?.File?.FileName);
                return StatusCode(500, CreateErrorResponse(
                    "An unexpected error occurred while processing your file. Please try again.",
                    "internal_server_error",
                    500
                ));
            }
        }

        /// <summary>
        /// Analyze text directly for bias
        /// </summary>
        [HttpPost("analyze")]
        public async Task<IActionResult> AnalyzeText([FromBody] AnalyzeRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Text))
                    return BadRequest(CreateErrorResponse("Text is required", "validation_error", 400));

                if (string.IsNullOrEmpty(request.UserEmail))
                    return BadRequest(CreateErrorResponse("User email is required", "validation_error", 400));

                // Enhanced text validation
                var validation = ValidateJobDescriptionText(request.Text);
                if (!validation.isValid)
                {
                    return BadRequest(CreateErrorResponse(validation.errorMessage, "validation_error", 400));
                }

                var response = await _jobService.AnalyzeTextAsync(request.Text, request.UserEmail, request.JobTitle);
                return Ok(response);
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP error during text analysis");
                
                // Try to parse the error message from Python API
                if (httpEx.Message.Contains("Python API error"))
                {
                    try
                    {
                        var errorStart = httpEx.Message.IndexOf("{");
                        if (errorStart >= 0)
                        {
                            var jsonError = httpEx.Message.Substring(errorStart);
                            var errorObj = JsonSerializer.Deserialize<JsonElement>(jsonError);
                            
                            if (errorObj.TryGetProperty("message", out var messageElement))
                            {
                                return StatusCode(503, CreateErrorResponse(
                                    "AI service is currently experiencing issues. Please try again in a few moments or rephrase your job description.",
                                    "ai_service_error",
                                    503
                                ));
                            }
                        }
                    }
                    catch (Exception parseEx)
                    {
                        _logger.LogWarning(parseEx, "Could not parse Python API error response");
                    }
                }
                
                return StatusCode(503, CreateErrorResponse(
                    "Our AI analysis service is temporarily unavailable. Please try again in a few moments.",
                    "service_unavailable",
                    503
                ));
            }
            catch (TaskCanceledException tcEx) when (tcEx.InnerException is TimeoutException)
            {
                _logger.LogWarning(tcEx, "Timeout during text analysis");
                return StatusCode(504, CreateErrorResponse(
                    "The analysis is taking longer than expected. Please try again with a shorter job description.",
                    "timeout_error",
                    504
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error analyzing text");
                return StatusCode(500, CreateErrorResponse(
                    "An unexpected error occurred while analyzing the text. Please try again.",
                    "internal_server_error",
                    500
                ));
            }
        }

        /// <summary>
        /// Get a specific job analysis by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetJob(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                    return BadRequest(CreateErrorResponse("Job ID is required", "validation_error", 400));

                var job = await _jobService.GetJobAsync(id);
                if (job == null)
                    return NotFound(CreateErrorResponse("Job not found", "not_found", 404));

                return Ok(job);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting job {Id}", id);
                return StatusCode(500, CreateErrorResponse("Error retrieving job", "internal_server_error", 500));
            }
        }

        /// <summary>
        /// Get all job analyses with pagination
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllJobs([FromQuery] int skip = 0, [FromQuery] int limit = 20)
        {
            try
            {
                // Validate pagination parameters
                if (skip < 0)
                    return BadRequest(CreateErrorResponse("Skip parameter cannot be negative", "validation_error", 400));
                
                if (limit <= 0 || limit > 100)
                    return BadRequest(CreateErrorResponse("Limit must be between 1 and 100", "validation_error", 400));

                var jobs = await _jobService.GetAllJobsAsync(skip, limit);
                return Ok(jobs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting jobs");
                return StatusCode(500, CreateErrorResponse("Error retrieving jobs", "internal_server_error", 500));
            }
        }

        /// <summary>
        /// Get job analyses by user email
        /// </summary>
        [HttpGet("user/{email}")]
        public async Task<ActionResult<List<JobDescription>>> GetUserJobs(string email)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                    return BadRequest(CreateErrorResponse("Email is required", "validation_error", 400));

                // Basic email validation
                if (!email.Contains("@") || !email.Contains("."))
                    return BadRequest(CreateErrorResponse("Invalid email format", "validation_error", 400));

                var jobs = await _jobService.GetByUserEmailAsync(email);
                return Ok(jobs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving jobs for user {Email}", email);
                return StatusCode(500, CreateErrorResponse("Internal server error", "internal_server_error", 500));
            }
        }

        /// <summary>
        /// Delete a specific job description by ID
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteJob(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                    return BadRequest(CreateErrorResponse("Job ID is required", "validation_error", 400));

                var deleted = await _jobService.DeleteJobAsync(id);

                if (!deleted)
                    return NotFound(CreateErrorResponse($"Job with ID {id} not found", "not_found", 404));

                return Ok(new { message = "Job deleted successfully", id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting job {Id}", id);
                return StatusCode(500, CreateErrorResponse(
                    "An error occurred while deleting the job. Please try again.",
                    "internal_server_error",
                    500
                ));
            }
        }
    }
}