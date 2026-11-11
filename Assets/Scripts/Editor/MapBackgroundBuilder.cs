using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Whiskers.UI.Map;

namespace Whiskers.Editor
{
    // Puts the map art behind the run map in the Map scene.
    // The background is the Image on the map's ScrollRect (the one MapScreen references), which
    // sits behind the Viewport, Lines and Nodes. Only its sprite, colour and type change, so no
    // layout moves. Safe to re-run: the import settings and the Image are reapplied every time.
    public static class MapBackgroundBuilder
    {
        private const string MenuPath = "Whiskers/Map/Apply Map Background";

        private const string ScenePath = "Assets/Scenes/Map.unity";
        private const string BackgroundPath = "Assets/Art/Map/dons-tab-map.png";
        private const string ScrollRectProperty = "scrollRect";

        // The art is 1920x1080 and the canvas reference is 1920x1080 at 100 reference pixels per unit,
        // so 100 draws one texture pixel per canvas pixel.
        private const float BackgroundPixelsPerUnit = 100f;
        private const int MaxTextureSize = 2048;

        [MenuItem(MenuPath)]
        public static void Apply()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            try
            {
                Sprite background = ImportBackground();

                Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                Image backgroundImage = FindBackgroundImage(scene);

                backgroundImage.sprite = background;
                backgroundImage.color = Color.white;
                backgroundImage.type = Image.Type.Simple;
                backgroundImage.preserveAspect = false;
                backgroundImage.useSpriteMesh = false;
                EditorUtility.SetDirty(backgroundImage);

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
                Debug.Log($"Map background set to {BackgroundPath} on '{backgroundImage.name}' in {ScenePath}.");
            }
            catch (InvalidOperationException exception)
            {
                Debug.LogError($"Map background stopped: {exception.Message}");
            }
        }

        private static Sprite ImportBackground()
        {
            TextureImporter importer = AssetImporter.GetAtPath(BackgroundPath) as TextureImporter;

            if (importer == null)
            {
                AssetDatabase.ImportAsset(BackgroundPath, ImportAssetOptions.ForceSynchronousImport);
                importer = AssetImporter.GetAtPath(BackgroundPath) as TextureImporter;
            }

            if (importer == null)
            {
                throw new InvalidOperationException($"{BackgroundPath} is missing. Copy it from Art/Map.");
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = MaxTextureSize;
            importer.npotScale = TextureImporterNPOTScale.None;

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spritePixelsPerUnit = BackgroundPixelsPerUnit;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spriteBorder = Vector4.zero;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);

            if (sprite == null)
            {
                throw new InvalidOperationException($"{BackgroundPath} did not import as a sprite.");
            }

            return sprite;
        }

        // Uses the ScrollRect MapScreen is wired to, not an object name.
        private static Image FindBackgroundImage(Scene scene)
        {
            MapScreen mapScreen = null;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                mapScreen = root.GetComponentInChildren<MapScreen>(true);

                if (mapScreen != null)
                {
                    break;
                }
            }

            if (mapScreen == null)
            {
                throw new InvalidOperationException($"{ScenePath} has no MapScreen.");
            }

            SerializedProperty scrollRectProperty = new SerializedObject(mapScreen).FindProperty(ScrollRectProperty);
            ScrollRect scrollRect = scrollRectProperty != null ? scrollRectProperty.objectReferenceValue as ScrollRect : null;

            if (scrollRect == null)
            {
                throw new InvalidOperationException("MapScreen has no Scroll Rect assigned.");
            }

            Image image = scrollRect.GetComponent<Image>();

            if (image == null)
            {
                throw new InvalidOperationException($"'{scrollRect.name}' has no Image to draw the background on.");
            }

            return image;
        }
    }
}
