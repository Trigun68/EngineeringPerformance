FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY backend/ ./

RUN dotnet restore EngineeringPerformance.Api/EngineeringPerformance.Api.csproj
RUN dotnet publish EngineeringPerformance.Api/EngineeringPerformance.Api.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_HTTP_PORTS=8080

RUN groupadd -r appuser && useradd -r -g appuser appuser

COPY --from=build /app/publish .
RUN chown -R appuser:appuser /app

USER appuser

EXPOSE 8080

ENTRYPOINT ["dotnet", "EngineeringPerformance.Api.dll"]
