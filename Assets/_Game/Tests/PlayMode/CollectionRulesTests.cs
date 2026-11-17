#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace IanQA.Tests
{
    public sealed class CollectionRulesTests
    {
        static Type T(string id) =>
            AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("IanGame." + id)).First(t => t != null);

        [Test]
        public void ProductionRandomSamplerUsesWeightedDistribution()
        {
            var t = T("GachaService");
            var g = Activator.CreateInstance(
                t,
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new object[] { null },
                null
            );
            var pick = t.GetMethod("Pick", BindingFlags.Instance | BindingFlags.NonPublic);
            var odds = new Dictionary<string, double> { { "usual", .9 }, { "rare", .1 } };
            int rare = 0;
            for (int i = 0; i < 10000; i++)
            {
                if ((string)pick.Invoke(g, new object[] { odds }) == "rare")
                {
                    rare++;
                }
            }

            Assert.That(
                rare,
                Is.InRange(800, 1200),
                "Actual service RNG must follow weights, not equal item selection"
            );
        }

        [Test]
        public void EquipmentModifiersNeverAccumulateOrOverwriteSavedStats()
        {
            var go = new GameObject("Collection stat test");
            try
            {
                var t = T("PlayerStats");
                var stats = go.AddComponent(t);
                var snapshot = Activator.CreateInstance(T("StatSnapshot"));
                snapshot.GetType().GetField("mental").SetValue(snapshot, 45);
                snapshot.GetType().GetField("stress").SetValue(snapshot, 55);
                t.GetMethod("Restore").Invoke(stats, new[] { snapshot });
                var m = Activator.CreateInstance(T("StatModifier"));
                m.GetType().GetField("stat").SetValue(m, Enum.Parse(T("ModifierTarget"), "Mental"));
                m.GetType().GetField("value").SetValue(m, 5f);
                var array = Array.CreateInstance(T("StatModifier"), 1);
                array.SetValue(m, 0);
                for (int i = 0; i < 20; i++)
                {
                    t.GetMethod("SetItemModifiers").Invoke(stats, new object[] { array });
                }

                Assert.AreEqual(50, t.GetProperty("Mental").GetValue(stats));
                var raw = t.GetMethod("Capture").Invoke(stats, null);
                Assert.AreEqual(45, raw.GetType().GetField("mental").GetValue(raw));
                Assert.AreEqual(55, raw.GetType().GetField("stress").GetValue(raw));
                t.GetMethod("SetItemModifiers")
                    .Invoke(stats, new object[] { Array.CreateInstance(T("StatModifier"), 0) });
                Assert.AreEqual(45, t.GetProperty("Mental").GetValue(stats));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void SaveValidatorRejectsDuplicatedEquipmentSlots()
        {
            var s = Activator.CreateInstance(T("ItemProgressSnapshot"));
            var item = Activator.CreateInstance(T("InventoryEntry"));
            item.GetType().GetField("itemId").SetValue(item, "sample");
            item.GetType().GetField("count").SetValue(item, 1);
            ((IList)s.GetType().GetField("inventory").GetValue(s)).Add(item);
            var equipment = (IList)s.GetType().GetField("equipment").GetValue(s);
            for (int i = 0; i < 2; i++)
            {
                var e = Activator.CreateInstance(T("EquipmentEntry"));
                e.GetType().GetField("itemId").SetValue(e, "sample");
                equipment.Add(e);
            }

            Assert.IsFalse((bool)T("ItemService").GetMethod("Valid").Invoke(null, new[] { s }));
            Assert.IsTrue((bool)T("ItemService").GetMethod("Valid").Invoke(null, new object[] { null }));
        }
    }
}
#endif
