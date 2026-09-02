FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# Esta fase é usada para compilar o projeto de serviço
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS with-node
RUN apt-get update && apt-get install -y curl
RUN curl -sL https://deb.nodesource.com/setup_20.x | bash
RUN apt-get -y install nodejs


FROM with-node AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["achiev-hub.Server/achiev-hub.Server.csproj", "achiev-hub.Server/"]
COPY ["achiev-hub.client/achiev-hub.client.esproj", "achiev-hub.client/"]
RUN dotnet restore "./achiev-hub.Server/achiev-hub.Server.csproj"
COPY . .
WORKDIR "/src/achiev-hub.Server"
RUN dotnet build "./achiev-hub.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Esta fase é usada para publicar o projeto de serviço a ser copiado para a fase final
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./achiev-hub.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Esta fase é usada na produção ou quando executada no VS no modo normal (padrão quando não está usando a configuração de Depuração)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "achiev-hub.Server.dll"]
