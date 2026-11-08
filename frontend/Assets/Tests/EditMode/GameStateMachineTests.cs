using System;
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

        private static readonly GameState[] WaveCycle =
        {
            GameState.WaveIncoming,
            GameState.WaveActive,
            GameState.WaveResolving,
            GameState.WaveComplete,
            GameState.Upgrade,
            GameState.AwaitingWave
        };

        private static GameStateMachine MachineIn(GameState state)
        {
            GameStateMachine machine = new GameStateMachine();
            foreach (GameState next in WaveCycle)
            {
                if (machine.Current == state)
                    break;
                machine.TryTransitionTo(next);
            }
            return machine;
        }

        private static GameState Successor(GameState state) => WaveCycle[(Array.IndexOf(WaveCycle, state) + 1) % WaveCycle.Length];

        private static IEnumerable<TestCaseData> ForbiddenTransitions()
        {
            foreach (GameState from in Enum.GetValues(typeof(GameState)))
                foreach (GameState to in Enum.GetValues(typeof(GameState)))
                    if (to != Successor(from))
                        yield return new TestCaseData(from, to);
        }

        [Test]
        public void Wave_GoesThroughAllStatesInOrderAndBackToAwaitingWave()
        {
            GameStateMachine machine = new GameStateMachine();
            List<GameState> changes = new List<GameState>();
            machine.Changed += changes.Add;

            foreach (GameState next in WaveCycle)
            {
                GameState from = machine.Current;
                Assert.IsTrue(machine.TryTransitionTo(next), $"{from} -> {next}");
            }

            CollectionAssert.AreEqual(WaveCycle, changes);
            Assert.AreEqual(GameState.AwaitingWave, machine.Current);
        }

        [TestCaseSource(nameof(ForbiddenTransitions))]
        public void OtherTransitions_AreRejected(GameState from, GameState to)
        {
            GameStateMachine machine = MachineIn(from);
            int changes = 0;
            machine.Changed += _ => changes++;

            Assert.IsFalse(machine.CanTransitionTo(to));
            Assert.IsFalse(machine.TryTransitionTo(to));

            Assert.AreEqual(from, machine.Current);
            Assert.AreEqual(0, changes);
        }

        [Test]
        public void AfterUpgrade_AwaitsNextWave()
        {
            GameStateMachine machine = new GameStateMachine();
            int waveWhenAwaiting = 0;
            machine.Changed += state =>
            {
                if (state == GameState.AwaitingWave)
                    waveWhenAwaiting = machine.NextWave;
            };

            foreach (GameState next in WaveCycle)
                machine.TryTransitionTo(next);

            Assert.AreEqual(2, machine.NextWave);
            Assert.AreEqual(2, waveWhenAwaiting);
            Assert.IsTrue(machine.CanStartWave);
        }
    }
}
