namespace HRMS.Web.Helpers;

public static class FileStorageHelper
{
    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };
    private static readonly HashSet<string> AllowedDocumentExtensions = new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png" };

    public const long MaxPhotoBytes = 2 * 1024 * 1024;    // 2 MB
    public const long MaxDocumentBytes = 10 * 1024 * 1024; // 10 MB

    public static async Task<(bool Ok, string? RelativePath, string? Error)> SaveEmployeePhotoAsync(IFormFile file, string webRootPath)
    {
        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(ext) || !AllowedImageExtensions.Contains(ext))
        {
            return (false, null, "Only .jpg, .jpeg, .png, or .webp images are allowed.");
        }

        if (file.Length <= 0 || file.Length > MaxPhotoBytes)
        {
            return (false, null, "Photo size must be between 1 byte and 2 MB.");
        }

        var folder = Path.GetFullPath(Path.Combine(webRootPath, "uploads", "employees", "photos"));
        Directory.CreateDirectory(folder);

        var safeName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var fullPath = Path.GetFullPath(Path.Combine(folder, safeName));
        if (!fullPath.StartsWith(folder + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            return (false, null, "Invalid storage path.");
        }

        await using var stream = new FileStream(fullPath, FileMode.CreateNew);
        await file.CopyToAsync(stream);

        return (true, $"/uploads/employees/photos/{safeName}", null);
    }

    public static async Task<(bool Ok, string? StoredFileName, string? Error)> SaveEmployeeDocumentAsync(IFormFile file, string contentRootPath)
    {
        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(ext) || !AllowedDocumentExtensions.Contains(ext))
        {
            return (false, null, "Only .pdf, .doc, .docx, .jpg, .jpeg, or .png files are allowed.");
        }

        if (file.Length <= 0 || file.Length > MaxDocumentBytes)
        {
            return (false, null, "Document size must be between 1 byte and 10 MB.");
        }

        var folder = Path.GetFullPath(Path.Combine(contentRootPath, "App_Data", "EmployeeDocuments"));
        Directory.CreateDirectory(folder);

        var safeName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var fullPath = Path.GetFullPath(Path.Combine(folder, safeName));
        if (!fullPath.StartsWith(folder + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            return (false, null, "Invalid document path.");
        }

        await using var stream = new FileStream(fullPath, FileMode.CreateNew);
        await file.CopyToAsync(stream);

        return (true, safeName, null);
    }

    public static string? ResolveEmployeeDocumentPath(string storedFileName, string contentRootPath)
    {
        if (string.IsNullOrWhiteSpace(storedFileName)) return null;
        var baseDir = Path.GetFullPath(Path.Combine(contentRootPath, "App_Data", "EmployeeDocuments"));
        var candidate = Path.GetFullPath(Path.Combine(baseDir, Path.GetFileName(storedFileName)));
        return candidate.StartsWith(baseDir + Path.DirectorySeparatorChar, StringComparison.Ordinal) && File.Exists(candidate)
            ? candidate
            : null;
    }

    public static async Task<(bool Ok, string? StoredFileName, string? Error)> SaveLeaveAttachmentAsync(IFormFile file, string contentRootPath)
    {
        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(ext) || !AllowedDocumentExtensions.Contains(ext))
        {
            return (false, null, "Only .pdf, .doc, .docx, .jpg, .jpeg, or .png attachments are allowed.");
        }

        if (file.Length <= 0 || file.Length > 5 * 1024 * 1024)
        {
            return (false, null, "Supporting attachment must be between 1 byte and 5 MB.");
        }

        var folder = Path.GetFullPath(Path.Combine(contentRootPath, "App_Data", "LeaveAttachments"));
        Directory.CreateDirectory(folder);

        var safeName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var fullPath = Path.GetFullPath(Path.Combine(folder, safeName));
        if (!fullPath.StartsWith(folder + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            return (false, null, "Invalid storage path.");
        }

        await using var stream = new FileStream(fullPath, FileMode.CreateNew);
        await file.CopyToAsync(stream);

        return (true, safeName, null);
    }

    public static string? ResolveLeaveAttachmentPath(string storedFileName, string contentRootPath)
    {
        if (string.IsNullOrWhiteSpace(storedFileName)) return null;
        var baseDir = Path.GetFullPath(Path.Combine(contentRootPath, "App_Data", "LeaveAttachments"));
        var candidate = Path.GetFullPath(Path.Combine(baseDir, Path.GetFileName(storedFileName)));
        return candidate.StartsWith(baseDir + Path.DirectorySeparatorChar, StringComparison.Ordinal) && File.Exists(candidate)
            ? candidate
            : null;
    }

    public static async Task<(bool Ok, string? StoredFileName, string? Error)> SaveCandidateResumeAsync(IFormFile file, string contentRootPath)
    {
        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(ext) || ext is not (".pdf" or ".doc" or ".docx" or ".PDF" or ".DOC" or ".DOCX"))
        {
            return (false, null, "Only .pdf, .doc, or .docx resumes are allowed.");
        }

        if (file.Length <= 0 || file.Length > 5 * 1024 * 1024)
        {
            return (false, null, "Resume file size must be between 1 byte and 5 MB.");
        }

        var folder = Path.GetFullPath(Path.Combine(contentRootPath, "App_Data", "CandidateResumes"));
        Directory.CreateDirectory(folder);

        var safeName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var fullPath = Path.GetFullPath(Path.Combine(folder, safeName));
        if (!fullPath.StartsWith(folder + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            return (false, null, "Invalid storage path.");
        }

        await using var stream = new FileStream(fullPath, FileMode.CreateNew);
        await file.CopyToAsync(stream);

        return (true, safeName, null);
    }

    public static string? ResolveCandidateResumePath(string storedFileName, string contentRootPath)
    {
        if (string.IsNullOrWhiteSpace(storedFileName)) return null;
        var baseDir = Path.GetFullPath(Path.Combine(contentRootPath, "App_Data", "CandidateResumes"));
        var candidate = Path.GetFullPath(Path.Combine(baseDir, Path.GetFileName(storedFileName)));
        return candidate.StartsWith(baseDir + Path.DirectorySeparatorChar, StringComparison.Ordinal) && File.Exists(candidate)
            ? candidate
            : null;
    }

    public static async Task<(bool Ok, string? StoredFileName, string? Error)> SaveExpenseReceiptAsync(IFormFile file, string contentRootPath)
    {
        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(ext) || !AllowedDocumentExtensions.Contains(ext))
        {
            return (false, null, "Only .pdf, .doc, .docx, .jpg, .jpeg, or .png receipts are allowed.");
        }

        if (file.Length <= 0 || file.Length > 5 * 1024 * 1024)
        {
            return (false, null, "Receipt file size must be between 1 byte and 5 MB.");
        }

        var folder = Path.GetFullPath(Path.Combine(contentRootPath, "App_Data", "ExpenseReceipts"));
        Directory.CreateDirectory(folder);

        var safeName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var fullPath = Path.GetFullPath(Path.Combine(folder, safeName));
        if (!fullPath.StartsWith(folder + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            return (false, null, "Invalid storage path.");
        }

        await using var stream = new FileStream(fullPath, FileMode.CreateNew);
        await file.CopyToAsync(stream);

        return (true, safeName, null);
    }

    private static readonly HashSet<string> AllowedCompanyDocExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".jpg", ".jpeg", ".png" };

    public const long MaxCompanyDocBytes = 15 * 1024 * 1024; // 15 MB

    public static async Task<(bool Ok, string? StoredFileName, string? Error)> SaveCompanyDocumentAsync(IFormFile file, string contentRootPath)
    {
        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(ext) || !AllowedCompanyDocExtensions.Contains(ext))
        {
            return (false, null, "Only .pdf, .doc, .docx, .xls, .xlsx, .ppt, .pptx, .txt, .jpg, .jpeg, or .png files are allowed.");
        }

        if (file.Length <= 0 || file.Length > MaxCompanyDocBytes)
        {
            return (false, null, "Company document size must be between 1 byte and 15 MB.");
        }

        var folder = Path.GetFullPath(Path.Combine(contentRootPath, "App_Data", "CompanyDocuments"));
        Directory.CreateDirectory(folder);

        var safeName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var fullPath = Path.GetFullPath(Path.Combine(folder, safeName));
        if (!fullPath.StartsWith(folder + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            return (false, null, "Invalid storage path.");
        }

        await using var stream = new FileStream(fullPath, FileMode.CreateNew);
        await file.CopyToAsync(stream);

        return (true, safeName, null);
    }

    public static string? ResolveCompanyDocumentPath(string storedFileName, string contentRootPath)
    {
        if (string.IsNullOrWhiteSpace(storedFileName)) return null;
        var baseDir = Path.GetFullPath(Path.Combine(contentRootPath, "App_Data", "CompanyDocuments"));
        var candidate = Path.GetFullPath(Path.Combine(baseDir, Path.GetFileName(storedFileName)));
        return candidate.StartsWith(baseDir + Path.DirectorySeparatorChar, StringComparison.Ordinal) && File.Exists(candidate)
            ? candidate
            : null;
    }

    public static string? ResolveExpenseReceiptPath(string storedFileName, string contentRootPath)
    {
        if (string.IsNullOrWhiteSpace(storedFileName)) return null;
        var baseDir = Path.GetFullPath(Path.Combine(contentRootPath, "App_Data", "ExpenseReceipts"));
        var candidate = Path.GetFullPath(Path.Combine(baseDir, Path.GetFileName(storedFileName)));
        return candidate.StartsWith(baseDir + Path.DirectorySeparatorChar, StringComparison.Ordinal) && File.Exists(candidate)
            ? candidate
            : null;
    }
}
