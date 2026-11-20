using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GoldRush.EditorTools
{
    /// <summary>
    /// Turns things the game makes in code into normal asset files, so students can see and use them:
    /// every sound as a .wav in Art/Audio (listed in GoldRushAssets.sounds) and the floor grid as a .png.
    /// </summary>
    public static class CourseExporter
    {
        public const string AudioDir = "Assets/GoldRush/Art/Audio/";
        public const string TextureDir = "Assets/GoldRush/Art/Textures/";

        [MenuItem("Gold Rush/Course/Export Sounds (.wav)", priority = 80)]
        public static void ExportSounds()
        {
            Directory.CreateDirectory(AudioDir);
            var clips = Synth.BuildAll();
            foreach (var kv in clips) WriteWav(AudioDir + kv.Key + ".wav", kv.Value);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var list = new List<AudioClip>();
            foreach (var id in clips.Keys)
            {
                string path = AudioDir + id + ".wav";
                var imp = (AudioImporter)AssetImporter.GetAtPath(path);
                var s = imp.defaultSampleSettings;
                bool music = id == "music";
                s.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
                s.compressionFormat = AudioCompressionFormat.Vorbis;
                s.quality = 0.7f;
                imp.defaultSampleSettings = s;
                imp.forceToMono = true;
                imp.SaveAndReimport();
                list.Add(AssetDatabase.LoadAssetAtPath<AudioClip>(path));
            }

            var ga = Resources.Load<GameAssets>("GoldRushAssets");
            if (ga)
            {
                ga.sounds = list.Where(c => c).ToArray();
                EditorUtility.SetDirty(ga);
                AssetDatabase.SaveAssets();
            }
            Debug.Log($"[GoldRush] Saved {list.Count} sounds to {AudioDir}");
        }

        [MenuItem("Gold Rush/Course/Export Grid Texture (.png)", priority = 81)]
        public static void ExportGrid()
        {
            Directory.CreateDirectory(TextureDir);
            string path = TextureDir + "Grid.png";
            File.WriteAllBytes(path, ProcTex.Grid.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.wrapMode = TextureWrapMode.Repeat;
            ti.mipmapEnabled = true;
            ti.anisoLevel = 4;
            ti.SaveAndReimport();
            Debug.Log("[GoldRush] Saved " + path);
        }

        /// <summary>16-bit PCM .wav writer (mono or stereo, any sample rate).</summary>
        static void WriteWav(string path, AudioClip clip)
        {
            var data = new float[clip.samples * clip.channels];
            clip.GetData(data, 0);
            using (var w = new BinaryWriter(File.Create(path)))
            {
                int bytes = data.Length * 2;
                w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + bytes);
                w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); w.Write(16);
                w.Write((short)1); w.Write((short)clip.channels); w.Write(clip.frequency);
                w.Write(clip.frequency * clip.channels * 2); w.Write((short)(clip.channels * 2)); w.Write((short)16);
                w.Write(System.Text.Encoding.ASCII.GetBytes("data")); w.Write(bytes);
                foreach (var f in data) w.Write((short)(Mathf.Clamp(f, -1f, 1f) * 32767f));
            }
        }
    }
}
