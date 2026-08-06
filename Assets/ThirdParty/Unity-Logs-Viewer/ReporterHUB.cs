using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Reporter))]
public class ReporterHUB : MonoBehaviour
{
    public GUISkin skin;
    Reporter reporter;

    public int offsetX;
    public int offsetY;

    private void Awake() {
        reporter = GetComponent<Reporter>();
        reporter.show = false;
    }

    void OnGUI() {
        GUI.skin = skin;
        GUILayout.BeginArea(new Rect(10 + offsetX, Screen.height - 40 + offsetY, 250, 40));
        if (GUILayout.Button("Console")) {
            if (!reporter.show) {
                reporter.doShow();
            }
            else {
                reporter.doHide();
            }
        }

        GUILayout.EndArea();
    }
}