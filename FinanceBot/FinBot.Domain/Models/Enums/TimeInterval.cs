namespace FinBot.Domain.Models.Enums;

/// <summary>
/// Период, за который строится отчёт. Берётся предыдущий завершённый период
/// </summary>
public enum TimeInterval
{
    /// <summary>
    /// Предыдущий календарный день
    /// </summary>
    Day,

    /// <summary>
    /// Предыдущая ISO-неделя
    /// </summary>
    Week,

    /// <summary>
    /// Предыдущий календарный месяц.
    ///</summary>
    Month
}