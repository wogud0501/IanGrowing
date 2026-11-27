using UnityEngine;
using UnityEngine.UI;

namespace IanGame
{
    public sealed partial class GameplayUXPresenter
    {
        Button encounterInvite;
        GameObject encounterWindow;
        string offeredEncounter;
        float offerExpiry;

        void UpdateEncounter(bool clear)
        {
            if (!clear || !game.missions.FreeLifeMode || !hud.world.CanControl)
            {
                if (encounterInvite)
                {
                    encounterInvite.gameObject.SetActive(false);
                }
                return;
            }

            if (!string.IsNullOrEmpty(offeredEncounter))
            {
                if (Time.unscaledTime < offerExpiry)
                {
                    encounterInvite.gameObject.SetActive(true);
                    return;
                }
                offeredEncounter = null;
                encounterInvite.gameObject.SetActive(false);
            }

            var state = city.Data;
            int day = game.clock.Day;
            if (day <= state.lastEncounterDay + 1)
            {
                return;
            }
            string zone = hud.world.CurrentZone?.id;
            string choice = zone switch
            {
                "shop" => "sample",
                "city" when hud.world.Service.Hour < 19 => "park",
                "alley" when hud.world.Service.Hour >= 16 => "commute",
                _ => null
            };
            if (choice == null)
            {
                return;
            }

            state.lastEncounterDay = day;
            if (choice != state.lastEncounter && Random.value < .68f)
            {
                state.lastEncounter = choice;
                offeredEncounter = choice;
                offerExpiry = Time.unscaledTime + 10;
                if (!encounterInvite)
                {
                    encounterInvite = ui.Button(card.parent, "", new Vector2(.04f,.72f),
                        new Vector2(.42f,.80f), ReadEncounter);
                }
                encounterInvite.GetComponentInChildren<TMPro.TMP_Text>().text = choice == "sample"
                    ? "무료 시식" : choice == "park" ? "공원에서 들린 인사" : "퇴근길 쪽지";
                encounterInvite.gameObject.SetActive(true);
            }
            game.SaveAutomaticCheckpoint(out _);
        }

        void ReadEncounter()
        {
            if (string.IsNullOrEmpty(offeredEncounter) || shell.Depth != 0)
            {
                return;
            }
            string id = offeredEncounter;
            offeredEncounter = null;
            encounterInvite.gameObject.SetActive(false);
            if (encounterWindow)
            {
                windows.Remove(encounterWindow);
                Destroy(encounterWindow);
            }
            encounterWindow = Window("도시의 하루");
            var parent = encounterWindow.transform;
            string line = id == "sample" ? "시식은 무료입니다. 감상평도 무료입니다."
                : id == "park" ? "산책하던 주민이 묻습니다. “오늘은 좀 걸을 만하죠?”"
                : "“귀하의 노고에 감사드립니다.” 뒷면에는 “본 쪽지에는 보상이 동봉되지 않았습니다.”";
            ui.Text(parent, "Encounter", 24, new Vector2(.09f,.46f), new Vector2(.91f,.80f)).text = line;
            string accept = id == "sample" ? "맛보기  ·  허기 +1"
                : id == "park" ? "인사하기  ·  사회성 +1" : "피식 웃기  ·  멘탈 +1";
            bool decided = false;
            void Resolve(bool participate)
            {
                if (decided || shell.Top != encounterWindow)
                {
                    return;
                }
                decided = true;
                shell.Close(encounterWindow);
                if (participate)
                {
                    var delta = id == "sample" ? new StatDelta { hunger = 1 } : id == "park"
                        ? new StatDelta { social = 1 } : new StatDelta { mental = 1 };
                    game.stats.ApplyCore(delta);
                    game.SaveAutomaticCheckpoint(out _);
                }
            }
            ui.Button(parent, accept, new Vector2(.08f,.27f), new Vector2(.92f,.40f), () => Resolve(true));
            ui.Button(parent, "지나가기", new Vector2(.08f,.09f), new Vector2(.92f,.22f), () => Resolve(false));
            Open(encounterWindow);
        }
    }
}
