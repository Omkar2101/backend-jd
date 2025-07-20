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

        [BsonElement("overall_assessment")] // Map to exact LLM field name
        public string OverallAssessment { get; set; } = string.Empty; // Add this field for overall assessment

        // Analysis Results
        public AnalysisResult? Analysis { get; set; }
    }

    //added class for analysis result
    public class AnalysisResult

    {
        public double? bias_score { get; set; }
        public double? inclusivity_score { get; set; }
        public double? clarity_score { get; set; }
        public string role { get; set; } = string.Empty;
        public string industry { get; set; } = string.Empty;

        // Add this property to capture improved_text from Python
        [JsonPropertyName("improved_text")]
        [BsonElement("improved_text")]
        public string ImprovedText { get; set; } = string.Empty;
        public List<Issue> Issues { get; set; } = new();
        public List<Suggestion> suggestions { get; set; } = new();
        public List<string> seo_keywords { get; set; } = new();

        public string overall_assessment { get; set; } = string.Empty; // Add this field for overall assessment
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
        // public string Original { get; set; } = string.Empty;
        // public string Improved { get; set; } = string.Empty;
        // public string rationale { get; set; } = string.Empty;
        // public string Category { get; set; } = string.Empty; // Bias, Clarity, SEO
            [JsonPropertyName("original")]
            public string Original { get; set; } = string.Empty;

            [JsonPropertyName("improved")]
            public string Improved { get; set; } = string.Empty;

            [JsonPropertyName("rationale")]
            public string rationale { get; set; } = string.Empty;

            [JsonPropertyName("category")]
            public string Category { get; set; } = string.Empty;
    }
}