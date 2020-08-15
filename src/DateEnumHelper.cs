using System;
using System.Globalization;

public static class DateEnumHelper
{
    public static string ReplaceDateValueToEnums(string originalDate, DateTime? nowDate = null)
    {
        var todayDate = nowDate ?? DateTime.Now;

        //=================================================
        //                        Month
        //=================================================
        var currentMonthStart = new DateTime(todayDate.Year, todayDate.Month, 1);
        if (originalDate == currentMonthStart.ToString("yyyy-MM-01T00:00:00", CultureInfo.InvariantCulture))
        {
            return "current_month";
        }

        var previousMonthStart = new DateTime(todayDate.Year, todayDate.Month, 1);
        previousMonthStart = previousMonthStart.AddMonths(-1);
        if (originalDate == previousMonthStart.ToString("yyyy-MM-01T00:00:00", CultureInfo.InvariantCulture))
        {
            return "previous_month";
        }

        var beforePreviousMonthStart = new DateTime(todayDate.Year, todayDate.Month, 1);
        beforePreviousMonthStart = beforePreviousMonthStart.AddMonths(-2);
        if (originalDate == beforePreviousMonthStart.ToString("yyyy-MM-01T00:00:00", CultureInfo.InvariantCulture))
        {
            return "before_previous_month";
        }

        for (int i = 0; i < 12; i++)
        {
            var firstDayTodayMonth = new DateTime(todayDate.Year, todayDate.Month, 1);
            var monthMinus = firstDayTodayMonth.AddMonths(-i);

            if (originalDate == monthMinus.ToString("yyyy-MM-01T00:00:00", CultureInfo.InvariantCulture))
            {
                return $"month_minus_{i+1:D2}";
            }
        }
        //=================================================

        //=================================================
        //                        Day
        //=================================================
        // "20200624" -> "today"
        if (originalDate == todayDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture))
        {
            return "today";
        }

        var yesterday = new DateTime(todayDate.Year, todayDate.Month, todayDate.Day);
        yesterday = yesterday.AddDays(-1);
        if (originalDate == yesterday.ToString("yyyyMMdd", CultureInfo.InvariantCulture))
        {
            return "yesterday";
        }

        var beforeYesterday = new DateTime(todayDate.Year, todayDate.Month, todayDate.Day);
        beforeYesterday = beforeYesterday.AddDays(-2);
        if (originalDate == beforeYesterday.ToString("yyyyMMdd", CultureInfo.InvariantCulture))
        {
            return "before_yesterday";
        }

        for (int i = 0; i < 367; i++)
        {
            var thisDayDate = new DateTime(todayDate.Year, todayDate.Month, todayDate.Day);
            var dayMinus = thisDayDate.AddDays(-i);

            if (originalDate == dayMinus.ToString("yyyyMMdd", CultureInfo.InvariantCulture))
            {
                return $"day_minus_{i+1:D3}";
            }
        }

        return originalDate;
        //=================================================
    }
}
