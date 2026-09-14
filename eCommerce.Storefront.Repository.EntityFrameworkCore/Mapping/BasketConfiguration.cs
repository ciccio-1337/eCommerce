using eCommerce.Storefront.Model.Basket;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eCommerce.Storefront.Repository.EntityFrameworkCore.Mapping
{
    public class BasketConfiguration : IEntityTypeConfiguration<Basket>
    {
        public void Configure(EntityTypeBuilder<Basket> builder)
        {
            builder.ToTable("Baskets");
            builder.Property(b => b.Id)
                   .HasColumnName("BasketId");
            builder.HasKey(b => b.Id);
            builder.HasMany(b => b.Items)
                   .WithOne(i => i.Basket)
                   .IsRequired()
                   .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(b => b.DeliveryOption);
            builder.HasOne(b => b.Customer)
                   .WithOne(c => c.Basket)
                   .HasForeignKey<Basket>("CustomerId")
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}