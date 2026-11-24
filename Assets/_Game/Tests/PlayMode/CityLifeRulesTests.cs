#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace IanQA.Tests
{
    public sealed class CityLifeRulesTests
    {
        static Type T(string name) =>
            AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("IanGame." + name)).First(t => t != null);

        [Test]
        public void ASecondResidentCannotClaimTheSeatUntilStandUpFinishes()
        {
            var furniture = new GameObject("Shared sofa");
            var parent = new GameObject("Seated parent");
            try
            {
                var slot = furniture.AddComponent(T("WorldActivitySlot"));
                var seat = slot.GetType();
                var actor = parent.AddComponent(T("WorldInteractable"));
                var renderer = parent.AddComponent<SpriteRenderer>();
                actor.GetType().GetField("visual").SetValue(actor, renderer);
                var animation = parent.AddComponent(T("WorldActorAnimation"));
                animation.GetType().GetField("visual").SetValue(animation, renderer);
                parent.transform.position = new Vector3(12, -4);
                T("WorldNPCScheduler").GetMethod("PrepareArtwork",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                    .Invoke(null, new object[] { actor });
                var body = (SpriteRenderer)actor.GetType().GetField("visual").GetValue(actor);
                Assert.AreSame(parent.transform, body.transform.parent);
                Assert.AreSame(body, animation.GetType().GetField("visual").GetValue(animation));
                Assert.IsFalse(renderer.enabled);

                var reserve = seat.GetMethod("TryReserve");
                Assert.AreEqual(true, reserve.Invoke(slot, new object[] { "father" }));
                seat.GetMethod("MarkOccupied").Invoke(slot, new object[] { "father" });
                var offset = T("FurnitureSeatPose").GetMethod("Offset");
                var floor = (Vector2)parent.transform.position;
                var cushion = floor + new Vector2(.02f, -.73f);
                var previous = Vector2.zero;
                for (int frame = 0; frame <= 60; frame++)
                {
                    var pose = (Vector2)offset.Invoke(null,
                        new object[] { floor, cushion, Vector2.up, frame / 60f });
                    body.transform.localPosition = pose;
                    Assert.Less(Vector2.Distance(previous, pose), .04f);
                    Assert.AreEqual(floor, (Vector2)parent.transform.position);
                    previous = pose;
                }

                Assert.That(Vector2.Distance(cushion, body.transform.position), Is.LessThan(.0001f));
                seat.GetMethod("Release").Invoke(slot, new object[] { "other" });
                Assert.AreEqual(false, reserve.Invoke(slot, new object[] { "player" }));
                seat.GetMethod("Release").Invoke(slot, new object[] { "father" });
                Assert.AreEqual(true, reserve.Invoke(slot, new object[] { "player" }));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(furniture);
                UnityEngine.Object.DestroyImmediate(parent);
            }
        }

        [TestCase("Studio", 1)]
        [TestCase("Small", 2)]
        [TestCase("Family", 2)]
        [TestCase("Older", 2)]
        public void RegisteredResidenceSofaHasContactsOnItsVisibleCushion(string layout, int expectedPlaces)
        {
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Game/Prefabs/World/Residence" + layout + ".prefab");
            Assert.IsNotNull(prefab);
            var couch = prefab.GetComponentsInChildren<Transform>(true)
                .Single(t => t.name == "Sofa facing television");
            var art = couch.Find("Visual").GetComponent<SpriteRenderer>();
            var footprint = couch.GetComponent<BoxCollider2D>();
            Assert.IsTrue(footprint.enabled);
            Assert.AreEqual(layout == "Studio" ? "StudioArmchair" : "Settee", art.sprite.name);

            var slots = prefab.GetComponentsInChildren(T("WorldActivitySlot"), true)
                .Where(c => c.name.StartsWith("Sofa place ")).ToArray();
            Assert.AreEqual(expectedPlaces, slots.Length);
            foreach (var slot in slots)
            {
                var type = slot.GetType();
                var hip = (Vector2)type.GetProperty("Point").GetValue(slot);
                var sole = (Vector2)type.GetProperty("SolePoint").GetValue(slot);
                var floor = (Vector2)type.GetProperty("ApproachPoint").GetValue(slot);
                Assert.AreEqual(Vector2.down, (Vector2)type.GetField("facing").GetValue(slot));
                Vector2 local = art.transform.InverseTransformPoint(hip);
                Assert.That(local.x, Is.InRange(art.sprite.bounds.min.x, art.sprite.bounds.max.x));
                Assert.That(local.y, Is.InRange(art.sprite.bounds.min.y, art.sprite.bounds.max.y));
                Assert.That(sole.x, Is.EqualTo(hip.x).Within(.01f));
                Assert.Less(floor.y, sole.y);
                Assert.Less(sole.y, hip.y);
                var colliderPoint = (Vector2)footprint.transform.InverseTransformPoint(floor);
                Assert.Less(colliderPoint.y, footprint.offset.y - footprint.size.y * .5f);
            }
        }

        [Test]
        public void CalendarRestoresTheSameWeekdayAndRollsOverAtMidnight()
        {
            var host = new GameObject("Calendar test");
            try
            {
                var type = T("TimeManager");
                var clock = host.AddComponent(type);
                var snapshot = Activator.CreateInstance(T("TimeSnapshot"));
                snapshot.GetType().GetField("subDaySeconds").SetValue(snapshot, 86340d);
                var weekday = type.GetProperty("WeekdayName");
                type.GetMethod("Restore").Invoke(clock, new object[] { 5, snapshot });
                Assert.AreEqual("금요일", weekday.GetValue(clock));
                type.GetMethod("AdvanceSimulation").Invoke(clock, new object[] { 60d });
                Assert.AreEqual(6, type.GetProperty("Day").GetValue(clock));
                Assert.AreEqual("토요일", weekday.GetValue(clock));
                type.GetMethod("AdvanceDays").Invoke(clock, new object[] { 2 });
                Assert.AreEqual("월요일", weekday.GetValue(clock));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void RestoringAnInteractionBesideFurnitureKeepsTheRealFootprintOnLocalFloor()
        {
            var host = new GameObject("Location test");
            host.SetActive(false);
            var obstacle = new GameObject("Furniture edge");
            try
            {
                Vector2 origin = new Vector2(8000, 8000);
                var actor = host.AddComponent(T("WorldPlayerMotor"));
                var feet = host.GetComponent<CircleCollider2D>();
                feet.radius = .28f;
                var controller = host.AddComponent(T("LifeWorldController"));
                controller.GetType().GetField("Player").SetValue(controller, actor);
                var zone = Activator.CreateInstance(T("WorldZoneDefinition"));
                var schema = zone.GetType();
                schema.GetField("origin").SetValue(zone, origin);
                schema.GetField("size").SetValue(zone, new Vector2(20, 16));
                schema.GetField("walkableTop").SetValue(zone, 7f);
                schema.GetField("spawn").SetValue(zone, new Vector2(-8, -4));
                var block = obstacle.AddComponent<BoxCollider2D>();
                block.size = new Vector2(.1f, 2);
                obstacle.transform.position = origin + Vector2.right * .35f;
                Physics2D.SyncTransforms();
                var safe = controller.GetType().GetMethod("Safe");
                Assert.AreEqual(origin, (Vector2)safe.Invoke(controller, new object[] { zone, Vector2.zero }));

                obstacle.transform.position = origin;
                Physics2D.SyncTransforms();
                var nearby = (Vector2)safe.Invoke(controller, new object[] { zone, Vector2.zero });
                Assert.That(Vector2.Distance(origin, nearby), Is.InRange(.28f, 1f));
                Assert.IsNull(Physics2D.OverlapCircle(nearby, feet.radius));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
                UnityEngine.Object.DestroyImmediate(obstacle);
            }
        }

        [Test]
        public void PersonalSpaceBlocksBothArrivalOrdersAndAllowsRetreat()
        {
            var t = T("NPCCrowdSpace");
            var s = Activator.CreateInstance(t);
            var set = t.GetMethod("Set");
            set.Invoke(s, new object[] { "late", new Vector2(-.25f, 0) });
            set.Invoke(s, new object[] { "early", new Vector2(.25f, 0) });
            var block = t.GetMethod("Blocks");
            Assert.IsTrue((bool)block.Invoke(s, new object[] { "late", new Vector2(-.25f, 0), new Vector2(-.15f, 0) }));
            Assert.IsTrue((bool)block.Invoke(s, new object[] { "early", new Vector2(.25f, 0), new Vector2(.15f, 0) }));
            Assert.IsFalse(
                (bool)block.Invoke(s, new object[] { "late", new Vector2(-.25f, 0), new Vector2(-.35f, 0) })
            );
        }

        [Test]
        public void WalkingSegmentCannotGrazeAnOccupiedSeatBetweenClearEndpoints()
        {
            var type = T("NPCCrowdSpace");
            var space = Activator.CreateInstance(type);
            type.GetMethod("Set").Invoke(space, new object[] { "customer", Vector2.zero });
            var blocks = type.GetMethod("Blocks");
            var from = new Vector2(-.15f, .77f);
            var to = new Vector2(.15f, .77f);
            Assert.IsTrue((bool)blocks.Invoke(space, new object[] { "clerk", from, to }));
            Assert.IsFalse((bool)blocks.Invoke(space, new object[] { "clerk", from, from + Vector2.up }));
        }

        [Test]
        public void SpatialQueryDoesNotReturnAllDistantResidents()
        {
            var t = T("NPCCrowdSpace");
            var s = Activator.CreateInstance(t);
            for (int i = 0; i < 200; i++)
            {
                t.GetMethod("Set").Invoke(s, new object[] { "person-" + i, new Vector2(i * 2, 0) });
            }

            var local = ((IEnumerable)t.GetMethod("Nearby").Invoke(s, new object[] { Vector2.zero }))
                .Cast<object>()
                .ToArray();
            Assert.Less(local.Length, 4);
            t.GetMethod("Remove").Invoke(s, new object[] { "person-0" });
            Assert.IsEmpty(
                ((IEnumerable)t.GetMethod("Nearby").Invoke(s, new object[] { Vector2.zero })).Cast<object>()
            );
        }

        [Test]
        public void OptionalOldSaveIsValidAndDuplicateDiscoveryIsRejected()
        {
            var valid = T("CityLifeService").GetMethod("Valid");
            Assert.IsTrue((bool)valid.Invoke(null, new object[] { null }));
            var d = Activator.CreateInstance(T("CityLifeProgress"));
            Assert.IsTrue((bool)valid.Invoke(null, new[] { d }));
            var list = (IList)d.GetType().GetField("discovered").GetValue(d);
            list.Add("shop");
            list.Add("shop");
            Assert.IsFalse((bool)valid.Invoke(null, new[] { d }));
        }
    }
}
#endif
