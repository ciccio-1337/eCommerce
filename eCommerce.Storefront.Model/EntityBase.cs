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
            if (obj is not EntityBase<TId> other)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (EqualityComparer<TId>.Default.Equals(Id, default) || EqualityComparer<TId>.Default.Equals(other.Id, default))
            {
                return false;
            }

            return EqualityComparer<TId>.Default.Equals(Id, other.Id);
        }

        public override int GetHashCode()
        {
            if (EqualityComparer<TId>.Default.Equals(Id, default))
            {
                return base.GetHashCode();
            }

            return Id.GetHashCode();
        }
    }
}