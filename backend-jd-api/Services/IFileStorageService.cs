// Services/IFileStorageService.cs
namespace backend_jd_api.Services
{
    public interface IFileStorageService
    {
        Task<(string storedFileName, string filePath)> SaveFileAsync(IFormFile file, string userEmail);
        Task<(byte[] fileData, string contentType, string fileName)> GetFileAsync(string storedFileName);
        Task<bool> DeleteFileAsync(string storedFileName);
        string GetFileUrl(string storedFileName);
    }
}

