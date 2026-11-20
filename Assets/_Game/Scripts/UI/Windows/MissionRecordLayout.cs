using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IanGame
{
    public static class MissionRecordLayout
    {
        public static void Install(GameObject panel)
        {
            var frame = panel.transform.Find("PixelModalWindow") as RectTransform;
            if (!frame || frame.GetComponent<RecordWindowBounds>())
            {
                return;
            }

            var body = frame.Find("Body");
            var mission = body?.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(r => r.name == "Current Mission");
            var heading = frame.Find("Heading");
            if (!mission || !heading)
            {
                return;
            }

            var title = mission.Find("Mission Title").GetComponent<TMP_Text>();
            var group = frame.GetComponent<LayoutGroup>();
            if (group)
            {
                group.enabled = false;
            }

            frame.gameObject.AddComponent<RecordWindowBounds>();
            LifeRecordUI.Skin(frame.GetComponent<Image>(), RecordSurface.Paper);
            var header = LifeRecordUI.Surface(frame, "Mission header", RecordSurface.Charcoal, .012f, .86f, .988f, .985f).transform;
            LifeRecordUI.Label(header, title.font, "미션 기록", 30, LifeRecordUI.Paper, .028f, .08f, .72f, .92f);
            var close = heading.GetComponentInChildren<Button>(true);
            Move(close.transform, header, .83f, .18f, .976f, .82f);
            LifeRecordUI.StyleButton(close);
            var stub = LifeRecordUI.Surface(frame, "Chapter ticket", RecordSurface.Inset, .024f, .17f, .346f, .827f).transform;
            LifeRecordUI.Label(stub, title.font, "현재 챕터", 19, LifeRecordUI.Jade, .07f, .83f, .94f, .97f);
            LifeRecordUI.Print(stub, RecordPrint.Mission, .07f, .29f, .93f, .77f);
            Move(mission.Find("Chapter"), stub, .07f, .03f, .94f, .27f);
            Move(title.transform, frame, .375f, .70f, .955f, .833f);
            SetText(title, 32, LifeRecordUI.Ink);
            var progress = mission.Find("Progress");
            Move(progress, frame, .375f, .615f, .955f, .70f);
            Move(mission.Find("Mission progress rail"), frame, .375f, .59f, .955f, .609f);
            Move(mission.Find("Exact conditions and action guide"), frame, .365f, .33f, .972f, .57f);
            var reward = LifeRecordUI.Surface(frame, "Reward voucher", RecordSurface.Reward, .366f, .193f, .968f, .308f).transform;
            LifeRecordUI.Label(reward, title.font, "보상", 20, LifeRecordUI.Ink, .035f, .05f, .23f, .95f);
            Move(mission.Find("Rewards"), reward, .23f, .05f, .98f, .95f);
            var rewardText = reward.GetComponentsInChildren<TMP_Text>().Last();
            SetText(rewardText, 21, LifeRecordUI.Ink);
            var next = mission.Find("Next");
            Move(next, frame, .036f, .108f, .966f, .174f);
            var actions = mission.Find("Mission actions").GetComponentsInChildren<Button>(true);
            var links = mission.Find("Mission links").GetComponentsInChildren<Button>(true);
            var visible = actions.Concat(links).Where(b => b.gameObject.activeSelf).ToArray();
            for (int i = 0; i < visible.Length; i++)
            {
                float start = .025f + .95f * i / visible.Length;
                Move(visible[i].transform, frame, start, .018f, start + .95f / visible.Length - .008f, .102f);
                LifeRecordUI.StyleButton(visible[i], i == 0);
            }

            heading.gameObject.SetActive(false);
            body.gameObject.SetActive(false);
        }

        static void Move(Transform child, Transform parent, float left, float bottom, float right, float top)
        {
            if (child == null)
            {
                return;
            }

            var rect = (RectTransform)child;
            rect.SetParent(parent, false);
            LifeRecordUI.Place(rect, left, bottom, right, top);
        }

        public static void SetText(TMP_Text text, float size, Color color)
        {
            var scale = text.GetComponent<UITypeScale>();
            if (scale)
            {
                scale.baseSize = size;
                scale.Apply();
            }
            else
            {
                text.fontSize = size;
            }

            text.color = color;
            text.alignment = TextAlignmentOptions.MidlineLeft;
        }
    }
}
