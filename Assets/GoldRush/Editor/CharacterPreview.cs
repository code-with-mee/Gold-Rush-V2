using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GoldRush.EditorTools
{
    /// <summary>Gold Rush > Render Character Preview: poses the cast in a temporary scene and saves
    /// screenshots (sunset + night lighting) to Temp/gr_preview/.</summary>
    public static class CharacterPreview
    {
        const string Dir = "Assets/GoldRush/Art/Characters/";

        struct Cast { public string path, clip, label; public float height, x, z, yaw, t; public Color glow; }

        static readonly Cast[] cast =
        {
            new Cast { path = Dir + "Monsters/Slime.fbx",        clip = "Walk",        label = "Slime (patroller)", height = 0.9f,  x = -2.6f, z = 0.6f, yaw = 160, t = 0.3f, glow = new Color(0.2f, 1f, 0.9f) },
            new Cast { path = Dir + "Hero/Hero.fbx",             clip = "Wave",        label = "Hero (player)",     height = 1.35f, x = -0.6f, z = -0.4f, yaw = 180, t = 0.6f, glow = new Color(1f, 0.8f, 0.5f) },
            new Cast { path = Dir + "Monsters/Ghost.fbx",        clip = "Flying_Idle", label = "Ghost (chaser)",    height = 1.2f,  x = 1.2f, z = 0.2f, yaw = 200, t = 0.4f, glow = new Color(1f, 0.3f, 0.8f) },
            new Cast { path = Dir + "Monsters/MushroomKing.fbx", clip = "Idle",        label = "Mushroom King (guard)", height = 2.6f, x = 3.6f, z = 1.8f, yaw = 215, t = 0.5f, glow = new Color(1f, 0.8f, 0.2f) },
        };

        [MenuItem("Gold Rush/Render Character Preview", priority = 22)]
        public static void Render()
        {
            Directory.CreateDirectory("Temp/gr_preview");
            Shot("sunset", false);
            Shot("night", true);
            Shot("hats", false, true);
            Debug.Log("[GoldRush] Character preview saved to Temp/gr_preview/");
        }

        static void Shot(string name, bool night, bool hats = false)
        {
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                LevelBuilder.BuildMenuDiorama();
                var camGo = new GameObject("PreviewCam");
                var cam = camGo.AddComponent<Camera>();
                cam.fieldOfView = 32;
                camGo.transform.position = new Vector3(0.4f, 2.2f, -8.2f);
                camGo.transform.LookAt(new Vector3(0.4f, 1.0f, 0.6f));
                EnvironmentSetup.Apply(cam, true);

                if (hats) PlaceHats(cam);
                else
                {
                    foreach (var c in cast) Place(c, night);
                    for (int i = 0; i < 5; i++)
                        Coin.Create(null, new Vector3(-3.2f + i * 1.7f, 0f, -2.2f + (i % 2) * 0.5f));
                }

                if (night) MakeNight(cam);

                var rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                tex.Apply();
                RenderTexture.active = null;
                File.WriteAllBytes($"Temp/gr_preview/{name}.png", tex.EncodeToPNG());
                cam.targetTexture = null;
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(tex);
            }
            finally
            {
                SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        static void Place(Cast c, bool night)
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(c.path);
            if (!src) { Debug.LogWarning("[GoldRush] missing " + c.path); return; }
            var go = (GameObject)Object.Instantiate(src);
            go.transform.position = new Vector3(c.x, 0, c.z);
            go.transform.rotation = Quaternion.Euler(0, c.yaw, 0);
            var clip = AssetDatabase.LoadAllAssetsAtPath(c.path).OfType<AnimationClip>()
                .FirstOrDefault(a => a.name == c.clip || a.name.EndsWith("|" + c.clip));
            if (clip) clip.SampleAnimation(go, clip.length * c.t);
            else Debug.LogWarning($"[GoldRush] clip {c.clip} not found in {c.path}: " +
                string.Join(", ", AssetDatabase.LoadAllAssetsAtPath(c.path).OfType<AnimationClip>().Select(a => a.name)));

            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length > 0)
            {
                var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                if (b.size.y > 0.01f) go.transform.localScale *= c.height / b.size.y;
                b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                go.transform.position += Vector3.up * (0f - b.min.y); // feet on the ground
            }
            if (night)
            {
                var l = new GameObject("Glow").AddComponent<Light>();
                l.type = LightType.Point;
                l.color = c.glow;
                l.range = c.height * 2.2f;
                l.intensity = 2.5f;
                l.transform.position = go.transform.position + new Vector3(0, c.height * 0.6f, -0.8f);
            }
        }

        /// <summary>One hero per hat, each in a different shop colour.</summary>
        static void PlaceHats(Camera cam)
        {
            var def = GameAssets.I.hero;
            for (int i = 0; i < Catalog.Hats.Length; i++)
            {
                var root = new GameObject("Hero" + i).transform;
                root.SetPositionAndRotation(new Vector3((i - 2) * 1.25f, 0, 0), Quaternion.Euler(0, 180, 0));
                var anim = CharacterFactory.BuildModel(def, root);
                var idle = AssetDatabase.LoadAllAssetsAtPath(Dir + "Hero/Hero.fbx").OfType<AnimationClip>().FirstOrDefault(a => a.name == "Idle");
                if (idle) idle.SampleAnimation(anim.gameObject, 0.2f);
                Cosmetics.Apply(anim, def, Catalog.Colors[i % Catalog.Colors.Length].id, Catalog.Hats[i].id, false);
            }
            cam.transform.position = new Vector3(0, 1.4f, -5.2f);
            cam.transform.LookAt(new Vector3(0, 0.8f, 0));
        }

        static void MakeNight(Camera cam)
        {
            RenderSettings.skybox = null;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.05f, 0.12f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.16f, 0.18f, 0.35f);
            RenderSettings.ambientEquatorColor = new Color(0.1f, 0.1f, 0.22f);
            RenderSettings.ambientGroundColor = new Color(0.05f, 0.04f, 0.08f);
            RenderSettings.fogColor = new Color(0.06f, 0.06f, 0.15f);
            foreach (var l in Object.FindObjectsByType<Light>())
                if (l.type == LightType.Directional)
                {
                    l.color = new Color(0.55f, 0.65f, 1f);
                    l.intensity = 0.45f;
                }
            var spot = new GameObject("Lantern").AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.color = new Color(1f, 0.8f, 0.5f);
            spot.intensity = 18f;
            spot.range = 12f;
            spot.spotAngle = 50f;
            spot.shadows = LightShadows.Soft;
            spot.transform.position = new Vector3(-0.6f, 5f, -2.5f);
            spot.transform.LookAt(new Vector3(-0.6f, 0f, -0.4f));
        }
    }
}
