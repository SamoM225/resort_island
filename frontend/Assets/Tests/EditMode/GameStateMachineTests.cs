using System.Collections.Generic;
using NUnit.Framework;
using ResortIsland.Waves;

namespace ResortIsland.Tests
{
    public class GameStateMachineTests
    {
        [Test]
        public void NewGame_AwaitsWave()
        {
            GameStateMachine machine = new GameStateMachine();

            Assert.AreEqual(GameState.AwaitingWave, machine.Current);
            Assert.IsTrue(machine.CanStartWave);
            Assert.AreEqual(1, machine.NextWave);
        }

        [Test]
        public void StartWave_MovesToWaveIncomingAndNotifiesOnce()
        {
            GameStateMachine machine = new GameStateMachine();
            List<GameState> changes = new List<GameState>();
            machine.Changed += changes.Add;

            Assert.IsTrue(machine.TryStartWave());

            Assert.AreEqual(GameState.WaveIncoming, machine.Current);
            Assert.IsFalse(machine.CanStartWave);
            CollectionAssert.AreEqual(new[] { GameState.WaveIncoming }, changes);
        }

        [Test]
        public void StartWave_DuringWave_IsRejected()
        {
            GameStateMachine machine = new GameStateMachine();
            machine.TryStartWave();
            int changes = 0;
            machine.Changed += _ => changes++;

            Assert.IsFalse(machine.TryStartWave());

            Assert.AreEqual(GameState.WaveIncoming, machine.Current);
            Assert.AreEqual(0, changes);
        }
    }
}
