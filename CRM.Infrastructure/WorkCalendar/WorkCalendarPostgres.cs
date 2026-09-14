using Npgsql;

namespace CRM.Infrastructure.WorkCalendar;

internal static class WorkCalendarPostgres
{
    public static bool IsMissingRelationOrColumn(Exception ex)
    {
        for (var e = ex; e != null; e = e.InnerException)
        {
            if (e is PostgresException pg && pg.SqlState is "42703" or "42P01")
                return true;
        }

        return false;
    }
}
