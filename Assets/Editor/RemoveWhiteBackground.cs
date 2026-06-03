using UnityEngine;
using UnityEditor;
using System.IO;

public static class RemoveWhiteBackground
{
    [MenuItem("Tools/Remove White Background/02_mountain_background_tile")]
    static void RemoveMountainBackground()
    {
        RemoveWhiteFromTexture("Assets/Sprites/Background/02_mountain_background_tile.png", 0.9f);
    }

    static void RemoveWhiteFromTexture(string texturePath, float threshold)
    {
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
        if (importer == null)
        {
            Debug.LogError("Texture not found: " + texturePath);
            return;
        }

        bool wasReadable = importer.isReadable;
        TextureImporterType originalType = importer.textureType;

        importer.isReadable = true;
        importer.textureType = TextureImporterType.Default;
        importer.SaveAndReimport();

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (texture == null)
        {
            Debug.LogError("Failed to load texture: " + texturePath);
            importer.isReadable = wasReadable;
            importer.textureType = originalType;
            importer.SaveAndReimport();
            return;
        }

        Color[] pixels = texture.GetPixels();

        for (int i = 0; i < pixels.Length; i++)
        {
            Color c = pixels[i];
            if (c.r >= threshold && c.g >= threshold && c.b >= threshold)
            {
                float whiteness = Mathf.Min(c.r, c.g, c.b);
                float alpha = 1f - Mathf.InverseLerp(threshold, 1f, whiteness);
                pixels[i] = new Color(c.r, c.g, c.b, alpha);
            }
        }

        Texture2D result = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
        result.SetPixels(pixels);
        result.Apply();

        byte[] pngBytes = result.EncodeToPNG();
        Object.DestroyImmediate(result);

        string fullPath = Application.dataPath + "/" + texturePath.Substring("Assets/".Length);
        File.WriteAllBytes(fullPath, pngBytes);

        importer.isReadable = wasReadable;
        importer.textureType = originalType;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();

        AssetDatabase.Refresh();
        Debug.Log("White background removed from: " + texturePath);
    }
}
