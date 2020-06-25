using System;
using System.Globalization;

internal static class DateEnumHelper
{
    public static string ReplaceDateValueToEnums(string dataColumnByKeyLabel)
    {
            
        var now = DateTime.Now;

        var currentMonthStart = new DateTime(now.Year, now.Month, 1);
        if (dataColumnByKeyLabel == currentMonthStart.ToString("MM/01/yyyy 00:00:00", CultureInfo.InvariantCulture))
        {
            return "current_month";
        }
            
        var lastMonthStart = new DateTime(now.Year, now.Month, 1);
        lastMonthStart = lastMonthStart.AddMonths(-1);
        if (dataColumnByKeyLabel == lastMonthStart.ToString("MM/01/yyyy 00:00:00", CultureInfo.InvariantCulture))
        {
            return "last_month";
        }

        // "20200624" -> "today"
        var today = new DateTime(now.Year, now.Month, now.Day);
        if (dataColumnByKeyLabel == today.ToString("yyyyMMdd", CultureInfo.InvariantCulture))
        {
            return "today";
        }
            
        var yesterday = new DateTime(now.Year, now.Month, now.Day);
        yesterday = yesterday.AddDays(-1);
        if (dataColumnByKeyLabel == yesterday.ToString("yyyyMMdd", CultureInfo.InvariantCulture))
        {
            return "yesterday";
        }
            
        var beforeYesterday = new DateTime(now.Year, now.Month, now.Day);
        beforeYesterday = beforeYesterday.AddDays(-2);
        if (dataColumnByKeyLabel == beforeYesterday.ToString("yyyyMMdd", CultureInfo.InvariantCulture))
        {
            return "before_yesterday";
        }
            
        return dataColumnByKeyLabel;
    }
}