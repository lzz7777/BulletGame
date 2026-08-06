#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(InputSystemManager))]
public class InputTypeEditor : Editor
{
    private InputSystemAction _inputActions;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (_inputActions == null) _inputActions = new InputSystemAction();

        if (Application.isPlaying)
        {
            var tar = (InputSystemManager)target;
            foreach (var map in tar.ActionMaps) EditorGUILayout.LabelField($"“{map.name}”当前状态:{map.enabled}");
        }
    }
}
#endif