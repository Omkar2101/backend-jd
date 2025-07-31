// using backend_jd_api.Data;
// using backend_jd_api.Models;
// using MongoDB.Driver;

// namespace backend_jd_api.Services

// {
    
//     // Create IJobService interface
//     public interface IJobService
//     {
//         Task<List<JobDescription>> GetByUserEmailAsync(string email);
//         Task<JobResponse> AnalyzeFromFileAsync(IFormFile file, string userEmail);
//         Task<JobResponse> AnalyzeTextAsync(string text, string userEmail, string? jobTitle = null);
//         Task<JobResponse?> GetJobAsync(string id);
//         Task<List<JobResponse>> GetAllJobsAsync(int skip = 0, int limit = 20);
//         Task<bool> DeleteJobAsync(string id);
//     }
//     /// <summary>
//     /// Service for handling job description analysis, storage, and retrieval operations
//     /// </summary>
//     public class JobService: IJobService
//     {
//         private readonly MongoDbContext _db;
//         private readonly PythonService _pythonService;

//         private readonly IFileStorageService _fileStorageService;
//         private readonly ILogger<JobService> _logger;

//         /// <summary>
//         /// Initializes a new instance of the JobService
//         /// </summary>
//         /// <param name="db">MongoDB context for data persistence</param>
//         /// <param name="pythonService">Service for text analysis and extraction</param>
//         /// <param name="logger">Logger for error tracking and monitoring</param>
//         public JobService(MongoDbContext db, PythonService pythonService, ILogger<JobService> logger,IFileStorageService fileStorageService)
//         {
//             _db = db;
//             _pythonService = pythonService;
//             _fileStorageService = fileStorageService;
//             _logger = logger;
//         }

//         /// <summary>
//         /// Deletes a job description by its ID
//         /// </summary>
//         /// <param name="id">The unique identifier of the job description</param>
//         /// <returns>True if deleted, false if not found</returns>
//         public async Task<bool> DeleteJobAsync(string id)
//         {
//             try
//             {
//                 return await _db.DeleteJobAsync(id);
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Error deleting job {Id}", id);
//                 throw;
//             }
//         }

     

//         /// <summary>
//         /// Analyzes a job description from an uploaded file
//         /// </summary>
//         /// <param name="file">The uploaded file containing the job description</param>
//         /// <param name="userEmail">The email of the user uploading the file</param>
//         /// <param name="isImage">Whether the file is an image that needs OCR processing</param>
//         /// <returns>Analysis results including bias detection and improvements</returns>
//         /// <exception cref="Exception">Thrown when file processing or analysis fails</exception>
//         public async Task<JobResponse> AnalyzeFromFileAsync(IFormFile file, string userEmail)
//         {
//             try
//             {   
//                 // Save the file first
//                 var (storedFileName, filePath) = await _fileStorageService.SaveFileAsync(file, userEmail);
//                 // Read file
//                 using var stream = new MemoryStream();
//                 await file.CopyToAsync(stream);
//                 var fileContent = stream.ToArray();

//                 // Extract text yahan pe errror aa rha
//                 var text = await _pythonService.ExtractTextFromFileAsync(fileContent, file.FileName);
//                 // Validate extracted text
//                 if (string.IsNullOrWhiteSpace(text))

//                 {
//                     // Clean up saved file if text extraction fails
//                     await _fileStorageService.DeleteFileAsync(storedFileName);
//                     _logger.LogWarning("No text extracted from file {FileName}", file.FileName);
                    
//                     throw new Exception($"No text could be extracted from the file {file.FileName}");
//                 }

//                 // Check minimum length requirement (Python API requires 50+ characters)
//                 if (text.Trim().Length < 50)
//                 {
//                     await _fileStorageService.DeleteFileAsync(storedFileName);
//                     _logger.LogWarning("Extracted text from {FileName} is too short: {Length} characters",
//                         file.FileName, text.Trim().Length);
//                     throw new Exception($"The extracted text is too short ({text.Trim().Length} characters). Job descriptions must be at least 50 characters long.");
//                 }

//                 _logger.LogInformation("Extracted text from {FileName}. Length: {Length} characters",
//                     file.FileName, text.Length);

//                 // Log first 200 characters for debugging (be careful with sensitive data)
//                 _logger.LogDebug("Extracted text preview: {TextPreview}",
//                     text.Length > 200 ? text.Substring(0, 200) + "..." : text);

