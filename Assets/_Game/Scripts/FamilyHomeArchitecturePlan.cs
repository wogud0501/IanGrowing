using System;
using UnityEngine;
namespace IanGame {
[Serializable]public sealed class FamilyRoomRegion {public string id,title;public Rect floor;}
[CreateAssetMenu(menuName="Ian/Housing/Family architecture plan")]
public sealed class FamilyHomeArchitecturePlan:ScriptableObject {
 public int version=2;public Vector2 size=new Vector2(24,18);public float playCameraSize=6;
 public FamilyRoomRegion[] rooms={new FamilyRoomRegion{id="parents",title="부모님 침실",floor=new Rect(-12,1,8,8)},new FamilyRoomRegion{id="ian",title="이안의 방",floor=new Rect(-4,1,8,8)},new FamilyRoomRegion{id="bath",title="가족 욕실",floor=new Rect(4,1,8,8)},new FamilyRoomRegion{id="living",title="거실",floor=new Rect(-12,-9,12,10)},new FamilyRoomRegion{id="kitchen",title="주방 · 가족 식사",floor=new Rect(0,-9,12,10)}};
 public Vector2 parentsDoor=new Vector2(-8,1),ianDoor=new Vector2(0,1),bathDoor=new Vector2(8,1);
 public Vector2 ianBed=new Vector2(0,4.8f),motherBed=new Vector2(-9,4.8f),fatherBed=new Vector2(-6,4.8f),fridge=new Vector2(10,-1.7f),cook=new Vector2(6.5f,-1.7f),sink=new Vector2(3,-1.7f),dining=new Vector2(6.5f,-7.4f),sofa=new Vector2(-8,-5.8f),tv=new Vector2(-8,-2);
}
}
