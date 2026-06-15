FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/ScalarApi/ScalarApi.csproj", "src/ScalarApi/"]
RUN dotnet restore "src/ScalarApi/ScalarApi.csproj" \
    --source https://api.nuget.org/v3/index.json
COPY . .
WORKDIR "/src/src/ScalarApi"
RUN dotnet build "ScalarApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ScalarApi.csproj" -c Release -o /app/publish \
    /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ScalarApi.dll"]
