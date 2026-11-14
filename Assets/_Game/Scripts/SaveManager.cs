using System;using System.IO;using System.Text.RegularExpressions;using UnityEngine;
namespace IanGame {
[Serializable]public class SaveData { public int optionalSchema;public bool hasFinance,hasProperty,hasHousing,hasWorld;public LifeWorldSnapshot world;
 public IdleProgressSnapshot idle=new IdleProgressSnapshot();
 public int birthIntroVersion=1;public bool birthIntroCompleted=true;
 public Phase23Snapshot phase23=new Phase23Snapshot();public int version=4,day;public MissionSnapshot missions=new MissionSnapshot();public StatSnapshot stats;public bool gameOver;
 public ProgressSnapshot progression;public EconomySnapshot economy;public SurvivalSnapshot survival;public TimeSnapshot time;
 public long lastSeenUtcTicks;public string runId;public int settledDay;
}
[Serializable]class SaveHeader {public int version;}
public enum SaveLoadStatus {None,Loaded,Missing,Corrupt,UnsupportedVersion,IOError}
public class SaveManager:MonoBehaviour {
 LifeWorldService world;public void ConfigureWorld(LifeWorldService value){world=value;} IdleActivityService idle;public void ConfigureIdle(IdleActivityService value){idle=value;}
 public bool BirthIntroCompleted{get;set;}=true;
 public const string CurrentSlot="ian-black-comedy-v4.json",PreviousSlot="ian-black-comedy-v3.json",LegacySlot="ian-black-comedy-v1.json";
 [SerializeField]string fileName=CurrentSlot;string activeBase,writePath;
 Phase23Services phase23;public void ConfigurePhase23(Phase23Services value){phase23=value;} MissionService missions;public void ConfigureMissions(MissionService service){missions=service;}
 LifeProgression progression;EconomySystem economy;SurvivalSystem survival;
 public string BasePath=>Path.Combine(Application.persistentDataPath,Path.GetFileName(fileName));
 public string SavePath{get{EnsureSlot();return writePath??BasePath;}}
 public string LastSourcePath{get;private set;}public int LastSourceVersion{get;private set;}public bool WasMigrated{get;private set;}
 public SaveLoadStatus LastLoadStatus{get;private set;}public string LastRecoveryCopy{get;private set;}public long LastSavedUtcTicks{get;private set;}
 public bool WritesBlocked{get;private set;}
 void EnsureSlot(){if(activeBase==BasePath)return;activeBase=BasePath;writePath=null;WritesBlocked=false;}
 static string MigratedPath(string source)=>Path.Combine(Path.GetDirectoryName(source),Path.GetFileNameWithoutExtension(source)+".phase2-v4.json");
 
 public string AutomaticSavePath=>Path.Combine(Application.persistentDataPath,Path.GetFileNameWithoutExtension(fileName)+".autosave.json");
 #if UNITY_EDITOR || DEVELOPMENT_BUILD
 [Serializable]public struct TestingChannel {public string slot,active,path,source,recovery;public int sourceVersion;public bool blocked,migrated;public long lastSaved;public SaveLoadStatus loadStatus;}
 public TestingChannel EnterTestingChannel(string slot){
  if(string.IsNullOrEmpty(slot)||!slot.StartsWith("qa-")||Path.GetFileName(slot)!=slot)throw new ArgumentException("QA slot name required");
  var old=new TestingChannel{slot=fileName,active=activeBase,path=writePath,blocked=WritesBlocked,lastSaved=LastSavedUtcTicks,source=LastSourcePath,sourceVersion=LastSourceVersion,recovery=LastRecoveryCopy,migrated=WasMigrated,loadStatus=LastLoadStatus};fileName=slot;activeBase=null;writePath=null;WritesBlocked=false;return old;
 }
 public void RestoreTestingChannel(TestingChannel old){fileName=old.slot;activeBase=old.active;writePath=old.path;WritesBlocked=old.blocked;LastSavedUtcTicks=old.lastSaved;LastSourcePath=old.source;LastSourceVersion=old.sourceVersion;LastRecoveryCopy=old.recovery;WasMigrated=old.migrated;LastLoadStatus=old.loadStatus;}
 #endif
 struct ChannelState {public string slot,active,path;public bool blocked;}
 ChannelState EnterAutomatic(){
 var old=new ChannelState{slot=fileName,active=activeBase,path=writePath,blocked=WritesBlocked};
 fileName=Path.GetFileNameWithoutExtension(fileName)+".autosave.json";activeBase=null;writePath=null;WritesBlocked=false;return old;
 }
 void LeaveAutomatic(ChannelState old){fileName=old.slot;activeBase=old.active;writePath=old.path;WritesBlocked=old.blocked;}
 static bool DefaultBlock<T>(T value)where T:class,new()=>value==null||JsonUtility.ToJson(value)==JsonUtility.ToJson(new T());
 public static SaveData StampOptionalBlocks(SaveData data){data.optionalSchema=1;data.hasFinance=data.economy?.finance!=null;data.hasProperty=data.economy?.property!=null;data.hasHousing=data.economy?.housing!=null;data.hasWorld=data.world!=null;return data;}
 public bool SaveAutomatic(PlayerStats stats,TimeManager clock,bool gameOver,string runId,int settledDay,out string message){
 var original=EnterAutomatic();try{return Save(stats,clock,gameOver,runId,settledDay,out message);}finally{LeaveAutomatic(original);}
 }
 public bool TryReadAutomatic(out SaveData data,out string message){
 var original=EnterAutomatic();try{return TryRead(out data,out message);}finally{LeaveAutomatic(original);}
 }

