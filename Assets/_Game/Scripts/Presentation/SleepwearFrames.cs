using UnityEngine;

namespace IanGame
{
    internal static class SleepwearFrames
    {
        static Sprite[,] poses;
        static readonly int[] AgeColumns = { 0, 0, 0, 1, 1, 2, 2, 2, 3, 4, 5 };

        public static Sprite Pose(VisualGrowthStage stage, int frame)
        {
            if (poses == null)
            {
                var source = Resources.Load<Texture2D>("FamilySleep/IanSleepwear");
                poses = new Sprite[5, 6];
                for (int age = 0; age < poses.GetLength(1); age++)
                {
                    for (int pose = 0; pose < poses.GetLength(0); pose++)
                    {
                        var rect = new Rect(age * 96, (4 - pose) * 96 + 3, 96, 78);
                        poses[pose, age] = Sprite.Create(source, rect, new Vector2(.5f, 0), 78, 0,
                            SpriteMeshType.FullRect);
                    }
                }
            }

            return poses[frame, AgeColumns[(int)stage]];
        }
    }
}
