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
                .Map(dest => dest.Total, src => src.Total().FormatMoney(CurrencySymbol));
            config.NewConfig<OrderItem, OrderItemView>()
                .Map(dest => dest.Price, src => src.Price.FormatMoney(CurrencySymbol));
            config.NewConfig<Order, OrderSummaryView>()
                .Map(dest => dest.IsSubmitted, src => src.Status == OrderStatus.Submitted);
            config.NewConfig<OrderView, OrderPaymentRequest>()
                .Map(dest => dest.Total, src => decimal.Parse(src.Total.Substring(1, src.Total.Length - 1)))
                .Map(dest => dest.ShippingCharge, src => decimal.Parse(src.ShippingCharge.Substring(1, src.ShippingCharge.Length - 1)))
                .Map(dest => dest.CurrencyCode, src => CurrencyCode);
            config.NewConfig<OrderItemView, OrderItemPaymentRequest>()
                .Map(dest => dest.Price, src => decimal.Parse(src.Price.Substring(1, src.Price.Length - 1)));
            config.NewConfig<DeliveryAddress, DeliveryAddress>();
        }
    }
}
