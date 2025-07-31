// // Services/IFileStorageService.cs

// // Services/FileStorageService.cs
// using backend_jd_api.Services;

// public class FileStorageService : IFileStorageService
// {
//     private readonly string _uploadPath;
//     private readonly ILogger<FileStorageService> _logger;
//     private readonly IWebHostEnvironment _environment;

//     public FileStorageService(IWebHostEnvironment environment, ILogger<FileStorageService> logger)
//     {
//         _environment = environment;
//         _logger = logger;
//         _uploadPath = Path.Combine(environment.ContentRootPath, "uploads");
        
//         // Create uploads directory if it doesn't exist
//         if (!Directory.Exists(_uploadPath))
//         {
//             Directory.CreateDirectory(_uploadPath);
//         }
//     }

//     public async Task<(string storedFileName, string filePath)> SaveFileAsync(IFormFile file, string userEmail)
//     {
//         try
//         {
//             // Generate unique filename
//             var fileExtension = Path.GetExtension(file.FileName);
//             var storedFileName = $"{Guid.NewGuid()}{fileExtension}";
            
//             // Create user-specific subdirectory
//             var userFolder = Path.Combine(_uploadPath, SanitizeEmail(userEmail));
//             if (!Directory.Exists(userFolder))
//             {
//                 Directory.CreateDirectory(userFolder);
//             }

//             var filePath = Path.Combine(userFolder, storedFileName);

//             // Save file
//             using var stream = new FileStream(filePath, FileMode.Create);
//             await file.CopyToAsync(stream);

//             _logger.LogInformation("File saved: {FileName} -> {StoredFileName}", file.FileName, storedFileName);

//             return (storedFileName, filePath);
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, "Error saving file {FileName}", file.FileName);
//             throw;
//         }
//     }

//     public async Task<(byte[] fileData, string contentType, string fileName)> GetFileAsync(string storedFileName)
//     {
//         try
//         {
//             // Find file in all user directories
//             var userDirs = Directory.GetDirectories(_uploadPath);
//             string? filePath = null;

//             foreach (var userDir in userDirs)
//             {
//                 var potentialPath = Path.Combine(userDir, storedFileName);
//                 if (File.Exists(potentialPath))
//                 {
//                     filePath = potentialPath;
//                     break;
//                 }
//             }

//             if (filePath == null || !File.Exists(filePath))
//             {
//                 throw new FileNotFoundException($"File {storedFileName} not found");
//             }

//             var fileData = await File.ReadAllBytesAsync(filePath);
//             var contentType = GetContentType(storedFileName);
//             var originalFileName = Path.GetFileName(filePath);

//             return (fileData, contentType, originalFileName);
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, "Error getting file {StoredFileName}", storedFileName);
//             throw;
//         }
//     }

//     public async Task<bool> DeleteFileAsync(string storedFileName)
//     {
//         try
//         {
//             // Find and delete file
//             var userDirs = Directory.GetDirectories(_uploadPath);
            
//             foreach (var userDir in userDirs)
//             {
//                 var filePath = Path.Combine(userDir, storedFileName);
//                 if (File.Exists(filePath))
//                 {
//                     File.Delete(filePath);
//                     _logger.LogInformation("File deleted: {StoredFileName}", storedFileName);
//                     return true;
//                 }
//             }

//             return false;
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, "Error deleting file {StoredFileName}", storedFileName);
//             return false;
//         }
//     }

//     public string GetFileUrl(string storedFileName)
//     {
//         return $"/api/files/{storedFileName}";
//     }

//     private string SanitizeEmail(string email)
//     {
//         return email.Replace("@", "_").Replace(".", "_");
//     }

//     private string GetContentType(string fileName)
//     {
//         var extension = Path.GetExtension(fileName).ToLowerInvariant();
//         return extension switch
//         {
//             ".pdf" => "application/pdf",
//             ".doc" => "application/msword",
//             ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
//             ".txt" => "text/plain",
//             ".jpg" or ".jpeg" => "image/jpeg",
//             ".png" => "image/png",
//             _ => "application/octet-stream"
//         };
//     }
// }


// Services/FileStorageService.cs
using backend_jd_api.Services;

namespace backend_jd_api.Services
{
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
            // FIXED: Issue #1 - Handle directory creation without throwing exceptions
            if (!Directory.Exists(_uploadPath))
            {
                try
                {
                    Directory.CreateDirectory(_uploadPath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create uploads directory: {UploadPath}", _uploadPath);
                    // Don't throw - let individual operations handle this gracefully
                }
            }
        }

        // FIXED: Issue #1 & #2 - Removed try-catch, return proper result object instead of tuple and exceptions
        public async Task<FileStorageResult> SaveFileAsync(IFormFile file, string userEmail)
        {
            // Input validation
            if (file == null)
            {
                return new FileStorageResult 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "File cannot be null" 
                };
            }

