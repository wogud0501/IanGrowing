using System;
using System.Collections.Generic;
using UnityEngine;
namespace IanGame {
// Light() in the existing world delegates here. This is the only tint writer
// for bound scenes; it never changes game time, stats, permissions or saves.
public sealed class SceneVisualPresentation:MonoBehaviour {
 public LifeWorldController world;public SceneVisualProfile profile;
 public SpriteRenderer[] emissive=Array.Empty<SpriteRenderer>();
 public event Action<string> AudioCueRequested;
 public VisualWeather Weather=>profile==null?VisualWeather.Clear:profile.weather;
 public void ApplyLighting(int hour,Dictionary<SpriteRenderer,Color> bases,SpriteRenderer[] lamps,bool lowDetail){
  if(profile==null)return;var tint=Color.Lerp(Color.white,profile.AtHour(hour),profile.paletteStrength);
  foreach(var pair in bases){var renderer=pair.Key;if(renderer==null||Array.IndexOf(lamps,renderer)>=0)continue;var c=pair.Value;renderer.color=new Color(c.r*tint.r,c.g*tint.g,c.b*tint.b,c.a);}
  bool night=hour<6||hour>=19;foreach(var lamp in lamps)if(lamp!=null)lamp.enabled=night&&!lowDetail;
  foreach(var light in emissive)if(light!=null)light.color=night?new Color(1,.89f,.69f,1):Color.white;
  if(world.Player!=null){var c=world.Player.sprite.color;world.Player.sprite.color=new Color(tint.r,tint.g,tint.b,c.a);}
  if(world.follow?.target!=null)world.follow.target.backgroundColor=profile.surround;
 }
 public void RequestAudio(string cue){AudioCueRequested?.Invoke(cue);}
}
}
