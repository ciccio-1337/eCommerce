using System.Collections.Generic;

namespace eCommerce.Storefront.Model.Products
{
    public class ProductTitle : EntityBase<long>
    {
        // Backed by a mutable List so EF Core can populate the inverse navigation during
        // fix-up. An IEnumerable<Product> property (whose collection expression binds to a
        // fixed-size array) made materialization throw NotSupportedException: Collection was
        // of a fixed size, taking down every page that loads product titles.
        private readonly IList<Product> _products = [];

        public string Name { get; set; }
        public decimal Price { get; set; }
        public Brand Brand { get; set; }
        public Category Category { get; set; }
        public ProductColor Color { get; set; }
        public IEnumerable<Product> Products => _products;
        
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