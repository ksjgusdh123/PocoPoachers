FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Server/ServerCore/ServerCore.csproj Server/ServerCore/
COPY Server/GameServer/GameServer.csproj Server/GameServer/
RUN dotnet restore Server/GameServer/GameServer.csproj

COPY Shared/Generated/ Shared/Generated/
COPY Server/ServerCore/ Server/ServerCore/
COPY Server/GameServer/ Server/GameServer/
WORKDIR /src/Server/GameServer
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 7000

ENV DOTNET_EnableDiagnostics=0
ENTRYPOINT ["dotnet", "GameServer.dll"]
