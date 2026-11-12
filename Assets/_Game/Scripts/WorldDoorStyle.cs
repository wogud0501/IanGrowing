using System;
using UnityEngine;
namespace IanGame {
[CreateAssetMenu(menuName="Ian/World/Door animation")]
public sealed class WorldDoorStyle:ScriptableObject {
 public DoorMechanism mechanism;public Sprite[] openingFrames=Array.Empty<Sprite>();[Min(.1f)]public float seconds=.32f;
}
}
