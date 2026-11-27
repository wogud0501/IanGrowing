#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

namespace IanGame
{
    // Opt-in physical-player fixture. The one-shot file lives in the app's QA
    // directory. Every change, including lifecycle saves, uses the QA slot.
    public sealed partial class IanGaitVerification : MonoBehaviour
    {
        string directory;
        GameManager game;
        StreamWriter trace;
        StreamWriter profiles;
        readonly HashSet<(int, string, int, int, Sprite)> seen = new();
        float nextRequest;
        int frame;
        bool collecting;
        bool filming;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            string dir = Path.Combine(Application.persistentDataPath, "IanGaitQA");
            if (!File.Exists(Path.Combine(dir, "start")))
            {
                return;
            }
            var host = new GameObject("Ian gait verification");
            DontDestroyOnLoad(host);
            host.AddComponent<IanGaitVerification>().directory = dir;
        }

        IEnumerator Start()
        {
            yield return new WaitUntil(() => GameContext.Instance && GameContext.Instance.transitions
                && GameContext.Instance.transitions.CurrentScene == SceneId.MainMenu
                && !GameContext.Instance.transitions.IsTransitioning
                && FindFirstObjectByType<MainMenuPresenter>());
            string marker = Path.Combine(directory, "start");
            string requested = File.ReadAllText(marker).Trim();
            File.Delete(marker);
            game = GameContext.Instance.game;
            bool worldSession = requested is "world" or "world-resume";
            game.saves.EnterTestingChannel(worldSession ? "qa-world-realism.json" : "qa-ian-gait.json");
            trace = new StreamWriter(Path.Combine(directory, "trace.tsv"), false);
            trace.WriteLine("seconds\tage\troom\tmotion\tdirection\tframe\tsprite\tx\ty\tsx\tsy\twidth\theight");
            profiles = new StreamWriter(Path.Combine(directory, "profiles.tsv"), false) { AutoFlush = true };
            profiles.WriteLine("age\tlifeStage\tvisualStage\troom\tmotion\tsprite\trootXY\tvisualXY\trendererXY\tfeetOffset\tppu\trectW\trectH\tcolliderRadius\tcolliderOffset\tlegacyAnimator\tpxPerUnitX\tpxPerUnitY\tviewport\tquality\ttexture\tshader\tbank\tpivotPx\tstride\tgroundPivot\trenderScale");
            if ((requested == "resume" || requested == "world-resume") && game.saves.ReadContinue(out var saved, out _, out _, out _))
            {
                game.ContinueSnapshot(saved);
                GameContext.Instance.transitions.Navigate(SceneId.LifeWorld);
            }
            else
            {
                yield return new WaitUntil(() => !LegalDocumentService.NeedsConsent);
                if (!game.TryStartNewLife(out string error))
                {
                    Debug.LogError("[GaitQA] " + error);
                    yield break;
                }
                if (!int.TryParse(requested, out int age) && requested != "audit" && !worldSession)
                {
                    yield break;
                }
                var data = game.CaptureState();
                if (worldSession)
                {
                    age = 20;
                    data.stats.money = 120000;
                }
                data.birthIntroCompleted = true;
                data.progression.age = Mathf.Clamp(age, 0, 100);
                game.ContinueSnapshot(data);
                string room = age < LifeProgression.IndependentOutdoorAge ? "family-ian-bedroom" : "school";
                GameContext.Instance.transitions.NavigateRoom(room, game.world.rules.Zone(room).spawn, Vector2.down);
            }
            collecting = true;
            if (worldSession)
            {
                yield return new WaitUntil(() => game.world.Host && !Transitions.IsTransitioning && World.Player);
                game.clock.realtimeEnabled = false;
                WorldStatus();
            }
            if (requested == "audit")
            {
                yield return new WaitUntil(() => game.world.Host && !Transitions.IsTransitioning && World.Player && World.Player.growthVisual.Current != null);
                yield return AuditLifecycle();
                yield return FurnitureAudit();
            }
        }

