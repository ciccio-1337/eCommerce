# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /eCommerce

COPY --chown=app:app eCommerce.Storefront.UI.Web.MVC/bin/Release/net10.0/publish/ ./

# The application persists data-protection keys (and, in development, the
# SQLite database) under the content root, so the non-root user must own it.
RUN mkdir -p /eCommerce/keys && chown -R app:app /eCommerce

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_HTTP_PORTS=8080

USER app

EXPOSE 8080

ENTRYPOINT ["dotnet", "eCommerce.Storefront.UI.Web.MVC.dll"]
