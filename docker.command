#!/bin/bash

set -euo pipefail

# Production startup fails closed unless these are set explicitly. Requiring
# them here gives a clear error instead of a container that exits on boot.
: "${APP_PUBLIC_BASE_URL:?Set APP_PUBLIC_BASE_URL to the public HTTPS URL, e.g. https://shop.example.com}"
: "${ALLOWED_HOSTS:?Set ALLOWED_HOSTS to the public host list, e.g. shop.example.com;www.shop.example.com}"

dotnet restore
dotnet publish -c Release
docker stop ecommerce || true
docker rm ecommerce || true
docker image rm ecommerce || true
docker build -t ecommerce .
docker run -d -p 8080:8080 --name ecommerce \
    -e ASPNETCORE_ENVIRONMENT=Production \
    -e "AllowedHosts=${ALLOWED_HOSTS}" \
    -e "App__PublicBaseUrl=${APP_PUBLIC_BASE_URL}" \
    ecommerce
