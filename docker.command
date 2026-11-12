#!/bin/bash

set -euo pipefail

# Production startup fails closed unless these are set explicitly. Requiring
# them here gives a clear error instead of a container that exits on boot.
: "${APP_PUBLIC_BASE_URL:?Set APP_PUBLIC_BASE_URL to the public HTTPS URL, e.g. https://shop.example.com}"
: "${ALLOWED_HOSTS:?Set ALLOWED_HOSTS to the public host list, e.g. shop.example.com;www.shop.example.com}"
: "${DATA_PROTECTION_CERT_THUMBPRINT:?Set DATA_PROTECTION_CERT_THUMBPRINT to the certificate used to encrypt the data-protection keys}"

dotnet restore
dotnet publish -c Release
docker stop ecommerce || true
docker rm ecommerce || true
docker image rm ecommerce || true
docker volume create ecommerce-keys >/dev/null
docker build -t ecommerce .
docker run -d -p 8080:8080 --name ecommerce \
    --read-only \
    --tmpfs /tmp \
    --cap-drop=ALL \
    --security-opt no-new-privileges \
    -v ecommerce-keys:/eCommerce/keys \
    -e ASPNETCORE_ENVIRONMENT=Production \
    -e "AllowedHosts=${ALLOWED_HOSTS}" \
    -e "App__PublicBaseUrl=${APP_PUBLIC_BASE_URL}" \
    -e "DataProtection__CertificateThumbprint=${DATA_PROTECTION_CERT_THUMBPRINT}" \
    ecommerce
