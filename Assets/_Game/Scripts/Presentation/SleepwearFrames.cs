using UnityEngine;

namespace IanGame
{
    internal static class SleepwearFrames
    {
        static Sprite[,] poses;
        static Sprite[,] residents;
        static readonly int[] AgeColumns = { 0, 0, 0, 1, 1, 2, 2, 2, 3, 4, 5 };

        public static Sprite Pose(VisualGrowthStage stage, int frame)
        {
            return Lookup(ref poses, "FamilySleep/IanSleepwear", 6, AgeColumns[(int)stage], frame);
        }

        public static Sprite ResidentPose(int column, int frame)
        {
            return Lookup(ref residents, "FamilySleep/ResidentSleepwear", 6, column, frame);
        }

        static Sprite Lookup(ref Sprite[,] frames, string path, int columns, int column, int frame)
        {
            if (frames == null)
            {
                var source = Resources.Load<Texture2D>(path);
                frames = new Sprite[5, columns];
                for (int age = 0; age < frames.GetLength(1); age++)
                {
                    for (int pose = 0; pose < frames.GetLength(0); pose++)
                    {
                        var rect = new Rect(age * 96, (4 - pose) * 96 + 3, 96, 78);
                        frames[pose, age] = Sprite.Create(source, rect, new Vector2(.5f, 0), 78, 0,
                            SpriteMeshType.FullRect);
                    }
                }
            }

            return frames[frame, column];
        }
    }
}
