using Microsoft.AspNetCore.Mvc;
using backend_jd_api.Models;
using backend_jd_api.Services;

namespace backend_jd_api.Controllers
{
    [ApiController]
    [Route("api/jobs")]
    public class JobController : ControllerBase
    {
        private readonly JobService _jobService;
        private readonly ILogger<JobController> _logger;

        public JobController(JobService jobService, ILogger<JobController> logger)
        {
            _jobService = jobService;
            _logger = logger;
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
                    return BadRequest("No file uploaded");

                if (string.IsNullOrEmpty(request.UserEmail))
                    return BadRequest("User email is required");

                // Validate file type
                var allowedTypes = new[] { ".txt", ".doc", ".docx", ".pdf", ".jpg", ".jpeg", ".png" };
                var fileExtension = Path.GetExtension(request.File.FileName).ToLowerInvariant();
                if (!allowedTypes.Contains(fileExtension))
                    return BadRequest($"Invalid file type. Allowed types are: {string.Join(", ", allowedTypes)}");

                // // Check if it's an image file
                // var imageTypes = new[] { ".jpg", ".jpeg", ".png" };
                // bool isImage = imageTypes.Contains(fileExtension);

                var response = await _jobService.AnalyzeFromFileAsync(request.File, request.UserEmail);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing file upload");
                return StatusCode(500, "An error occurred while processing your file. Please try again.");
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
                    return BadRequest("Text is required");

                if (request.Text.Trim().Length < 50)
                    return BadRequest($"Job description text must be at least 50 characters long. Current length: {request.Text.Trim().Length} characters");

                if (string.IsNullOrEmpty(request.UserEmail))
                    return BadRequest("User email is required");

                var response = await _jobService.AnalyzeTextAsync(request.Text, request.UserEmail, request.JobTitle);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing text");
                return StatusCode(500, "An error occurred while analyzing the text. Please try again.");
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
                var job = await _jobService.GetJobAsync(id);
                if (job == null)
                    return NotFound();

                return Ok(job);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting job {Id}", id);
                return StatusCode(500, "Error retrieving job");
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
                var jobs = await _jobService.GetAllJobsAsync(skip, limit);
                return Ok(jobs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting jobs");
                return StatusCode(500, "Error retrieving jobs");
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
                var jobs = await _jobService.GetByUserEmailAsync(email);
                return Ok(jobs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving jobs for user {Email}", email);
                return StatusCode(500, "Internal server error");
            }
        }

        // /// <summary>
        // /// Health check endpoint
        // /// </summary>
        // [HttpGet("health")]
        // public IActionResult Health()
        // {
        //     return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
        // }
    }
}