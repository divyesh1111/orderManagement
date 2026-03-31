FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/ApiGateway/ApiGateway.Api/ApiGateway.Api.csproj
RUN dotnet publish src/ApiGateway/ApiGateway.Api/ApiGateway.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ApiGateway.Api.dll"]