//                 // Analyze text
//                 var analysis = await _pythonService.AnalyzeTextAsync(text);
//                 _logger.LogInformation("Analysis completed. ImprovedText length: {Length}, Suggestions count: {Count}",
//                     analysis.ImprovedText?.Length ?? 0, analysis.suggestions?.Count ?? 0);

//                 // Create and save job description record
//                 var jd = new JobDescription
//                 {
//                     UserEmail = userEmail,
//                     OriginalText = text,
//                     ImprovedText = analysis.ImprovedText ?? string.Empty,
//                     // OverallAssessment = analysis.overall_assessment, // Add overall assessment
//                     FileName = file.FileName,

//                     //added file properties
//                     OriginalFileName = file.FileName,
//                     StoredFileName = storedFileName,
//                     ContentType = file.ContentType,
//                     FileSize = file.Length,
//                     FilePath = filePath,

//                     CreatedAt = DateTime.UtcNow,
//                     Analysis = analysis
//                 };

//                 var savedJob = await _db.CreateJobAsync(jd);

//                 return new JobResponse
//                 {
//                     Id = savedJob.Id,
//                     UserEmail = savedJob.UserEmail,
//                     OriginalText = savedJob.OriginalText,
//                     ImprovedText = savedJob.ImprovedText,
//                     // OverallAssessment = savedJob.OverallAssessment, // Add this line

//                     //aded file properties
//                     OriginalFileName = savedJob.OriginalFileName,
//                     ContentType = savedJob.ContentType,
//                     FileSize = savedJob.FileSize,
//                     FileUrl = _fileStorageService.GetFileUrl(savedJob.StoredFileName!),
//                     FileName = savedJob.FileName,
//                     CreatedAt = savedJob.CreatedAt,
//                     Analysis = savedJob.Analysis
//                 };
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Error analyzing file {FileName}", file.FileName);
//                 throw;
//             }
//         }

//         /// <summary>
//         /// Analyzes job description text directly
//         /// </summary>
//         /// <param name="text">The job description text to analyze</param>
//         /// <param name="userEmail">The email of the user submitting the text</param>
//         /// <param name="jobTitle">Optional title for the job description</param>
//         /// <returns>Analysis results including bias detection and improvements</returns>
//         /// <exception cref="Exception">Thrown when text analysis fails</exception>
//         public async Task<JobResponse> AnalyzeTextAsync(string text, string userEmail, string? jobTitle = null)
//         {
//             try
//             {

//                 // Validate input text
//                 if (string.IsNullOrWhiteSpace(text))
//                 {
//                     throw new ArgumentException("Text cannot be empty or whitespace", nameof(text));
//                 }


//                 // Check minimum length requirement (Python API requires 50+ characters)
//                 if (text.Trim().Length < 50)
//                 {
//                     throw new ArgumentException($"Job description text must be at least 50 characters long. Current length: {text.Trim().Length} characters");
//                 }

//                 _logger.LogInformation("Analyzing text with length: {Length} characters", text.Length);


//                 // Analyze text
//                 var analysis = await _pythonService.AnalyzeTextAsync(text);
//                 _logger.LogInformation("Text analysis completed successfully{analysis}", analysis);

//                 // Create job record
//                 var job = new JobDescription
//                 {
//                     OriginalText = text,
//                     ImprovedText = analysis.ImprovedText,
//                     // OverallAssessment = analysis.overall_assessment, // Add overall assessment
//                     FileName = jobTitle ?? "Direct Input",
//                     UserEmail = userEmail,  // Add the email
//                     Analysis = analysis,
//                     CreatedAt = DateTime.UtcNow
//                 };

//                 // Save to database
//                 var savedJob = await _db.CreateJobAsync(job);

//                 return MapToResponse(savedJob);
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Error analyzing text");
//                 throw;
//             }
//         }

//         /// <summary>
//         /// Retrieves a specific job description by its ID
//         /// </summary>
//         /// <param name="id">The unique identifier of the job description</param>
//         /// <returns>The job description if found, null otherwise</returns>
//         public async Task<JobResponse?> GetJobAsync(string id)
//         {
//             var job = await _db.GetJobAsync(id);
//             return job != null ? MapToResponse(job) : null;
//         }

//         /// <summary>
//         /// Retrieves a paginated list of all job descriptions
//         /// </summary>
//         /// <param name="skip">Number of records to skip for pagination</param>
//         /// <param name="limit">Maximum number of records to return</param>
//         /// <returns>List of job descriptions</returns>
//         public async Task<List<JobResponse>> GetAllJobsAsync(int skip = 0, int limit = 20)
//         {
//             var jobs = await _db.GetAllJobsAsync(skip, limit);
//             return jobs.Select(MapToResponse).ToList();
//         }

