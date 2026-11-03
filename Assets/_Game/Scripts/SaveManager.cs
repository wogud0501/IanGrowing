using System;using System.IO;using System.Text.RegularExpressions;using UnityEngine;
namespace IanGame {
[Serializable]public class SaveData {public int version=2;public int day;public StatSnapshot stats;public bool gameOver;}
public enum SaveLoadStatus {None,Loaded,Missing,Corrupt,UnsupportedVersion,IOError}
public class SaveManager:MonoBehaviour {
 [SerializeField]string fileName="ian-black-comedy-v1.json";
 public string SavePath=>Path.Combine(Application.persistentDataPath,Path.GetFileName(fileName));
 public SaveLoadStatus LastLoadStatus{get;private set;}
 public string LastRecoveryCopy{get;private set;}
 public bool Save(PlayerStats stats,TimeManager time,out string message)=>Save(stats,time,false,out message);
 public bool Save(PlayerStats stats,TimeManager time,bool gameOver,out string message){
 try{
 if(stats==null||time==null)throw new InvalidOperationException("시스템 연결 누락");
 var data=new SaveData{day=time.Day,stats=stats.Capture(),gameOver=gameOver};
 if(!Valid(data))throw new InvalidDataException("유효하지 않은 게임 상태");
 Directory.CreateDirectory(Application.persistentDataPath);
 var temp=SavePath+".tmp";File.WriteAllText(temp,JsonUtility.ToJson(data,true));
 if(File.Exists(SavePath))File.Replace(temp,SavePath,SavePath+".bak");else File.Move(temp,SavePath);
 message="저장 완료. 행복까지 저장되는 시대입니다.";return true;
 }catch(Exception e){message="저장 실패: "+e.Message;return false;}
 }
 public bool TryRead(out SaveData data,out string message){
 data=null;LastRecoveryCopy=null;
 try{
 if(!File.Exists(SavePath)){LastLoadStatus=SaveLoadStatus.Missing;message="저장된 인생이 없습니다.";return false;}
 if(new FileInfo(SavePath).Length>1024*1024)throw new InvalidDataException("저장 파일 크기 오류");
 string json=File.ReadAllText(SavePath);
 foreach(string field in new[]{"version","day","stats","happiness","stress","energy","money"})
 if(!Regex.IsMatch(json,"\\\""+field+"\\\"\\s*:"))throw new InvalidDataException("필수 저장 필드 누락: "+field);
 var value=JsonUtility.FromJson<SaveData>(json);
 if(value==null)throw new InvalidDataException("빈 저장 데이터");
 if(value.version!=1&&value.version!=2){LastLoadStatus=SaveLoadStatus.UnsupportedVersion;message="지원하지 않는 저장 버전입니다. 원본은 보존됩니다.";return false;}
 if(!Valid(value))throw new InvalidDataException("저장 값 범위 오류");
 data=value;LastLoadStatus=SaveLoadStatus.Loaded;message="불러오기 완료. 현실도 함께 돌아왔습니다.";return true;
 }catch(UnauthorizedAccessException e){LastLoadStatus=SaveLoadStatus.IOError;message="저장 접근 실패: "+e.Message;return false;}
 catch(InvalidDataException e){return Corrupt(e,out message);}
 catch(IOException e){LastLoadStatus=SaveLoadStatus.IOError;message="저장 읽기 실패: "+e.Message;return false;}
 catch(Exception e){return Corrupt(e,out message);}
 }
 bool Corrupt(Exception e,out string message){
 LastLoadStatus=SaveLoadStatus.Corrupt;message="저장 파일 손상: "+e.Message;
 try{string copy=SavePath+".corrupt-"+DateTime.UtcNow.Ticks;File.Copy(SavePath,copy,false);LastRecoveryCopy=copy;}catch(Exception backup){message+=" / 보존 복사 실패: "+backup.Message;}
 return false;
 }
 public bool Load(PlayerStats stats,TimeManager time,out string message){
 if(stats==null||time==null){message="시스템 연결 누락";return false;}
 SaveData data;if(!TryRead(out data,out message))return false;stats.Restore(data.stats);time.SetDay(data.day);return true;
 }
 static bool Valid(SaveData d)=>d!=null&&d.stats!=null&&d.day>=1&&d.day<=TimeManager.MaximumDay&&d.stats.happiness>=0&&d.stats.happiness<=100&&d.stats.stress>=0&&d.stats.stress<=100&&d.stats.energy>=0&&d.stats.energy<=100&&d.stats.money>=0;
}
}