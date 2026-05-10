using FinBot.Domain.Models.Enums;

namespace FinBot.ExcelService.Reports;

public record ReportRequest(
    Guid UserId,
    Guid GroupId,
    ReportType ReportType,
    ExcelType ExcelType,
    TimeInterval TimeInterval);
