#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace IanGame.Editor
{
    public static class DoorVisualVerification
    {
        public static IEnumerator Run(
            LifeWorldController w,
            GameManager g,
            Action<bool, string> check,
            bool captureOnly = false
        )
        {
            System.IO.Directory.CreateDirectory("Docs/DoorVisual/Screenshots");
            g.growth.Restore(new ProgressSnapshot { age = 19 });
            g.clock.realtimeEnabled = false;
            g.clock.Restore(1, new TimeSnapshot { subDaySeconds = 12 * 3600 });
            foreach (
                var id in captureOnly
                    ? new[] { "office-front", "finance-front", "house-entrance" }
                    : new[] { "front-door", "bath-door", "work-exit" }
            )
            {
                var door = w.objects.First(o => o.id == id).GetComponent<WorldDoor>();
                w.Travel(door.portal.zone);
                while (w.Busy)
                {
                    yield return null;
                }

                w.Player.Warp(door.portal.Point);
                w.follow.Snap();
                yield return new WaitForSecondsRealtime(.3f);
                door.enabled = false;
                door.ResetClosed();
                w.follow.enabled = false;
                w.follow.target.orthographicSize = 2.8f;
                w.follow.transform.position = new Vector3(
                    door.transform.position.x,
                    door.transform.position.y + .8f,
                    -10
                );
                var root = door.transform.position;
                var hinge = door.leaf.transform.position;
                var frame = door.frame.transform.position;
                try
                {
                    for (int phase = 0; phase < 3; phase++)
                    {
                        if (phase == 1)
                        {
                            door.Acquire("player", 12, out _);
                        }

                        if (phase > 0)
                        {
                            door.AdvanceAnimation(phase == 1 ? door.style.seconds * .5f : door.style.seconds);
                        }

                        var solid = door.GetComponentInChildren<WorldDoorLeafVisual>();
                        check(
                            solid != null
                                && solid.FrameSurface.transform.position
                                    == door.frame.transform.position + door.VisualOffset,
                            "Visible frame uses physical entrance anchor " + id
                        );
                        check(
                            solid != null && solid.Geometry.vertexCount >= 24,
                            "Real thickness geometry " + id + " phase " + phase
                        );
                        check(solid.FrameGeometry.vertexCount == 16, "Frame contains only four borders " + id);
                        check(door.leaf.color.a == 1, "Leaf remains opaque behind player " + id);
                        check(
                            hinge == door.leaf.transform.position
                                && frame == door.frame.transform.position
                                && root == door.transform.position,
                            "Fixed root frame and hinge " + id
                        );
                        check(
                            solid.Geometry.colors.Where((c, i) => i % 28 >= 4).All(c => c.a == 1),
                            "All solid faces opaque " + id
                        );
                        // The component is disabled solely to advance exact sample times in this test.
                        solid.Surface.enabled = true;
                        solid.FrameSurface.enabled = true;
                        yield return null;
                        ScreenCapture.CaptureScreenshot("Docs/DoorVisual/Screenshots/" + id + "-" + phase + ".png");
                        yield return new WaitForSecondsRealtime(.15f);
                    }
                }
                finally
                {
                    door.ResetClosed();
                    door.enabled = true;
                }

                if (id == "front-door")
                {
                    w.Player.Warp(door.Plane + Vector2.right * 2);
                    door.Acquire("player", 12, out _);
                    var shapes = new System.Collections.Generic.HashSet<string>();
                    for (int n = 0; n < 24; n++)
                    {
                        if (n == 10)
                        {
                            door.Release("player");
                        }

                        var solid = door.GetComponentInChildren<WorldDoorLeafVisual>();
                        shapes.Add(string.Join(";", solid.Geometry.vertices.Select(v => v.ToString("F3"))));
                        ScreenCapture.CaptureScreenshot(
                            "Docs/DoorVisual/Screenshots/Motion-" + n.ToString("D2") + ".png"
                        );
                        yield return new WaitForSecondsRealtime(.07f);
                    }

                    check(shapes.Count > 5, "Actual Update animation changes solid geometry continuously");
                    check(door.State == DoorState.Closed, "Actual Update closing restores closed pose");
                }

                w.follow.enabled = true;
                w.follow.Snap();
            }

            if (captureOnly)
            {
                foreach (string id in new[] { "office-front", "work-exit" })
                {
                    var o = w.objects.First(x => x.id == id);
                    if (w.CurrentZone.id != o.zone)
                    {
                        w.Travel(o.zone);
                        while (w.Busy)
                        {
                            yield return null;
                        }
                    }

                    w.Player.Warp(o.Point);
                    w.follow.enabled = false;
                    w.follow.target.orthographicSize = 2.8f;
                    w.follow.transform.position = new Vector3(
                        o.transform.position.x,
                        o.transform.position.y + .8f,
                        -10
                    );
                    check(w.Use(o), "Actual cinematic " + id);
                    for (int n = 0; n < 24; n++)
                    {
                        if (w.CurrentZone.id != o.zone)
                        {
                            w.follow.enabled = true;
                        }

                        ScreenCapture.CaptureScreenshot(
                            "Docs/DoorVisual/Screenshots/" + id + "-Motion-" + n.ToString("D2") + ".png"
                        );
                        yield return new WaitForSecondsRealtime(.07f);
                    }

                    while (w.Busy)
                    {
                        yield return null;
                    }

                    check(w.CurrentZone.id == o.targetZone && w.CanControl, "Cinematic passage and controls " + id);
                    w.follow.enabled = true;
                    w.follow.Snap();
                }

                yield break;
            }

            yield return CriticalRecoveryVerification.Doors(w, g, check);
            foreach (
                var pair in new[]
                {
                    new[] { "house-entrance", "front-door" },
                    new[] { "office-front", "work-exit" },
                    new[] { "finance-front", "finance-exit" },
                    new[] { "realestate-front", "realestate-exit" },
                    new[] { "bath-door", "bath-exit" },
                }
            )
            {
                for (int n = 0; n < 10; n++)
                {
                    foreach (string id in pair)
                    {
                        var o = w.objects.First(x => x.id == id);
                        if (w.CurrentZone.id != o.zone)
                        {
                            check(w.Travel(o.zone), "Pair setup " + id);
                            while (w.Busy)
                            {
                                yield return null;
                            }
                        }

                        w.Player.Warp(o.Point);
                        yield return null;
                        check(w.Use(o), "Entry/exit interaction " + n + " / " + id);
                        check(!w.Use(o), "Repeated input rejected " + id);
                        while (w.Busy)
                        {
                            yield return null;
                        }

                        check(w.CurrentZone.id == o.targetZone, "Entry/exit committed " + n + " / " + id);
                        check(w.CanControl, "Controls restored " + id);
                        var expected = g.world.rules.Zone(o.targetZone).origin + o.arrival;
                        check(
                            Vector2.Distance(w.Player.Position, expected) < .6f,
                            "Arrival matches existing destination " + id
                        );
                    }
                }
            }

            g.growth.Restore(new ProgressSnapshot { age = 0 });
            yield return CriticalRecoveryVerification.Cycles(w, g, check);
            check(
                g.SaveAutomaticCheckpoint(out _) && g.saves.TryReadAutomatic(out var saved, out _),
                "Door traversal state saves to isolated slot"
            );
            yield return CriticalRecoveryVerification.Visitors(w, g, check);
        }
    }
}
#endif
