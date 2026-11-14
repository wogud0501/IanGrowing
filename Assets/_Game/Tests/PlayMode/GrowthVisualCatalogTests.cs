#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
namespace IanQA.Tests {
public sealed class GrowthVisualCatalogTests {
 static object Catalog=>AssetDatabase.LoadMainAssetAtPath("Assets/_Game/Data/AgeGrowth/GrowthVisuals.asset");
 static object Field(object o,string name)=>o.GetType().GetField(name).GetValue(o);
 [Test]public void EveryAgeHasExactlyOneVisualProfileAndOriginalAdultRemainsDistinct(){var catalog=Catalog;Assert.NotNull(catalog);var profiles=((IEnumerable)Field(catalog,"profiles")).Cast<object>().ToArray();Assert.AreEqual(9,profiles.Length);for(int age=0;age<=120;age++){var matches=profiles.Where(p=>age>=(int)Field(p,"minAge")&&age<=(int)Field(p,"maxAge")).ToArray();Assert.AreEqual(1,matches.Length,"Age "+age);Assert.AreSame(matches[0],catalog.GetType().GetMethod("AtAge").Invoke(catalog,new object[]{age}));}Assert.AreEqual(9,profiles.Select(p=>((Sprite)Field(p,"idlePreview")).texture).Distinct().Count());Assert.AreEqual(19,(int)Field(profiles.Last(),"minAge"));}
 [Test]public void InfantNeverContainsLocomotionAndAllAgeBanksHaveRealFrames(){var profiles=((IEnumerable)Field(Catalog,"profiles")).Cast<object>().ToArray();for(int i=0;i<profiles.Length;i++){var bank=Field(profiles[i],"animations");var clips=((IEnumerable)Field(bank,"clips")).Cast<object>().ToArray();foreach(var clip in clips){var frames=(Sprite[])Field(clip,"frames");Assert.Greater(frames.Length,1);Assert.IsTrue(frames.All(s=>s!=null));}if(i==0)Assert.IsFalse(clips.Any(c=>new[]{"Walk","Run"}.Contains(Field(c,"motion").ToString())));else foreach(string direction in new[]{"Down","Up","Left","Right"})Assert.IsTrue(clips.Any(c=>Field(c,"motion").ToString()=="Walk"&&Field(c,"direction").ToString()==direction));}}
}
}
#endif
