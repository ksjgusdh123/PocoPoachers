FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy everything because Server references sibling projects.
COPY . .

# Skip Windows-only PreBuildEvent scripts (generate.bat) inside Linux container build.
RUN dotnet publish "./Server/Server.csproj" -c Release -o /app/publish /p:RunPreBuildEvent=Never

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 7000

ENTRYPOINT ["dotnet", "Server.dll"]
