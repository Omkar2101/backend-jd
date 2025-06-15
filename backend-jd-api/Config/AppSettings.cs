namespace backend_jd_api.Config
{
    public class AppSettings
    {
        public DatabaseConfig Database { get; set; } = new();
        public PythonApiConfig PythonApi { get; set; } = new();
        public FileConfig Files { get; set; } = new();
    }

    public class DatabaseConfig
    {
        public string ConnectionString { get; set; } = "mongodb://localhost:27017";
        public string DatabaseName { get; set; } = "JobAnalyzerDB";
        public string CollectionName { get; set; } = "JobDescriptions";
    }

    public class PythonApiConfig
    {
        public string BaseUrl { get; set; } = "http://localhost:8000";
        public int TimeoutSeconds { get; set; } = 300;
    }

    public class FileConfig
    {
        public long MaxSizeMB { get; set; } = 10;
        public List<string> AllowedTypes { get; set; } = new() { ".txt", ".pdf", ".doc", ".docx" };
    }
}