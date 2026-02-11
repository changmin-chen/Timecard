FROM node:22-alpine AS client-build
WORKDIR /src

COPY client/package.json client/package-lock.json ./client/
RUN npm ci --prefix client

COPY client ./client
COPY src ./src
RUN npm run --prefix client build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS publish
WORKDIR /src

COPY src/Timecard.Api/Timecard.Api.csproj ./src/Timecard.Api/
RUN dotnet restore ./src/Timecard.Api/Timecard.Api.csproj

COPY src ./src
COPY --from=client-build /src/src/Timecard.Api/wwwroot ./src/Timecard.Api/wwwroot

RUN dotnet publish ./src/Timecard.Api/Timecard.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

# Use the built-in non-root user provided by .NET images.
USER app
ENTRYPOINT ["dotnet", "Timecard.Api.dll"]