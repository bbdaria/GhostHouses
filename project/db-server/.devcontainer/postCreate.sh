#!/usr/bin/env bash
set -euo pipefail

apt-get update
DEBIAN_FRONTEND=noninteractive apt-get install -y \
    ca-certificates \
    curl \
    gnupg \
    lsb-release \
    postgresql \
    postgresql-client \
    postgresql-contrib

psql --version

echo "db-server dependencies installed"
