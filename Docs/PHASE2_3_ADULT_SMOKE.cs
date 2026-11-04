// Method body executed with Unity MCP execute_code in Play Mode, after routing to School.
var g=IanGame.GameContext.Instance.game;
var original=new IanGame.SaveData {day=g.clock.Day,stats=g.stats.Capture(),progression=g.growth.Capture(),economy=g.economy.Capture(),survival=g.survival.Capture(),time=g.clock.Capture(),missions=g.missions.Capture(),phase23=g.phase23.Capture()};
bool auto=g.autoSaveAfterAction,lifecycle=g.autoSaveOnLifecycle,realtime=g.clock.realtimeEnabled;
float events=g.balance.eventChance;var random=UnityEngine.Random.state;
var passed=new System.Collections.Generic.List<string>();
try {
 g.autoSaveAfterAction=false;g.autoSaveOnLifecycle=false;g.clock.realtimeEnabled=false;g.balance.eventChance=0;
 foreach(int age in new[]{19,30,60}) {
  g.growth.Restore(new IanGame.ProgressSnapshot{age=age});g.phase23.Progress.education=new IanGame.EducationProgress();
  System.Action<int> act=id=>{g.stats.ApplyCore(new IanGame.StatDelta{hunger=100,thirst=100,health=100,mental=100,knowledge=100});if(!g.TryAction(id))throw new System.Exception("Adult action failed age="+age+" id="+id);passed.Add("PASS age "+age+" action "+id);};
  act(7);var current=g.phase23.Education.Current;var data=g.phase23.Education.Available;
  while(current.studies<data.graduationStudies)act(14);
  while(current.homework<data.graduationHomework)act(10);
  act(17);act(11);act(12);act(16);
  if(!current.graduated)throw new System.Exception("Adult graduation failed");
  passed.Add("PASS actual curriculum graduation at age "+age+" / "+current.educationId);
 }
 System.IO.File.WriteAllText("Docs/PHASE2_3_ADULT_SMOKE_RESULTS.json","{\"passed\":["+string.Join(",",passed.ConvertAll(v=>"\""+v+"\"").ToArray())+"],\"failure\":null}");
 return passed.ToArray();
} finally {
 g.saves.ApplyToModels(original,g.stats,g.clock);g.autoSaveAfterAction=auto;g.autoSaveOnLifecycle=lifecycle;g.clock.realtimeEnabled=realtime;g.balance.eventChance=events;UnityEngine.Random.state=random;
}
