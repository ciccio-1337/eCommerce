using System;
using eCommerce.Storefront.Model.Products;

namespace eCommerce.Storefront.Model.Orders
{
    public class OrderItem : EntityBase<long>
    {
        private readonly Product _product;
        private readonly Order _order;
        private readonly int _qty;
        private readonly decimal _price;

        public OrderItem()
        {
        }

        public OrderItem(Product product, Order order, int qty)
        {
            if (qty <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(qty), "Quantity must be greater than zero.");
            }
            
            _product = product;
            _order = order;
            _price = product.Price;
            _qty = qty;
        }

        public Product Product
        {
            get { return _product; }
        }

        public int Qty
        {
            get { return _qty; }
        }

        public decimal Price
        {
            get { return _price; }
        }

        public Order Order
        {
            get { return _order; }
        }

        public decimal LineTotal()
        {
            return Qty * Price;
        }

        protected override void Validate()
        {  
            if (Order == null)
            {
                AddBrokenRule(new BusinessRule() { Property = nameof(Order), Rule = "An order item must be associated with an order." });
            }

            if (Product == null)
            {
                AddBrokenRule(new BusinessRule() { Property = nameof(Product), Rule = "An order item must be associated with a valid product." });
            }

            if (Price <= 0)
            {
                AddBrokenRule(new BusinessRule() { Property = nameof(Price), Rule = "An order item must have a positive price value." });
            }

            if (Qty <= 0)
            {
                AddBrokenRule(new BusinessRule() { Property = nameof(Qty), Rule = "An order item must have a positive qty value." });
            }
        }

        public bool Contains(Product product)
        {
            if (product is null) 
            {
                return false;
            }
            
            return ReferenceEquals(Product, product) || Product.Id == product.Id;
        }
    }
}
