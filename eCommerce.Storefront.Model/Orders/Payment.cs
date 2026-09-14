using System;

namespace eCommerce.Storefront.Model.Orders
{
    public class Payment : ValueObjectBase
    {
        private readonly DateTime _datePaid;
        private readonly string _transactionId;
        private readonly string _merchant;
        private readonly decimal _amount;

        public Payment(DateTime datePaid, string transactionId, string merchant, decimal amount)
        {
            _datePaid = datePaid;
            _transactionId = transactionId;
            _merchant = merchant;
            _amount = amount;

            ThrowExceptionIfInvalid();
        }

        public DateTime DatePaid
        {
            get { return _datePaid; }
        }

        public string TransactionId
        {
            get { return _transactionId; }
        }

        public string Merchant
        {
            get { return _merchant; }
        }

        public decimal Amount
        {
            get { return _amount; }
        }

        protected override void Validate()
        {
            if (string.IsNullOrWhiteSpace(_transactionId))
            {
                AddBrokenRule(new BusinessRule() { Property = nameof(TransactionId), Rule = "A payment must have a transaction id." });
            }

            if (string.IsNullOrWhiteSpace(_merchant))
            {
                AddBrokenRule(new BusinessRule() { Property = nameof(Merchant), Rule = "A payment must have a Merchant." });
            }

            if (_amount <= 0)
            {
                AddBrokenRule(new BusinessRule() { Property = nameof(Amount), Rule = "A payment must be for a positive amount." });
            }
        }
    }
}
