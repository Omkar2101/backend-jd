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

        // FIXED: Issue #2 - Created ValidationResult class instead of returning tuple
        /// <summary>
        /// Result of job description text validation
        /// </summary>
        public class ValidationResult
        {
            public bool IsValid { get; set; }
            public string ErrorMessage { get; set; } = string.Empty;
        }

        /// <summary>
        /// Validates if the provided text is a valid job description
        /// </summary>
        private ValidationResult ValidateJobDescriptionText(string text)
        {
            var trimmedText = text.Trim();

            // Check minimum length
            if (trimmedText.Length < 50)
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"Job description text must be at least 50 characters long. Current length: {trimmedText.Length} characters"
                };
            }

            // Check for repetitive characters (more than 5 consecutive same characters)
            var repetitivePattern = new Regex(@"(.)\1{5,}");
            if (repetitivePattern.IsMatch(trimmedText))
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Text contains too many repetitive characters. Please provide a proper job description."
                };
            }

            // Check for excessive special characters
            var specialCharPattern = new Regex(@"[^\w\s.,!?;:()\-'""/]");
            var specialCharMatches = specialCharPattern.Matches(trimmedText);
            var specialCharRatio = (double)specialCharMatches.Count / trimmedText.Length;
            if (specialCharRatio > 0.3)
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Text contains too many special characters. Please provide a valid job description."
                };
            }

            // Check for meaningful words (at least 10 words with 3+ characters)
            var words = trimmedText.Split(new char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var meaningfulWords = words.Where(word =>
                Regex.Replace(word, @"[^\w]", "").Length >= 3
            ).ToList();

            if (meaningfulWords.Count < 10)
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Please provide a more detailed job description with proper words."
                };
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
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Text doesn't appear to be a job description. Please provide a valid job posting."
                };
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
                    return new ValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Text appears to be invalid. Please provide a proper job description."
                    };
                }
            }

            return new ValidationResult { IsValid = true, ErrorMessage = string.Empty };
        }

        /// <summary>
        /// Creates a standardized error response
        /// </summary>
        private object CreateErrorResponse(string message, string type = "error", int statusCode = 400) // FIXED: Changed default from 500 to 400
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
        // FIXED: Issue #3 - Added ProducesResponseType attributes
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)] // Success response from _jobService.AnalyzeFromFileAsync
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)] // Validation errors
        [ProducesResponseType(typeof(object), StatusCodes.Status422UnprocessableEntity)] // Service errors handled gracefully
        public async Task<IActionResult> UploadFile([FromForm] UploadRequest request)
        {
            // FIXED: Issue #1 - Removed unnecessary try-catch, handle errors properly with BadRequest
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

            // FIXED: Issue #4 - Handle service calls gracefully without throwing exceptions
            var serviceResult = await _jobService.AnalyzeFromFileAsync(request.File, request.UserEmail);

            // Assuming the service returns a result object with success/error information
            // If the service fails, return 422 instead of 500
            if (serviceResult == null)
            {
                _logger.LogWarning("Service returned null result for file: {FileName}", request.File.FileName);
                return UnprocessableEntity(CreateErrorResponse(
                    "Unable to process the file at this time. Please try again later.",
                    "processing_error",
                    422
                ));
            }

            return Ok(serviceResult);

        }

        /// <summary>
        /// Analyze text directly for bias
        /// </summary>
        [HttpPost("analyze")]
        // FIXED: Issue #3 - Added ProducesResponseType attributes
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)] // Success response from _jobService.AnalyzeTextAsync
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)] // Validation errors
        [ProducesResponseType(typeof(object), StatusCodes.Status422UnprocessableEntity)] // Service errors handled gracefully
        public async Task<IActionResult> AnalyzeText([FromBody] AnalyzeRequest request)
        {
            // FIXED: Issue #1 - Removed unnecessary try-catch, handle errors properly with BadRequest
            if (string.IsNullOrEmpty(request.Text))
                return BadRequest(CreateErrorResponse("Text is required", "validation_error", 400));

            if (string.IsNullOrEmpty(request.UserEmail))
                return BadRequest(CreateErrorResponse("User email is required", "validation_error", 400));

            // Enhanced text validation - FIXED: Issue #2 - Now using ValidationResult class instead of tuple
            var validation = ValidateJobDescriptionText(request.Text);
            if (!validation.IsValid)
            {
                return BadRequest(CreateErrorResponse(validation.ErrorMessage, "validation_error", 400));
            }

            // FIXED: Issue #4 - Handle service calls gracefully without throwing exceptions
            var serviceResult = await _jobService.AnalyzeTextAsync(request.Text, request.UserEmail, request.JobTitle);

            if (serviceResult == null)
            {
                _logger.LogWarning("Service returned null result for text analysis");
                return UnprocessableEntity(CreateErrorResponse(
                    "Unable to analyze the text at this time. Please try again later.",
                    "processing_error",
                    422
                ));
            }

            return Ok(serviceResult);


        }

        /// <summary>
        /// Get a specific job analysis by ID
        /// </summary>
        [HttpGet("{id}")]
        // FIXED: Issue #3 - Added ProducesResponseType attributes
        [ProducesResponseType(typeof(JobDescription), StatusCodes.Status200OK)] // Assuming JobDescription is the return type
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)] // Validation errors
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)] // Job not found
        [ProducesResponseType(typeof(object), StatusCodes.Status422UnprocessableEntity)] // Service errors
        public async Task<IActionResult> GetJob(string id)
        {
            // FIXED: Issue #1 - Removed unnecessary try-catch, handle errors properly with BadRequest
            if (string.IsNullOrEmpty(id))
                return BadRequest(CreateErrorResponse("Job ID is required", "validation_error", 400));

            // FIXED: Issue #4 - Handle service calls gracefully without throwing exceptions
            var job = await _jobService.GetJobAsync(id);
            if (job == null)
                return NotFound(CreateErrorResponse("Job not found", "not_found", 404));

            return Ok(job);

        }

        /// <summary>
        /// Get all job analyses with pagination
        /// </summary>
        [HttpGet]
        // FIXED: Issue #3 - Added ProducesResponseType attributes
        [ProducesResponseType(typeof(List<JobDescription>), StatusCodes.Status200OK)] // Assuming List<JobDescription> is the return type
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)] // Validation errors
        [ProducesResponseType(typeof(object), StatusCodes.Status422UnprocessableEntity)] // Service errors
        public async Task<IActionResult> GetAllJobs([FromQuery] int skip = 0, [FromQuery] int limit = 20)
        {
            // FIXED: Issue #1 - Removed unnecessary try-catch, handle errors properly with BadRequest
            // Validate pagination parameters
            if (skip < 0)
                return BadRequest(CreateErrorResponse("Skip parameter cannot be negative", "validation_error", 400));

            if (limit <= 0 || limit > 100)
                return BadRequest(CreateErrorResponse("Limit must be between 1 and 100", "validation_error", 400));

            // FIXED: Issue #4 - Handle service calls gracefully without throwing exceptions
            var jobs = await _jobService.GetAllJobsAsync(skip, limit);
            if (jobs == null)
            {
                _logger.LogWarning("Service returned null result for GetAllJobs");
                return UnprocessableEntity(CreateErrorResponse(
                    "Unable to retrieve jobs at this time. Please try again later.",
                    "processing_error",
                    422
                ));
            }

            return Ok(jobs);

        }

        /// <summary>
        /// Get job analyses by user email
        /// </summary>
        [HttpGet("user/{email}")]
        // FIXED: Issue #3 - Added ProducesResponseType attributes
        [ProducesResponseType(typeof(List<JobDescription>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)] // Validation errors
        [ProducesResponseType(typeof(object), StatusCodes.Status422UnprocessableEntity)] // Service errors
        public async Task<ActionResult<List<JobDescription>>> GetUserJobs(string email)
        {
            // FIXED: Issue #1 - Removed unnecessary try-catch, handle errors properly with BadRequest
            if (string.IsNullOrEmpty(email))
                return BadRequest(CreateErrorResponse("Email is required", "validation_error", 400));

            // Basic email validation
            if (!email.Contains("@") || !email.Contains("."))
                return BadRequest(CreateErrorResponse("Invalid email format", "validation_error", 400));

            // FIXED: Issue #4 - Handle service calls gracefully without throwing exceptions
            var jobs = await _jobService.GetByUserEmailAsync(email);
            if (jobs == null)
            {
                _logger.LogWarning("Service returned null result for user: {Email}", email);
                return UnprocessableEntity(CreateErrorResponse(
                    "Unable to retrieve user jobs at this time. Please try again later.",
                    "processing_error",
                    422
                ));
            }

            return Ok(jobs);


        }

        /// <summary>
        /// Delete a specific job description by ID
        /// </summary>
        [HttpDelete("{id}")]
        // FIXED: Issue #3 - Added ProducesResponseType attributes
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)] // Success deletion
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)] // Validation errors
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)] // Job not found
        [ProducesResponseType(typeof(object), StatusCodes.Status422UnprocessableEntity)] // Service errors
        public async Task<IActionResult> DeleteJob(string id)
        {
            // FIXED: Issue #1 - Removed unnecessary try-catch, handle errors properly with BadRequest
            if (string.IsNullOrEmpty(id))
                return BadRequest(CreateErrorResponse("Job ID is required", "validation_error", 400));

            // FIXED: Issue #4 - Handle service calls gracefully without throwing exceptions
            var deleted = await _jobService.DeleteJobAsync(id);

            // Check if the operation was successful
            if (!deleted.IsSuccess)
            {
                // Check if it's a "not found" error or other error
                if (deleted.ErrorMessage.Contains("not found", StringComparison.OrdinalIgnoreCase))
                    return NotFound(CreateErrorResponse(deleted.ErrorMessage, "not_found", 404));

                // For other errors, return BadRequest
                return BadRequest(CreateErrorResponse(deleted.ErrorMessage, "service_error", 400));
            }

            return Ok(new { message = "Job deleted successfully", id });
        }
    }
}