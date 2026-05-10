using FinBot.Domain.Models;
using FinBot.Domain.Models.Enums;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace FinBot.ExcelService.Reports;

public class ExcelTableBuilder : IReportBuilder
{
    public ExcelType Type => ExcelType.ExcelTable;
    public string ContentType => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public byte[] Build(IReadOnlyList<Expense> expenses, ReportRequest request)
    {
        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("Расходы");

        sheet.Cells[1, 1].Value = "Дата";
        sheet.Cells[1, 2].Value = "Категория";
        sheet.Cells[1, 3].Value = "Сумма";

        using (var header = sheet.Cells[1, 1, 1, 3])
        {
            header.Style.Font.Bold = true;
            header.Style.Fill.PatternType = ExcelFillStyle.Solid;
            header.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
        }

        var row = 2;
        foreach (var expense in expenses.OrderBy(e => e.Date))
        {
            sheet.Cells[row, 1].Value = expense.Date;
            sheet.Cells[row, 1].Style.Numberformat.Format = "dd.MM.yyyy";
            sheet.Cells[row, 2].Value = expense.Category.ToString();
            sheet.Cells[row, 3].Value = expense.Amount;
            sheet.Cells[row, 3].Style.Numberformat.Format = "#,##0.00 ₽";
            row++;
        }

        sheet.Cells[row, 2].Value = "Итого";
        sheet.Cells[row, 2].Style.Font.Bold = true;
        sheet.Cells[row, 3].Formula = $"SUM(C2:C{row - 1})";
        sheet.Cells[row, 3].Style.Numberformat.Format = "#,##0.00 ₽";
        sheet.Cells[row, 3].Style.Font.Bold = true;

        sheet.Cells[1, 1, row, 3].AutoFitColumns();

        return package.GetAsByteArray();
    }
}
