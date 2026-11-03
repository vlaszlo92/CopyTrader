FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY src/web/CopyTrader.Web.csproj ./
RUN dotnet restore CopyTrader.Web.csproj
COPY src/web/ ./
RUN dotnet publish CopyTrader.Web.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:7.0
RUN adduser --disabled-password --gecos "" --uid 10001 app
WORKDIR /app
COPY --from=build /app ./
USER 10001
ENV ASPNETCORE_URLS=http://+:8080 \
    HttpsRedirection__Enabled=false
EXPOSE 8080
ENTRYPOINT ["dotnet", "CopyTrader.Web.dll"]
