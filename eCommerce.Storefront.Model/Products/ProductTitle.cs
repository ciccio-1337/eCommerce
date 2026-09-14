using System.Collections.Generic;

namespace eCommerce.Storefront.Model.Products
{
    public class ProductTitle : EntityBase<long>
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
        public Brand Brand { get; set; }
        public Category Category { get; set; }
        public ProductColor Color { get; set; }
        public IEnumerable<Product> Products { get; set; } = [];
        
        protected override void Validate()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                AddBrokenRule(new BusinessRule() { Property = nameof(Name), Rule = "Product title name is required." });
            }
            
            if (Price <= 0)
            {
                AddBrokenRule(new BusinessRule() { Property = nameof(Price), Rule = "Product title price must be greater than zero." });
            }
        }
    }
}