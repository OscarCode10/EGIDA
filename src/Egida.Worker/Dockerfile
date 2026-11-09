# Contexto de build: raíz del repositorio.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props Egida.slnx ./
COPY src/Egida.Domain/Egida.Domain.csproj src/Egida.Domain/
COPY src/Egida.Application/Egida.Application.csproj src/Egida.Application/
COPY src/Egida.Infrastructure/Egida.Infrastructure.csproj src/Egida.Infrastructure/
COPY src/Egida.Worker/Egida.Worker.csproj src/Egida.Worker/
RUN dotnet restore src/Egida.Worker/Egida.Worker.csproj
COPY src/ src/
RUN dotnet publish src/Egida.Worker/Egida.Worker.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ENTRYPOINT ["dotnet", "Egida.Worker.dll"]