 public void Configure(LifeProgression p,EconomySystem e,SurvivalSystem s){progression=p;economy=e;survival=s;}
 public void AllowExplicitNewGame(){EnsureSlot();WritesBlocked=false;}
 public bool Save(PlayerStats stats,TimeManager clock,out string message)=>Save(stats,clock,false,out message);
 public bool Save(PlayerStats stats,TimeManager clock,bool gameOver,out string message)=>Save(stats,clock,gameOver,Guid.NewGuid().ToString("N"),clock==null?1:clock.Day,out message);
 public bool Save(PlayerStats stats,TimeManager clock,bool gameOver,string runId,int settledDay,out string message){
 EnsureSlot();
 #if UNITY_EDITOR
 // A test session may outlive a scene or serialized-channel reset on Play exit.
 // Protect at the final write boundary as well as at the startup scene callback.
 if((UnityEditor.SessionState.GetBool("ian.phase26.protectWrites",false)||UnityEditor.SessionState.GetBool("ian.phase26.automationHold",false))&&!Path.GetFileName(fileName).StartsWith("phase26-")&&!Path.GetFileName(fileName).StartsWith("qa-")){Directory.CreateDirectory("Docs/FacilityStandardization");File.AppendAllText("Docs/FacilityStandardization/SAVE_WRITE_GUARD.log",DateTime.UtcNow.ToString("O")+" blocked "+Path.GetFileName(fileName)+" hold="+UnityEditor.SessionState.GetBool("ian.phase26.automationHold",false)+" play="+Application.isPlaying+"\n"+Environment.StackTrace+"\n");message="자동 검증 중 원본 슬롯 쓰기가 차단되었습니다.";return false;}
 #endif
 if(WritesBlocked){message="원본 저장 보호 중입니다. 새 인생을 명시적으로 시작하거나 저장 파일을 복구해 주세요.";return false;}
 try{
 if(stats==null||clock==null)throw new InvalidOperationException("시스템 연결 누락");
 var d=new SaveData{world=world?.Capture(),phase23=phase23==null?new Phase23Snapshot():phase23.Capture(),stats=stats.Capture(),day=clock.Day,gameOver=gameOver,progression=progression==null?new ProgressSnapshot{age=19}:progression.Capture(),economy=economy==null?new EconomySnapshot():economy.Capture(),survival=survival==null?new SurvivalSnapshot():survival.Capture(),time=clock.Capture(),lastSeenUtcTicks=DateTime.UtcNow.Ticks,runId=runId,settledDay=Mathf.Clamp(settledDay,1,clock.Day),missions=missions==null?new MissionSnapshot{startDay=clock.Day}:missions.Capture()};
 StampOptionalBlocks(d);d.birthIntroVersion=1;d.birthIntroCompleted=BirthIntroCompleted;d.idle=idle==null?new IdleProgressSnapshot():idle.Capture();
 if(!Valid(d))throw new InvalidDataException("유효하지 않은 게임 상태");
 string target=SavePath;
 if(File.Exists(target)){SaveHeader header=null;try{header=JsonUtility.FromJson<SaveHeader>(File.ReadAllText(target));}catch{}
 if(header==null||header.version!=4){target=MigratedPath(BasePath);writePath=target;}}
 if(File.Exists(target)){SaveHeader targetHeader=null;try{targetHeader=JsonUtility.FromJson<SaveHeader>(File.ReadAllText(target));}catch{}if(targetHeader!=null&&(targetHeader.version<4||targetHeader.version>4))throw new InvalidDataException("마이그레이션 대상에도 구버전 저장이 있습니다. 원본을 보존하고 새 슬롯을 선택하세요.");}
 Directory.CreateDirectory(Application.persistentDataPath);string temp=target+".tmp";File.WriteAllText(temp,JsonUtility.ToJson(d,true));
 if(File.Exists(target)){
 try{File.Replace(temp,target,target+".bak");}
 catch(PlatformNotSupportedException){File.Copy(target,target+".bak",true);File.Copy(temp,target,true);}
 }else File.Move(temp,target);
 LastSavedUtcTicks=d.lastSeenUtcTicks;message="저장 완료. 부채도 빠짐없이 보관했습니다.";return true;
 }catch(Exception e){message="저장 실패: "+e.Message;return false;}
 }
 string ResolveReadPath(){
 EnsureSlot();if(writePath!=null&&File.Exists(writePath))return writePath;
 string migrated=MigratedPath(BasePath);if(File.Exists(migrated)){writePath=migrated;return migrated;}
 if(File.Exists(BasePath))return BasePath;
 if(Path.GetFileName(fileName)==CurrentSlot){string previous=Path.Combine(Application.persistentDataPath,PreviousSlot);string previousMigrated=Path.Combine(Application.persistentDataPath,Path.GetFileNameWithoutExtension(PreviousSlot)+".phase2-v3.json");if(File.Exists(previousMigrated))return previousMigrated;if(File.Exists(previous))return previous;string legacy=Path.Combine(Application.persistentDataPath,LegacySlot);if(File.Exists(legacy))return legacy;}
 if(Path.GetFileName(fileName)==Path.GetFileNameWithoutExtension(CurrentSlot)+".autosave.json"){string previousAuto=Path.Combine(Application.persistentDataPath,Path.GetFileNameWithoutExtension(PreviousSlot)+".autosave.json");if(File.Exists(previousAuto))return previousAuto;}
 return BasePath;
 }
 public bool TryRead(out SaveData data,out string message){
 data=null;LastRecoveryCopy=null;WasMigrated=false;LastSourceVersion=0;string path=ResolveReadPath();LastSourcePath=path;
 try{
 if(!File.Exists(path)){LastLoadStatus=SaveLoadStatus.Missing;message="저장된 인생이 없습니다.";return false;}
 if(new FileInfo(path).Length>8*1024*1024)throw new InvalidDataException("저장 크기 오류");
 string json=File.ReadAllText(path);Require(json,new[]{"version","day","stats","happiness","stress","energy","money"});
 var header=JsonUtility.FromJson<SaveHeader>(json);if(header==null)throw new InvalidDataException("저장 헤더 오류");
 LastSourceVersion=header.version;
 if(header.version<1||header.version>4){WritesBlocked=true;LastLoadStatus=SaveLoadStatus.UnsupportedVersion;message="지원하지 않는 저장 버전입니다. 원본과 현재 상태를 보존합니다.";return false;}
 var value=JsonUtility.FromJson<SaveData>(json);if(value==null)throw new InvalidDataException("빈 데이터");if(!Regex.IsMatch(json,"\"world\"\\s*:"))value.world=null;if(value.economy!=null&&!Regex.IsMatch(json,"\"housing\"\\s*:"))value.economy.housing=null;
 if(value.economy!=null&&!Regex.IsMatch(json,"\\\"finance\\\"\\s*:"))value.economy.finance=null;
 if(header.version<3){if(!ValidLegacy(value))throw new InvalidDataException("구버전 저장 범위 오류");value=Migrate(value);}
 else {Require(json,new[]{"hunger","thirst","knowledge","mental","social","credit","progression","economy","survival","time","runId","settledDay","lastSeenUtcTicks","age","level","experience","socialStanding","completedGrowthIds","debt","otherAssets","processedIds","hungerRemainder","thirstRemainder","hungerDangerSeconds","thirstDangerSeconds","failed","statuses","subDaySeconds","elapsedSeconds"});if(header.version==3){value.version=4;value.missions=new MissionSnapshot{startDay=value.day};}else Require(json,new[]{"missions","activeMissionId","startDay","eventSequence","counters","seenEventIds","completedMissionIds","claimedMissionIds","walletReceiptIds","milestoneIds","unlocks","pendingRewards","progress"});}
 if(header.version<4){
 WasMigrated=true;string backup=path+".pre-phase22.bak";if(!File.Exists(backup))File.Copy(path,backup,false);LastRecoveryCopy=backup;
 bool defaultSlot=Path.GetFileName(fileName)==CurrentSlot||Path.GetFileName(fileName)==Path.GetFileNameWithoutExtension(CurrentSlot)+".autosave.json";
 writePath=defaultSlot&&path!=BasePath?BasePath:MigratedPath(BasePath);
 }
 if(!Regex.IsMatch(json,"\"phase23\"\\s*:"))value.phase23=new Phase23Snapshot();if(!Regex.IsMatch(json,"\"idle\"\\s*:"))value.idle=new IdleProgressSnapshot();if(value.optionalSchema==1){
  if(!value.hasWorld&&!DefaultBlock(value.world)||value.hasWorld&&value.world==null)throw new InvalidDataException("월드 블록 존재 표시 불일치");
  if(value.economy!=null){
   if(!value.hasFinance&&!DefaultBlock(value.economy.finance)||value.hasFinance&&value.economy.finance==null)throw new InvalidDataException("금융 블록 존재 표시 불일치");
   if(!value.hasProperty&&!DefaultBlock(value.economy.property)||value.hasProperty&&value.economy.property==null)throw new InvalidDataException("부동산 블록 존재 표시 불일치");
   if(!value.hasHousing&&!DefaultBlock(value.economy.housing)||value.hasHousing&&value.economy.housing==null)throw new InvalidDataException("주거 블록 존재 표시 불일치");
  }
  if(!value.hasWorld)value.world=null;
  if(value.economy!=null){if(!value.hasFinance)value.economy.finance=null;if(!value.hasProperty)value.economy.property=null;if(!value.hasHousing)value.economy.housing=null;}
 }
 if(!Valid(value))throw new InvalidDataException("저장 범위/구조 오류");
 if(!Regex.IsMatch(json,"\"birthIntroVersion\"\\s*:")){value.birthIntroVersion=1;value.birthIntroCompleted=true;}
 if(value.progression!=null&&!Regex.IsMatch(json,"\"walkingIntroductionSeen\"\\s*:"))value.progression.walkingIntroductionSeen=value.progression.age>LifeProgression.WalkingAge;
 data=value;WritesBlocked=false;LastLoadStatus=SaveLoadStatus.Loaded;message=WasMigrated?"기존 인생을 보존해 v4로 읽었습니다. 다음 저장은 별도 파일입니다.":"불러오기 완료. 부채도 함께 돌아왔습니다.";return true;
 }catch(UnauthorizedAccessException e){WritesBlocked=true;LastLoadStatus=SaveLoadStatus.IOError;message="저장 접근 실패: "+e.Message;return false;}
 catch(InvalidDataException e){return Corrupt(path,e,out message);}
 catch(IOException e){WritesBlocked=true;LastLoadStatus=SaveLoadStatus.IOError;message="저장 읽기 실패: "+e.Message;return false;}
 catch(Exception e){return Corrupt(path,e,out message);}
 }
 static void Require(string json,string[] fields){foreach(string field in fields)if(!Regex.IsMatch(json,"\\\""+field+"\\\"\\s*:"))throw new InvalidDataException("필수 저장 필드 누락: "+field);}
 static SaveData Migrate(SaveData old){
 var s=old.stats;s.hunger=100;s.thirst=100;s.knowledge=0;s.mental=100-s.stress;s.social=0;s.credit=50;
 return new SaveData{day=old.day,stats=s,gameOver=old.gameOver,progression=new ProgressSnapshot{age=19},economy=new EconomySnapshot(),survival=new SurvivalSnapshot(),time=new TimeSnapshot{elapsedSeconds=(old.day-1)*TimeManager.SecondsPerDay},lastSeenUtcTicks=0,runId="legacy-"+Guid.NewGuid().ToString("N"),settledDay=old.day,missions=new MissionSnapshot{startDay=old.day}};
 }
 bool Corrupt(string path,Exception e,out string message){
 LastLoadStatus=SaveLoadStatus.Corrupt;WritesBlocked=true;message="저장 손상: "+e.Message+" / 현재 인생과 원본은 초기화하지 않습니다.";
 try{string copy=path+".corrupt-"+DateTime.UtcNow.Ticks;File.Copy(path,copy,false);LastRecoveryCopy=copy;}catch(Exception backup){message+=" / 보존 복사 실패: "+backup.Message;}return false;
 }
 public void ApplyToModels(SaveData value,PlayerStats stats,TimeManager clock){BirthIntroCompleted=value.birthIntroVersion<1||value.birthIntroCompleted;missions?.BeginRestore();try{stats.Restore(value.stats);progression?.Restore(value.progression);economy?.Restore(value.economy);survival?.Restore(value.survival);clock.Restore(value.day,value.time);phase23?.Restore(value.phase23);missions?.Restore(value.missions,value.day);idle?.Restore(value.idle);world?.Restore(value.world);}finally{missions?.EndRestore();}}
 public bool Load(PlayerStats stats,TimeManager clock,out string message){if(stats==null||clock==null){message="시스템 연결 누락";return false;}SaveData d;if(!TryRead(out d,out message))return false;ApplyToModels(d,stats,clock);return true;}
 static bool Range(int v)=>v>=0&&v<=100;
 static bool Finite(double v)=>!double.IsNaN(v)&&!double.IsInfinity(v)&&v>=0;
 static bool ValidLegacy(SaveData d)=>d!=null&&d.stats!=null&&d.day>=1&&d.day<=TimeManager.MaximumDay&&Range(d.stats.happiness)&&Range(d.stats.stress)&&Range(d.stats.energy)&&d.stats.money>=0;
 static bool Valid(SaveData d){
 if(!EconomySystem.ValidFinance(d.economy)||d.economy!=null&&!EconomySystem.ValidHousing(d.economy.housing))return false;
 if(!LifeWorldService.Valid(d.world))return false; if(!IdleActivityService.Valid(d.idle))return false;
 if(!Phase23Services.Valid(d.phase23)||!MissionService.ValidSnapshot(d.missions)||!ValidLegacy(d)||!Range(d.stats.hunger)||!Range(d.stats.thirst)||!Range(d.stats.knowledge)||!Range(d.stats.mental)||!Range(d.stats.social)||!Range(d.stats.credit)||d.stats.stress!=100-d.stats.mental)return false;
 if(d.progression==null||d.progression.age<0||d.progression.level<1||d.progression.experience<0||d.economy==null||d.economy.debt<0||d.economy.otherAssets<0||d.survival==null||d.time==null||string.IsNullOrEmpty(d.runId)||d.settledDay<1||d.settledDay>d.day)return false;
 if(d.lastSeenUtcTicks<0||d.lastSeenUtcTicks>DateTime.MaxValue.Ticks||!Finite(d.time.subDaySeconds)||d.time.subDaySeconds>=TimeManager.SecondsPerDay||!Finite(d.time.elapsedSeconds))return false;
 if(d.economy.processedIds==null||d.progression.completedGrowthIds==null||d.survival.statuses==null)return false;var seen=new System.Collections.Generic.HashSet<string>();foreach(var id in d.economy.processedIds)if(string.IsNullOrWhiteSpace(id)||!seen.Add(id))return false;
 var s=d.survival;if(!Finite(s.hungerRemainder)||s.hungerRemainder>=1||!Finite(s.thirstRemainder)||s.thirstRemainder>=1||!Finite(s.hungerDangerSeconds)||!Finite(s.thirstDangerSeconds))return false;
 if(s.statuses!=null)foreach(var a in s.statuses)if(a==null||string.IsNullOrEmpty(a.id)||!Finite(a.remainingSeconds)||double.IsNaN(a.healthRemainder)||double.IsInfinity(a.healthRemainder)||double.IsNaN(a.mentalRemainder)||double.IsInfinity(a.mentalRemainder))return false;
 return true;
 }
}
}