//         /// <summary>
//         /// Retrieves all job descriptions associated with a specific user's email
//         /// </summary>
//         /// <param name="email">The email address of the user</param>
//         /// <returns>List of job descriptions belonging to the user</returns>
//         /// <exception cref="ArgumentException">Thrown when email is null or empty</exception>
//         /// <exception cref="Exception">Thrown when database operation fails</exception>
//         public virtual async Task<List<JobDescription>> GetByUserEmailAsync(string email)
//         {
//             try
//             {
//                 if (string.IsNullOrEmpty(email))
//                 {
//                     throw new ArgumentException("Email cannot be null or empty", nameof(email));
//                 }

//                 var filter = Builders<JobDescription>.Filter.Eq(x => x.UserEmail, email);
//                 var sort = Builders<JobDescription>.Sort.Descending(x => x.CreatedAt);

//                 return await _db.Jobs
//                     .Find(filter)
//                     .Sort(sort)
//                     .ToListAsync();
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Error retrieving jobs for user {Email}", email);
//                 throw;
//             }
//         }

//         /// <summary>
//         /// Maps a JobDescription entity to a JobResponse DTO
//         /// </summary>
//         /// <param name="job">The job description entity to map</param>
//         /// <returns>A JobResponse object suitable for API responses</returns>
//         /// 
//         /// //removed static keyword
//         private  JobResponse MapToResponse(JobDescription job)
//         {
//             return new JobResponse
//             {
//                 Id = job.Id,
//                 OriginalText = job.OriginalText,
//                 // OverallAssessment = job.OverallAssessment, // Add this line
//                 ImprovedText = job.ImprovedText,
//                 UserEmail = job.UserEmail,  // Add this line
//                 Analysis = job.Analysis,
//                 CreatedAt = job.CreatedAt,
//                 FileName = job.FileName,
//                 OriginalFileName = job.OriginalFileName,
//                 ContentType = job.ContentType,
//                 FileSize = job.FileSize,
//                 FileUrl = !string.IsNullOrEmpty(job.StoredFileName) ? _fileStorageService.GetFileUrl(job.StoredFileName) : null
//             };
//         }
//     }
// }



using backend_jd_api.Data;
using backend_jd_api.Models;
using MongoDB.Driver;

