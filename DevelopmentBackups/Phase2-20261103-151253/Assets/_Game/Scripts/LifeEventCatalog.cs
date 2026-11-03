using System;using UnityEngine;
namespace IanGame {
[Serializable]public class LifeEvent {
 public string title;[TextArea]public string description;[Min(0)]public float weight=1;
 public int happiness,stress,energy,money;
}
[CreateAssetMenu(menuName="Ian/Life Event Catalog")]
public class LifeEventCatalog:ScriptableObject {public LifeEvent[] events=new LifeEvent[0];}
}