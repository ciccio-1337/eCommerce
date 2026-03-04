using eCommerce.Storefront.Model.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eCommerce.Storefront.Repository.EntityFrameworkCore.Mapping
{
    public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
    {
        public void Configure(EntityTypeBuilder<Customer> builder)
        {
            builder.ToTable("Customers");
            builder.Property(c => c.Id)
                   .HasColumnName("CustomerId");
            builder.HasKey(c => c.Id);
            builder.Property(c => c.UserId)
                   .HasColumnName("UserId")
                   .HasMaxLength(450)
                   .IsRequired();
            builder.Property(c => c.FirstName)
                   .HasColumnName("FirstName")
                   .HasMaxLength(100)
                   .IsRequired();
            builder.Property(c => c.SecondName)
                   .HasColumnName("SecondName")
                   .HasMaxLength(100)
                   .IsRequired();
            builder.Ignore(c => c.Email);
            builder.HasMany(c => c.DeliveryAddressBook)
                   .WithOne(d => d.Customer)
                   .OnDelete(DeleteBehavior.Cascade)
                   .IsRequired();
            builder.HasMany(c => c.Orders)
                   .WithOne(d => d.Customer)
                   .OnDelete(DeleteBehavior.Cascade)
                   .IsRequired();
            builder.HasOne(c => c.Basket);
        }
    }
}