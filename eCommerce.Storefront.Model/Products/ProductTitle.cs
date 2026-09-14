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
        }
    }
}