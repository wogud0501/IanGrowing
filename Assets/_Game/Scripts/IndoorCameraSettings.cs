using System;
using UnityEngine;
namespace IanGame {
[Serializable]public sealed class IndoorCameraSettings {
 public bool enabled;
 public Rect bounds;
 public Vector2 offset;
 public float halfHeight=3.6f,referencePixelsPerUnit=16,settleSeconds=.14f;
 public int minScale=4,maxScale=8;
 public float QuantizedSize(int pixelHeight){
  int referenceScale=Mathf.Clamp(Mathf.RoundToInt(720f/(2*halfHeight*referencePixelsPerUnit)),minScale,maxScale);
  int scale=Mathf.Max(1,Mathf.RoundToInt(referenceScale*pixelHeight/720f));
  return pixelHeight/(2*referencePixelsPerUnit*scale);
 }
}
}
