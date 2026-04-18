FROM mcr.microsoft.com/devcontainers/dotnet:8.0

RUN apt-get update && export DEBIAN_FRONTEND=noninteractive \
    && apt-get install -y --no-install-recommends iproute2 netcat-openbsd \
    && apt-get clean -y && rm -rf /var/lib/apt/lists/*

WORKDIR /workspaces/repo/Server
