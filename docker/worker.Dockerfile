FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY src/trader-service/CopyTrader.Worker/CopyTrader.Worker.csproj ./
RUN dotnet restore CopyTrader.Worker.csproj
COPY src/trader-service/CopyTrader.Worker/ ./
RUN dotnet publish CopyTrader.Worker.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/runtime:7.0
RUN adduser --disabled-password --gecos "" --uid 10001 app
WORKDIR /app
COPY --from=build /app ./
USER 10001
ENTRYPOINT ["dotnet", "CopyTrader.Worker.dll"]
