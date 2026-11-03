using System;using System.Linq;using UnityEngine;using UnityEditor;
namespace IanGame.Editor { public static class PrototypeVerification {
[MenuItem("Ian/Verify Active 2D Play Mode")] public static void RunMenu(){foreach(var line in Run())Debug.Log(line);}
public static string[] Run(){if(!Application.isPlaying)throw new InvalidOperationException("Enter MainGame2D Play Mode first.");
var game=UnityEngine.Object.FindAnyObjectByType<IanGame.GameManager>();var ui=UnityEngine.Object.FindAnyObjectByType<IanGame.UIManager>();var s=game.stats;var original=s.Capture();int originalDay=game.clock.Day;float probability=game.balance.eventChance;var random=UnityEngine.Random.state;
var results=new System.Collections.Generic.List<string>();System.Action<bool,string> check=(ok,name)=>{if(!ok)throw new Exception("FAIL "+name);results.Add("PASS "+name);};
var character=GameObject.Find("Ian 2D Character").GetComponent<IanGame.Character2DPresentation>();var slot=typeof(IanGame.SaveManager).GetField("fileName",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);string originalSlot=(string)slot.GetValue(game.saves);slot.SetValue(game.saves,"ian-verification-"+Guid.NewGuid().ToString("N")+".json");
try{
game.balance.eventChance=0;s.ResetStats(game.balance);game.clock.SetDay(1);
ui.workButton.onClick.Invoke();check(s.Money==68&&s.Energy==52&&s.Stress==35&&game.clock.Day==2,"Work button dispatch");
check(character.CurrentAction=="Work","Work sprite reaction");
ui.restButton.onClick.Invoke();check(s.Money==64&&s.Energy==80&&s.Stress==23&&game.clock.Day==3,"Rest button dispatch");check(character.CurrentAction=="Rest","Rest sprite reaction");
ui.playButton.onClick.Invoke();check(s.Money==52&&s.Happiness==70&&s.Energy==72&&game.clock.Day==4,"Play button dispatch");check(character.CurrentAction=="Play","Play sprite reaction");

s.Apply(1000,-1000,1000,1000);check(s.Happiness==100&&s.Stress==0&&s.Energy==100&&s.Money==100,"Stat clamping");
s.Restore(new IanGame.StatSnapshot());int d=game.clock.Day;game.Work();game.Play();check(game.clock.Day==d,"Blocked work/play preserve day");
ui.restButton.onClick.Invoke();check(s.Energy==28&&s.Money==0&&game.clock.Day==d+1,"Emergency rest prevents soft lock");ui.workButton.onClick.Invoke();check(s.Money==18&&s.Energy==10,"Can earn money after emergency rest");
s.ResetStats(game.balance);game.clock.SetDay(8);string message;
check(!game.saves.Load(s,game.clock,out message),"Missing save safe");
check(game.saves.Save(s,game.clock,out message),"JSON save");
s.Apply(5,5,5,5);game.clock.AdvanceDay();check(game.saves.Load(s,game.clock,out message)&&s.Money==50&&s.Happiness==50&&game.clock.Day==8,"Save/load round trip");
check(game.saves.Save(s,game.clock,out message),"Atomic save replacement");
System.IO.File.WriteAllText(game.saves.SavePath,"{bad");check(!game.saves.Load(s,game.clock,out message)&&s.Money==50&&game.clock.Day==8,"Corrupt save leaves live state intact");
game.saves.Save(s,game.clock,out message);
check(game.lifeEvents.TryEvent(s,0)==null,"Events at zero probability");
check(!string.IsNullOrEmpty(game.lifeEvents.TryEvent(s,1)),"Events at full probability");
check(ui.header.text.Contains("008")&&ui.happinessLabel.text.Contains(s.Happiness.ToString()),"Reactive HUD");
check(character.spriteRenderer!=null&&character.spriteRenderer.sprite!=null&&character.visuals.idle.Length==8,"SpriteRenderer and replaceable frame data");
check(Camera.main!=null&&Camera.main.orthographic&&Camera.main.GetComponent<AudioListener>()!=null,"Orthographic camera and audio");
Canvas.ForceUpdateCanvases();var center=RectTransformUtility.WorldToScreenPoint(null,ui.workButton.GetComponent<RectTransform>().position);var rect=ui.workButton.GetComponent<RectTransform>();center=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
var pointer=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){position=center};var hits=new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointer,hits);check(hits.Any(h=>h.gameObject==ui.workButton.gameObject),"Work button reachable by UI raycast");

var archive=GameObject.Find("ARCHIVE - 3D Elements (Inactive)"); // Find excludes inactive roots; use the scene root list.
archive=UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().First(g=>g.name.StartsWith("ARCHIVE"));
check(!archive.activeSelf&&archive.GetComponentsInChildren<Camera>(true).Length==1&&archive.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length>0,"3D elements retained but inactive");
var hudAsset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/GameHUD.prefab");var hudView=hudAsset.GetComponent<IanGame.UIManager>();
check(hudView.game==null&&hudView.saveButton!=null&&hudView.loadButton!=null,"Reusable HUD asset has no scene manager reference");
check(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Ian2DCharacter.prefab").GetComponent<IanGame.Character2DPresentation>().visuals==character.visuals,"Reusable character prefab uses shared visual data");
game.clock.SetDay(12);s.ResetStats(game.balance);ui.Bind(game);ui.Bind(game);ui.workButton.onClick.Invoke();check(game.clock.Day==13&&s.Money==68,"Repeated binding dispatches once");
ui.gameObject.SetActive(false);ui.gameObject.SetActive(true);ui.workButton.onClick.Invoke();check(game.clock.Day==14&&s.Money==86,"HUD disable/enable does not duplicate listeners");
ui.saveButton.onClick.Invoke();s.Apply(0,0,0,-10);game.clock.SetDay(21);ui.loadButton.onClick.Invoke();check(s.Money==86&&game.clock.Day==14,"Save/load buttons wired");
var hashes=results.ToArray();return hashes;
}finally{s.Restore(original);game.clock.SetDay(originalDay);game.balance.eventChance=probability;UnityEngine.Random.state=random;slot.SetValue(game.saves,originalSlot);}
}
}}
