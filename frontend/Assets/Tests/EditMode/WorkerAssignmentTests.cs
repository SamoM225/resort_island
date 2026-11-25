using NUnit.Framework;
using ResortIsland.Characters;

namespace ResortIsland.Tests
{
    public class WorkerAssignmentTests
    {
        [Test]
        public void RequiredWorkerCount_IsTheWorkersOfTheConfigs()
        {
            Assert.AreEqual(2, WorkerAssignment.RequiredWorkerCount(3, 2));
            Assert.AreEqual(2, WorkerAssignment.RequiredWorkerCount(5, 2));
            Assert.AreEqual(3, WorkerAssignment.RequiredWorkerCount(4, 3));
        }

        [Test]
        public void RequiredWorkerCount_IsNeverMoreThanBuildings()
        {
            Assert.AreEqual(0, WorkerAssignment.RequiredWorkerCount(0, 2));
            Assert.AreEqual(1, WorkerAssignment.RequiredWorkerCount(1, 2));
            Assert.AreEqual(0, WorkerAssignment.RequiredWorkerCount(3, -1));
        }

        [Test]
        public void RequiredWorkerCount_RejectsNegativeBuildingCount()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => WorkerAssignment.RequiredWorkerCount(-1, 2));
        }
    }
}
