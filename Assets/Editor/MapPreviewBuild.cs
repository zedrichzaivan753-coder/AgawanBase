using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Captures a real picture of each map and hangs it on that map's MapDefinition.
///
/// The picture is rendered by a temporary orthographic camera that frames the whole arena the map
/// asset describes, so the card on the map select screen shows the ACTUAL scene - not a stock
/// photo and not a hand-drawn mock. Re-run it after any map is rebuilt and the cards follow.
/// </summary>
public static class MapPreviewBuild
{
    const string PreviewFolder = "Assets/UI/Previews";
    const int Width = 768;
    const int Height = 432;

    struct Job
    {
        public string scenePath;
        public string mapAssetPath;
        public string fileName;
    }

    static readonly Job[] Jobs =
    {
        new Job { scenePath = "Assets/Scenes/Game.unity",
                  mapAssetPath = "Assets/Config/Map_Classic.asset",
                  fileName = "Map_Classic.png" },

        new Job { scenePath = "Assets/Scenes/Map_Palengke.unity",
                  mapAssetPath = "Assets/Config/Map_Palengke.asset",
                  fileName = "Map_Palengke.png" },

        new Job { scenePath = "Assets/Scenes/Map_SchoolCampus.unity",
                  mapAssetPath = "Assets/Config/Map_SchoolCampus.asset",
                  fileName = "Map_SchoolCampus.png" }
    };

    public static void Capture()
    {
        Directory.CreateDirectory(PreviewFolder);

        for (int i = 0; i < Jobs.Length; i++)
        {
            CaptureOne(Jobs[i]);
        }

        AssetDatabase.Refresh();
        AssetDatabase.SaveAssets();

        // The import settings have to be applied AFTER the files exist, so the sprites are made in
        // a second pass.
        for (int i = 0; i < Jobs.Length; i++)
        {
            MakeSpriteAndAttach(Jobs[i]);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[Preview] " + Jobs.Length + " map previews captured into " + PreviewFolder + ".");
    }

    static void CaptureOne(Job job)
    {
        MapDefinition map = AssetDatabase.LoadAssetAtPath<MapDefinition>(job.mapAssetPath);

        if (map == null)
        {
            Debug.LogError("[Preview] missing map definition " + job.mapAssetPath);
            return;
        }

        EditorSceneManager.OpenScene(job.scenePath, OpenSceneMode.Single);

        GameObject cameraObject = new GameObject("[PreviewCamera]");
        Camera camera = cameraObject.AddComponent<Camera>();

        float aspect = (float)Width / Height;

        // Frame the whole arena: the width is what has to fit, exactly the way the in-game camera
        // is framed from the map asset.
        float orthoSize = Mathf.Max(map.arenaSize.x / aspect, map.arenaSize.y) * 0.5f;

        const float pitch = 55f;
        const float distance = 70f;

        Vector3 forward = Quaternion.Euler(pitch, 0f, 0f) * Vector3.forward;
        Vector3 centre = new Vector3(map.arenaCenter.x, 0f, map.arenaCenter.y);

        cameraObject.transform.position = centre - forward * distance;
        cameraObject.transform.rotation = Quaternion.Euler(pitch, 0f, 0f);

        camera.orthographic = true;
        camera.orthographicSize = orthoSize;
        camera.nearClipPlane = 0.3f;
        camera.farClipPlane = 400f;
        camera.clearFlags = CameraClearFlags.Skybox;

        int ui = LayerMask.NameToLayer("UI");
        camera.cullingMask = ui >= 0 ? ~(1 << ui) : ~0;

        RenderTexture target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
        RenderTexture previous = RenderTexture.active;

        camera.targetTexture = target;
        camera.Render();

        RenderTexture.active = target;

        Texture2D picture = new Texture2D(Width, Height, TextureFormat.RGB24, false);
        picture.ReadPixels(new Rect(0f, 0f, Width, Height), 0, 0);
        picture.Apply();

        RenderTexture.active = previous;
        camera.targetTexture = null;

        File.WriteAllBytes(Path.Combine(PreviewFolder, job.fileName), picture.EncodeToPNG());

        Object.DestroyImmediate(picture);
        Object.DestroyImmediate(target);
        Object.DestroyImmediate(cameraObject);

        Debug.Log("[Preview] captured " + job.fileName + " (" + Width + "x" + Height + ")");
    }

    static void MakeSpriteAndAttach(Job job)
    {
        string path = PreviewFolder + "/" + job.fileName;

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

        if (importer == null)
        {
            Debug.LogError("[Preview] " + path + " did not import, so the card keeps its accent colour.");
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100f;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = false;
        importer.maxTextureSize = 1024;
        importer.textureCompression = TextureImporterCompression.Compressed;
        importer.SaveAndReimport();

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

        if (sprite == null)
        {
            Debug.LogError("[Preview] " + path + " has no sprite sub-asset.");
            return;
        }

        MapDefinition map = AssetDatabase.LoadAssetAtPath<MapDefinition>(job.mapAssetPath);

        if (map == null) return;

        map.preview = sprite;
        EditorUtility.SetDirty(map);
    }
}
