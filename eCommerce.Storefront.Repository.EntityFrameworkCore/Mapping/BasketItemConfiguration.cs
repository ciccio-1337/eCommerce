using eCommerce.Storefront.Model.Basket;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eCommerce.Storefront.Repository.EntityFrameworkCore.Mapping
{
    public class BasketItemConfiguration : IEntityTypeConfiguration<BasketItem>
    {
        public void Configure(EntityTypeBuilder<BasketItem> builder)
        {
            builder.ToTable("BasketItems");
            builder.Property(b => b.Id)
                   .HasColumnName("BasketItemId");
            builder.HasKey(b => b.Id);
            builder.Property(b => b.Qty)
                   .HasColumnName("Qty")
                   .IsRequired();
            builder.HasOne(b => b.Product);
        }
    }
}