            if (file.Length == 0)
            {
                return new FileStorageResult 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "File cannot be empty" 
                };
            }

            if (string.IsNullOrWhiteSpace(userEmail))
            {
                return new FileStorageResult 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "User email is required" 
                };
            }

            // Check if upload directory exists
            if (!Directory.Exists(_uploadPath))
            {
                return new FileStorageResult 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Upload directory is not available" 
                };
            }

            // Generate unique filename
            var fileExtension = Path.GetExtension(file.FileName);
            if (string.IsNullOrEmpty(fileExtension))
            {
                return new FileStorageResult 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "File must have a valid extension" 
                };
            }

            var storedFileName = $"{Guid.NewGuid()}{fileExtension}";
            
            // Create user-specific subdirectory
            var sanitizedEmail = SanitizeEmail(userEmail);
            var userFolder = Path.Combine(_uploadPath, sanitizedEmail);
            
            if (!Directory.Exists(userFolder))
            {
                try
                {
                    Directory.CreateDirectory(userFolder);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create user directory: {UserFolder}", userFolder);
                    return new FileStorageResult 
                    { 
                        IsSuccess = false, 
                        ErrorMessage = "Unable to create user directory for file storage" 
                    };
                }
            }

            var filePath = Path.Combine(userFolder, storedFileName);

            // Save file
            try
            {
                using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Access denied when saving file: {FilePath}", filePath);
                return new FileStorageResult 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Access denied - unable to save file" 
                };
            }
            catch (DirectoryNotFoundException ex)
            {
                _logger.LogError(ex, "Directory not found when saving file: {FilePath}", filePath);
                return new FileStorageResult 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Storage directory not found" 
                };
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "IO error when saving file: {FilePath}", filePath);
                return new FileStorageResult 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Unable to save file due to storage error" 
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error saving file {FileName}", file.FileName);
                return new FileStorageResult 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "An unexpected error occurred while saving the file" 
                };
            }

            _logger.LogInformation("File saved: {FileName} -> {StoredFileName}", file.FileName, storedFileName);

            return new FileStorageResult 
            { 
                IsSuccess = true, 
                StoredFileName = storedFileName, 
                FilePath = filePath 
            };

         
        }

        // FIXED: Issue #1 & #2 - Removed try-catch, return proper result object instead of tuple and exceptions
        public async Task<FileRetrievalResult> GetFileAsync(string storedFileName)
        {
            // Input validation
            if (string.IsNullOrWhiteSpace(storedFileName))
            {
                return new FileRetrievalResult 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Stored filename is required" 
                };
            }

            // Check if upload directory exists
            if (!Directory.Exists(_uploadPath))
            {
                return new FileRetrievalResult 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Upload directory is not available" 
                };
            }

            // Find file in all user directories
            string[] userDirs;
            try
            {
                userDirs = Directory.GetDirectories(_uploadPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accessing upload directories");
                return new FileRetrievalResult 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Unable to access file storage directories" 
                };
            }

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

            if (filePath == null)
            {
                _logger.LogWarning("File not found: {StoredFileName}", storedFileName);
                return new FileRetrievalResult 
                { 
                    IsSuccess = false, 
                    ErrorMessage = $"File {storedFileName} not found" 
                };
            }

            // Read file data
            byte[] fileData;
            try
            {
                fileData = await File.ReadAllBytesAsync(filePath);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Access denied when reading file: {FilePath}", filePath);
                return new FileRetrievalResult 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Access denied - unable to read file" 
                };
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogError(ex, "File not found when reading: {FilePath}", filePath);
                return new FileRetrievalResult 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "File not found" 
                };
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "IO error when reading file: {FilePath}", filePath);
                return new FileRetrievalResult 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Unable to read file due to storage error" 
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error reading file {StoredFileName}", storedFileName);
                return new FileRetrievalResult 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "An unexpected error occurred while reading the file" 
                };
            }

            var contentType = GetContentType(storedFileName);
            var originalFileName = Path.GetFileName(filePath);

            return new FileRetrievalResult 
            { 
                IsSuccess = true, 
                FileData = fileData, 
                ContentType = contentType, 
                FileName = originalFileName 
            };

          
        }

        // FIXED: Issue #1 - Removed try-catch, return proper result object instead of bool and exceptions
        public async Task<FileOperationResult> DeleteFileAsync(string storedFileName)
        {
            // Input validation
            if (string.IsNullOrWhiteSpace(storedFileName))
            {
                return new FileOperationResult 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Stored filename is required" 
                };
            }

            // Check if upload directory exists
            if (!Directory.Exists(_uploadPath))
            {
                return new FileOperationResult 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Upload directory is not available" 
                };
            }

            // Find and delete file
            string[] userDirs;
            try
            {
                userDirs = Directory.GetDirectories(_uploadPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accessing upload directories for deletion");
                return new FileOperationResult 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Unable to access file storage directories" 
                };
            }

            foreach (var userDir in userDirs)
            {
                var filePath = Path.Combine(userDir, storedFileName);
                if (File.Exists(filePath))
                {
                    try
                    {
                        File.Delete(filePath);
                        _logger.LogInformation("File deleted: {StoredFileName}", storedFileName);
                        return new FileOperationResult { IsSuccess = true };
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        _logger.LogError(ex, "Access denied when deleting file: {FilePath}", filePath);
                        return new FileOperationResult 
                        { 
                            IsSuccess = false, 
                            ErrorMessage = "Access denied - unable to delete file" 
                        };
                    }
                    catch (IOException ex)
                    {
                        _logger.LogError(ex, "IO error when deleting file: {FilePath}", filePath);
                        return new FileOperationResult 
                        { 
                            IsSuccess = false, 
                            ErrorMessage = "Unable to delete file due to storage error" 
                        };
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Unexpected error deleting file {StoredFileName}", storedFileName);
                        return new FileOperationResult 
                        { 
                            IsSuccess = false, 
                            ErrorMessage = "An unexpected error occurred while deleting the file" 
                        };
                    }
                }
            }

            _logger.LogWarning("File not found for deletion: {StoredFileName}", storedFileName);
            return new FileOperationResult 
            { 
                IsSuccess = false, 
                ErrorMessage = "File not found" 
            };

            
            
        }

        // This method is safe as it doesn't perform any operations that can fail
        public string GetFileUrl(string storedFileName)
        {
            return $"/api/files/{storedFileName}";
        }

        private string SanitizeEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return "unknown";
                
            return email.Replace("@", "_").Replace(".", "_").Replace("/", "_").Replace("\\", "_");
        }

        private string GetContentType(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return "application/octet-stream";
                
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
}