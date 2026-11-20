using UnityEngine;
using UnityEditor;
using System.Linq;
public static class Diag
{
    public static void Sandbox()
    {
        CourseBuilder.Open("Sandbox");
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            CourseBuilder.Log($"{r.name} bounds {r.bounds.center} size {r.bounds.size} mat {string.Join(",", r.sharedMaterials.Select(m => m ? m.name + "/" + m.shader.name : "null"))}");
        CourseBuilder.Shot("00", "diag.jpg", new Vector3(6, 5, -10), new Vector3(20, -30, 0));
    }
}
