# How to run locally

1. Log to Azure

```bash
az login
```


2. Download ServicePrincipals from KeyVault to get costs info
This SP should have access to `BillingReader`<https://docs.microsoft.com/en-us/azure/cost-management-billing/manage/manage-billing-access>

```bash
az keyvault secret download --file .secrets/billing_reader_sp.json --name azure-billing-reader-report --vault-name dev-keyvault-dodo
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

DateTime Constants (using server datetime). If today is '2020-06-23T08:12:45':
`CurrentMonthStart` - This month start date time. For instance '2020-06-01T00:00:00.0000000'
`PrevMonthStart` - Previous month start date. For instance '2020-05-01T00:00:00.0000000'
`TodayEnd` - End of current date. For instance '2020-06-22T23:59:59.0000000'
`YesterdayStart` - Yesterday start date. Form instance '2020-06-22T00:00:00.0000000'

All this constants you can use into billing query json files:
```json
  "timePeriod": {
    "from": "{{ PrevMonthStart }}",
    "to": "{{ TodayEnd }}"
  }
```

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
    query_file: './custom_queries/azure_billing_by_resource_group.json'
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