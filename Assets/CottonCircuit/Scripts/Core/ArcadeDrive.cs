using System;

namespace CottonCircuit
{
    public enum DrivingStyle { Kart, Downhill }

    public sealed class ArcadeDrive
    {
        public RaceCourse Course { get; private set; }
        public RoadPoint Position { get; private set; }
        public double Heading { get; private set; }
        public double Speed { get; private set; }
        public RoadPoint Velocity { get; private set; }
        public double MaximumSpeed { get; set; }
        public double SteeringMultiplier = 1;
        DrivingStyle style;
        public DrivingStyle Style
        {
            get { return style; }
            set
            {
                style = value;
                if (style != DrivingStyle.Downhill) return;
                ClearBoost(); StoredBoosts = 0; LastBoostedRewardDistance = 0;
            }
        }
        public double SteeringInput { get; private set; }
        public double DriftCharge { get; private set; }
        public double BoostRemaining { get; private set; }
        public double BoostDuration { get; private set; }
        public int BoostTier { get; private set; }
        public int StoredBoosts { get; private set; }
        public bool ManualBoostActive { get; private set; }
        public bool IsDrifting { get; private set; }
        public int BoostCount { get; private set; }
        public int DriftCount { get; private set; }
        public int SkillCount { get { return Style == DrivingStyle.Downhill ? DriftCount : BoostCount; } }
        public int WallHits { get; private set; }
        public int Laps { get; private set; }
        public double BestLapSeconds { get; private set; }
        public double LapSeconds { get; private set; }
        public double TotalProgress { get; private set; }
        public double LastRewardDistance { get; private set; }
        public double LastBoostedRewardDistance { get; private set; }
        public CourseSample Sample { get; private set; }

        // After a counted wall hit, further contacts are free until the kart speeds back
        // up to this speed, so one scrape or bounce costs a single star. Zero when armed.
        const double WallRecoveryShare = .75, WallRecoveryMargin = .15;
        double wallRecoverySpeed;
        double projectedProgress, furthestProgress;
        bool touchingWall;

        public ArcadeDrive() : this(RaceCourse.Shared) { }
        public ArcadeDrive(RaceCourse course)
        {
            if (course == null) throw new ArgumentNullException("course");
            Course = course;
            MaximumSpeed = 26;
            Reset();
        }

        public void Reset()
        {
            Sample = Course.Sample(0);
            Position = Sample.Position;
            Heading = Math.Atan2(Sample.Tangent.X, Sample.Tangent.Z);
            Stop();
            DriftCharge = BoostRemaining = LastRewardDistance = LastBoostedRewardDistance = TotalProgress = 0;
            BoostCount = DriftCount = WallHits = Laps = 0;
            StoredBoosts = Style == DrivingStyle.Kart ? 1 : 0;
            BestLapSeconds = LapSeconds = projectedProgress = furthestProgress = wallRecoverySpeed = 0;
            IsDrifting = touchingWall = false;
        }

        public void Stop()
        {
            Speed = 0;
            Velocity = new RoadPoint(0, 0);
            SteeringInput = 0;
            IsDrifting = false;
            DriftCharge = 0;
            ClearBoost();
            LastRewardDistance = LastBoostedRewardDistance = 0;
        }

        void ClearBoost()
        {
            BoostRemaining = BoostDuration = 0;
            BoostTier = 0;
            ManualBoostActive = false;
        }

        void BeginBoost(int tier, double duration, double speedLimit)
        {
            BoostTier = tier;
            BoostDuration = BoostRemaining = duration;
            double impulse = Math.Min(tier == 2 ? 8 : 5,
                Math.Max(0, speedLimit + (tier == 2 ? 14 : 9) - Speed));
            Speed += impulse;
            Velocity += new RoadPoint(Math.Sin(Heading), Math.Cos(Heading)) * impulse;
            BoostCount++;
        }

