using System.ComponentModel.DataAnnotations;

namespace backend_jd_api.Models
{
    public class UploadRequest
    {
        [Required]
        public IFormFile File { get; set; } = null!;

        [Required]
        [EmailAddress]
        public string UserEmail { get; set; } = string.Empty;
    }

    public class AnalyzeRequest
    {
        [Required]
        [MinLength(50, ErrorMessage = "Job description must be at least 50 characters")]
        public string Text { get; set; } = string.Empty;

        public string? JobTitle { get; set; }

        [Required]
        [EmailAddress]
        public string UserEmail { get; set; } = string.Empty;
    }

    public class JobResponse
    {
        public string Id { get; set; } = string.Empty;
        public string OriginalText { get; set; } = string.Empty;
        public string ImprovedText { get; set; } = string.Empty;

        // public string OverallAssessment { get; set; } = string.Empty; // Add this line
        public string UserEmail { get; set; } = string.Empty;  // Add this line
        public AnalysisResult? Analysis { get; set; }
        public DateTime CreatedAt { get; set; }
        public string FileName { get; set; } = string.Empty;

        // New file storage properties
        // New file properties
        public string? OriginalFileName { get; set; }
        public string? ContentType { get; set; }
        public long FileSize { get; set; }
        public string? FileUrl { get; set; } // URL to access the file

    }
    // In your Models or DTOs folder
    public class ErrorResponse
    {
        public bool error { get; set; }
        public string message { get; set; }
        public string type { get; set; }
        public int status_code { get; set; }
        public DateTime timestamp { get; set; }
    }
}