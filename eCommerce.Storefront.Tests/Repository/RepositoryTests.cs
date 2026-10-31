using eCommerce.Storefront.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace eCommerce.Storefront.Tests.Repository
{
    [TestClass]
    public class RepositoryTests
    {
        public class TestEntity : EntityBase<int>
        {
            public string Name { get; set; }
            protected override void Validate() { }
        }

        [TestMethod]
        public void EntityBase_Equals_DifferentTypesSameId_ReturnsFalse()
        {
            var entity1 = new TestEntity { Id = 5 };
            var entity2 = new TestEntity2 { Id = 5 };
            Assert.IsFalse(entity1.Equals(entity2));
        }

        [TestMethod]
        public void EntityBase_Equals_SameReference_ReturnsTrue()
        {
            var entity = new TestEntity { Id = 5 };
            Assert.IsTrue(entity.Equals(entity));
        }

        [TestMethod]
        public void EntityBase_Equals_DifferentInstancesSameTypeSameId_ReturnsTrue()
        {
            var entity1 = new TestEntity { Id = 5 };
            var entity2 = new TestEntity { Id = 5 };
            Assert.IsTrue(entity1.Equals(entity2));
        }

        [TestMethod]
        public void EntityBase_Equals_DefaultId_ReturnsFalse()
        {
            var entity1 = new TestEntity { Id = 0 };
            var entity2 = new TestEntity { Id = 0 };
            Assert.IsFalse(entity1.Equals(entity2));
        }

        public class TestEntity2 : EntityBase<int>
        {
            public string Name { get; set; }
            protected override void Validate() { }
        }
    }
}
