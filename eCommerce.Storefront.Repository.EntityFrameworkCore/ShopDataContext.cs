using System.Collections.Generic;
using eCommerce.Storefront.Repository.EntityFrameworkCore.Mapping;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace eCommerce.Storefront.Repository.EntityFrameworkCore
{
    public class ShopDataContext : IdentityDbContext
    {
        public ShopDataContext() : base()
        {
        }

        public ShopDataContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<IdentityRole>().HasData(new List<IdentityRole>
            {
                new()
                {
                    Id = "f01eb6e7-a59f-4094-a38e-db1acb888a27",
                    Name = "Admin",
                    NormalizedName = "ADMIN",
                    ConcurrencyStamp = "c25e7311-4d62-4cd1-a42a-43fadecdac0a"
                },
                new()
                {
                    Id = "4c93af0c-9921-4abb-a5eb-26f8e01b2249",
                    Name = "Customer",
                    NormalizedName = "CUSTOMER",
                    ConcurrencyStamp = "b497576b-9358-46eb-ab44-4b8aa5cd50f6"
                }
            });
            builder.ApplyConfiguration(new BrandConfiguration());
            builder.ApplyConfiguration(new CategoryConfiguration());
            builder.ApplyConfiguration(new ProductColorConfiguration());
            builder.ApplyConfiguration(new ProductConfiguration());
            builder.ApplyConfiguration(new ProductSizeConfiguration());
            builder.ApplyConfiguration(new ProductTitleConfiguration());
            builder.ApplyConfiguration(new BasketConfiguration());
            builder.ApplyConfiguration(new BasketItemConfiguration());
            builder.ApplyConfiguration(new CourierConfiguration());
            builder.ApplyConfiguration(new DeliveryOptionConfiguration());
            builder.ApplyConfiguration(new ShippingServiceConfiguration());
            builder.ApplyConfiguration(new CustomerConfiguration());
            builder.ApplyConfiguration(new DeliveryAddressConfiguration());
            builder.ApplyConfiguration(new OrderConfiguration());
            builder.ApplyConfiguration(new OrderItemConfiguration());
        }
    }
}