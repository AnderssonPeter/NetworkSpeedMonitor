FROM microsoft/dotnet:2.2-sdk AS build

WORKDIR /app

COPY ./NetworkSpeedMonitor/* ./NetworkSpeedMonitor/
COPY ./NetworkSpeedMonitor/* ./NetworkSpeedMonitor/
COPY ./NetworkSpeedMonitor.sln ./NetworkSpeedMonitor.sln
RUN dotnet restore

RUN dotnet build -c release

RUN dotnet publish -o out -c Release

FROM microsoft/dotnet:2.2-runtime

WORKDIR /app

COPY --from=build /app/NetworkSpeedMonitor/out ./

ENTRYPOINT ["dotnet", "NetworkSpeedMonitor.dll"]
CMD ["dotnet", "NetworkSpeedMonitor.dll", "--help"]