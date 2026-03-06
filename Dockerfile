# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/aspnet:10.0
COPY eCommerce.Storefront.UI.Web.MVC/bin/Release/net10.0/publish/ eCommerce/
WORKDIR /eCommerce
ENTRYPOINT ["dotnet", "eCommerce.Storefront.UI.Web.MVC.dll"]