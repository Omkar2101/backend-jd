using backend_jd_api.Data;
using backend_jd_api.Models;
using MongoDB.Driver;

namespace backend_jd_api.Services

{
    
    // Create IJobService interface
    public interface IJobService
    {
        Task<List<JobDescription>> GetByUserEmailAsync(string email);
        Task<JobResponse> AnalyzeFromFileAsync(IFormFile file, string userEmail);
        Task<JobResponse> AnalyzeTextAsync(string text, string userEmail, string? jobTitle = null);
        Task<JobResponse?> GetJobAsync(string id);
        Task<List<JobResponse>> GetAllJobsAsync(int skip = 0, int limit = 20);
        Task<bool> DeleteJobAsync(string id);
    }
    /// <summary>
    /// Service for handling job description analysis, storage, and retrieval operations
    /// </summary>
    public class JobService: IJobService
    {
        private readonly MongoDbContext _db;
        private readonly PythonService _pythonService;
        private readonly ILogger<JobService> _logger;

        /// <summary>
        /// Initializes a new instance of the JobService
        /// </summary>
        /// <param name="db">MongoDB context for data persistence</param>
        /// <param name="pythonService">Service for text analysis and extraction</param>
        /// <param name="logger">Logger for error tracking and monitoring</param>
        public JobService(MongoDbContext db, PythonService pythonService, ILogger<JobService> logger)
        {
            _db = db;
            _pythonService = pythonService;
            _logger = logger;
        }

        /// <summary>
        /// Deletes a job description by its ID
        /// </summary>
        /// <param name="id">The unique identifier of the job description</param>
        /// <returns>True if deleted, false if not found</returns>
        public async Task<bool> DeleteJobAsync(string id)
        {
            try
            {
                return await _db.DeleteJobAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting job {Id}", id);
                throw;
            }
        }

     

