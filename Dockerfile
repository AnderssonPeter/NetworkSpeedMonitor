FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build

WORKDIR /app

COPY ./NetworkSpeedMonitor/* ./NetworkSpeedMonitor/
COPY ./NetworkSpeedMonitor.sln ./NetworkSpeedMonitor.sln
RUN dotnet restore

RUN dotnet build -c Release

RUN dotnet publish NetworkSpeedMonitor -o out -c Release

FROM mcr.microsoft.com/dotnet/runtime:7.0

WORKDIR /app

COPY --from=build /app/out ./

ENTRYPOINT ["dotnet", "NetworkSpeedMonitor.dll"]
CMD ["dotnet", "NetworkSpeedMonitor.dll", "--help"]