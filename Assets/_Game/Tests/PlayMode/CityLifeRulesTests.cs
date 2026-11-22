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
