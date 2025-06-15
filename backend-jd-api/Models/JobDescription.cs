using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace backend_jd_api.Models
{
    public class JobDescription
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        public string UserEmail { get; set; } = string.Empty;  // Add this field

        public string OriginalText { get; set; } = string.Empty;

        [BsonElement("improved_text")] // Map to exact LLM field name
        public string ImprovedText { get; set; } = string.Empty; // Use PascalCase in C#
        public string FileName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Analysis Results
        public AnalysisResult? Analysis { get; set; }
    }

    public class AnalysisResult
    {
        public double bias_score { get; set; }
        public double inclusivity_score { get; set; }
        public double clarity_score { get; set; }

         // Add this property to capture improved_text from Python
        [JsonPropertyName("improved_text")]
        [BsonElement("improved_text")]
        public string ImprovedText { get; set; } = string.Empty;
        public List<Issue> Issues { get; set; } = new();
        public List<Suggestion> suggestions { get; set; } = new();
        public List<string> seo_keywords { get; set; } = new();
    }

    public class Issue
    {
        public string Type { get; set; } = string.Empty; // Gender, Age, etc.
        public string Text { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty; // Low, Medium, High
        public string Explanation { get; set; } = string.Empty;
    }

    public class Suggestion
    {
        public string Original { get; set; } = string.Empty;
        public string Improved { get; set; } = string.Empty;
        public string rationale { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty; // Bias, Clarity, SEO
    }
}