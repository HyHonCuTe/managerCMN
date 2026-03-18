using System.ComponentModel.DataAnnotations;
using managerCMN.Helpers;

namespace managerCMN.Attributes;

/// <summary>
/// Validation attribute cho single file upload
/// </summary>
public class ValidateFileAttribute : ValidationAttribute
{
    private readonly string[] _allowedExtensions;
    private readonly bool _isRequired;

    public ValidateFileAttribute(string allowedExtensions, bool isRequired = false)
    {
        _allowedExtensions = allowedExtensions.Split(',').Select(ext => ext.Trim().ToLowerInvariant()).ToArray();
        _isRequired = isRequired;
    }

    public override bool IsValid(object? value)
    {
        var file = value as IFormFile;
        var result = FileUploadHelper.ValidateFile(file, _allowedExtensions, _isRequired);

        if (result != ValidationResult.Success)
        {
            ErrorMessage = result.ErrorMessage;
            return false;
        }

        return true;
    }
}

/// <summary>
/// Validation attribute cho multiple files upload
/// </summary>
public class ValidateFilesAttribute : ValidationAttribute
{
    private readonly string[] _allowedExtensions;
    private readonly bool _isRequired;

    public ValidateFilesAttribute(string allowedExtensions, bool isRequired = false)
    {
        _allowedExtensions = allowedExtensions.Split(',').Select(ext => ext.Trim().ToLowerInvariant()).ToArray();
        _isRequired = isRequired;
    }

    public override bool IsValid(object? value)
    {
        var files = value as IList<IFormFile>;
        var result = FileUploadHelper.ValidateFiles(files, _allowedExtensions, _isRequired);

        if (result != ValidationResult.Success)
        {
            ErrorMessage = result.ErrorMessage;
            return false;
        }

        return true;
    }
}