// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// File Upload Validation - Client Side
(function() {
    'use strict';

    // Constants matching server-side FileUploadHelper
    const MAX_FILE_SIZE = 5 * 1024 * 1024; // 5MB in bytes
    const MAX_FILE_COUNT = 2; // Maximum 2 files

    // Allowed extensions
    const ALLOWED_EXCEL_EXTENSIONS = ['.xlsx', '.xls'];
    const ALLOWED_DOCUMENT_EXTENSIONS = ['.pdf', '.doc', '.docx', '.txt'];
    const ALLOWED_IMAGE_EXTENSIONS = ['.jpg', '.jpeg', '.png', '.gif', '.bmp'];
    const ALLOWED_REQUEST_EXTENSIONS = ['.pdf', '.doc', '.docx', '.jpg', '.jpeg', '.png', '.gif', '.txt'];

    // Dangerous extensions to block
    const DANGEROUS_EXTENSIONS = ['.exe', '.bat', '.cmd', '.com', '.scr', '.vbs', '.js', '.jar'];

    // Utility functions
    function formatFileSize(bytes) {
        const sizes = ['B', 'KB', 'MB', 'GB'];
        if (bytes === 0) return '0 B';
        const i = Math.floor(Math.log(bytes) / Math.log(1024));
        return Math.round(bytes / Math.pow(1024, i) * 100) / 100 + ' ' + sizes[i];
    }

    function getFileExtension(fileName) {
        return '.' + fileName.split('.').pop().toLowerCase();
    }

    function isAllowedExtension(fileName, allowedExtensions) {
        const extension = getFileExtension(fileName);
        return allowedExtensions.includes(extension);
    }

    function isDangerousFile(fileName) {
        const extension = getFileExtension(fileName);
        return DANGEROUS_EXTENSIONS.includes(extension);
    }

    function validateSingleFile(file, allowedExtensions, isRequired = false) {
        const errors = [];

        if (!file) {
            if (isRequired) {
                errors.push('Vui lòng chọn file để upload.');
            }
            return errors;
        }

        // Check file size
        if (file.size > MAX_FILE_SIZE) {
            errors.push(`File '${file.name}' vượt quá giới hạn ${formatFileSize(MAX_FILE_SIZE)} cho phép.`);
        }

        // Check dangerous files
        if (isDangerousFile(file.name)) {
            errors.push(`File '${file.name}' chứa nội dung không an toàn.`);
        }

        // Check file extension
        if (!isAllowedExtension(file.name, allowedExtensions)) {
            errors.push(`File '${file.name}' có định dạng không được hỗ trợ. Chỉ cho phép: ${allowedExtensions.join(', ')}`);
        }

        return errors;
    }

    function validateMultipleFiles(files, allowedExtensions, isRequired = false) {
        const errors = [];

        if (!files || files.length === 0) {
            if (isRequired) {
                errors.push('Vui lòng chọn ít nhất một file để upload.');
            }
            return errors;
        }

        // Check file count
        if (files.length > MAX_FILE_COUNT) {
            errors.push(`Chỉ được upload tối đa ${MAX_FILE_COUNT} files. Bạn đã chọn ${files.length} files.`);
        }

        // Validate each file
        for (let i = 0; i < files.length; i++) {
            const fileErrors = validateSingleFile(files[i], allowedExtensions, false);
            errors.push(...fileErrors);
        }

        // Check total size
        const totalSize = Array.from(files).reduce((sum, file) => sum + file.size, 0);
        const maxTotalSize = MAX_FILE_SIZE * MAX_FILE_COUNT;
        if (totalSize > maxTotalSize) {
            errors.push(`Tổng dung lượng các files vượt quá ${formatFileSize(maxTotalSize)} cho phép.`);
        }

        return errors;
    }

    function displayErrors(errorContainer, errors) {
        if (!errorContainer) return;

        errorContainer.innerHTML = '';
        if (errors.length > 0) {
            errorContainer.style.display = 'block';
            errorContainer.innerHTML = errors.map(error =>
                `<div class="alert alert-danger alert-sm mb-1">${error}</div>`
            ).join('');
        } else {
            errorContainer.style.display = 'none';
        }
    }

    // Setup file validation for different types of inputs
    function setupFileValidation() {
        // Excel file inputs (Asset Import, Employee Import, Attendance Import)
        const excelInputs = document.querySelectorAll('input[type="file"][name*="excel"], input[type="file"][name*="Excel"], input[type="file"][name*="excelFile"], input[type="file"][name*="ExcelFile"]');
        excelInputs.forEach(input => {
            const errorContainer = document.createElement('div');
            errorContainer.className = 'file-validation-errors';
            errorContainer.style.display = 'none';
            input.parentNode.insertBefore(errorContainer, input.nextSibling);

            input.addEventListener('change', function() {
                const errors = validateSingleFile(this.files[0], ALLOWED_EXCEL_EXTENSIONS, true);
                displayErrors(errorContainer, errors);
            });
        });

        // Contract file inputs
        const contractInputs = document.querySelectorAll('input[type="file"][name*="contract"], input[type="file"][name*="Contract"]');
        contractInputs.forEach(input => {
            const errorContainer = document.createElement('div');
            errorContainer.className = 'file-validation-errors';
            errorContainer.style.display = 'none';
            input.parentNode.insertBefore(errorContainer, input.nextSibling);

            input.addEventListener('change', function() {
                const errors = validateSingleFile(this.files[0], ALLOWED_DOCUMENT_EXTENSIONS, false);
                displayErrors(errorContainer, errors);
            });
        });

        // Request attachment inputs (multiple files)
        const attachmentInputs = document.querySelectorAll('input[type="file"][name*="attachment"], input[type="file"][name*="Attachment"]');
        attachmentInputs.forEach(input => {
            const errorContainer = document.createElement('div');
            errorContainer.className = 'file-validation-errors';
            errorContainer.style.display = 'none';
            input.parentNode.insertBefore(errorContainer, input.nextSibling);

            input.addEventListener('change', function() {
                const errors = validateMultipleFiles(this.files, ALLOWED_REQUEST_EXTENSIONS, false);
                displayErrors(errorContainer, errors);
            });
        });

        // Generic file inputs - try to determine allowed extensions from accept attribute
        const allFileInputs = document.querySelectorAll('input[type="file"]:not([data-validated])');
        allFileInputs.forEach(input => {
            input.setAttribute('data-validated', 'true');

            let allowedExtensions = ALLOWED_DOCUMENT_EXTENSIONS; // Default

            // Try to determine allowed extensions from accept attribute or name
            if (input.accept) {
                if (input.accept.includes('excel') || input.accept.includes('spreadsheet')) {
                    allowedExtensions = ALLOWED_EXCEL_EXTENSIONS;
                } else if (input.accept.includes('image')) {
                    allowedExtensions = ALLOWED_IMAGE_EXTENSIONS;
                }
            }

            const errorContainer = document.createElement('div');
            errorContainer.className = 'file-validation-errors';
            errorContainer.style.display = 'none';
            input.parentNode.insertBefore(errorContainer, input.nextSibling);

            input.addEventListener('change', function() {
                const isMultiple = this.hasAttribute('multiple');
                const isRequired = this.hasAttribute('required');

                let errors;
                if (isMultiple) {
                    errors = validateMultipleFiles(this.files, allowedExtensions, isRequired);
                } else {
                    errors = validateSingleFile(this.files[0], allowedExtensions, isRequired);
                }

                displayErrors(errorContainer, errors);
            });
        });
    }

    // Prevent form submission if there are file validation errors
    function preventSubmitOnErrors() {
        document.addEventListener('submit', function(e) {
            const form = e.target;
            const errorContainers = form.querySelectorAll('.file-validation-errors');
            let hasErrors = false;

            errorContainers.forEach(container => {
                if (container.style.display !== 'none' && container.innerHTML.trim() !== '') {
                    hasErrors = true;
                }
            });

            if (hasErrors) {
                e.preventDefault();
                alert('Vui lòng khắc phục các lỗi file upload trước khi gửi form.');
                return false;
            }
        });
    }

    // Initialize when DOM is ready
    document.addEventListener('DOMContentLoaded', function() {
        setupFileValidation();
        preventSubmitOnErrors();
    });

    // Re-initialize when content is dynamically loaded
    document.addEventListener('DOMNodeInserted', function() {
        // Debounce to avoid excessive calls
        clearTimeout(window.fileValidationTimeout);
        window.fileValidationTimeout = setTimeout(setupFileValidation, 100);
    });

    // Export functions for manual use if needed
    window.FileUploadValidator = {
        validateSingleFile: validateSingleFile,
        validateMultipleFiles: validateMultipleFiles,
        setupFileValidation: setupFileValidation,
        MAX_FILE_SIZE: MAX_FILE_SIZE,
        MAX_FILE_COUNT: MAX_FILE_COUNT
    };
})();
