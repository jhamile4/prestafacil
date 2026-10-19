# ---------- Etapa 1: compilar ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Primero solo los .csproj: si no cambian, Docker reutiliza la capa del restore
COPY Directory.Build.props ./
COPY src/PrestaFacil.Domain/PrestaFacil.Domain.csproj src/PrestaFacil.Domain/
COPY src/PrestaFacil.Application/PrestaFacil.Application.csproj src/PrestaFacil.Application/
COPY src/PrestaFacil.Infrastructure/PrestaFacil.Infrastructure.csproj src/PrestaFacil.Infrastructure/
COPY src/PrestaFacil.Api/PrestaFacil.Api.csproj src/PrestaFacil.Api/
RUN dotnet restore src/PrestaFacil.Api/PrestaFacil.Api.csproj

COPY src/ src/
RUN dotnet publish src/PrestaFacil.Api/PrestaFacil.Api.csproj -c Release -o /app/publish --no-restore

# ---------- Etapa 2: ejecutar ----------
# Imagen solo con el runtime de ASP.NET: más liviana y sin el SDK
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Usuario sin privilegios que trae la imagen oficial
USER $APP_UID

ENTRYPOINT ["dotnet", "PrestaFacil.Api.dll"]
