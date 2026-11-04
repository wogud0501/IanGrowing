using UnityEngine;
namespace IanGame {
// Action IDs 0..14 are preserved for existing HUD and saved mission counters.
public enum LifeActivity { Work=0, Enroll=7, Read=8, Befriend=9, Homework=10, Exam=11, Report=12, AfterSchool=13, Study=14, Career=15, Graduate=16, Learn=17, Talk=18, Maintain=19, Resume=20, Interview=21, Promote=22, Resign=23, Headphones=24, Equipment=25, Repay=26 }
public class LifeActivityData:ScriptableObject {
 public LifeActivity action; public string displayName; [TextArea]public string message;
 public int minAge=7,maxAge=150; public LifeStage requiredStage=LifeStage.Childhood;
 [Min(1)]public int days=1; [Min(0)]public int cost; public StatDelta delta;
 public int experience=5, preparation=10, maxCount; public MissionCounter missionEvent;
 public SceneId[] locations=new SceneId[0];
}
[CreateAssetMenu(menuName="Ian/Education/School Activity")]
public class SchoolActivityData:LifeActivityData {}
}
