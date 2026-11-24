using UnityEngine;

namespace IanGame
{
    // The three chair backs and the table share a single painted sprite. Only the
    // tabletop is drawn in front; each diner keeps their own seated artwork.
    [DefaultExecutionOrder(2000)]
    public sealed class FamilySharedDiningPresentation : MonoBehaviour
    {
        public LifeWorldController world;
        public WorldActivitySlot mother;
        public WorldActivitySlot father;
        public WorldActivitySlot player;
        public SpriteRenderer visual;
        public Sprite[] frames;
        public int Mask { get; private set; }
        public int Frame { get; private set; }
        public int[] ActorFrames { get; } = new int[3];

        public void BindChairs()
        {
            if (!visual || frames == null || frames.Length == 0 || !mother || !father || !player)
            {
                return;
            }

            visual.sprite = frames[0];
            var bounds = visual.bounds;
            Bind(mother, .295f);
            Bind(father, .51f);
            Bind(player, .723f);
            mother.AddForeground(visual, .67f);

            void Bind(WorldActivitySlot seat, float across)
            {
                float x = bounds.min.x + across * bounds.size.x;
                seat.Surface = visual;
                seat.SetSeatContacts(new Vector2(x, bounds.min.y + bounds.size.y * .71f),
                    new Vector2(x, bounds.min.y + bounds.size.y * .34f));
            }
        }

        void LateUpdate()
        {
            if (!world || world.Service == null || !visual)
            {
                return;
            }

            Mask = (mother.IsOccupied ? 1 : 0) | (father.IsOccupied ? 2 : 0) | (player.IsOccupied ? 4 : 0);
            Frame = ShowDiner(mother, 0);
            ShowDiner(father, 1);
            ShowDiner(player, 2);
        }

        int ShowDiner(WorldActivitySlot seat, int index)
        {
            int frame = Mathf.FloorToInt(Time.time * 2.6f + index * 1.35f) % 4;
            var pose = seat.OccupiedPose;
            ActorFrames[index] = seat.IsOccupied && pose && pose.SetDiningFrame(frame) ? frame : 0;
            return ActorFrames[index];
        }
    }
}
