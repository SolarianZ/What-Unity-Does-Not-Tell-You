using UnityEditor;
using UnityEngine;

public class HeaderButtonSampleWindow : EditorWindow
{
    [MenuItem("Samples/Header Button")]
    public static void Open() => GetWindow<HeaderButtonSampleWindow>("Header Button");

    /// <summary>
    /// Draw buttons on the header area of the window.
    /// Automatically called by unity.
    /// </summary>
    /// <param name="position"></param>
    private void ShowButton(Rect position)
    {
        // draw button
        if (GUI.Button(position, EditorGUIUtility.IconContent("_Help"), GUI.skin.FindStyle("IconButton")))
        {
            Application.OpenURL("https://docs.unity3d.com/Manual/index.html");
        }
    }
}
