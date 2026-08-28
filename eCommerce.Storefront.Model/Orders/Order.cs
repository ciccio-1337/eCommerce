using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using eCommerce.Storefront.Model.Customers;
using eCommerce.Storefront.Model.Products;
using eCommerce.Storefront.Model.Shipping;

namespace eCommerce.Storefront.Model.Orders
{
    public class Order : EntityBase<long>
    {
        private readonly IList<OrderItem> _items;
        private readonly DateTime _created;
        private readonly Lock _paymentLock = new();
        private Payment _payment;

        public Order()
        {
            _created = DateTime.Now;
            _items = [];
            Status = OrderStatus.Open;
        }

        public DateTime Created
        {
            get { return _created; }
        }

        public decimal ShippingCharge { get; set; }

        public ShippingService ShippingService { get; set; }

        public decimal ItemTotal()
        {
            return Items.Sum(i => i.LineTotal());
        }

        public decimal Total()
        {
            return Items.Sum(i => i.LineTotal()) + ShippingCharge;
        }

        public Payment Payment
        {
            get { return _payment; }
        }

        public void SetPayment(Payment payment)
        {
            lock (_paymentLock)
            {
                if (OrderHasBeenPaidFor())
                {
                    throw new OrderAlreadyPaidForException(GetDetailsOnExisitingPayment());
                }

                if (OrderTotalMatches(payment))
                {
                    _payment = payment;
                }
                else
                {
                    throw new PaymentAmountDoesNotEqualOrderTotalException(GetDetailsOnIssueWith(payment));
                }
            }
        }

        private string GetDetailsOnExisitingPayment()
        {
            return string.Format("Order has already been paid for. {0} was paid on {1}. Payment token '{2}'", Payment.Amount, Payment.DatePaid, Payment.TransactionId);
        }

        private string GetDetailsOnIssueWith(Payment payment)
        {
            return string.Format("Payment amount is invalid. Order total is {0} but payment for {1}. Payment token '{2}'", Total(), payment.Amount, payment.TransactionId);
        }

        public bool OrderHasBeenPaidFor()
        {
            lock (_paymentLock)
            {
                return Payment != null && OrderTotalMatches(Payment);
            }
        }

        private bool OrderTotalMatches(Payment payment)
        {
            return Total() == payment.Amount;
        }

        public Customer Customer { get; set; }

        public DeliveryAddress DeliveryAddress { get; set; }

        public IEnumerable<OrderItem> Items
        {
            get { return _items; }
        }

        public OrderStatus Status { get; set; }

        public void AddItem(Product product, int qty)
        {
            if (CanAddProduct())
            {
                if (!OrderContains(product))
                {
                    _items.Add(new OrderItem(product, this, qty));
                }
            }
            else
            {
                throw new CannotAmendOrderException(string.Format("You cannot add an item to an order with the status of '{0}'.", Status.ToString()));
            }
        }

        private bool CanAddProduct()
        {
            if (Status == OrderStatus.Open)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool OrderContains(Product product)
        {
            return _items.Any(i => i.Contains(product));
        }

        protected override void Validate()
        {
            if (Created == DateTime.MinValue)
            {
                AddBrokenRule(new BusinessRule() { Property = nameof(Created), Rule = "An order must have a created date." });
            }

            if (Customer == null)
            {
                AddBrokenRule(new BusinessRule() { Property = nameof(Customer), Rule = "An order must be associated with a customer." });
            }

            if (DeliveryAddress == null)
            {
                AddBrokenRule(new BusinessRule() { Property = nameof(DeliveryAddress), Rule = "An order must have a valid delilvery address." });
            }

            if (Items == null || !Items.Any())
            {
                AddBrokenRule(new BusinessRule() { Property = nameof(Items), Rule = "An order must contain at least one order item." });
            }
            else
            {
                if (Items.Any(i => i.GetBrokenRules().Any()))
                {
                    foreach (OrderItem item in Items.Where(i => i.GetBrokenRules().Any()))
                    {
                        foreach (BusinessRule businessRule in item.GetBrokenRules())
                        {
                            AddBrokenRule(businessRule);
                        }
                    }
                }
            }

            if (ShippingService == null)
            {
                AddBrokenRule(new BusinessRule() { Property = nameof(ShippingService), Rule = "An order must have a shipping service set." });
            }
        }

        public override string ToString()
        {
            var orderInfo = new StringBuilder();

            foreach (OrderItem item in _items)
            {
                orderInfo.AppendLine(string.Format("{0} of {1} ", item.Qty, item.Product.Name));
            }

            orderInfo.AppendLine(string.Format("Shipping: {0}", ShippingCharge));
            orderInfo.AppendLine(string.Format("Total: {0}", Total()));

            return orderInfo.ToString();
        }
    }
}
