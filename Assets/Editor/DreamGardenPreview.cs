using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// renders review images in the isolated batch project with a real graphics device
public static class DreamGardenPreview
{
    public static void Run()
    {
        try
        {
            var scene = EditorSceneManager.OpenScene(DreamGardenVisualUpgrade.StandaloneScene);
            DreamGardenGenerator garden = DreamGardenVisualUpgrade.Find<DreamGardenGenerator>(scene);
            CaptureGarden(garden);
            Debug.Log("DREAM_GARDEN_PREVIEW_PASS");
            EditorApplication.Exit(0);
        }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }

    public static void CaptureGarden(DreamGardenGenerator garden)
    {
            Camera camera = Camera.main;
            camera.GetComponent<PlanetCamera>().enabled = false;
            camera.GetComponent<DreamCameraOcclusion>().enabled = false;
            camera.farClipPlane = 500;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.11f, .075f, .15f);
            camera.transform.position = garden.transform.TransformPoint(new Vector3(0, 120, -64));
            camera.transform.LookAt(garden.transform.TransformPoint(new Vector3(0, 65, 0)), garden.transform.up);
            Capture(camera, garden.gameObject.scene.name + "-overview.png");
            DreamInteraction keeper = Array.Find(garden.GetComponentsInChildren<DreamInteraction>(), i => i.kind == DreamInteraction.Kind.Keeper);
            DreamLantern moon = Array.Find(garden.GetComponentsInChildren<DreamLantern>(), i => i.orderIndex == 0);
            camera.transform.position = Vector3.Lerp(keeper.transform.position, moon.transform.position, .8f) + keeper.transform.up * 2.4f;
            camera.transform.LookAt(keeper.transform.position + keeper.transform.up * 1.2f, keeper.transform.up);
            Capture(camera, garden.gameObject.scene.name + "-keeper.png");
    }

    private static void Capture(Camera camera, string filename)
    {
        RenderTexture target = RenderTexture.GetTemporary(1280, 900, 24, RenderTextureFormat.ARGB32);
        camera.targetTexture = target;
        camera.Render();
        camera.Render();
        camera.Render();
        RenderTexture.active = target;
        Texture2D image = new Texture2D(1280, 900, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0,0,1280,900), 0, 0); image.Apply();
        File.WriteAllBytes(filename, image.EncodeToPNG());
        camera.targetTexture = null; RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(target); UnityEngine.Object.DestroyImmediate(image);
    }
}
