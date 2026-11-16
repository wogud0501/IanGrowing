using UnityEngine;
namespace IanGame {
// Serialized presentation overrides keep the furnished showcase and legacy saves
// intact. Only new lives opt into the bed-only home through starterRevision.
public sealed class StarterHomePresentation : MonoBehaviour {
 public GameObject[] optionalDecorations;
 public SpriteRenderer kitchenFloor,kitchenWall,frontThreshold;
 public Sprite woodFloor,whiteWall,legacyFloor,legacyWall,legacyThreshold,starterThreshold;
 public GameObject productionShell;
 public GameObject[] legacyArchitecture=new GameObject[0];
 public bool[] legacyArchitectureEnabled=new bool[0];
 public static bool Optional(string id){switch(id){
  case "closet":case "fridge":case "tap-home":case "kitchen-prep":
  case "meal-table":case "home-chair":case "starter-desk":case "writing-chair":
  case "bedside-table":case "entry-shoes":case "living-settee":return true;
  default:return false;
 }}
 public void Apply(bool legacy){
  if(productionShell!=null){productionShell.SetActive(!legacy);for(int i=0;i<legacyArchitecture.Length;i++)if(legacyArchitecture[i]!=null)legacyArchitecture[i].SetActive(legacy&&i<legacyArchitectureEnabled.Length&&legacyArchitectureEnabled[i]);}
  foreach(var decoration in optionalDecorations)if(decoration!=null)decoration.SetActive(legacy);
  if(frontThreshold!=null)frontThreshold.sprite=legacy?legacyThreshold:starterThreshold;
  if(kitchenFloor!=null)kitchenFloor.sprite=legacy?legacyFloor:woodFloor;
  if(kitchenWall!=null)kitchenWall.sprite=legacy?legacyWall:whiteWall;
 }
}
}
