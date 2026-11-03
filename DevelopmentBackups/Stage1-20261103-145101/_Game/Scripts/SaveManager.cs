using System; using System.IO; using UnityEngine;
namespace IanGame {
[Serializable] public class SaveData { public int version=1; public int day; public StatSnapshot stats; }
public class SaveManager:MonoBehaviour {
 [SerializeField] string fileName="ian-black-comedy-v1.json"; public string SavePath=>Path.Combine(Application.persistentDataPath,fileName);
 public bool Save(PlayerStats stats,TimeManager time,out string message){
 try{if(stats==null||time==null)throw new InvalidOperationException("시스템 연결 누락");
 var json=JsonUtility.ToJson(new SaveData{day=time.Day,stats=stats.Capture()},true);
 // Stage a complete file before replacing the previous save.
 var temp=SavePath+".tmp"; File.WriteAllText(temp,json);
 if(File.Exists(SavePath))File.Replace(temp,SavePath,SavePath+".bak");else File.Move(temp,SavePath);
 message="저장 완료. 행복까지 저장되는 시대입니다.";return true;
 }catch(Exception e){message="저장 실패: "+e.Message;return false;}
 }
 public bool Load(PlayerStats stats,TimeManager time,out string message){
 try{if(!File.Exists(SavePath)){message="저장된 인생이 없습니다.";return false;}
 var d=JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
 if(d==null||d.version!=1||d.stats==null||d.day<1||d.day>999999||!Valid(d.stats))throw new InvalidDataException("저장 데이터 형식 오류");
 if(stats==null||time==null)throw new InvalidOperationException("시스템 연결 누락");
 stats.Restore(d.stats);time.SetDay(d.day);message="불러오기 완료. 현실도 함께 돌아왔습니다.";return true;
 }catch(Exception e){message="불러오기 실패: "+e.Message;return false;}
 }
 bool Valid(StatSnapshot s)=>s.happiness>=0&&s.happiness<=100&&s.stress>=0&&s.stress<=100&&s.energy>=0&&s.energy<=100&&s.money>=0&&s.money<=100;
}
}