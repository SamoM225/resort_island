using NUnit.Framework;
using ResortIsland.Characters;

namespace ResortIsland.Tests
{
    public class WorkerAssignmentTests
    {
        [Test]
        public void RequiredWorkerCount_IsOneLessThanBuildings()
        {
            Assert.AreEqual(0, WorkerAssignment.RequiredWorkerCount(0));
            Assert.AreEqual(0, WorkerAssignment.RequiredWorkerCount(1));
            Assert.AreEqual(2, WorkerAssignment.RequiredWorkerCount(3));
            Assert.AreEqual(4, WorkerAssignment.RequiredWorkerCount(5));
        }

        [Test]
        public void RequiredWorkerCount_RejectsNegativeBuildingCount()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => WorkerAssignment.RequiredWorkerCount(-1));
        }
    }
}
