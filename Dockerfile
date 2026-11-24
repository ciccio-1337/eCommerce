# syntax=docker/dockerfile:1
# Pin the runtime image by digest so a moved tag cannot silently change the
# image that runs in production. Refresh with `docker buildx imagetools inspect`.
FROM mcr.microsoft.com/dotnet/aspnet:11.0@sha256:bc0d6653979efba491ab3d6f589d691f96c309c3c3ef81325efa6978e7b7f242

WORKDIR /eCommerce

COPY --chown=app:app eCommerce.Storefront.UI.Web.MVC/bin/Release/net11.0/publish/ ./

# The application persists data-protection keys (and, in development, the
# SQLite database) under the content root, so the non-root user must own it.
RUN mkdir -p /eCommerce/keys && chown -R app:app /eCommerce

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_HTTP_PORTS=8080

USER app

EXPOSE 8080

ENTRYPOINT ["dotnet", "eCommerce.Storefront.UI.Web.MVC.dll"]
