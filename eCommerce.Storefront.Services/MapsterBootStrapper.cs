using System;
using System.Collections.Generic;
using Mapster;
using eCommerce.Storefront.Model;
using eCommerce.Storefront.Model.Basket;
using eCommerce.Storefront.Model.Customers;
using eCommerce.Storefront.Model.Orders;
using eCommerce.Storefront.Model.Products;
using eCommerce.Storefront.Model.Shipping;
using eCommerce.Storefront.Services.ViewModels;

namespace eCommerce.Storefront.Services
{
    public class MapsterBootStrapper : IRegister
    {
        private const string CurrencySymbol = "€";
        private const string CurrencyCode = "EUR";

        private static decimal ParseCurrency(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return 0m;
            }

            var parsed = decimal.TryParse(value.Replace(CurrencySymbol, "").Trim(),
                System.Globalization.NumberStyles.Number | System.Globalization.NumberStyles.AllowDecimalPoint,
                System.Globalization.CultureInfo.InvariantCulture,
                out var result);

            return parsed ? result : 0m;
        }

        private static string GetAddressField(DeliveryAddressView address, Func<DeliveryAddressView, string> selector)
        {
            return address == null ? "" : selector(address);
        }

        public void Register(TypeAdapterConfig config)
        {
            // Product Title and Product
            config.NewConfig<ProductTitle, ProductSummaryView>()
                .Map(dest => dest.Price, src => src.Price.FormatMoney(CurrencySymbol));
            config.NewConfig<ProductTitle, ProductView>()
                .Map(dest => dest.Price, src => src.Price.FormatMoney(CurrencySymbol));
            config.NewConfig<Product, ProductSummaryView>()
                .Map(dest => dest.Price, src => src.Price.FormatMoney(CurrencySymbol));
            config.NewConfig<Product, ProductSizeOption>();
            // Category
            config.NewConfig<Category, CategoryView>();
            // IProductAttribute
            config.NewConfig<IProductAttribute, Refinement>();
            // Basket
            config.NewConfig<DeliveryOption, DeliveryOptionView>();
            config.NewConfig<BasketItem, BasketItemView>()
                .Map(dest => dest.ProductPrice, src => src.Product.Price.FormatMoney(CurrencySymbol))
                .Map(dest => dest.LineTotal, src => src.LineTotal().FormatMoney(CurrencySymbol));
            config.NewConfig<Basket, BasketView>()
                .Map(dest => dest.BasketTotal, src => src.BasketTotal.FormatMoney(CurrencySymbol))
                .Map(dest => dest.ItemsTotal, src => src.ItemsTotal.FormatMoney(CurrencySymbol))
                .Map(dest => dest.DeliveryCost, src => src.DeliveryCost().FormatMoney(CurrencySymbol))
                .Map(dest => dest.ShippingServiceDescription, src => src.DeliveryOption.ShippingService.Description);
            // Customer
            config.NewConfig<Customer, CustomerView>();
            config.NewConfig<DeliveryAddress, DeliveryAddressView>();
            // Orders
            config.NewConfig<Order, OrderView>()
                .Map(dest => dest.ShippingCharge, src => src.ShippingCharge.FormatMoney(CurrencySymbol))
                .Map(dest => dest.Total, src => src.Total().FormatMoney(CurrencySymbol))
                .Map(dest => dest.CustomerEmail, src => src.Customer.Email);
            config.NewConfig<OrderItem, OrderItemView>()
                .Map(dest => dest.Price, src => src.Price.FormatMoney(CurrencySymbol));
            config.NewConfig<Order, OrderSummaryView>()
                .Map(dest => dest.IsSubmitted, src => src.Status == OrderStatus.Submitted);
            config.NewConfig<OrderView, OrderPaymentRequest>()
                .Map(dest => dest.Total, src => ParseCurrency(src.Total))
                .Map(dest => dest.ShippingCharge, src => ParseCurrency(src.ShippingCharge))
                .Map(dest => dest.CurrencyCode, src => CurrencyCode)
                .Map(dest => dest.CustomerFirstName, src => src.CustomerFirstName)
                .Map(dest => dest.CustomerSecondName, src => src.CustomerSecondName)
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.DeliveryAddressAddressLine, src => GetAddressField(src.DeliveryAddress, a => a.AddressLine))
                .Map(dest => dest.DeliveryAddressCity, src => GetAddressField(src.DeliveryAddress, a => a.City))
                .Map(dest => dest.DeliveryAddressState, src => GetAddressField(src.DeliveryAddress, a => a.State))
                .Map(dest => dest.DeliveryAddressCountry, src => GetAddressField(src.DeliveryAddress, a => a.Country))
                .Map(dest => dest.DeliveryAddressZipCode, src => GetAddressField(src.DeliveryAddress, a => a.ZipCode))
                .Map(dest => dest.Items, src => src.Items.Adapt<List<OrderItemPaymentRequest>>());
            config.NewConfig<OrderItemView, OrderItemPaymentRequest>()
                .Map(dest => dest.Price, src => ParseCurrency(src.Price));
            config.NewConfig<DeliveryAddress, DeliveryAddress>();
        }
    }
}
