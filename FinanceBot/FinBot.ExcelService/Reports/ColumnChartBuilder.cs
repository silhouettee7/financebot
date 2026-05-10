using FinBot.Domain.Models;
using FinBot.Domain.Models.Enums;
using ScottPlot;

namespace FinBot.ExcelService.Reports;

public class ColumnChartBuilder : IReportBuilder
{
    public ExcelType Type => ExcelType.ColumnChart;
    public string ContentType => "image/png";

    public byte[] Build(IReadOnlyList<Expense> expenses, ReportRequest request)
    {
        var grouped = expenses
            .GroupBy(e => e.Category)
            .Select(g => new { Category = g.Key.ToString(), Total = (double)g.Sum(e => e.Amount) })
            .OrderByDescending(g => g.Total)
            .ToList();

        var plot = new Plot();
        plot.Title("Расходы по категориям");
        plot.YLabel("Сумма, ₽");

        var bars = grouped
            .Select((g, i) => new Bar { Position = i, Value = g.Total, Label = g.Total.ToString("N2") })
            .ToList();

        plot.Add.Bars(bars);

        var positions = Enumerable.Range(0, grouped.Count).Select(i => (double)i).ToArray();
        var labels = grouped.Select(g => g.Category).ToArray();
        plot.Axes.Bottom.SetTicks(positions, labels);
        plot.Axes.Margins(bottom: 0);

        return plot.GetImageBytes(900, 500, ImageFormat.Png);
    }
}
