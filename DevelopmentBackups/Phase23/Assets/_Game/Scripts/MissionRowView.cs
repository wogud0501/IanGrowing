using TMPro;using UnityEngine;
namespace IanGame {
public class MissionRowView:MonoBehaviour {
 public TMP_Text titleText,stateText,progressText;
 public void Render(MissionData data,MissionProgress progress){if(titleText!=null)titleText.text=data.Id+"  "+data.title+"  ["+(data.source==MissionSource.ConfirmedV1?"원본 v1.0":"제안")+"]";if(stateText!=null)stateText.text=StateName(progress.state)+(progress.state==MissionState.Planned?" · "+data.futurePhase:"");if(progressText!=null)progressText.text=progress.current.ToString("0")+" / "+progress.target.ToString("0");}
 public static string StateName(MissionState state){switch(state){case MissionState.Locked:return "잠김";case MissionState.Available:return "진행 가능";case MissionState.InProgress:return "진행 중";case MissionState.Completed:return "완료 · 수령 대기";case MissionState.RewardClaimed:return "보상 수령";default:return "추후 해금 예정";}}
}
}