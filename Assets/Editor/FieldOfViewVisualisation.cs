using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(FieldOfView))]
public class FieldOfViewVisualisation : Editor
{
    void OnSceneGUI()
    {
        FieldOfView fov = (FieldOfView)target;

        //Colors
        Color blue = new Color(0, 0, 1, 0.5f);
        Color red = new Color(1, 0, 0, 0.5f);

        Handles.color = (fov.ShowEditorUI() && fov.CanSeePlayerTest()) ? red : blue;

        Vector3 viewAngleA = fov.DirFromAngle(-fov.viewAngle / 2, false);
        Vector3 viewAngleB = fov.DirFromAngle(fov.viewAngle / 2, false);

        //Handles.DrawWireArc(fov.eyePoint.position, Vector3.up, viewAngleA, fov.viewAngle, fov.viewRadius);
        Handles.DrawSolidArc(fov.eyePoint.position, Vector3.up, viewAngleA, fov.viewAngle, fov.viewRadius);
        Handles.DrawLine(fov.eyePoint.position, fov.eyePoint.position + viewAngleA * fov.viewRadius);
        Handles.DrawLine(fov.eyePoint.position, fov.eyePoint.position + viewAngleB * fov.viewRadius);
    }
}
