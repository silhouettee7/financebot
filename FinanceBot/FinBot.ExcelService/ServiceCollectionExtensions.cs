using FinBot.ExcelService.Reports;
using FinBot.ExcelService.Repositories;
using FinBot.ExcelService.Services;
using FinBot.MinIOS3;
using OfficeOpenXml;

namespace FinBot.ExcelService;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddExcelReportPipeline(this IServiceCollection services,
        IConfiguration configuration)
    {
        ExcelPackage.License.SetNonCommercialPersonal("FinBot");

        services.Configure<StorageOptions>(configuration.GetSection("Storage"));

        services.PostConfigure<MinioOptions>(minio =>
        {
            var storage = configuration.GetSection("Storage").Get<StorageOptions>()
                          ?? throw new InvalidOperationException("Storage section is missing");

            minio.Buckets =
            [
                storage.ExcelTablesBucket,
                storage.BarChartsBucket,
                storage.LineChartsBucket
            ];
        });

        services.AddScoped<IExpenseRepository, ExpenseRepository>();

        services.AddSingleton<IReportBuilder, ExcelTableBuilder>();
        services.AddSingleton<IReportBuilder, ColumnChartBuilder>();
        services.AddSingleton<IReportBuilder, LineChartBuilder>();
        services.AddSingleton<IReportBuilderFactory, ReportBuilderFactory>();

        services.AddScoped<IReportService, ReportService>();

        services.TryAddSingletonTimeProvider();

        return services;
    }

    private static void TryAddSingletonTimeProvider(this IServiceCollection services)
    {
        if (services.Any(d => d.ServiceType == typeof(TimeProvider))) return;
        services.AddSingleton(TimeProvider.System);
    }
}