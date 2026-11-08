using System;

namespace ResortIsland.Waves
{
    public class GamePause
    {
        private readonly ITimeScale time;
        private float scaleBeforePause = 1f;
        private bool resumePending;
        private int resumeAfterFrame;

        public bool IsPaused { get; private set; }

        public GamePause(ITimeScale time)
        {
            this.time = time ?? throw new ArgumentNullException(nameof(time));
        }

        public void Pause()
        {
            resumePending = false;
            if (IsPaused)
                return;

            scaleBeforePause = time.Value;
            time.Value = 0f;
            IsPaused = true;
        }

        public void ResumeAfter(int frame)
        {
            if (!IsPaused)
                return;

            resumePending = true;
            resumeAfterFrame = frame;
        }

        public void Tick(int frame)
        {
            if (!resumePending || frame <= resumeAfterFrame)
                return;

            resumePending = false;
            time.Value = scaleBeforePause;
            IsPaused = false;
        }
    }
}
