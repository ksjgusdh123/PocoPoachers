# 개발용: SDK + 일반적인 CLI 도구 (CMake 환경과 유사하게 “전체 워크스페이스를 컨테이너 안에서” 사용)
FROM mcr.microsoft.com/devcontainers/dotnet:8.0

# 선택: 네트워크 디버깅·스크립트용
RUN apt-get update && export DEBIAN_FRONTEND=noninteractive \
    && apt-get install -y --no-install-recommends iproute2 netcat-openbsd \
    && apt-get clean -y && rm -rf /var/lib/apt/lists/*

WORKDIR /workspaces/Server
