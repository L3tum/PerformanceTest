FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["PerformanceTest/PerformanceTest.csproj", "PerformanceTest/"]
RUN dotnet restore "PerformanceTest/PerformanceTest.csproj"
COPY . .
WORKDIR "/src/PerformanceTest"
RUN dotnet build "PerformanceTest.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PerformanceTest.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PerformanceTest.dll"]
