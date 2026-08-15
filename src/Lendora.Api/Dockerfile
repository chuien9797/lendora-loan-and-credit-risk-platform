FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props NuGet.Config Lendora.slnx ./
COPY src/Lendora.Domain/Lendora.Domain.csproj src/Lendora.Domain/
COPY src/Lendora.Application/Lendora.Application.csproj src/Lendora.Application/
COPY src/Lendora.Infrastructure/Lendora.Infrastructure.csproj src/Lendora.Infrastructure/
COPY src/Lendora.Api/Lendora.Api.csproj src/Lendora.Api/
RUN dotnet restore src/Lendora.Api/Lendora.Api.csproj --configfile NuGet.Config

COPY src ./src
RUN dotnet publish src/Lendora.Api/Lendora.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Lendora.Api.dll"]
