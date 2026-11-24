#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;

namespace IanQA.Tests
{
    public sealed class GrowthVisualCatalogTests
    {
        static object Catalog =>
            AssetDatabase.LoadMainAssetAtPath("Assets/_Game/Data/Characters/Growth/GrowthVisuals.asset");

        static object Field(object o, string name) => o.GetType().GetField(name).GetValue(o);

        [Test]
        public void EveryAgeHasExactlyOneVisualProfileAndOriginalAdultRemainsDistinct()
        {
            var catalog = Catalog;
            Assert.NotNull(catalog);
            var profiles = ((IEnumerable)Field(catalog, "profiles")).Cast<object>().ToArray();
            Assert.AreEqual(11, profiles.Length);
            for (int age = 0; age <= 120; age++)
            {
                var matches = profiles
                    .Where(p => age >= (int)Field(p, "minAge") && age <= (int)Field(p, "maxAge"))
                    .ToArray();
                Assert.AreEqual(1, matches.Length, "Age " + age);
                Assert.AreSame(matches[0], catalog.GetType().GetMethod("AtAge").Invoke(catalog, new object[] { age }));
            }

            Assert.AreEqual(11, profiles.Select(p => ((Sprite)Field(p, "idlePreview")).texture).Distinct().Count());
            Assert.AreEqual(19, (int)Field(profiles[8], "minAge"));
            Assert.AreEqual(30, (int)Field(profiles[9], "minAge"));
            Assert.AreEqual(60, (int)Field(profiles[10], "minAge"));
        }

        [Test]
        public void SleepwearAndQuiltRetainTheirAuthoredPixelFrames()
        {
            var source = Resources.Load<Texture2D>("FamilySleep/IanSleepwear");
            Assert.NotNull(source);
            Assert.AreEqual(FilterMode.Point, source.filterMode);
            var profile = ((IEnumerable)Field(Catalog, "profiles")).Cast<object>().First();
            var stage = Field(profile, "stage").GetType();
            var bank = stage.Assembly.GetType("IanGame.SleepwearFrames", true);
            var pose = bank.GetMethod("Pose");
            foreach (var age in Enum.GetValues(stage))
            {
                var sprites = Enumerable.Range(0, 5)
                    .Select(i => (Sprite)pose.Invoke(null, new[] { age, (object)i })).ToArray();
                Assert.IsTrue(sprites.All(s => s.texture == source && s.rect.yMin >= 0
                    && s.rect.yMax <= source.height && s.rect.xMax <= source.width), age.ToString());
                Assert.AreEqual(5, sprites.Select(s => s.rect).Distinct().Count(), age.ToString());
            }

            var quilt = Resources.Load<Texture2D>("FamilySleep/Quilt");
            Assert.NotNull(quilt);
            Assert.AreEqual(FilterMode.Point, quilt.filterMode);
            Assert.Greater(quilt.height, 21);

            var doorway = Resources.Load<Texture2D>("FamilySleep/DoorwayFamily");
            Assert.NotNull(doorway);
            Assert.AreEqual(FilterMode.Point, doorway.filterMode);
            Assert.AreEqual(4 * 96, doorway.width);
            Assert.AreEqual(2 * 96, doorway.height);
        }

        [TestCase("ResidentSleepwear", 6, 5)]
        [TestCase("ResidentHomewear", 16, 8)]
        public void ResidentialClothingSurvivesTheWorldAlphaCut(string name, int columns, int rows)
        {
            var texture = Resources.Load<Texture2D>("FamilySleep/" + name);
            Assert.NotNull(texture);
            Assert.AreEqual(columns * 96, texture.width);
            Assert.AreEqual(rows * 96, texture.height);
            Assert.AreEqual(FilterMode.Point, texture.filterMode);

            var readable = new Texture2D(2, 2);
            try
            {
                byte[] png = System.IO.File.ReadAllBytes(AssetDatabase.GetAssetPath(texture));
                Assert.IsTrue(readable.LoadImage(png));
                var pixels = readable.GetPixels32();
                Assert.IsFalse(pixels.Any(pixel => pixel.a >= 128 && pixel.a < 250),
                    "The world shader clips these body and face pixels.");
                for (int row = 0; row < rows; row++)
                {
                    for (int column = 0; column < columns; column++)
                    {
                        Assert.Greater(readable.GetPixels(column * 96, row * 96, 96, 96)
                            .Count(pixel => pixel.a > .98f), 500, $"{name} [{column}, {row}]");
                    }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(readable);
            }
        }

        [Test]
        public void InfantNeverContainsLocomotionAndAllAgeBanksHaveRealFrames()
        {
            var profiles = ((IEnumerable)Field(Catalog, "profiles")).Cast<object>().ToArray();
            for (int i = 0; i < profiles.Length; i++)
            {
                var bank = Field(profiles[i], "animations");
                var clips = ((IEnumerable)Field(bank, "clips")).Cast<object>().ToArray();
                foreach (var clip in clips)
                {
                    var frames = (Sprite[])Field(clip, "frames");
                    Assert.Greater(frames.Length, 1);
                    Assert.IsTrue(frames.All(s => s != null));
                }

                if (i == 0)
                {
                    Assert.IsFalse(clips.Any(c => new[] { "Walk", "Run" }.Contains(Field(c, "motion").ToString())));
                }
                else
                {
                    foreach (string direction in new[] { "Down", "Up", "Left", "Right" })
                    {
                        Assert.IsTrue(
                            clips.Any(c =>
                                Field(c, "motion").ToString() == "Walk" && Field(c, "direction").ToString() == direction
                            )
                        );
                    }
                }
            }
        }
    }
}
#endif
