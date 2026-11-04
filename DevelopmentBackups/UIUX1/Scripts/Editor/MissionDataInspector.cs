using UnityEditor;
namespace IanGame.Editor {
[CustomEditor(typeof(MissionData))]
public class MissionDataInspector:UnityEditor.Editor {
 public override void OnInspectorGUI(){serializedObject.Update();EditorGUI.BeginDisabledGroup(true);EditorGUILayout.PropertyField(serializedObject.FindProperty("stableId"));EditorGUI.EndDisabledGroup();DrawPropertiesExcluding(serializedObject,"m_Script","stableId");serializedObject.ApplyModifiedProperties();}
}
}
