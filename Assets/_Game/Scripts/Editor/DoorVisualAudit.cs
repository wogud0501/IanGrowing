using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace IanGame.Editor {
public static class DoorVisualAudit {
 public static void Run(){if(Application.isPlaying)throw new System.Exception("Stop Play first");EditorSceneManager.OpenScene("Assets/_Game/Scenes/LifeWorld.unity");var w=Object.FindAnyObjectByType<LifeWorldController>();Directory.CreateDirectory("Docs/DoorVisual");File.WriteAllLines("Docs/DoorVisual/ALIGNMENT_BEFORE.txt",w.GetComponentsInChildren<WorldDoor>(true).Select(d=>$"{d.portal.id}: {d.portal.zone}->{d.portal.targetZone} root={d.transform.position:F3} point={d.portal.Point:F3} plane={d.Plane:F3} normal={d.normal:F3} leaf={d.leaf.transform.position:F3} frame={d.frame.transform.position:F3} arrival={d.portal.arrival:F3} collider={d.closedCollider.bounds} "));}
}
}
