using UnityEngine;
namespace IanGame {
[System.Serializable]public class WorldPedestrianLook {public Sprite[] frames;}
[CreateAssetMenu(menuName="Ian/World/Visual Catalog")]
public sealed class WorldVisualCatalog:ScriptableObject {
 public Sprite homeShell,nightHomeShell,cityGround,bathroomShell;
 public Sprite[] signalStates;
 // Bed, fridge, table, wardrobe, sink, toilet, shower, door,
 // desk, books, chair, bench, tree, lamp, bin/planter, utility pole.
 public Sprite[] furniture;
 // Food shop, workspace, school, residence, car, traffic signal.
 public Sprite[] urban;
 public WorldPedestrianLook[] pedestrians;
 public Sprite[] homeArchitecture,homeProps;
}
}
