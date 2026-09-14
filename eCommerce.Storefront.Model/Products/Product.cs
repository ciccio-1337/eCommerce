namespace eCommerce.Storefront.Model.Products
{
    public class Product : EntityBase<long>
    {
        public ProductSize Size { get; set; }

        public ProductTitle Title { get; set; }

        public string Name
        {
            get { return Title.Name; }
        }

        public decimal Price
        {
            get { return Title.Price; }
        }

        public Brand Brand
        {
            get { return Title.Brand; }
        }

        public ProductColor Color
        {
            get { return Title.Color; }
        }

        public Category Category
        {
            get { return Title.Category; }
        }
        
        protected override void Validate()
        {
            if (Title == null)
            {
                AddBrokenRule(new BusinessRule() { Property = nameof(Title), Rule = "Product must have a title." });
            }
        }
    }
}