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
| `azure_billing_daily_today`  | Today all costs |
| `azure_billing_daily_yesterday`  | Yesterday all costs |
| `azure_billing_daily_before_yesterday`  | Day before yesterday all costs |
