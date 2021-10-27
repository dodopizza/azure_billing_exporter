FROM mcr.microsoft.com/dotnet/sdk:3.1-focal AS build-env

WORKDIR /app

# Copy csproj and restore as distinct layers
COPY ./src/*.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY ./src/* ./
COPY ./src/queries/get_daily_or_monthly_costs.json ./custom_queries/get_daily_or_monthly_costs.json
COPY ./src/queries/azure_billing_by_resource_group.json ./custom_queries/azure_billing_by_resource_group.json
COPY ./src/configs/custom_collectors.yml ./custom_queries/custom_collectors.yml
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:3.1-focal
WORKDIR /app
EXPOSE 8080

COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "AzureBillingExporter.dll"]