        /// <summary>
        /// Analyzes a job description from an uploaded file
        /// </summary>
        /// <param name="file">The uploaded file containing the job description</param>
        /// <param name="userEmail">The email of the user uploading the file</param>
        /// <param name="isImage">Whether the file is an image that needs OCR processing</param>
        /// <returns>Analysis results including bias detection and improvements</returns>
        /// <exception cref="Exception">Thrown when file processing or analysis fails</exception>
        public async Task<JobResponse> AnalyzeFromFileAsync(IFormFile file, string userEmail)
        {
            try
            {
                // Read file
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                var fileContent = stream.ToArray();

                // Extract text yahan pe errror aa rha
                var text = await _pythonService.ExtractTextFromFileAsync(fileContent, file.FileName);
                // Validate extracted text
                if (string.IsNullOrWhiteSpace(text))
                {
                    _logger.LogWarning("No text extracted from file {FileName}", file.FileName);
                    throw new Exception($"No text could be extracted from the file {file.FileName}");
                }

                // Check minimum length requirement (Python API requires 50+ characters)
                if (text.Trim().Length < 50)
                {
                    _logger.LogWarning("Extracted text from {FileName} is too short: {Length} characters",
                        file.FileName, text.Trim().Length);
                    throw new Exception($"The extracted text is too short ({text.Trim().Length} characters). Job descriptions must be at least 50 characters long.");
                }

                _logger.LogInformation("Extracted text from {FileName}. Length: {Length} characters",
                    file.FileName, text.Length);

                // Log first 200 characters for debugging (be careful with sensitive data)
                _logger.LogDebug("Extracted text preview: {TextPreview}",
                    text.Length > 200 ? text.Substring(0, 200) + "..." : text);

                // Analyze text
                var analysis = await _pythonService.AnalyzeTextAsync(text);
                _logger.LogInformation("Analysis completed. ImprovedText length: {Length}, Suggestions count: {Count}",
                    analysis.ImprovedText?.Length ?? 0, analysis.suggestions?.Count ?? 0);

                // Create and save job description record
                var jd = new JobDescription
                {
                    UserEmail = userEmail,
                    OriginalText = text,
                    ImprovedText = analysis.ImprovedText ?? string.Empty,
                    // OverallAssessment = analysis.overall_assessment, // Add overall assessment
                    FileName = file.FileName,
                    CreatedAt = DateTime.UtcNow,
                    Analysis = analysis
                };

                var savedJob = await _db.CreateJobAsync(jd);

                return new JobResponse
                {
                    Id = savedJob.Id,
                    UserEmail = savedJob.UserEmail,
                    OriginalText = savedJob.OriginalText,
                    ImprovedText = savedJob.ImprovedText,
                    // OverallAssessment = savedJob.OverallAssessment, // Add this line
                    FileName = savedJob.FileName,
                    CreatedAt = savedJob.CreatedAt,
                    Analysis = savedJob.Analysis
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing file {FileName}", file.FileName);
                throw;
            }
        }

        /// <summary>
        /// Analyzes job description text directly
        /// </summary>
        /// <param name="text">The job description text to analyze</param>
        /// <param name="userEmail">The email of the user submitting the text</param>
        /// <param name="jobTitle">Optional title for the job description</param>
        /// <returns>Analysis results including bias detection and improvements</returns>
        /// <exception cref="Exception">Thrown when text analysis fails</exception>
        public async Task<JobResponse> AnalyzeTextAsync(string text, string userEmail, string? jobTitle = null)
        {
            try
            {

                // Validate input text
                if (string.IsNullOrWhiteSpace(text))
                {
                    throw new ArgumentException("Text cannot be empty or whitespace", nameof(text));
                }


                // Check minimum length requirement (Python API requires 50+ characters)
                if (text.Trim().Length < 50)
                {
                    throw new ArgumentException($"Job description text must be at least 50 characters long. Current length: {text.Trim().Length} characters");
                }

                _logger.LogInformation("Analyzing text with length: {Length} characters", text.Length);


                // Analyze text
                var analysis = await _pythonService.AnalyzeTextAsync(text);
                _logger.LogInformation("Text analysis completed successfully{analysis}", analysis);

                // Create job record
                var job = new JobDescription
                {
                    OriginalText = text,
                    ImprovedText = analysis.ImprovedText,
                    // OverallAssessment = analysis.overall_assessment, // Add overall assessment
                    FileName = jobTitle ?? "Direct Input",
                    UserEmail = userEmail,  // Add the email
                    Analysis = analysis,
                    CreatedAt = DateTime.UtcNow
                };

                // Save to database
                var savedJob = await _db.CreateJobAsync(job);

                return MapToResponse(savedJob);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing text");
                throw;
            }
        }

        /// <summary>
        /// Retrieves a specific job description by its ID
        /// </summary>
        /// <param name="id">The unique identifier of the job description</param>
        /// <returns>The job description if found, null otherwise</returns>
        public async Task<JobResponse?> GetJobAsync(string id)
        {
            var job = await _db.GetJobAsync(id);
            return job != null ? MapToResponse(job) : null;
        }

        /// <summary>
        /// Retrieves a paginated list of all job descriptions
        /// </summary>
        /// <param name="skip">Number of records to skip for pagination</param>
        /// <param name="limit">Maximum number of records to return</param>
        /// <returns>List of job descriptions</returns>
        public async Task<List<JobResponse>> GetAllJobsAsync(int skip = 0, int limit = 20)
        {
            var jobs = await _db.GetAllJobsAsync(skip, limit);
            return jobs.Select(MapToResponse).ToList();
        }

        /// <summary>
        /// Retrieves all job descriptions associated with a specific user's email
        /// </summary>
        /// <param name="email">The email address of the user</param>
        /// <returns>List of job descriptions belonging to the user</returns>
        /// <exception cref="ArgumentException">Thrown when email is null or empty</exception>
        /// <exception cref="Exception">Thrown when database operation fails</exception>
        public virtual async Task<List<JobDescription>> GetByUserEmailAsync(string email)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                {
                    throw new ArgumentException("Email cannot be null or empty", nameof(email));
                }

                var filter = Builders<JobDescription>.Filter.Eq(x => x.UserEmail, email);
                var sort = Builders<JobDescription>.Sort.Descending(x => x.CreatedAt);

                return await _db.Jobs
                    .Find(filter)
                    .Sort(sort)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving jobs for user {Email}", email);
                throw;
            }
        }

        /// <summary>
        /// Maps a JobDescription entity to a JobResponse DTO
        /// </summary>
        /// <param name="job">The job description entity to map</param>
        /// <returns>A JobResponse object suitable for API responses</returns>
        private static JobResponse MapToResponse(JobDescription job)
        {
            return new JobResponse
            {
                Id = job.Id,
                OriginalText = job.OriginalText,
                // OverallAssessment = job.OverallAssessment, // Add this line
                ImprovedText = job.ImprovedText,
                UserEmail = job.UserEmail,  // Add this line
                Analysis = job.Analysis,
                CreatedAt = job.CreatedAt,
                FileName = job.FileName
            };
        }
    }
}