using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Voxel.Utility
{
    public class ProcessTextureAtlas : AssetPostprocessor
    {
        private static readonly List<string> uvs = new List<string>();

        private void OnPreprocessTexture()
        {
            TextureImporter textureImporter = (TextureImporter)assetImporter;
            textureImporter.textureType = TextureImporterType.Sprite;
            textureImporter.spriteImportMode = SpriteImportMode.Multiple;
            textureImporter.mipmapEnabled = false;
            textureImporter.filterMode = FilterMode.Point;
            textureImporter.npotScale = TextureImporterNPOTScale.None;
        }

        private static void WriteUVs()
        {
            const string path = "Assets/Scripts/Utility/BlockUVs.txt";

            var writer = new StreamWriter(path);
            foreach (string s in uvs)
            {
                writer.WriteLine(s);
            }

            writer.Close();

            // Re-import the file to update the reference in the editor
            AssetDatabase.ImportAsset(path);
        }

        public void OnPostprocessTexture(Texture2D texture)
        {
            // Enable this texture processor for atlas textures ONLY
            if (!Path.GetFileName(assetPath).ToLower(CultureInfo.InvariantCulture).Contains("atlas")
                || GetTextureImporter().spritesheet.Length > 0)
            {
                return;
            }

            const int rowCount = 16;
            const int colCount = 16;

            //const int rowOffset = 32;
            //const int colOffset = 32;
            int sh = texture.height / rowCount/* + rowOffset*/;
            int sw = texture.width / colCount /*+ colOffset*/;

            List<SpriteMetaData> metas = new List<SpriteMetaData>();

            for (int r = 0; r < rowCount; r++)
            {
                for (int c = 0; c < colCount; c++)
                {
                    SpriteMetaData meta = new SpriteMetaData
                    {
                        rect = new Rect(c * sw, r * sh, sw, sh)
                    };

                    float uv1x = c * sw / (float)texture.width;
                    float uv1y = r * sh / (float)texture.height;

                    float uv2x = ((c * sw) + sw) / (float)texture.width;
                    float uv2y = r * sh / (float)texture.height;

                    float uv3x = c * sw / (float)texture.width;
                    float uv3y = ((r * sh) + sh) / (float)texture.height;

                    float uv4x = ((c * sw) + sw) / (float)texture.width;
                    float uv4y = ((r * sh) + sh) / (float)texture.height;

                    meta.name = uv1x + "," + uv1y + "|" +
                                uv2x + "," + uv2y + "|" +
                                uv3x + "," + uv3y + "|" +
                                uv4x + "," + uv4y;
                    metas.Add(meta);
                    string n = "Row: " + r + " Col: " + c + " { new Vector2(" + uv1x + "f," + uv1y + "f), " +
                                " new Vector2(" + uv2x + "f," + uv2y + "f),\n" +
                                " new Vector2(" + uv3x + "f," + uv3y + "f)," +
                                " new Vector2(" + uv4x + "f," + uv4y + "f)}";
                    uvs.Add(n);
                }
            }

            GetTextureImporter().spritesheet = metas.ToArray();
            WriteUVs();
        }

        private TextureImporter GetTextureImporter() => (TextureImporter)assetImporter;
    }
}