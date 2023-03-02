using UnityEditor;
using UnityEngine;

public class HeaderButtonSampleWindow : EditorWindow
{
    private GUIStyle _headerButtonStyle;

    [MenuItem("Samples/Header Button")]
    public static void Open() => GetWindow<HeaderButtonSampleWindow>("Header Button");

    /// <summary>
    /// Draw buttons on the header area of the window.
    /// Automatically called by unity.
    /// </summary>
    /// <param name="position"></param>
    private void ShowButton(Rect position)
    {
        // Button style
        if (_headerButtonStyle == null)
        {
            _headerButtonStyle = new GUIStyle(GUI.skin.button)
            {
                // Remove paddings
                padding = new RectOffset()
            };
        }

        // Draw a help button
        if (GUI.Button(position, EditorGUIUtility.IconContent("_Help"), _headerButtonStyle))
        {
            Application.OpenURL("https://docs.unity3d.com/Manual/index.html");
        }
    }
}