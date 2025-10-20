# Stage 1: сборка
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

# Stage 2: финальный образ
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8310
ENV ASPNETCORE_URLS=http://+:8310
ENTRYPOINT ["dotnet", "WarmHouse.TelemetryService.dll"]