using UnityEngine;
namespace IanGame {
// Facility furniture belongs to the world, independently of player ownership.
// A stable staff ID reserves the approach point for the complete activity.
public sealed class WorldActivitySlot:MonoBehaviour {
 public SpriteRenderer seat;public string staffId;public Vector2 facing=Vector2.up;
 public string Occupant{get;private set;}
 public Vector2 Point=>GetComponent<WorldInteractable>().Point;
 public bool TryReserve(string id){if(string.IsNullOrEmpty(id)||!string.IsNullOrEmpty(staffId)&&staffId!=id||!string.IsNullOrEmpty(Occupant)&&Occupant!=id)return false;Occupant=id;if(seat!=null)seat.enabled=false;return true;}
 public void Release(string id){if(Occupant==id){Occupant=null;if(seat!=null)seat.enabled=true;}}
 void OnDisable(){Occupant=null;if(seat!=null)seat.enabled=true;}
}
}
