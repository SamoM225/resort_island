using System.Collections.Generic;
using NUnit.Framework;
using ResortIsland.Analytics;
using ResortIsland.Waves;

namespace ResortIsland.Tests
{
    public class WorkerMoveMetricTests
    {
        private GameStateMachine machine;
        private FakeWorkerMoves moves;
        private WorkerMoveMetric metric;

        [SetUp]
        public void SetUp()
        {
            machine = new GameStateMachine();
            moves = new FakeWorkerMoves();
            metric = new WorkerMoveMetric(machine, moves);
        }

        [TearDown]
        public void TearDown()
        {
            metric.Dispose();
        }

        private (int PreWave, int InWave) Report()
        {
            Dictionary<string, object> payload = new Dictionary<string, object>();
            metric.AddTo(payload);
            return ((int)payload[WorkerMoveMetric.PreWaveKey], (int)payload[WorkerMoveMetric.InWaveKey]);
        }

        private void Move(int times)
        {
            for (int i = 0; i < times; i++)
                moves.Move();
        }

        private void FinishWave()
        {
            machine.TryTransitionTo(GameState.WaveActive);
            machine.TryTransitionTo(GameState.WaveResolving);
            machine.TryTransitionTo(GameState.WaveComplete);
        }

        [Test]
        public void MovesBeforeStart_CountAsPreWave()
        {
            Move(2);
            machine.TryStartWave();
            FinishWave();

            Assert.AreEqual((2, 0), Report());
        }

        [Test]
        public void MovesDuringArrivalActiveWaveAndDrain_CountAsInWave()
        {
            machine.TryStartWave();
            Move(1);
            machine.TryTransitionTo(GameState.WaveActive);
            Move(2);
            machine.TryTransitionTo(GameState.WaveResolving);
            Move(1);
            machine.TryTransitionTo(GameState.WaveComplete);

            Assert.AreEqual((0, 4), Report());
        }

        [Test]
        public void EachWave_CountsOnlyItsOwnMoves()
        {
            Move(1);
            machine.TryStartWave();
            Move(2);
            FinishWave();
            machine.TryTransitionTo(GameState.Upgrade);
            machine.TryTransitionTo(GameState.AwaitingWave);

            Move(3);
            machine.TryStartWave();
            Move(1);
            FinishWave();

            Assert.AreEqual((3, 1), Report());
        }

        [Test]
        public void MovesAfterTheResult_CountForTheNextWave()
        {
            machine.TryStartWave();
            FinishWave();
            Move(1);
            machine.TryTransitionTo(GameState.Upgrade);
            Move(1);
            machine.TryTransitionTo(GameState.AwaitingWave);

            machine.TryStartWave();
            FinishWave();

            Assert.AreEqual((2, 0), Report());
        }

        [Test]
        public void Dispose_StopsCounting()
        {
            metric.Dispose();

            Move(2);
            machine.TryStartWave();
            FinishWave();

            Assert.AreEqual((0, 0), Report());
        }
    }
}