        public void Recover()
        {
            var road = Course.Project(Position, .8);
            Position = road.Position;
            Heading = Math.Atan2(road.Tangent.X, road.Tangent.Z);
            Stop();
            Sample = Course.Project(Position, .8);
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

        public void Step(double throttle, double steering, bool brake, bool drift, double dt, bool boost = false)
        {
            LastRewardDistance = LastBoostedRewardDistance = 0;
            if (!Finite(throttle) || !Finite(steering) || !Finite(dt) || dt <= 0) return;
            throttle = Clamp(throttle, 0, 1);
            steering = Clamp(steering, -1, 1);
            // Large caller frames use the same stable integration as normal Unity frames.
            int parts = Math.Max(1, (int)Math.Ceiling(dt / .02));
            double step = dt / parts;
            for (int i = 0; i < parts; i++) Integrate(throttle, steering, brake, drift, step, boost && i == 0);
        }

        void Integrate(double throttle, double steering, bool brake, bool drift, double dt, bool boost)
        {
            LapSeconds += dt;
            bool downhill = Style == DrivingStyle.Downhill;
            double speedLimit = Math.Max(1, Finite(MaximumSpeed) ? MaximumSpeed : 26);
            SteeringInput += (steering - SteeringInput) * (1 - Math.Exp(-dt / (downhill ? .14 : .085)));
            if (downhill) ClearBoost();
            if (brake && !downhill)
            {
                ClearBoost();
                DriftCharge = 0;
            }
            if (boost && !downhill && !brake && throttle > 0 && StoredBoosts > 0 && BoostRemaining <= 0)
            {
                StoredBoosts--;
                BeginBoost(2, 2.5, speedLimit);
                ManualBoostActive = true;
            }
            bool slideInput = downhill ? drift || (brake && Math.Abs(SteeringInput) >= .22) : drift && !brake;
            bool canCharge = slideInput && Speed >= 6 && Math.Abs(SteeringInput) >= .22;
            if (canCharge) DriftCharge = Clamp(DriftCharge + dt * 1.5 * Math.Abs(SteeringInput), 0, 1);
            else if (slideInput) DriftCharge = Math.Max(0, DriftCharge - dt * .45);
            bool completedSlide = IsDrifting && !slideInput && (!brake || downhill) && Speed >= 6 && DriftCharge >= .32;
            if (!downhill && completedSlide && !ManualBoostActive)
            {
                int tier = DriftCharge >= .75 ? 2 : 1;
                BeginBoost(tier, tier == 2 ? 1.7 : 1.1, speedLimit);
            }
            if (!slideInput) DriftCharge = 0;
            IsDrifting = slideInput && Speed >= 6;
            bool boosted = BoostRemaining > 0;
            int activeTier = BoostTier;
            if (boosted)
            {
                BoostRemaining = Math.Max(0, BoostRemaining - dt);
                if (BoostRemaining < 1e-9) ClearBoost();
            }

            double previousSpeed = Speed;
            Speed = DriveAcceleration.Advance(Speed, throttle, brake, Style, speedLimit, boosted ? activeTier : 0, dt, drift);
            // Slow the existing road motion as well as the target speed, retaining
            // the slip direction instead of waiting for the low drift grip to catch up.
            if (downhill && drift && Speed < previousSpeed)
                Velocity = Velocity * Clamp(Speed / previousSpeed, 0, 1);
            double rollingSteer = Speed / (Speed + 4);
            double highSpeedStability = 1 - .18 * Clamp(Speed / 26, 0, 1);
            double steeringRate = (slideInput ? (downhill ? 2.6 : 2.85) : 2.45) * highSpeedStability *
                (.25 * throttle + (1 - .25 * throttle) * rollingSteer);
            Heading += SteeringInput * steeringRate * dt * SteeringMultiplier;
            RoadPoint facing = new RoadPoint(Math.Sin(Heading), Math.Cos(Heading));
            RoadPoint desiredVelocity = facing * Speed;
            double grip = slideInput ? (downhill ? 2.2 : 2.7) : (downhill ? 7.5 : 13);
            Velocity += (desiredVelocity - Velocity) * (1 - Math.Exp(-grip * dt));
            RoadPoint oldPosition = Position;
            Position += Velocity * dt;
            var road = Course.Project(Position, .8);
            double limit = road.HalfWidth - .8;
            bool wall = !Course.Fits(Position, .8);
            if (wall)
            {
                double side = Math.Sign(road.Lateral);
                RoadPoint right = new RoadPoint(road.Tangent.Z, -road.Tangent.X);
                Position = road.Position + right * (side * limit);
                double along = Math.Max(0, Dot(Velocity, road.Tangent));
                Velocity = road.Tangent * (along * .7);
                Speed = Magnitude(Velocity);
                ClearBoost();
                DriftCharge = 0;
                IsDrifting = false;
                boosted = false;
                if (!touchingWall && wallRecoverySpeed == 0)
                {
                    WallHits++;
                    // The kart must beat its post-hit speed too, or a fast downhill
                    // scrape that stays above the share would count again at once.
                    wallRecoverySpeed = Math.Max(WallRecoveryShare * speedLimit, Speed + WallRecoveryMargin * speedLimit);
                }
            }
            if (completedSlide && !wall)
            {
                DriftCount++;
                if (!downhill) StoredBoosts = Math.Min(2, StoredBoosts + 1);
            }
            touchingWall = wall;
            if (Speed >= wallRecoverySpeed) wallRecoverySpeed = 0;
            Sample = Course.Project(Position, .8);

            double previousProgress = projectedProgress;
            double nextProgress = NearestUnwrapped(Sample.Progress, previousProgress);
            double delta = nextProgress - previousProgress;
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
            // A legitimate forward seam crossing completes a lap even when a
            // shortcut junction consumed a projection jump without candy reward.
            while (previousProgress < (Laps + 1) * Course.Length &&
                nextProgress >= (Laps + 1) * Course.Length)
            {
                Laps++;
                if (BestLapSeconds == 0 || LapSeconds < BestLapSeconds) BestLapSeconds = LapSeconds;
                LapSeconds = 0;
            }
        }
    }
}
