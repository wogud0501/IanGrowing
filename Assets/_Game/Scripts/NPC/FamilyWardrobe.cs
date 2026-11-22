using UnityEngine;

namespace IanGame
{
    // Art is indexed the same way as the existing walk cycle. The renderer and collider keep their pivots.
    internal static class FamilyWardrobe
    {
        public const float ChangeDuration = .9f;
        static Sprite[,] frames;
        static Sprite[,] doorwayFrames;
        static readonly Sprite[] watchingTV = new Sprite[2];
        static readonly int[] DirectionRow = { 0, 1, 3, 2 };

        public static void Dress(WorldActorAnimation animation, string id, ActorMotion motion,
            Vector2 facing, float bedPhase)
        {
            if (!animation || !animation.visual)
            {
                return;
            }

            if (frames == null)
            {
                var sheet = Resources.Load<Texture2D>("FamilySleep/IndoorFamily");
                if (!sheet)
                {
                    return;
                }

                frames = new Sprite[9, 8];
                for (int y = 0; y < 9; y++)
                {
                    for (int x = 0; x < 8; x++)
                    {
                        int height = y == 8 ? (x % 4 == 0 ? 64 : x % 4 == 1 ? 57 : 52)
                            : y == 7 ? 62 : y == 6 ? 73 : y == 5 ? 80 : 81;
                        var rect = new Rect(x * 96, (8 - y) * 96 + 3, 96, height);
                        frames[y, x] = Sprite.Create(sheet, rect, new Vector2(.5f, 0), 43, 0, SpriteMeshType.Tight);
                    }
                }
            }

            var direction = ActorAnimationSet.Direction(facing, animation.Direction);
            int row = DirectionRow[(int)direction];
            int frame = motion == ActorMotion.Walk ? animation.Frame % 4 : 0;
            bool mirror = direction == ActorDirection.Left;
            float phase = Time.unscaledTime * 3f + (id == "family-father" ? .65f : 0);
            switch (motion)
            {
                case ActorMotion.WashDishes:
                    row = 4;
                    frame = Mathf.FloorToInt(phase) % 4;
                    break;
                case ActorMotion.Sweep:
                case ActorMotion.Vacuum:
                case ActorMotion.Mop:
                case ActorMotion.WipeTable:
                case ActorMotion.CarryTrash:
                case ActorMotion.CarryLaundry:
                    row = 6;
                    frame = Mathf.FloorToInt(phase) % 4;
                    break;
                case ActorMotion.SitDown:
                case ActorMotion.StandUp:
                case ActorMotion.SeatedIdle:
                case ActorMotion.SitRelax:
                case ActorMotion.Eat:
                case ActorMotion.SeatedDrink:
                case ActorMotion.SeatedPhone:
                case ActorMotion.TalkSit:
                    row = 7;
                    frame = motion == ActorMotion.SeatedDrink || motion == ActorMotion.Eat ? 1
                        : motion == ActorMotion.SeatedPhone ? 2 : motion == ActorMotion.TalkSit ? 3 : 0;
                    break;
            }

            if (bedPhase >= 0)
            {
                row = 8;
                frame = Mathf.Clamp(Mathf.FloorToInt(bedPhase * 4), 0, 3);
                mirror = false;
            }

            animation.visual.sprite = frames[row, (id == "family-father" ? 4 : 0) + frame];
            animation.visual.flipX = mirror;
        }

        public static void ChangeAtDoor(WorldActorAnimation animation, string id, float indoorProgress)
        {
            if (!animation || !animation.visual)
            {
                return;
            }

            if (doorwayFrames == null)
            {
                var sheet = Resources.Load<Texture2D>("FamilySleep/DoorwayFamily");
                if (!sheet)
                {
                    return;
                }

                doorwayFrames = new Sprite[2, 4];
                for (int row = 0; row < 2; row++)
                {
                    for (int column = 0; column < 4; column++)
                    {
                        var rect = new Rect(column * 96, (1 - row) * 96 + 3, 96, 81);
                        doorwayFrames[row, column] = Sprite.Create(sheet, rect, new Vector2(.5f, 0),
                            43, 0, SpriteMeshType.Tight);
                    }
                }
            }

            int frame = Mathf.Clamp(Mathf.FloorToInt(indoorProgress * 4), 0, 3);
            animation.visual.sprite = doorwayFrames[id == "family-father" ? 1 : 0, frame];
            animation.visual.flipX = false;
        }

        public static void WatchTV(WorldActorAnimation animation, string id)
        {
            int parent = id == "family-father" ? 1 : 0;
            if (!watchingTV[parent])
            {
                var back = frames[1, parent * 4];
                var rect = back.rect;
                rect.yMin += 18;
                watchingTV[parent] = Sprite.Create(back.texture, rect, new Vector2(.5f, 0),
                    back.pixelsPerUnit, 0, SpriteMeshType.Tight);
            }

            animation.visual.sprite = watchingTV[parent];
            animation.visual.flipX = false;
        }

        public static void Cook(WorldActorAnimation animation, string id, Vector2 facing)
        {
            int frame = Mathf.FloorToInt(Time.unscaledTime * 3f) % 4;
            animation.visual.sprite = frames[5, (id == "family-father" ? 4 : 0) + frame];
            animation.visual.flipX = facing.x < 0;
        }
    }
}
