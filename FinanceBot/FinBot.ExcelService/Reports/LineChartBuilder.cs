using FinBot.Domain.Models;
using FinBot.Domain.Models.Enums;
using ScottPlot;

namespace FinBot.ExcelService.Reports;

public class LineChartBuilder : IReportBuilder
{
    public ExcelType Type => ExcelType.LineChart;
    public string ContentType => "image/png";

    public byte[] Build(IReadOnlyList<Expense> expenses, ReportRequest request)
    {
        var grouped = expenses
            .GroupBy(e => e.Date.Date)
            .Select(g => new { Date = g.Key, Total = (double)g.Sum(e => e.Amount) })
            .OrderBy(g => g.Date)
            .ToList();

        var plot = new Plot();
        plot.Title("Динамика трат по дням");
        plot.YLabel("Сумма, ₽");

        var xs = grouped.Select(g => g.Date.ToOADate()).ToArray();
        var ys = grouped.Select(g => g.Total).ToArray();

        var scatter = plot.Add.Scatter(xs, ys);
        scatter.MarkerSize = 6;
        scatter.LineWidth = 2;

        plot.Axes.DateTimeTicksBottom();

        return plot.GetImageBytes(1000, 500, ImageFormat.Png);
    }
}
