# Contexto de build: raíz del repositorio.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props Egida.slnx ./
COPY src/Egida.Domain/Egida.Domain.csproj src/Egida.Domain/
COPY src/Egida.Application/Egida.Application.csproj src/Egida.Application/
COPY src/Egida.Infrastructure/Egida.Infrastructure.csproj src/Egida.Infrastructure/
COPY src/Egida.Api/Egida.Api.csproj src/Egida.Api/
RUN dotnet restore src/Egida.Api/Egida.Api.csproj
COPY src/ src/
RUN dotnet publish src/Egida.Api/Egida.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Egida.Api.dll"]
