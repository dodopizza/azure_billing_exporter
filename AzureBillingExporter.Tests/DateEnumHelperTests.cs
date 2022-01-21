using System;
using AzureBillingExporter.PrometheusMetrics;
using NUnit.Framework;

namespace AzureBillingExporter.Tests
{
    public class DateEnumHelperTests
    {
        [Test]
        [TestCase("2020-08-14", "2020-01-01T00:00:00","month_minus_08")]
        [TestCase("2020-08-14", "2020-03-01T00:00:00","month_minus_06")]
        [TestCase("2020-08-14", "2019-10-01T00:00:00","month_minus_11")]
        [TestCase("2020-08-14", "2020-04-01T00:00:00","month_minus_05")]
        [TestCase("2020-08-14", "2020-02-01T00:00:00","month_minus_07")]
        [TestCase("2020-08-14", "2019-11-01T00:00:00","month_minus_10")]
        [TestCase("2020-08-14", "2020-05-01T00:00:00","month_minus_04")]
        [TestCase("2020-08-14", "2019-12-01T00:00:00","month_minus_09")]
        [TestCase("2020-08-14", "2020-06-01T00:00:00","before_previous_month")]
        [TestCase("2020-08-14", "2020-08-01T00:00:00","current_month")]
        [TestCase("2020-08-14", "2020-07-01T00:00:00","previous_month")]
        [TestCase("2020-08-14", "2019-09-01T00:00:00","month_minus_12")]
        [TestCase("2020-05-01", "2020-05-01T00:00:00","current_month")]
        public void MonthDateShouldReplacedToTextEnums(string currentDate, string originalDate, string replacedString)
        {
            var today = DateTime.Parse(currentDate);
            var res = DateEnumHelper.ReplaceDateValueToEnums(originalDate, today);
            StringAssert.AreEqualIgnoringCase(replacedString, res);
        }


        [Test]
        [TestCase("2020-08-15", "20200808","day_minus_008")]
        [TestCase("2020-08-15", "20200809","day_minus_007")]
        [TestCase("2020-08-15", "20200810","day_minus_006")]
        [TestCase("2020-08-15", "20200811","day_minus_005")]
        [TestCase("2020-08-15", "20200812","day_minus_004")]
        [TestCase("2020-08-15", "20200814","yesterday")]
        [TestCase("2020-08-15", "20200815","today")]
        [TestCase("2020-08-15", "20200813","before_yesterday")]
        [TestCase("2020-08-15", "20190816","day_minus_366")]
        public void DayDateShouldReplacesToTextEnum(string currentDate, string originalDate, string replacedString)
        {
            var today = DateTime.Parse(currentDate);
            var res = DateEnumHelper.ReplaceDateValueToEnums(originalDate, today);
            StringAssert.AreEqualIgnoringCase(replacedString, res);
        }
    }
}
