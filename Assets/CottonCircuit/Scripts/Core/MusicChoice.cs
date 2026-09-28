namespace CottonCircuit
{
    public enum MusicCue { None, Title, Story, Tutorial, Preparation, Machine1, Machine2, Machine3 }

    // What is on screen, gathered by GameController for the audio layer.
    public struct MusicScene
    {
        public bool Title, Story, InGame, Tutorial, Progression, ShiftExists, ShiftOpen;
        public BusinessPhase Phase;
        public GameMode Mode;
        public int Machine;
        public double RemainingSeconds;
    }

    public static class MusicChoice
    {
        public const double HurrySeconds = 30;
        public const float HurryPitch = 1.06f;

        public static MusicCue Cue(MusicScene scene)
        {
            if (scene.Story) return MusicCue.Story;
            if (scene.Title) return MusicCue.Title;
            if (!scene.InGame) return MusicCue.None;
            if (scene.Tutorial) return MusicCue.Tutorial;
            if (scene.Progression)
            {
                if (scene.Phase == BusinessPhase.Preparation) return MusicCue.Preparation;
                return scene.Phase == BusinessPhase.Operating && scene.ShiftOpen ? Machine(scene.Machine) : MusicCue.None;
            }
            if (scene.ShiftExists) return scene.ShiftOpen ? MusicCue.Machine1 : MusicCue.None;
            return scene.Mode == GameMode.Shop ? MusicCue.Preparation : scene.Mode == GameMode.Racing ? MusicCue.Machine1 : MusicCue.None;
        }

        public static MusicCue Machine(int index) => index == 1 ? MusicCue.Machine2 : index == 2 ? MusicCue.Machine3 : MusicCue.Machine1;

        public static bool Hurry(MusicScene scene) =>
            !scene.Tutorial && scene.ShiftOpen && scene.RemainingSeconds > 0 && scene.RemainingSeconds <= HurrySeconds;

        // A song with an authored ending restarts from its loop start once playback reaches loopEnd.
        public static bool WrapDue(double time, double loopEnd) => loopEnd > 0 && time >= loopEnd;

        // Machine songs resume where they stopped, so switching machines does not restart them.
        public static bool Remembers(MusicCue cue) => cue >= MusicCue.Machine1;
    }
}
