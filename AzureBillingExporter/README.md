## How to run locally

1. Log to Azure

```bash
az login
```


2. Download ServicePrincipals from KeyVault to get costs info
This SP should have access to `BillingReader`<https://docs.microsoft.com/en-us/azure/cost-management-billing/manage/manage-billing-access>

```bash
az keyvault secret download --file .secrets/billing_reader_sp.json --name azure-billing-reader-report --vault-name dev-keyvault-dodo
```