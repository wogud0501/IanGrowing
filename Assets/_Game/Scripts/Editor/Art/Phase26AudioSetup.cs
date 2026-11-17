using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace IanGame.Editor
{
    // Original, modest procedural effects. These are not represented as recorded Foley.
    public static class Phase26AudioSetup
    {
        const string Folder = "Assets/_Game/Audio/World/";
        const int Rate = 22050;

        static AudioClip Wave(string name, float seconds, Func<float, int, float> sample)
        {
            Directory.CreateDirectory(Folder);
            string path = Folder + name + ".wav";
            int count = Mathf.RoundToInt(seconds * Rate);
            if (!File.Exists(path))
            {
                using (var writer = new BinaryWriter(File.Open(path, FileMode.CreateNew)))
                {
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                    writer.Write(36 + count * 2);
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                    writer.Write(16);
                    writer.Write((short)1);
                    writer.Write((short)1);
                    writer.Write(Rate);
                    writer.Write(Rate * 2);
                    writer.Write((short)2);
                    writer.Write((short)16);
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                    writer.Write(count * 2);
                    for (int i = 0; i < count; i++)
                    {
                        writer.Write((short)(Mathf.Clamp(sample(i / (float)Rate, i), -1, 1) * short.MaxValue));
                    }
                }
            }

            AssetDatabase.ImportAsset(path);
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        public static void Attach(LifeWorldController world)
        {
            var rng = new System.Random(2606);
            Func<float> noise = () => (float)(rng.NextDouble() * 2 - 1);
            var wood = Wave(
                "Step-Wood",
                .14f,
                (t, i) => (.42f * Mathf.Sin(t * 2 * Mathf.PI * 115) + noise() * .19f) * Mathf.Exp(-t * 38)
            );
            var stone = Wave(
                "Step-Stone",
                .11f,
                (t, i) => (.25f * Mathf.Sin(t * 2 * Mathf.PI * 190) + noise() * .27f) * Mathf.Exp(-t * 45)
            );
            var door = Wave(
                "Door-Latch",
                .4f,
                (t, i) =>
                    .17f * Mathf.Sin(t * 2 * Mathf.PI * (180 + 35 * Mathf.Sin(t * 20))) * Mathf.Exp(-t * 9)
                    + noise() * .4f * Mathf.Exp(-t * 100)
            );
            float low = 0;
            var city = Wave(
                "City-Air",
                8,
                (t, i) =>
                {
                    low = low * .96f + noise() * .04f;
                    float envelope = Mathf.Pow(Mathf.Sin(Mathf.PI * t / 8), 2);
                    return (
                            low * .65f
                            + .035f * Mathf.Sin(t * 2 * Mathf.PI * 65) * (1 + Mathf.Sin(t * 2 * Mathf.PI / 8))
                        ) * envelope;
                }
            );
            low = 0;
            var room = Wave(
                "Room-Air",
                8,
                (t, i) =>
                {
                    low = low * .97f + noise() * .03f;
                    return (low * .2f + .015f * Mathf.Sin(t * 2 * Mathf.PI * 50))
                        * Mathf.Pow(Mathf.Sin(Mathf.PI * t / 8), 2);
                }
            );
            var audio = world.GetComponent<WorldLifeAudio>();
            if (audio == null)
            {
                audio = world.gameObject.AddComponent<WorldLifeAudio>();
            }

            audio.world = world;
            audio.woodStep = wood;
            audio.stoneStep = stone;
            audio.door = door;
            audio.cityAir = city;
            audio.roomAir = room;
            if (audio.effects == null)
            {
                var go = new GameObject("World effects", typeof(AudioSource));
                go.transform.SetParent(world.transform, false);
                audio.effects = go.GetComponent<AudioSource>();
                audio.effects.playOnAwake = false;
                audio.effects.spatialBlend = 0;
            }

            if (audio.ambience == null)
            {
                var go = new GameObject("World ambience", typeof(AudioSource));
                go.transform.SetParent(world.transform, false);
                audio.ambience = go.GetComponent<AudioSource>();
                audio.ambience.playOnAwake = false;
                audio.ambience.loop = true;
                audio.ambience.spatialBlend = 0;
            }

            world.audio = audio;
            EditorUtility.SetDirty(audio);
            EditorUtility.SetDirty(world);
        }
    }
}
