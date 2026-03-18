using System.ComponentModel.DataAnnotations;

namespace managerCMN.Helpers;

public static class FileUploadHelper
{
    // File upload constants
    public const int MaxFileSize = 5 * 1024 * 1024; // 5MB trong bytes
    public const int MaxFileCount = 2; // Tối đa 2 files

    // Allowed file extensions
    public static readonly string[] AllowedExcelExtensions = { ".xlsx", ".xls" };
    public static readonly string[] AllowedDocumentExtensions = { ".pdf", ".doc", ".docx", ".txt" };
    public static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
    public static readonly string[] AllowedAllExtensions = AllowedExcelExtensions
        .Concat(AllowedDocumentExtensions)
        .Concat(AllowedImageExtensions)
        .ToArray();

    /// <summary>
    /// Validate single file upload
    /// </summary>
    public static ValidationResult ValidateFile(IFormFile? file, string[] allowedExtensions, bool isRequired = false)
    {
        if (file == null || file.Length == 0)
        {
            if (isRequired)
                return new ValidationResult("Vui lòng chọn file để upload.");
            return ValidationResult.Success;
        }

        // Check file size
        if (file.Length > MaxFileSize)
        {
            var maxSizeMB = MaxFileSize / (1024 * 1024);
            return new ValidationResult($"File '{file.FileName}' vượt quá giới hạn {maxSizeMB}MB cho phép.");
        }

        // Check file extension
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(fileExtension))
        {
            var allowedList = string.Join(", ", allowedExtensions);
            return new ValidationResult($"File '{file.FileName}' có định dạng không được hỗ trợ. Chỉ cho phép: {allowedList}");
        }

        // Check for potentially dangerous files
        if (IsPotentiallyDangerousFile(file))
        {
            return new ValidationResult($"File '{file.FileName}' chứa nội dung không an toàn.");
        }

        return ValidationResult.Success;
    }

    /// <summary>
    /// Validate multiple file uploads
    /// </summary>
    public static ValidationResult ValidateFiles(IList<IFormFile>? files, string[] allowedExtensions, bool isRequired = false)
    {
        if (files == null || files.Count == 0)
        {
            if (isRequired)
                return new ValidationResult("Vui lòng chọn ít nhất một file để upload.");
            return ValidationResult.Success;
        }

        // Check file count
        if (files.Count > MaxFileCount)
        {
            return new ValidationResult($"Chỉ được upload tối đa {MaxFileCount} files. Bạn đã chọn {files.Count} files.");
        }

        // Validate each file
        for (int i = 0; i < files.Count; i++)
        {
            var result = ValidateFile(files[i], allowedExtensions);
            if (result != ValidationResult.Success)
            {
                return result;
            }
        }

        // Check total size of all files
        var totalSize = files.Sum(f => f.Length);
        var maxTotalSize = MaxFileSize * MaxFileCount;
        if (totalSize > maxTotalSize)
        {
            var maxTotalMB = maxTotalSize / (1024 * 1024);
            return new ValidationResult($"Tổng dung lượng các files vượt quá {maxTotalMB}MB cho phép.");
        }

        return ValidationResult.Success;
    }

    /// <summary>
    /// Check for potentially dangerous file content
    /// </summary>
    private static bool IsPotentiallyDangerousFile(IFormFile file)
    {
        // Check file header/magic bytes để detect file thật
        // Implement thêm security checks nếu cần

        // Basic check: file không được rỗng hoặc chỉ có whitespace
        if (file.Length == 0)
            return true;

        // Check filename cho dangerous patterns
        var fileName = file.FileName.ToLowerInvariant();
        var dangerousExtensions = new[] { ".exe", ".bat", ".cmd", ".com", ".scr", ".vbs", ".js", ".jar" };

        foreach (var ext in dangerousExtensions)
        {
            if (fileName.EndsWith(ext))
                return true;
        }

        // Check cho script injection trong filename
        if (fileName.Contains("<script") || fileName.Contains("javascript:"))
            return true;

        return false;
    }

    /// <summary>
    /// Generate secure filename
    /// </summary>
    public static string GenerateSecureFileName(string originalFileName, string? prefix = null)
    {
        var extension = Path.GetExtension(originalFileName);
        var nameWithoutExt = Path.GetFileNameWithoutExtension(originalFileName);

        // Sanitize filename
        var sanitizedName = string.Join("_", nameWithoutExt.Split(Path.GetInvalidFileNameChars()));

        // Limit length
        if (sanitizedName.Length > 50)
            sanitizedName = sanitizedName.Substring(0, 50);

        // Add timestamp để avoid conflicts
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var secureFileName = $"{prefix ?? "file"}_{timestamp}_{sanitizedName}{extension}";

        return secureFileName;
    }

    /// <summary>
    /// Get file size in human readable format
    /// </summary>
    public static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }
}