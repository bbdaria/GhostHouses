#!/usr/bin/env bash
set -euo pipefail

apt-get update
DEBIAN_FRONTEND=noninteractive apt-get install -y \
    ca-certificates \
    curl \
    git \
    gnupg \
    lsb-release \
    software-properties-common

if ! command -v dotnet >/dev/null 2>&1; then
    echo "Installing .NET SDK 8.0..."
    curl -fsSL https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -o packages-microsoft-prod.deb
    dpkg -i packages-microsoft-prod.deb
    rm packages-microsoft-prod.deb
    apt-get update
    DEBIAN_FRONTEND=noninteractive apt-get install -y dotnet-sdk-8.0
fi

if ! command -v node >/dev/null 2>&1; then
    echo "Installing Node.js 20..."
    curl -fsSL https://deb.nodesource.com/setup_20.x | bash -
    apt-get update
    DEBIAN_FRONTEND=noninteractive apt-get install -y nodejs
fi

if ! command -v npm >/dev/null 2>&1; then
    echo "npm not detected even after Node.js install" >&2
    exit 1
fi

pushd /workspace/web-server >/dev/null
dotnet restore

pushd ClientApp >/dev/null
npm install
popd >/dev/null
popd >/dev/null

echo "web-app dependencies installed"
