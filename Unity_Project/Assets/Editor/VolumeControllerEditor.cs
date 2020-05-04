using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(VolumeController))]
public class VolumeControllerEditor : Editor
{
    bool light = true;
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        VolumeController myTarget = (VolumeController)target;

        RenderMode oldRenderMode = myTarget.GetRenderMode();
        RenderMode newRenderMode = (RenderMode)EditorGUILayout.EnumPopup("Rendermode", oldRenderMode);

        if (newRenderMode != oldRenderMode)
            myTarget.SetRenderMode(newRenderMode);

        light = EditorGUILayout.Toggle("Lightning", light);
        myTarget.SetLightning(light);

    }
}