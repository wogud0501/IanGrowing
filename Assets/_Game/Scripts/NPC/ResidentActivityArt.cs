using UnityEngine;

namespace IanGame
{
    public sealed class ResidentActivityArt : ScriptableObject
    {
        public Sprite[] tools;
        public Sprite cup;
        public Sprite phone;

        public Sprite Prop(ActorMotion motion)
        {
            return motion switch
            {
                ActorMotion.Sweep => tools[0],
                ActorMotion.Mop => tools[1],
                ActorMotion.Vacuum => tools[2],
                ActorMotion.WipeTable => tools[3],
                ActorMotion.WashDishes => tools[4],
                ActorMotion.CarryTrash => tools[5],
                ActorMotion.CarryLaundry => tools[6],
                ActorMotion.SeatedDrink => cup,
                ActorMotion.SeatedPhone => phone,
                _ => null,
            };
        }
    }
}
