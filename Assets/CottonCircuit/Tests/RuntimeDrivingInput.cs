#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;

namespace CottonCircuit.Tests
{
    // This is synthetic player input for repeatable production tests. It deliberately
    // calls the normal Tick boundary; the game exposes no player automatic-driving mode.
    static class RuntimeDrivingInput
    {
        public static void Tick(GameController game, float seconds)
        {
            double remaining = seconds;
            while (remaining > .000001)
            {
                float step = (float)Math.Min(.05, remaining);
                CottonCircuit.AutoDrive.Input(game.World.Kart.DriveModel,
                    out double throttle, out double steering, out bool brake);
                game.Tick((float)throttle, (float)steering, brake, step);
                remaining -= step;
            }
        }
    }
}
#endif
