using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MacroExamples {
    using MacroFramework.Tools;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using WinUtilities;

    namespace MacroApp {
        public static class AnimationExtensions {
            public static async Task Move(this Window window, Area area, IAnimation animation) {
                animation.Start(window, area);
                await animation.WaitForStop();
            }
        }
    }

    public static class AnimationManager {

        public static uint CurrentID { get; private set; }
        public static uint NewID => ++CurrentID;
        public static Dictionary<Window, IAnimation> Animations { get; private set; } = new Dictionary<Window, IAnimation>();
        private static readonly object locker = new object();

        public static void Add(Window window, IAnimation animation) {
            lock (locker) {
                if (animation.ID == 0) {
                    throw new Exception("Illegal animation ID");
                }

                if (Animations.ContainsKey(window)) {
                    var ani = Animations[window];
                    Animations[window] = animation;
                    if (ani.ID != animation.ID) {
                        ani.Stop();
                    }
                } else {
                    Animations.Add(window, animation);
                }
            }
        }

        public static void Remove(Window window, IAnimation animation) {
            lock (locker) {
                if (Animations.ContainsKey(window) && Animations[window].ID == animation.ID) {
                    Animations.Remove(window);
                }
            }
        }
    }

    public interface IAnimation {
        uint ID { get; }
        Window Window { get; }
        Area InitialArea { get; }
        Area TargetArea { get; }
        bool Running { get; }

        void Start(Window window, Area target = default);
        void Stop();
        void Skip();
        void Cancel();
        Task WaitForStop();
        IAnimation Copy();
    }

    /// <summary>An animation class that uses <see cref="Tools.Curve"/> to determine its positiong through time</summary>
    public class Animation : IAnimation {

        private uint id;
        private double progress;
        protected TaskCompletionSource<object> stopSource;

        #region properties
        public uint ID {
            get {
                if (id == 0)
                    id = AnimationManager.NewID;
                return id;
            }
        }
        public Window Window { get; protected set; }
        public bool Running { get; protected set; }
        public int RepeatDelay { get; set; } = 5;
        public double Progress { get => progress; protected set => progress = Math.Min(Math.Max(0, value), 1); }
        public uint UseCount { get; private set; }

        public Curve Curve { get; protected set; }
        public bool TimeBased { get; protected set; }
        public long Duration { get; set; }
        public double Speed { get; set; }

        public Area InitialArea { get; protected set; }
        public Area CurrentArea { get; protected set; }
        public Area TargetArea { get; protected set; }
        public Area DeltaArea => TargetArea - InitialArea;

        public long StartTime { get; protected set; }
        protected long PrevTime { get; set; }
        protected long CurrentTime { get; set; }
        public long DeltaTime => CurrentTime - PrevTime;
        #endregion

        #region presets
        /// <summary>An animation using <see cref="Curves.FExpo(double)"/> and a duration of 500 ms</summary>
        public static Animation Expo => new Animation(Curves.FExpo, 500);
        /// <summary>An animation using <see cref="Curves.FExpo(double)"/> and a duration of 1000 ms</summary>
        public static Animation ExpoSlow => new Animation(Curves.FExpo, 1000);
        /// <summary>An animation using <see cref="Curves.Bounce(double)"/> and a duration of 750 ms</summary>
        public static Animation Bounce => new Animation(Curves.Bounce, 750);
        #endregion

        #region constructors
        public Animation(Curve curve, long duration) {
            if (curve == null)
                throw new ArgumentNullException("Curve can't be null");
            if (duration < 0)
                throw new ArgumentException("Duration can't be negative");
            if (duration == 0)
                duration = 1;

            Curve = curve;
            Duration = duration;
            TimeBased = true;
        }

        public Animation(Curve curve, double speed) {
            if (curve == null)
                throw new ArgumentNullException("Curve can't be null");
            if (speed <= 0)
                throw new ArgumentException("Speed must be positive");

            Curve = curve;
            Speed = speed;
        }
        #endregion

        protected virtual void StartInit(Window window, Area target) {
            TargetArea = target;
            Window = window;
            Running = true;
            Progress = 0;
            UseCount++;

            InitialArea = Window.Area;
            CurrentArea = InitialArea;

            StartTime = Timer.Milliseconds;
            PrevTime = StartTime;
            CurrentTime = StartTime;
        }

        public virtual void Start(Window window, Area target) {
            if (Running && window != Window)
                throw new Exception("This animation is already running with another window");
            StartInit(window, target);
            AnimationLoop();
        }

        protected virtual void Body() {
            if (TimeBased) {
                Progress += DeltaTime / (double)Duration;
            } else {
                Progress += Speed * DeltaTime / DeltaArea.Magnitude;
            }

            CurrentArea = Area.Lerp(InitialArea, TargetArea, Curve(Progress));

            var offset = Area.Zero;
            if (Window.Title.Contains("Sublime"))
                offset = Window.RawArea - Window.BorderlessArea;

            Window.Move(CurrentArea);

            if (Window.Title.Contains("Sublime")) {
                var offset2 = Window.RawArea - Window.BorderlessArea;
                if (offset2 != offset) {
                    Console.WriteLine($"Something weird happened {offset2}");
                }
            }

            if (Progress >= 1) {
                Stop();
            }
        }

        public virtual void Stop() {
            Running = false;
        }

        public virtual void Cancel() {
            if (Running) {
                Running = false;
                Window.Move(InitialArea);
            }
        }

        public virtual void Skip() {
            if (Running) {
                Running = false;
                Window.Move(TargetArea);
            }
        }

        public virtual Task WaitForStop() {
            if (stopSource == null)
                return Task.CompletedTask;
            return stopSource.Task;
        }

        public virtual IAnimation Copy() {
            var copy = (Animation)MemberwiseClone();
            copy.StartInit(Window.None, Area.NaN);
            return copy;
        }

        protected async void AnimationLoop() {
            var count = UseCount;
            stopSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            AnimationManager.Add(Window, this);

            await Task.Delay(RepeatDelay).ConfigureAwait(false);
            while (Running && count == UseCount) {
                UpdateTime();
                Body();

                await Task.Delay(RepeatDelay).ConfigureAwait(false);
            }

            if (count != UseCount)
                return;
            AnimationManager.Remove(Window, this);
            stopSource.TrySetResult(null);
        }

        protected void UpdateTime() {
            PrevTime = CurrentTime;
            CurrentTime = Timer.Milliseconds;
        }
    }
}

