using NUnit.Framework;
using ResortIsland.Waves;

namespace ResortIsland.Tests
{
    public class GamePauseTests
    {
        private FakeTimeScale time;
        private GamePause pause;

        [SetUp]
        public void SetUp()
        {
            time = new FakeTimeScale { Value = 1f };
            pause = new GamePause(time);
        }

        [Test]
        public void GoingToBackground_FreezesGameTime()
        {
            pause.Pause();

            Assert.AreEqual(0f, time.Value);
            Assert.IsTrue(pause.IsPaused);
        }

        [Test]
        public void ComingBack_ResumesOnTheNextFrame()
        {
            pause.Pause();
            pause.ResumeAfter(10);

            pause.Tick(10);
            Assert.AreEqual(0f, time.Value);

            pause.Tick(11);
            Assert.AreEqual(1f, time.Value);
            Assert.IsFalse(pause.IsPaused);
        }

        [Test]
        public void ComingBack_RestoresTheSpeedTheGameHad()
        {
            time.Value = 0.5f;
            pause.Pause();

            pause.ResumeAfter(1);
            pause.Tick(2);

            Assert.AreEqual(0.5f, time.Value);
        }

        [Test]
        public void RepeatedPause_KeepsTheOriginalSpeed()
        {
            pause.Pause();
            pause.Pause();

            pause.ResumeAfter(1);
            pause.Tick(2);

            Assert.AreEqual(1f, time.Value);
        }

        [Test]
        public void BackgroundAgainBeforeResuming_StaysFrozen()
        {
            pause.Pause();
            pause.ResumeAfter(5);

            pause.Pause();
            pause.Tick(6);

            Assert.AreEqual(0f, time.Value);
            Assert.IsTrue(pause.IsPaused);
        }

        [Test]
        public void ResumeWithoutPause_ChangesNothing()
        {
            pause.ResumeAfter(1);
            pause.Tick(2);

            Assert.AreEqual(1f, time.Value);
            Assert.IsFalse(pause.IsPaused);
        }

        private class FakeTimeScale : ITimeScale
        {
            public float Value { get; set; }
        }
    }
}
