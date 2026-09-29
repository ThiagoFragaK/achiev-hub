FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS with-node
RUN apt-get update && apt-get install -y curl
RUN curl -sL https://deb.nodesource.com/setup_20.x | bash
RUN apt-get -y install nodejs

FROM with-node AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
# Build context is MyApps (parent of achiev-hub + steam-sync)
COPY ["achiev-hub/achiev-hub.Server/achiev-hub.Server.csproj", "achiev-hub/achiev-hub.Server/"]
COPY ["achiev-hub/achiev-hub.client/achiev-hub.client.esproj", "achiev-hub/achiev-hub.client/"]
COPY ["steam-sync/src/SteamSync.Shared/SteamSync.Shared.csproj", "steam-sync/src/SteamSync.Shared/"]
RUN dotnet restore "achiev-hub/achiev-hub.Server/achiev-hub.Server.csproj"
COPY achiev-hub/achiev-hub.Server/ achiev-hub/achiev-hub.Server/
COPY achiev-hub/achiev-hub.client/ achiev-hub/achiev-hub.client/
COPY steam-sync/src/SteamSync.Shared/ steam-sync/src/SteamSync.Shared/
WORKDIR /src/achiev-hub/achiev-hub.Server
RUN dotnet build "./achiev-hub.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./achiev-hub.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "achiev-hub.Server.dll"]