        void LateUpdate()
        {
            if (!collecting || !game.world.Host || GameContext.Instance.transitions.IsTransitioning)
            {
                return;
            }
            var world = game.world.Host;
            var player = world.Player;
            var sprite = player.sprite;
            if (!sprite || !sprite.sprite)
            {
                return;
            }
            var scale = sprite.transform.lossyScale;
            var pos = player.Position;
            var size = sprite.bounds.size;
            if (frame < 24000)
            {
                frame++;
                trace.WriteLine(FormattableString.Invariant(
                    $"{Time.unscaledTime:F3}\t{game.growth.Age}\t{world.Room.roomId}\t{player.advanced.Motion}\t{player.advanced.Direction}\t{player.advanced.Frame}\t{sprite.sprite.name}\t{pos.x:F4}\t{pos.y:F4}\t{scale.x:F4}\t{scale.y:F4}\t{size.x:F4}\t{size.y:F4}"));
            }
            if (player.growthVisual.Current != null && seen.Add((game.growth.Age, world.Room.roomId,
                Screen.width, Screen.height, sprite.sprite)))
            {
                RecordProfile(player);
            }
            if (Time.unscaledTime < nextRequest)
            {
                return;
            }
            nextRequest = Time.unscaledTime + .25f;
            trace.Flush();
            string path = Path.Combine(directory, "command");
            if (!File.Exists(path))
            {
                return;
            }
            string action = File.ReadAllText(path).Trim();
            File.Delete(path);
            if (WorldCommand(action))
            {
                return;
            }
            if (action == "audit" && !auditing)
            {
                StartCoroutine(AuditLifecycle());
            }
            else if (action == "furniture" && !auditing)
            {
                StartCoroutine(FurnitureAudit());
            }
            else if (auditing)
            {
                return;
            }
            else if (int.TryParse(action, NumberStyles.None, CultureInfo.InvariantCulture, out int age))
            {
                var data = game.CaptureState();
                data.progression.age = age;
                game.RestoreState(data);
            }
            else if (action is "school" or "city" or "family-ian-bedroom" or "family-living")
            {
                GameContext.Instance.transitions.NavigateRoom(action, world.Service.rules.Zone(action).spawn, Vector2.down);
            }
            else if (action == "film" && !filming)
            {
                StartCoroutine(FilmSchool());
            }
            else if (action == "save")
            {
                bool saved = game.SaveForMenu(out string message);
                File.WriteAllText(Path.Combine(directory, "save-result"), saved ? "saved" : message);
            }
            else if (action is "desk" or "bed")
            {
                string id = action == "desk" ? "study-desk" : "family-bed";
                var item = world.objects.FirstOrDefault(o => o && o.id == id);
                if (item)
                {
                    player.Warp(world.Safe(world.CurrentZone, item.Point - world.CurrentZone.origin));
                }
            }
        }

        IEnumerator FilmSchool()
        {
            filming = true;
            string folder = Path.Combine(directory, "school-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
            Directory.CreateDirectory(folder);
            try
            {
                using var times = new StreamWriter(Path.Combine(folder, "seconds.tsv"));
                float start = Time.realtimeSinceStartup;
                int number = 0;
                while (Time.realtimeSinceStartup - start < 8)
                {
                    yield return new WaitForEndOfFrame();
                    var frame = ScreenCapture.CaptureScreenshotAsTexture();
                    try
                    {
                        File.WriteAllBytes(Path.Combine(folder, number.ToString("D4") + ".jpg"), frame.EncodeToJPG(84));
                        times.WriteLine((Time.realtimeSinceStartup - start).ToString("F4", CultureInfo.InvariantCulture));
                    }
                    finally
                    {
                        Destroy(frame);
                    }
                    number++;
                }
            }
            finally
            {
                filming = false;
            }
        }

        void RecordProfile(WorldPlayerMotor player)
        {
            var renderer = player.sprite;
            var sprite = renderer.sprite;
            var circle = player.GetComponent<CircleCollider2D>();
            var cam = Camera.main;
            var center = renderer.bounds.center;
            float x = cam ? Vector2.Distance(cam.WorldToScreenPoint(center), cam.WorldToScreenPoint(center + Vector3.right)) : 0;
            float y = cam ? Vector2.Distance(cam.WorldToScreenPoint(center), cam.WorldToScreenPoint(center + Vector3.up)) : 0;
            string Pair(Vector2 v) => FormattableString.Invariant($"{v.x:F4},{v.y:F4}");
            profiles.WriteLine(string.Join("\t", game.growth.Age, game.growth.Stage, player.growthVisual.Current.id,
                game.world.Host.Room.roomId, player.advanced.Motion, sprite.name, Pair(player.transform.localScale),
                Pair(player.bodyVisual.localScale), Pair(renderer.transform.localScale), Pair(renderer.transform.localPosition),
                sprite.pixelsPerUnit.ToString("F4", CultureInfo.InvariantCulture), sprite.rect.width, sprite.rect.height,
                circle.radius.ToString("F4", CultureInfo.InvariantCulture), Pair(circle.offset),
                player.locomotion && player.locomotion.enabled, x.ToString("F4", CultureInfo.InvariantCulture),
                y.ToString("F4", CultureInfo.InvariantCulture), $"{Screen.width}x{Screen.height}",
                QualitySettings.names[QualitySettings.GetQualityLevel()], sprite.texture ? sprite.texture.format.ToString() : "Unavailable", renderer.sharedMaterial ? renderer.sharedMaterial.shader.name : "Unavailable",
                player.advanced.library ? player.advanced.library.name : "Unavailable", Pair(sprite.pivot), player.growthVisual.Current.stride.ToString("F4", CultureInfo.InvariantCulture),
                sprite.bounds.min.y.ToString("F4", CultureInfo.InvariantCulture), WorldGraphicsSettings.RenderScale));
        }

        void OnDestroy()
        {
            trace?.Dispose();
            profiles?.Dispose();
        }
    }
}
#endif
