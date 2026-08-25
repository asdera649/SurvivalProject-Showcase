using System.IO;
using UnityEditor;
using UnityEngine;

namespace SP.Editor
{
    public class NeutralLUTGenerator : MonoBehaviour
    {
        [MenuItem("Tools/Generate Neutral LUT")]
        private static void Generate()
        {
            const int lutSize = 32; // 16, 32 или 64 - под настройку в Universal Renderer Data
            const int width = lutSize * lutSize; // 1024 при lutSize = 32

            var lut = new Texture2D(width, lutSize, TextureFormat.RGBA32, false, true);

            for (var b = 0; b < lutSize; b++)
            {
                for (var g = 0; g < lutSize; g++)
                {
                    for (var r = 0; r < lutSize; r++)
                    {
                        var rf = r / (float)(lutSize - 1);
                        var gf = g / (float)(lutSize - 1);
                        var bf = b / (float)(lutSize - 1);

                        var x = b * lutSize + r;

                        lut.SetPixel(x, g, new Color(rf, gf, bf, 1f));
                    }
                }
            }

            lut.Apply();

            var png = lut.EncodeToPNG();
            const string path = "Assets/NeutralLUT.png";
            File.WriteAllBytes(path, png);
            AssetDatabase.Refresh();

            Debug.Log("Neutral LUT saved to " + path);
        }
    }
}