// Services/IFileStorageService.cs

// Services/FileStorageService.cs
using backend_jd_api.Services;

public class FileStorageService : IFileStorageService
{
    private readonly string _uploadPath;
    private readonly ILogger<FileStorageService> _logger;
    private readonly IWebHostEnvironment _environment;

    public FileStorageService(IWebHostEnvironment environment, ILogger<FileStorageService> logger)
    {
        _environment = environment;
        _logger = logger;
        _uploadPath = Path.Combine(environment.ContentRootPath, "uploads");
        
        // Create uploads directory if it doesn't exist
        if (!Directory.Exists(_uploadPath))
        {
            Directory.CreateDirectory(_uploadPath);
        }
    }

    public async Task<(string storedFileName, string filePath)> SaveFileAsync(IFormFile file, string userEmail)
    {
        try
        {
            // Generate unique filename
            var fileExtension = Path.GetExtension(file.FileName);
            var storedFileName = $"{Guid.NewGuid()}{fileExtension}";
            
            // Create user-specific subdirectory
            var userFolder = Path.Combine(_uploadPath, SanitizeEmail(userEmail));
            if (!Directory.Exists(userFolder))
            {
                Directory.CreateDirectory(userFolder);
            }

            var filePath = Path.Combine(userFolder, storedFileName);

            // Save file
            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            _logger.LogInformation("File saved: {FileName} -> {StoredFileName}", file.FileName, storedFileName);

            return (storedFileName, filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving file {FileName}", file.FileName);
            throw;
        }
    }

    public async Task<(byte[] fileData, string contentType, string fileName)> GetFileAsync(string storedFileName)
    {
        try
        {
            // Find file in all user directories
            var userDirs = Directory.GetDirectories(_uploadPath);
            string? filePath = null;

            foreach (var userDir in userDirs)
            {
                var potentialPath = Path.Combine(userDir, storedFileName);
                if (File.Exists(potentialPath))
                {
                    filePath = potentialPath;
                    break;
                }
            }

            if (filePath == null || !File.Exists(filePath))
            {
                throw new FileNotFoundException($"File {storedFileName} not found");
            }

            var fileData = await File.ReadAllBytesAsync(filePath);
            var contentType = GetContentType(storedFileName);
            var originalFileName = Path.GetFileName(filePath);

            return (fileData, contentType, originalFileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file {StoredFileName}", storedFileName);
            throw;
        }
    }

    public async Task<bool> DeleteFileAsync(string storedFileName)
    {
        try
        {
            // Find and delete file
            var userDirs = Directory.GetDirectories(_uploadPath);
            
            foreach (var userDir in userDirs)
            {
                var filePath = Path.Combine(userDir, storedFileName);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _logger.LogInformation("File deleted: {StoredFileName}", storedFileName);
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {StoredFileName}", storedFileName);
            return false;
        }
    }

    public string GetFileUrl(string storedFileName)
    {
        return $"/api/files/{storedFileName}";
    }

    private string SanitizeEmail(string email)
    {
        return email.Replace("@", "_").Replace(".", "_");
    }

    private string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".txt" => "text/plain",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => "application/octet-stream"
        };
    }
}
