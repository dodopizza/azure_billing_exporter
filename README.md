# Azure Billing Exporter

![Build](https://github.com/dodopizza/azure_billing_exporter/workflows/Build/badge.svg?branch=master)
[![Docker Pulls](https://img.shields.io/docker/pulls/pritchinpavel/azure_billing_exporter)](https://hub.docker.com/r/pritchinpavel/azure_billing_exporter)

# Install

## Docker images

```bash
docker run\
        --env ApiSettings__SubscriptionId='YOUR_SUBSCRIPTION_ID'\
        --env ApiSettings__TenantId='YOUR_TENANT_ID'\
        --env ApiSettings__ClientId='YOUR_CLIENT_ID'\
        --env ApiSettings__ClientSecret='CLIENT_SECRET_SP'\
        -v /PATH_TO_REPO/custom_queries/:/app/custom_queries/\
        -v /PATH_TO_REPO/custom_collectors.yml:/app/custom_collectors.yml\
        -p 9301:80 azure_billing_exporter:latest

```

# How to run locally

1. Create ServicePrincipal
This SP should have access as Billing reader role [see Manage billing access](https://docs.microsoft.com/en-us/azure/cost-management-billing/manage/manage-billing-access)

2. Set configuration

2.1. Environment Variables

```bash
    EXPORT ApiSettings__SubscriptionId="YOUR_SUBSCRIPTION_ID"
    EXPORT ApiSettings__TenantId="YOUR_TENANT_ID"
    EXPORT ApiSettings__ClientId="YOUR_CLIENT_ID"
    EXPORT ApiSettings__ClientSecret="CLIENT_SECRET_SP"
```

2.2. `appsettings.json`
Using for local developing

```json
  "ApiSettings": {
    "SubscriptionId": "YOUR_SUBSCRIPTION_ID", 
    "TenantId": "YOUR_TENANT_ID", 
    "ClientId": "YOUR_CLIENT_ID", 
    "ClientSecret": "CLIENT_SECRET_SP"
  },
```

2.3 Tracing logs

For trace all billing query and response set log level to trace info `appsettings.Development.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Trace",   
```

3. Install dotnet SDK
Download and install .NET Core 3.1 SDK or above 
<https://dotnet.microsoft.com/download/dotnet-core/3.1>


4. Run dotnet

```bash
dotnet run --project AzureBillingExporter/AzureBillingExporter.csproj
```

5. Open metrics

```bash
curl http://localhost:5000/metrics
```

# Metrics

| *Metrics Name*  | *Description* |
|---|---|
| `azure_billing_daily_today`  | Today all costs !!! Cost data could delay in 12-48 hours !!! |
| `azure_billing_daily_yesterday`  | Yesterday all costs !!! Cost data could delay in 12-48 hours !!! |
| `azure_billing_daily_before_yesterday`  | Day before yesterday all costs |
| `azure_billing_monthly`  | Costs by current month |

# Custom Metrics

## Set custom metrics configs into `custom_collectors.yml`

```yaml
# A Prometheus metric with (optional) additional labels, value and labels populated from one query.
metrics:
  - metric_name: azure_billing_by_resource_group
    type: gauge
    help: 'Costs by resource group by current month'
    key_labels:
      # Populated from the `market` column of each row.
      - ResourceGroupName
    static_labels:
      # Arbitrary key/value pair
      company: dodo
    value: PreTaxCost
    replace_labels_to_enum: true  # replace `05/01/2020 00:00:00` to `last_month`, `UsageDate="20200624"` to `yesterday`. Default false
    query_file: './custom_queries/azure_billing_by_resource_group.json'
```

## Query to billing api

```json
{
  "type": "ActualCost",
  "dataSet": {
    "granularity": "None",
    "aggregation": {
      "totalCost": {
        "name": "PreTaxCost",
        "function": "Sum"
      }
    },
    "grouping": [
      {
        "type": "Dimension",
        "name": "ResourceGroupName"
      }
    ],
    "sorting": [
      {
        "direction": "descending",
        "name": "PreTaxCost"
      }
    ]
  },
  "timeframe": "Custom",
  "timePeriod": {
    "from": "{{ CurrentMonthStart }}",
    "to": "{{ TodayEnd }}"
  }
}
```

## Datetime constants into query files

You can use special constant into query file. For this use `{{ }}` template notation [Liquid Template Language](https://shopify.github.io/liquid/) . 
DateTime Constants (using server datetime). If today is '2020-06-23T08:12:45':

| *Constant*  | *Description* |  *Example* |
|---|---|---|
| `CurrentMonthStart` |  This month start date time. | '2020-06-01T00:00:00.0000000' |
| `PrevMonthStart` |  Previous month start date. | '2020-05-01T00:00:00.0000000' |
| `BeforePrevMonthStart` |  Before previous month. | '2020-04-01T00:00:00.0000000' |
| `TodayEnd` |  End of current date. | '2020-06-22T23:59:59.0000000' |
| `YesterdayStart` | Yesterday start date. | '2020-06-22T00:00:00.0000000' |
| `WeekAgo` |  This day of week but week ago, start of the day. | '2020-06-16T00:00:00.0000000' |
| `YearAgo` |  This month first day year ago. | '2019-06-01T00:00:00.0000000' |

All this constants you can use into billing query json files:
```json
  "timePeriod": {
    "from": "{{ PrevMonthStart }}",
    "to": "{{ TodayEnd }}"
  }
```



# Try Azure Billing Query on sandbox

Go to docs:
<https://docs.microsoft.com/en-us/rest/api/cost-management/query/usage>

Click `Try It`

Content-type: application/json
Scope: `subscriptions/YOUR_SUBSCRIPTION_ID`
Api version: `2019-10-01`

Body:

```json
{
  "type": "ActualCost",
  "dataSet": {
    "granularity": "Daily",
    "aggregation": {
      "totalCost": {
        "name": "PreTaxCost",
        "function": "Sum"
      }
    },
    "sorting": [
      {
        "direction": "ascending",
        "name": "UsageDate"
      }
    ]
  },
  "timeframe": "Custom",
  "timePeriod": {
    "from": "2020-06-24T00:00:00+00:00",
    "to": "2020-06-24T23:59:59+00:00"
  }
}
```


# Notice

Request duration measuring for exporter:
```
▶ curl -o /dev/null -s -w 'Total: %{time_total}s\n' http://localhost:5000/metrics
Total: 19.220319s

~
▶ curl -o /dev/null -s -w 'Total: %{time_total}s\n' http://localhost:5000/metrics
Total: 17.939426s

~
▶ curl -o /dev/null -s -w 'Total: %{time_total}s\n' http://localhost:5000/metrics
Total: 18.603152s
```