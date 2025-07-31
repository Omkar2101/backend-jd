// FIXED: Added proper interface with result classes instead of tuples

using Microsoft.AspNetCore.Mvc;

namespace backend_jd_api.Services
{
    // FIXED: Issue #2 - Created result classes instead of returning tuples
    public class FileStorageResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string StoredFileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
    }

    public class FileRetrievalResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public byte[] FileData { get; set; } = Array.Empty<byte>();
        public string ContentType { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
    }

    public class FileOperationResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public interface IFileStorageService
    {
        Task<FileStorageResult> SaveFileAsync(IFormFile file, string userEmail);
        Task<FileRetrievalResult> GetFileAsync(string storedFileName);
        Task<FileOperationResult> DeleteFileAsync(string storedFileName);
        string GetFileUrl(string storedFileName);
    }
}