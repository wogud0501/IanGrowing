using System.Linq;
using UnityEditor;
using UnityEngine;

namespace IanGame.Editor
{
    [CustomEditor(typeof(WorldInteractable))]
    public class ResidentActivityInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var actor = (WorldInteractable)target;
            if (!Application.isPlaying || actor.kind != WorldInteractionKind.NPC)
            {
                return;
            }

            var scheduler = actor.GetComponentInParent<WorldNPCScheduler>();
            var game = scheduler?.world.Service.Game;
            var npc = game?.world.rules.npcs.FirstOrDefault(n => n.npcId == actor.npcId);
            if (npc == null || !scheduler.TryInspectActivity(actor.npcId, out var activity, out var anchor))
            {
                return;
            }

            int day = game.clock.Day;
            int minute = (int)(game.clock.SubDaySeconds / 60);
            int index = WorldNPCScheduler.Schedule(npc, day, minute);
            var entry = npc.schedule[index];
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("주민 일정", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("ID / 시각", $"{npc.npcId} · D{day} {minute / 60:00}:{minute % 60:00}");
            EditorGUILayout.LabelField("현재 공간 / 행동", $"{actor.zone} · {activity}");
            EditorGUILayout.LabelField("거주지 / 직장·학교", $"{npc.homeZone} · {npc.workZone}");
            EditorGUILayout.LabelField("예정 행동", $"{entry.hour:00}:{entry.minute:00} {entry.action}");
            EditorGUILayout.LabelField("목적지", $"{entry.zone} · {entry.objectId} {entry.position}");
            EditorGUILayout.LabelField("행동 지점", anchor);
            for (int offset = 0; offset <= 7; offset++)
            {
                int mask = 1 << TimeManager.WeekdayIndex(day + offset);
                var next = npc
                    .schedule.Where(e => (e.dayMask & mask) != 0 && (offset > 0 || e.hour * 60 + e.minute > minute))
                    .OrderBy(e => e.hour * 60 + e.minute)
                    .FirstOrDefault();
                if (next != null)
                {
                    EditorGUILayout.LabelField(
                        "다음 일정",
                        $"D{day + offset} {next.hour:00}:{next.minute:00} {next.zone} · {next.action}"
                    );
                    break;
                }
            }
        }
    }
}
