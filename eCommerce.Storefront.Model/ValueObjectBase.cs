using System.Collections.Generic;
using System.Text;

namespace eCommerce.Storefront.Model
{
    public abstract class ValueObjectBase
    {
        private readonly List<BusinessRule> _brokenRules = [];

        protected ValueObjectBase()
        {
        }

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

                throw new ValueObjectIsInvalidException(issues.ToString());
            }
        }

        protected void AddBrokenRule(BusinessRule businessRule)
        {
            _brokenRules.Add(businessRule);
        }
    }
}