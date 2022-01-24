using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AzureBillingExporter.AzureApi;
using DotLiquid;

namespace AzureBillingExporter.Cost
{
    public class BillingQueryClient
    {
        private readonly AzureCostManagementClient _costManagementClient;

        public BillingQueryClient(
            AzureCostManagementClient costManagementClient)
        {
            _costManagementClient = costManagementClient;
        }

        public async Task<IAsyncEnumerable<CostResultRows>> GetCustomData(CancellationToken cancel, string templateFileName)
        {
            var billingQuery = await GenerateBillingQuery(DateTime.MaxValue, DateTime.MaxValue, "None", templateFileName);
            return _costManagementClient.ExecuteBillingQuery(billingQuery, cancel);
        }

        public async Task<IAsyncEnumerable<CostResultRows>> GetDailyData(CancellationToken cancel)
        {
            var dateTimeNow = DateTime.Now;
            var dateStart = new DateTime(dateTimeNow.Year, dateTimeNow.Month, dateTimeNow.Day);
            dateStart = dateStart.AddDays(-2);
            var dateEnd = new DateTime(dateTimeNow.Year, dateTimeNow.Month, dateTimeNow.Day, 23, 59, 59);
            var granularity = "Daily";

            var billingQuery = await GenerateBillingQuery(dateStart, dateEnd, granularity);
            return _costManagementClient.ExecuteBillingQuery(billingQuery, cancel);
        }

        public async Task<IAsyncEnumerable<CostResultRows>> GetMonthlyData(CancellationToken cancel)
        {
            var dateTimeNow = DateTime.Now;

            var dateStart = new DateTime(dateTimeNow.Year, dateTimeNow.Month, 1);
            dateStart = dateStart.AddMonths(-2);
            var dateEnd = new DateTime(dateTimeNow.Year, dateTimeNow.Month, dateTimeNow.Day, 23, 59, 59);
            var granularity = "Monthly";

            var billingQuery = await GenerateBillingQuery(dateStart, dateEnd, granularity);
            return _costManagementClient.ExecuteBillingQuery(billingQuery, cancel);
        }

        private async Task<string> GenerateBillingQuery(DateTime dateStart, DateTime dateEnd, string granularity = "None", string templateFile = "./queries/get_daily_or_monthly_costs.json")
        {
            var dateTimeNow = DateTime.Now;

            var templateQuery =await File.ReadAllTextAsync(templateFile);
            var template = Template.Parse(templateQuery);

            var currentMonthStart = new DateTime(dateTimeNow.Year, dateTimeNow.Month, 1);

            var prevMonthStart = new DateTime(dateTimeNow.Year, dateTimeNow.Month, 1);
            prevMonthStart = prevMonthStart.AddMonths(-1);

            var beforePrevMonthStart = new DateTime(dateTimeNow.Year, dateTimeNow.Month, 1);
            beforePrevMonthStart = beforePrevMonthStart.AddMonths(-2);

            var todayEnd = new DateTime(dateTimeNow.Year, dateTimeNow.Month, dateTimeNow.Day, 23, 59, 59);

            var yesterdayStart = new DateTime(dateTimeNow.Year, dateTimeNow.Month, dateTimeNow.Day);
            yesterdayStart = yesterdayStart.AddDays(-1);

            var weekAgo = new DateTime(dateTimeNow.Year, dateTimeNow.Month, dateTimeNow.Day);
            weekAgo = weekAgo.AddDays(-7);

            //YearAgo
            var yearAgo = new DateTime(dateTimeNow.Year, dateTimeNow.Month, 1);
            yearAgo = yearAgo.AddYears(-1);
            yearAgo = yearAgo.AddMonths(1);

            return template.Render(Hash.FromAnonymousObject(new
            {
                DayStart = dateStart.ToString("o", CultureInfo.InvariantCulture),
                DayEnd = dateEnd.ToString("o", CultureInfo.InvariantCulture),
                CurrentMonthStart = currentMonthStart.ToString("o", CultureInfo.InvariantCulture),
                PrevMonthStart = prevMonthStart.ToString("o", CultureInfo.InvariantCulture),
                BeforePrevMonthStart = beforePrevMonthStart.ToString("o", CultureInfo.InvariantCulture),
                TodayEnd = todayEnd.ToString("o", CultureInfo.InvariantCulture),
                YesterdayStart = yesterdayStart.ToString("o", CultureInfo.InvariantCulture),
                WeekAgo = weekAgo.ToString("o", CultureInfo.InvariantCulture),
                YearAgo = yearAgo.ToString("o", CultureInfo.InvariantCulture),
                Granularity = granularity
            }));
        }
    }
}