namespace backend_jd_api.Services
{
    // Result classes remain the same
    public class JobAnalysisResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public JobResponse? JobResponse { get; set; }
    }

    public class JobRetrievalResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public JobResponse? JobResponse { get; set; }
    }

    public class JobListResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public List<JobResponse> Jobs { get; set; } = new List<JobResponse>();
    }

    public class JobDeleteResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class UserJobsResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public List<JobDescription> Jobs { get; set; } = new List<JobDescription>();
    }

    // Updated interface to use result classes
    public interface IJobService
    {
        Task<UserJobsResult> GetByUserEmailAsync(string email);
        Task<JobAnalysisResult> AnalyzeFromFileAsync(IFormFile file, string userEmail);
        Task<JobAnalysisResult> AnalyzeTextAsync(string text, string userEmail, string? jobTitle = null);
        Task<JobRetrievalResult> GetJobAsync(string id);
        Task<JobListResult> GetAllJobsAsync(int skip = 0, int limit = 20);
        Task<JobDeleteResult> DeleteJobAsync(string id);
    }

    /// <summary>
    /// Service for handling job description analysis, storage, and retrieval operations
    /// </summary>
    public class JobService : IJobService
    {
        private readonly MongoDbContext _db;
        private readonly PythonService _pythonService;
        private readonly IFileStorageService _fileStorageService;
        private readonly ILogger<JobService> _logger;

        /// <summary>
        /// Initializes a new instance of the JobService
        /// </summary>
        /// <param name="db">MongoDB context for data persistence</param>
        /// <param name="pythonService">Service for text analysis and extraction</param>
        /// <param name="logger">Logger for error tracking and monitoring</param>
        /// <param name="fileStorageService">Service for file storage operations</param>
        public JobService(MongoDbContext db, PythonService pythonService, ILogger<JobService> logger, IFileStorageService fileStorageService)
        {
            _db = db;
            _pythonService = pythonService;
            _fileStorageService = fileStorageService;
            _logger = logger;
        }

        /// <summary>
        /// Deletes a job description by its ID
        /// </summary>
        /// <param name="id">The unique identifier of the job description</param>
        /// <returns>JobDeleteResult indicating success or failure</returns>
        public async Task<JobDeleteResult> DeleteJobAsync(string id)
        {
            // Input validation
            if (string.IsNullOrWhiteSpace(id))
            {
                return new JobDeleteResult 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Job ID is required" 
                };
            }

            var deleteResult = await _db.DeleteJobAsync(id);
            if (!deleteResult)
            {
                _logger.LogWarning("Job not found for deletion: {Id}", id);
                return new JobDeleteResult 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Job not found" 
                };
            }

            _logger.LogInformation("Job deleted successfully: {Id}", id);
            return new JobDeleteResult { IsSuccess = true };
        }

        /// <summary>
        /// Analyzes a job description from an uploaded file
        /// </summary>
        /// <param name="file">The uploaded file containing the job description</param>
        /// <param name="userEmail">The email of the user uploading the file</param>
        /// <returns>JobAnalysisResult with analysis results or error information</returns>
        public async Task<JobAnalysisResult> AnalyzeFromFileAsync(IFormFile file, string userEmail)
        {
            // Input validation
            if (file == null)
            {
                return new JobAnalysisResult 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "File cannot be null" 
                };
            }

            if (file.Length == 0)
            {
                return new JobAnalysisResult 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "File cannot be empty" 
                };
            }

            if (string.IsNullOrWhiteSpace(userEmail))
            {
                return new JobAnalysisResult 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "User email is required" 
                };
            }

            // Save the file first
            var saveResult = await _fileStorageService.SaveFileAsync(file, userEmail);
            if (!saveResult.IsSuccess)
            {
                _logger.LogError("Failed to save file {FileName}: {Error}", file.FileName, saveResult.ErrorMessage);
                return new JobAnalysisResult 
                { 
                    IsSuccess = false, 
                    ErrorMessage = $"Failed to save file: {saveResult.ErrorMessage}" 
                };
            }

            // Read file content
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            var fileContent = stream.ToArray();

            // Extract text from file using updated PythonService
            var textExtractionResult = await _pythonService.ExtractTextFromFileAsync(fileContent, file.FileName);
            
            // Handle text extraction failure
            if (!textExtractionResult.IsSuccess)
            {
                // Clean up saved file if text extraction fails
                await _fileStorageService.DeleteFileAsync(saveResult.StoredFileName);
                _logger.LogWarning("Text extraction failed for file {FileName}: {Error}", file.FileName, textExtractionResult.ErrorMessage);
                
                return new JobAnalysisResult 
                { 
                    IsSuccess = false, 
                    ErrorMessage = textExtractionResult.ErrorMessage 
                };
            }

            // Validate extracted text
            if (string.IsNullOrWhiteSpace(textExtractionResult.ExtractedText))
            {
                // Clean up saved file if no text extracted
                await _fileStorageService.DeleteFileAsync(saveResult.StoredFileName);
                _logger.LogWarning("No text extracted from file {FileName}", file.FileName);
                
                return new JobAnalysisResult 
                { 
                    IsSuccess = false, 
                    ErrorMessage = $"No text could be extracted from the file {file.FileName}" 
                };
            }

            // Check minimum length requirement
            if (textExtractionResult.ExtractedText.Trim().Length < 50)
            {
                await _fileStorageService.DeleteFileAsync(saveResult.StoredFileName);
                _logger.LogWarning("Extracted text from {FileName} is too short: {Length} characters",
                    file.FileName, textExtractionResult.ExtractedText.Trim().Length);
                
                return new JobAnalysisResult 
                { 
                    IsSuccess = false, 
                    ErrorMessage = $"The extracted text is too short ({textExtractionResult.ExtractedText.Trim().Length} characters). Job descriptions must be at least 50 characters long." 
                };
            }

            _logger.LogInformation("Extracted text from {FileName}. Length: {Length} characters",
                file.FileName, textExtractionResult.ExtractedText.Length);

            // Log first 200 characters for debugging
            _logger.LogDebug("Extracted text preview: {TextPreview}",
                textExtractionResult.ExtractedText.Length > 200 ? textExtractionResult.ExtractedText.Substring(0, 200) + "..." : textExtractionResult.ExtractedText);

            // Analyze text using updated PythonService
            var analysisResult = await _pythonService.AnalyzeTextAsync(textExtractionResult.ExtractedText);
            
            // Handle analysis failure
            if (!analysisResult.IsSuccess)
            {
                await _fileStorageService.DeleteFileAsync(saveResult.StoredFileName);
                _logger.LogError("Text analysis failed: {Error}", analysisResult.ErrorMessage);
                
                return new JobAnalysisResult 
                { 
                    IsSuccess = false, 
                    ErrorMessage = analysisResult.ErrorMessage 
                };
            }

            // Validate analysis result
            if (analysisResult.AnalysisResult == null)
            {
                await _fileStorageService.DeleteFileAsync(saveResult.StoredFileName);
                return new JobAnalysisResult 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Analysis service returned invalid results" 
                };
            }

            _logger.LogInformation("Analysis completed. ImprovedText length: {Length}, Suggestions count: {Count}",
                analysisResult.AnalysisResult.ImprovedText?.Length ?? 0, analysisResult.AnalysisResult.suggestions?.Count ?? 0);

            // Create and save job description record
            var jd = new JobDescription
            {
                UserEmail = userEmail,
                OriginalText = textExtractionResult.ExtractedText,
                ImprovedText = analysisResult.AnalysisResult.ImprovedText ?? string.Empty,
                FileName = file.FileName,
                OriginalFileName = file.FileName,
                StoredFileName = saveResult.StoredFileName,
                ContentType = file.ContentType,
                FileSize = file.Length,
                FilePath = saveResult.FilePath,
                CreatedAt = DateTime.UtcNow,
                Analysis = analysisResult.AnalysisResult
            };

            var savedJob = await _db.CreateJobAsync(jd);
            if (savedJob == null)
            {
                // Clean up saved file if database save fails
                await _fileStorageService.DeleteFileAsync(saveResult.StoredFileName);
                return new JobAnalysisResult 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Failed to save job analysis to database" 
                };
            }

            var jobResponse = new JobResponse
            {
                Id = savedJob.Id,
                UserEmail = savedJob.UserEmail,
                OriginalText = savedJob.OriginalText,
                ImprovedText = savedJob.ImprovedText,
                OriginalFileName = savedJob.OriginalFileName,
                ContentType = savedJob.ContentType,
                FileSize = savedJob.FileSize,
                FileUrl = _fileStorageService.GetFileUrl(savedJob.StoredFileName!),
                FileName = savedJob.FileName,
                CreatedAt = savedJob.CreatedAt,
                Analysis = savedJob.Analysis
            };

            return new JobAnalysisResult 
            { 
                IsSuccess = true, 
                JobResponse = jobResponse 
            };
        }

        /// <summary>
        /// Analyzes job description text directly
        /// </summary>
        /// <param name="text">The job description text to analyze</param>
        /// <param name="userEmail">The email of the user submitting the text</param>
        /// <param name="jobTitle">Optional title for the job description</param>
        /// <returns>JobAnalysisResult with analysis results or error information</returns>
        public async Task<JobAnalysisResult> AnalyzeTextAsync(string text, string userEmail, string? jobTitle = null)
        {
            // Input validation
            if (string.IsNullOrWhiteSpace(text))
            {
                return new JobAnalysisResult 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Text cannot be empty or whitespace" 
                };
            }

            if (string.IsNullOrWhiteSpace(userEmail))
            {
                return new JobAnalysisResult 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "User email is required" 
                };
            }

            // Check minimum length requirement
            if (text.Trim().Length < 50)
            {
                return new JobAnalysisResult 
                { 
                    IsSuccess = false, 
                    ErrorMessage = $"Job description text must be at least 50 characters long. Current length: {text.Trim().Length} characters" 
                };
            }

            _logger.LogInformation("Analyzing text with length: {Length} characters", text.Length);

            // Analyze text using updated PythonService
            var analysisResult = await _pythonService.AnalyzeTextAsync(text);
            
            // Handle analysis failure
            if (!analysisResult.IsSuccess)
            {
                _logger.LogError("Text analysis failed: {Error}", analysisResult.ErrorMessage);
                return new JobAnalysisResult 
                { 
                    IsSuccess = false, 
                    ErrorMessage = analysisResult.ErrorMessage 
                };
            }

            // Validate analysis result
            if (analysisResult.AnalysisResult == null)
            {
                return new JobAnalysisResult 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Analysis service returned invalid results" 
                };
            }

            _logger.LogInformation("Text analysis completed successfully");

            // Create job record
            var job = new JobDescription
            {
                OriginalText = text,
                ImprovedText = analysisResult.AnalysisResult.ImprovedText,
                FileName = jobTitle ?? "Direct Input",
                UserEmail = userEmail,
                Analysis = analysisResult.AnalysisResult,
                CreatedAt = DateTime.UtcNow
            };

            // Save to database
            var savedJob = await _db.CreateJobAsync(job);
            if (savedJob == null)
            {
                return new JobAnalysisResult 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Failed to save job analysis to database" 
                };
            }

            var jobResponse = MapToResponse(savedJob);
            return new JobAnalysisResult 
            { 
                IsSuccess = true, 
                JobResponse = jobResponse 
            };
        }

        /// <summary>
        /// Retrieves a specific job description by its ID
        /// </summary>
        /// <param name="id">The unique identifier of the job description</param>
        /// <returns>JobRetrievalResult with the job description or error information</returns>
        public async Task<JobRetrievalResult> GetJobAsync(string id)
        {
            // Input validation
            if (string.IsNullOrWhiteSpace(id))
            {
                return new JobRetrievalResult 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Job ID is required" 
                };
            }

            var job = await _db.GetJobAsync(id);
            if (job == null)
            {
                _logger.LogWarning("Job not found: {Id}", id);
                return new JobRetrievalResult 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Job not found" 
                };
            }

            return new JobRetrievalResult 
            { 
                IsSuccess = true, 
                JobResponse = MapToResponse(job) 
            };
        }

        /// <summary>
        /// Retrieves a paginated list of all job descriptions
        /// </summary>
        /// <param name="skip">Number of records to skip for pagination</param>
        /// <param name="limit">Maximum number of records to return</param>
        /// <returns>JobListResult with list of job descriptions or error information</returns>
        public async Task<JobListResult> GetAllJobsAsync(int skip = 0, int limit = 20)
        {
            // Input validation
            if (skip < 0)
            {
                return new JobListResult 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Skip value cannot be negative" 
                };
            }

            if (limit <= 0 || limit > 100)
            {
                return new JobListResult 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Limit must be between 1 and 100" 
                };
            }

            var jobs = await _db.GetAllJobsAsync(skip, limit);
            if (jobs == null)
            {
                return new JobListResult 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Failed to retrieve jobs from database" 
                };
            }

            return new JobListResult 
            { 
                IsSuccess = true, 
                Jobs = jobs.Select(MapToResponse).ToList() 
            };
        }

        /// <summary>
        /// Retrieves all job descriptions associated with a specific user's email
        /// </summary>
        /// <param name="email">The email address of the user</param>
        /// <returns>UserJobsResult with list of job descriptions or error information</returns>
        public virtual async Task<UserJobsResult> GetByUserEmailAsync(string email)
        {
            // Input validation
            if (string.IsNullOrWhiteSpace(email))
            {
                return new UserJobsResult 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Email is required" 
                };
            }

            var filter = Builders<JobDescription>.Filter.Eq(x => x.UserEmail, email);
            var sort = Builders<JobDescription>.Sort.Descending(x => x.CreatedAt);

            var jobs = await _db.Jobs
                .Find(filter)
                .Sort(sort)
                .ToListAsync();

            if (jobs == null)
            {
                return new UserJobsResult 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Failed to retrieve user jobs from database" 
                };
            }

            _logger.LogInformation("Retrieved {Count} jobs for user {Email}", jobs.Count, email);

            return new UserJobsResult 
            { 
                IsSuccess = true, 
                Jobs = jobs 
            };
        }

        /// <summary>
        /// Maps a JobDescription entity to a JobResponse DTO
        /// </summary>
        /// <param name="job">The job description entity to map</param>
        /// <returns>A JobResponse object suitable for API responses</returns>
        private JobResponse MapToResponse(JobDescription job)
        {
            return new JobResponse
            {
                Id = job.Id,
                OriginalText = job.OriginalText,
                ImprovedText = job.ImprovedText,
                UserEmail = job.UserEmail,
                Analysis = job.Analysis,
                CreatedAt = job.CreatedAt,
                FileName = job.FileName,
                OriginalFileName = job.OriginalFileName,
                ContentType = job.ContentType,
                FileSize = job.FileSize,
                FileUrl = !string.IsNullOrEmpty(job.StoredFileName) ? _fileStorageService.GetFileUrl(job.StoredFileName) : null
            };
        }
    }
}
