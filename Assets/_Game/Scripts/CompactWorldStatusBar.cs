using TMPro;
using UnityEngine;
namespace IanGame {
// Three independent layout elements replace the cramped clock/message sentence.
public sealed class CompactWorldStatusBar:MonoBehaviour {
 public TMP_Text place,time,message;public string fixedPlace;public bool world;float next;
 void Update(){if(Time.unscaledTime<next||GameContext.Instance==null)return;next=Time.unscaledTime+.25f;var game=GameContext.Instance.game;var w=game.world;int hour=(int)(game.clock.SubDaySeconds/3600),minute=(int)(game.clock.SubDaySeconds/60)%60;place.text=world&&w!=null&&w.Active?(w.State.zone=="home"?(game.economy.Housing.residenceId=="improved"?"집 · 개선":"집 · 기본"):w.rules.Zone(w.State.zone).title):fixedPlace;time.text=$"DAY {game.clock.Day}  {hour:00}:{minute:00}";message.text=hour<6?"새벽 · 조용한 생활권":hour<9?"아침 · 하루를 시작하세요":hour<18?"낮 · 각자의 하루":hour<22?"저녁 · 돌아갈 시간":"밤 · 충분히 쉬세요";}
}
}
