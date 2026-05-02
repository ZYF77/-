FROM node:22-alpine AS client-build
WORKDIR /src/client

COPY client/package*.json ./
RUN npm ci

COPY client/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS backend-build
WORKDIR /src

COPY BuildingMaterialsAuditAgent.csproj ./
RUN dotnet restore

COPY . ./
COPY --from=client-build /src/wwwroot ./wwwroot
RUN dotnet publish BuildingMaterialsAuditAgent.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

COPY --from=backend-build /app/publish ./

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "BuildingMaterialsAuditAgent.dll"]
