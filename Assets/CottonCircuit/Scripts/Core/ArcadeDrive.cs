using System;

namespace CottonCircuit
{
    public sealed class ArcadeDrive
    {
        public RaceCourse Course { get; private set; }
        public RoadPoint Position { get; private set; }
        public double Heading { get; private set; }
        public double Speed { get; private set; }
        public RoadPoint Velocity { get; private set; }
        public double MaximumSpeed { get; set; }
        public double DriftCharge { get; private set; }
        public double BoostRemaining { get; private set; }
        public bool IsDrifting { get; private set; }
        public int BoostCount { get; private set; }
        public int WallHits { get; private set; }
        public int Laps { get; private set; }
        public double BestLapSeconds { get; private set; }
        public double LapSeconds { get; private set; }
        public double TotalProgress { get; private set; }
        public double LastRewardDistance { get; private set; }
        public double LastBoostedRewardDistance { get; private set; }
        public CourseSample Sample { get; private set; }

        double projectedProgress, furthestProgress;
        bool touchingWall;

        public ArcadeDrive() : this(RaceCourse.Shared) { }
        public ArcadeDrive(RaceCourse course)
        {
            if (course == null) throw new ArgumentNullException("course");
            Course = course;
            MaximumSpeed = 18;
            Reset();
        }

        public void Reset()
        {
            Sample = Course.Sample(0);
            Position = Sample.Position;
            Heading = Math.Atan2(Sample.Tangent.X, Sample.Tangent.Z);
            Stop();
            DriftCharge = BoostRemaining = LastRewardDistance = LastBoostedRewardDistance = TotalProgress = 0;
            BoostCount = WallHits = Laps = 0;
            BestLapSeconds = LapSeconds = projectedProgress = furthestProgress = 0;
            IsDrifting = touchingWall = false;
        }

        public void Stop()
        {
            Speed = 0;
            Velocity = new RoadPoint(0, 0);
            IsDrifting = false;
            DriftCharge = 0;
            BoostRemaining = 0;
            LastRewardDistance = LastBoostedRewardDistance = 0;
        }

        public void Recover()
        {
            var road = Course.Project(Position);
            Position = road.Position;
            Heading = Math.Atan2(road.Tangent.X, road.Tangent.Z);
            Stop();
            Sample = Course.Project(Position);
            projectedProgress = NearestUnwrapped(Sample.Progress, projectedProgress);
            if (projectedProgress > furthestProgress) furthestProgress = projectedProgress;
            LastRewardDistance = LastBoostedRewardDistance = 0;
            touchingWall = false;
        }

        static bool Finite(double number) { return !double.IsNaN(number) && !double.IsInfinity(number); }
        static double Clamp(double value, double min, double max) { return Math.Max(min, Math.Min(max, value)); }
        static double Magnitude(RoadPoint vector) { return Math.Sqrt(vector.X * vector.X + vector.Z * vector.Z); }
        static double Dot(RoadPoint a, RoadPoint b) { return a.X * b.X + a.Z * b.Z; }
        double NearestUnwrapped(double progress, double reference)
        { return progress + Math.Round((reference - progress) / Course.Length) * Course.Length; }

        public void Step(double throttle, double steering, bool brake, bool drift, double dt)
        {
            LastRewardDistance = LastBoostedRewardDistance = 0;
            if (!Finite(throttle) || !Finite(steering) || !Finite(dt) || dt <= 0) return;
            throttle = Clamp(throttle, 0, 1);
            steering = Clamp(steering, -1, 1);
            // Large caller frames use the same stable integration as normal Unity frames.
            int parts = Math.Max(1, (int)Math.Ceiling(dt / .02));
            double step = dt / parts;
            for (int i = 0; i < parts; i++) Integrate(throttle, steering, brake, drift, step);
        }

        void Integrate(double throttle, double steering, bool brake, bool drift, double dt)
        {
            LapSeconds += dt;
            if (brake)
            {
                BoostRemaining = 0;
                DriftCharge = 0;
            }
            bool canCharge = drift && !brake && Speed >= 6 && Math.Abs(steering) >= .22;
            if (canCharge) DriftCharge = Clamp(DriftCharge + dt * 1.5 * Math.Abs(steering), 0, 1);
            else if (drift && !brake) DriftCharge = Math.Max(0, DriftCharge - dt * .45);
            if (IsDrifting && !drift && !brake && DriftCharge >= .32)
            {
                BoostRemaining = .9;
                BoostCount++;
            }
            if (!drift) DriftCharge = 0;
            IsDrifting = drift && !brake;
            bool boosted = BoostRemaining > 0;
            if (boosted) BoostRemaining = Math.Max(0, BoostRemaining - dt);

            double speedLimit = Math.Max(1, Finite(MaximumSpeed) ? MaximumSpeed : 18);
            speedLimit += boosted ? 7 : 0;
            double acceleration = throttle * (boosted ? 19 : 14);
            double deceleration = brake ? 25 : 1.7 + Speed * .11;
            Speed = Clamp(Speed + (acceleration - deceleration) * dt, 0, speedLimit);
            double rollingSteer = Speed / (Speed + 4);
            double steeringRate = (drift ? 2.65 : 2.2) *
                (.25 * throttle + (1 - .25 * throttle) * rollingSteer);
            Heading += steering * steeringRate * dt;
            RoadPoint facing = new RoadPoint(Math.Sin(Heading), Math.Cos(Heading));
            RoadPoint desiredVelocity = facing * Speed;
            double grip = drift ? 2.7 : 9;
            Velocity += (desiredVelocity - Velocity) * Math.Min(1, grip * dt);
            RoadPoint oldPosition = Position;
            Position += Velocity * dt;
            var road = Course.Project(Position);
            double limit = road.HalfWidth - .8;
            bool wall = Math.Abs(road.Lateral) > limit;
            if (wall)
            {
                double side = Math.Sign(road.Lateral);
                RoadPoint right = new RoadPoint(road.Tangent.Z, -road.Tangent.X);
                Position = road.Position + right * (side * limit);
                double along = Math.Max(0, Dot(Velocity, road.Tangent));
                Velocity = road.Tangent * (along * .7);
                Speed = Magnitude(Velocity);
                BoostRemaining = DriftCharge = 0;
                boosted = false;
                if (!touchingWall) WallHits++;
            }
            touchingWall = wall;
            Sample = Course.Project(Position);

            double nextProgress = NearestUnwrapped(Sample.Progress, projectedProgress);
            double delta = nextProgress - projectedProgress;
            double moved = Magnitude(Position - oldPosition);
            double plausible = Math.Max(.35, moved * 2.2 + .15);
            if (Math.Abs(delta) > plausible)
            {
                // Route switching can change the projection abruptly. Consume a forward
                // jump without paying it, so swapping ribbons cannot farm progress.
                projectedProgress = nextProgress;
                furthestProgress = Math.Max(furthestProgress, nextProgress);
                return;
            }
            projectedProgress = nextProgress;
            if (delta <= 0 || nextProgress <= furthestProgress) return;
            double reward = Math.Min(delta, nextProgress - furthestProgress);
            furthestProgress = nextProgress;
            TotalProgress += reward;
            LastRewardDistance += reward;
            if (boosted) LastBoostedRewardDistance += reward;
            while (TotalProgress >= (Laps + 1) * Course.Length)
            {
                Laps++;
                if (BestLapSeconds == 0 || LapSeconds < BestLapSeconds) BestLapSeconds = LapSeconds;
                LapSeconds = 0;
            }
        }
    }
}
