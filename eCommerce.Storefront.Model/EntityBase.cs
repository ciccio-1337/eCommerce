using System.Collections.Generic;
using System.Text;

namespace eCommerce.Storefront.Model
{
    public abstract class EntityBase<TId>
    {
        private readonly List<BusinessRule> _brokenRules = [];

        public TId Id { get; set; }

        protected abstract void Validate();

        public void ThrowExceptionIfInvalid()
        {
            _brokenRules.Clear();
            Validate();

            if (_brokenRules.Count > 0)
            {
                var issues = new StringBuilder();

                foreach (BusinessRule businessRule in _brokenRules)
                {
                    issues.AppendLine(businessRule.Rule);
                }

                throw new EntityBaseIsInvalidException(issues.ToString());
            }
        }

        public IEnumerable<BusinessRule> GetBrokenRules()
        {
            _brokenRules.Clear();
            Validate();

            return _brokenRules;
        }

        protected void AddBrokenRule(BusinessRule businessRule)
        {
            _brokenRules.Add(businessRule);
        }

        public override bool Equals(object obj)
        {
            return obj is EntityBase<TId> other && this == other;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}