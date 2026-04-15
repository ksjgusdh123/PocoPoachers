FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY PPServer/PPServer.csproj PPServer/
RUN dotnet restore PPServer/PPServer.csproj

COPY PPServer/ PPServer/
WORKDIR /src/PPServer
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 7000

ENV DOTNET_EnableDiagnostics=0
ENTRYPOINT ["dotnet", "PPServer.dll"]

CMD ["7000"]
