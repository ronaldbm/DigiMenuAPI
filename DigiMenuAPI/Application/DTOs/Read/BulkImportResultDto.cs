namespace DigiMenuAPI.Application.DTOs.Read
{
    /// <summary>Result of a bulk import operation.</summary>
    public class BulkImportResultDto
    {
        public int CreatedCount { get; init; }
        public int ReactivatedCount { get; init; }
        public int SkippedCount { get; init; }
        public List<BulkImportRowError> Errors { get; init; } = [];
        public List<BulkImportRowWarning> Warnings { get; init; } = [];
    }

    /// <summary>Validation error for a specific row — blocks import.</summary>
    public class BulkImportRowError
    {
        public int Row { get; init; }
        public string Field { get; init; } = string.Empty;
        public string ErrorKey { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
    }

    /// <summary>Non-blocking warning for a specific row (e.g. image not found).</summary>
    public class BulkImportRowWarning
    {
        public int Row { get; init; }
        public string Field { get; init; } = string.Empty;
        public string WarningKey { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
    }

    /// <summary>CSV template headers for a specific import type.</summary>
    public class CsvTemplateDto
    {
        public List<string> Headers { get; init; } = [];
        public string ImportType { get; init; } = string.Empty;
    }
}
