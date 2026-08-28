using System;
using System.Collections.Generic;
using System.Linq;
using eCommerce.Storefront.Model.Products;
using eCommerce.Storefront.Model.Shipping;
using eCommerce.Storefront.Model.Customers;

namespace eCommerce.Storefront.Model.Basket
{
    public class Basket : EntityBase<Guid>
    {
        private readonly List<BasketItem> _items;
        private DeliveryOption _deliveryOption;
        private Customer _customer;

        public Basket()
        {
            Id = Guid.NewGuid();
            _items = [];
        }

        public int NumberOfItems
        {
            get
            {
                return _items.Sum(i => i.Qty);
            }
        }

        public decimal BasketTotal
        {
            get { return ItemsTotal + DeliveryCost(); }
        }

        public decimal ItemsTotal
        {
            get { return _items.Sum(i => i.Qty * i.Product.Price); }
        }

        public void Add(Product product)
        {
            if (BasketContainsAnItemFor(product))
            {
                GetItemFor(product).IncreaseItemQtyBy(1);
            }
            else
            {
                _items.Add(new BasketItem(product, this, 1));
            }
        }

        public BasketItem GetItemFor(Product product)
        {
            return _items.FirstOrDefault(i => i.Contains(product));
        }

        private bool BasketContainsAnItemFor(Product product)
        {
            return _items.Any(i => i.Contains(product));
        }

        public void Remove(Product product)
        {
            if (BasketContainsAnItemFor(product))
            {
                _items.Remove(GetItemFor(product));
            }
        }

        public void ChangeQtyOfProduct(int qty, Product product)
        {
            if (BasketContainsAnItemFor(product))
            {
                GetItemFor(product).ChangeItemQtyTo(qty);
            }
        }

        public int NumberOfItemsInBasket()
        {
            return _items.Sum(i => i.Qty);
        }

        public IEnumerable<BasketItem> Items
        {
            get { return _items; }
        }

        public decimal DeliveryCost()
        {
            return DeliveryOption?.GetDeliveryChargeForBasketTotalOf(ItemsTotal) ?? 0m;
        }

        public DeliveryOption DeliveryOption
        {
            get { return _deliveryOption; }
        }

        public void SetDeliveryOption(DeliveryOption deliveryOption)
        {
            _deliveryOption = deliveryOption;
        }

        public Customer Customer
        {
            get { return _customer; }
        }

        public void SetCustomer(Customer customer)
        {
            _customer = customer;
        }

        protected override void Validate()
        {
            if (DeliveryOption == null)
            {
                AddBrokenRule(new BusinessRule() { Property = nameof(DeliveryOption), Rule = "A basket must have a valid delivery option." });
            }

            if (Customer == null)
            {
                AddBrokenRule(new BusinessRule() { Property = nameof(Customer), Rule = "A basket must have a valid customer." });
            }

            foreach (BasketItem item in Items)
            {
                if (item.GetBrokenRules().Any())
                {
                    AddBrokenRule(new BusinessRule() { Property = nameof(Items), Rule = "A basket cannot have any invalid items." });
                }
            }
        }
    }
}
