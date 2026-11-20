using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Non-batch entry: opens the scene named in _coursecheck.txt, sizes the Game view and enters Play mode.
/// The runtime part is Assets/_CourseBuilder/Runtime/CourseCheck.cs.</summary>
public static class CourseRun
{
    public static void Play()
    {
        var cmd = File.ReadAllText(Path.Combine(Application.dataPath, "../_coursecheck.txt")).Trim().Split(' ');
        EditorSceneManager.OpenScene("Assets/GoldRush/Scenes/" + cmd[1] + ".unity");
        try { SizeGameView(1600, 900); } catch (Exception e) { Debug.LogWarning("[RC] game view size failed: " + e.Message); }
        EditorApplication.delayCall += () => EditorApplication.isPlaying = true;
    }

    static void SizeGameView(int w, int h)
    {
        var asm = typeof(EditorWindow).Assembly;
        var gvType = asm.GetType("UnityEditor.GameView");
        var sizesType = asm.GetType("UnityEditor.GameViewSizes");
        var single = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
        var inst = single.GetProperty("instance").GetValue(null);
        var group = sizesType.GetProperty("currentGroup").GetValue(inst);
        var sizeType = asm.GetType("UnityEditor.GameViewSize");
        var sizeTypeEnum = asm.GetType("UnityEditor.GameViewSizeType");
        var ctor = sizeType.GetConstructor(new[] { sizeTypeEnum, typeof(int), typeof(int), typeof(string) });
        var size = ctor.Invoke(new object[] { Enum.Parse(sizeTypeEnum, "FixedResolution"), w, h, "Course" });
        group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size });
        int count = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null);
        var gv = EditorWindow.GetWindow(gvType);
        gv.maximized = true;
        var m = gvType.GetMethod("SizeSelectionCallback", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        m.Invoke(gv, new object[] { count - 1, null });
        Debug.Log("[RC] game view set to " + w + "x" + h);
    }
}
