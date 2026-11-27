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
        public void EveryAgeSelectsOneOfficialStageAndItsOwnAnimationBank()
        {
            var catalog = Catalog;
            var profiles = ((IEnumerable)Field(catalog, "profiles")).Cast<object>().ToArray();
            int[] starts = { 0, 1, 3, 5, 7, 9, 11, 13, 15, 17, 19, 22, 26, 30, 35, 40, 45, 50, 55, 65 };
            Assert.AreEqual(starts.Length, profiles.Length);
            Assert.NotNull(Field(catalog, "growthSheet"));
            Assert.AreEqual(profiles.Length, profiles.Select(p => Field(p, "animations")).Distinct().Count());
            for (int i = 0; i < starts.Length; i++)
            {
                var profile = profiles[i];
                string id = $"S{i + 1:D2}";
                int max = i == starts.Length - 1 ? int.MaxValue : starts[i + 1] - 1;
                Assert.AreEqual(starts[i], Field(profile, "minAge"));
                Assert.AreEqual(max, Field(profile, "maxAge"));
                Assert.AreEqual(id, Field(profile, "id"));
                Assert.AreEqual("Ian_" + id, ((Sprite)Field(profile, "idlePreview")).name);
                foreach (int age in new[] { starts[i], starts[i] + (max - starts[i]) / 2, max })
                {
                    Assert.AreSame(profile, catalog.GetType().GetMethod("AtAge").Invoke(catalog, new object[] { age }));
                }
            }
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
        public void LocomotionPreservesCrownHeightAndSolePivotAcrossDirections()
        {
            foreach (object profile in ((IEnumerable)Field(Catalog, "profiles")).Cast<object>())
            {
                var preview = (Sprite)Field(profile, "idlePreview");
                var clips = ((IEnumerable)Field(Field(profile, "animations"), "clips")).Cast<object>();
                foreach (object clip in clips)
                {
                    string motion = Field(clip, "motion").ToString();
                    if (motion is not ("Idle" or "Walk" or "Run" or "Turn" or "Stop"))
                    {
                        continue;
                    }
                    var frames = (Sprite[])Field(clip, "frames");
                    Assert.AreEqual(motion is "Walk" or "Run" ? 6 : 2, frames.Length);
                    var left = (Sprite[])Field(clip, "diagonalLeft") ?? Array.Empty<Sprite>();
                    var right = (Sprite[])Field(clip, "diagonalRight") ?? Array.Empty<Sprite>();
                    foreach (var sprite in frames.Concat(left).Concat(right))
                    {
                        Assert.IsTrue(sprite == preview || sprite.texture.name == preview.name + "_Motion", sprite.name);
                        Assert.AreEqual(preview.rect.height, sprite.rect.height, sprite.name);
                        Assert.AreEqual(preview.pixelsPerUnit, sprite.pixelsPerUnit, sprite.name);
                        Assert.AreEqual(preview.pivot.y, sprite.pivot.y, .01f, sprite.name);
                    }
                }
            }
        }

        [Test]
        public void AllPosesBelongToTheSelectedAgeAndOnlyMobileStagesHaveGait()
        {
            foreach (var profile in ((IEnumerable)Field(Catalog, "profiles")).Cast<object>())
            {
                var idle = (Sprite)Field(profile, "idlePreview");
                var clips = ((IEnumerable)Field(Field(profile, "animations"), "clips")).Cast<object>().ToArray();
                foreach (var clip in clips)
                {
                    var frames = (Sprite[])Field(clip, "frames");
                    Assert.That(frames.Length, Is.GreaterThan(1));
                    Assert.IsTrue(frames.All(s => s && (s == idle || s.texture.name == idle.name + "_Motion")));
                }
                var walks = clips.Where(c => Field(c, "motion").ToString() == "Walk").ToArray();
                Assert.AreEqual((int)Field(profile, "minAge") == 0 ? 0 : 4, walks.Length);
                foreach (var walk in walks)
                {
                    Assert.AreEqual(6, ((Sprite[])Field(walk, "frames")).Distinct().Count());
                    if (Field(walk, "direction").ToString() is "Down" or "Up")
                    {
                        Assert.AreEqual(6, ((Sprite[])Field(walk, "diagonalLeft")).Distinct().Count());
                        Assert.IsTrue((bool)Field(walk, "mirrorDiagonalRight"));
                    }
                }
            }
        }
    }
}
#endif
