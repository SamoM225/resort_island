using NUnit.Framework;
using ResortIsland.Characters;

namespace ResortIsland.Tests
{
    public class GuestNeedManagerTests
    {
        [Test]
        public void Set_DeduplicatesNeeds()
        {
            var manager = new GuestNeedManager();
            manager.Set("food", "food", "drinks");

            CollectionAssert.AreEqual(new[] { "food", "drinks" }, manager.Remaining);
        }

        [Test]
        public void Fulfill_RemovesOnlyCompletedNeed()
        {
            var manager = new GuestNeedManager();
            manager.Set("food", "drinks");

            manager.Fulfill("food");

            CollectionAssert.AreEqual(new[] { "drinks" }, manager.Remaining);
        }

        [Test]
        public void EmptyNeeds_IsMarkedComplete()
        {
            var manager = new GuestNeedManager();

            Assert.IsTrue(manager.IsEmpty);
            manager.Set("rooms");
            Assert.IsFalse(manager.IsEmpty);
        }
    }
}
