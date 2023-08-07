FROM mcr.microsoft.com/dotnet/sdk:6.0.412-bullseye-slim AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY ./src/*.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY ./src/* ./
COPY ./src/queries/get_daily_or_monthly_costs.json ./queries/get_daily_or_monthly_costs.json
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:6.0.20-bullseye-slim
WORKDIR /app
EXPOSE 80

COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "AzureBillingExporter.dll"]
