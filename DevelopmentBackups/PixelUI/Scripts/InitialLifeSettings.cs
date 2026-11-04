using UnityEngine;
namespace IanGame {
[CreateAssetMenu(menuName="Ian/Initial Life Settings")]
public class InitialLifeSettings:ScriptableObject {
 [Range(0,100)]public int hunger=100,thirst=100,knowledge=0,health=100,mental=100,social=0,credit=50,happiness=50;
 public StatSnapshot NewStats()=>new StatSnapshot{hunger=hunger,thirst=thirst,knowledge=knowledge,energy=health,mental=mental,stress=100-mental,social=social,credit=credit,happiness=happiness,money=0};
 public const long NewbornDebt=1000000;
}
}