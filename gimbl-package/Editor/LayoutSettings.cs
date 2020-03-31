using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LayoutSettings
{
    //Layouts.
    public static GUILayoutOption editWidth = GUILayout.Width(330);
    public static GUILayoutOption editFieldOp = GUILayout.Width(300);
    public static GUILayoutOption tabTextOp = GUILayout.Width(50);
    public static GUILayoutOption buttonOp = GUILayout.Width(100);
    public static GUILayoutOption linkObjectLayout = GUILayout.Width(147);
    public static GUILayoutOption linkFieldLayout = GUILayout.Width(150);
    public static GUIStyle linkFieldStyle = new GUIStyle() { alignment = TextAnchor.MiddleLeft, normal = UnityEditor.EditorStyles.label.normal, fontStyle = FontStyle.Normal, richText = true, fixedWidth = 10};
    public static GUIStyle sectionLabel = new GUIStyle() { alignment = TextAnchor.MiddleLeft, normal = UnityEditor.EditorStyles.label.normal, fontSize = 12, fontStyle = FontStyle.Bold, richText = true };
    public static GUIStyle controllerLabel = new GUIStyle() { alignment = TextAnchor.MiddleLeft, normal = UnityEditor.EditorStyles.label.normal, fontSize = 12, fontStyle = FontStyle.Bold, richText = true };
    public static SubBox subBox = new SubBox();
    public static MainBox mainBox = new MainBox();
}

public class MainBox
{
    public GUIStyle style;
    public MainBox()
    {
        style = new GUIStyle("HelpBox");
        style.margin = new RectOffset(10, 10, 10, 5);
        style.padding = new RectOffset(10, 5, 5, 15);
        style.fixedWidth = 350;
    }
}

public class SubBox
{
    public GUIStyle style;
    public SubBox()
    {
        style = new GUIStyle("HelpBox");
        style.margin = new RectOffset(15, 15, 10, 5);
        style.padding = new RectOffset(10,5,5,15);
    }
}

