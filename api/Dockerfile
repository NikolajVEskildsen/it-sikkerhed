# ---- build ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY api.csproj .
RUN dotnet restore
COPY . .
# Udgiv projektet eksplicit (ikke api.sln, som også refererer testprojektet uden for build-konteksten).
RUN dotnet publish api.csproj -c Release -o /app

# ---- runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "api.dll"]
