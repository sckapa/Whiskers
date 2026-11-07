using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Whiskers.Dice;
using Whiskers.Dice.Geometry;
using Whiskers.Dice.Visuals;
using Object = UnityEngine.Object;

namespace Whiskers.Editor
{
    // Meshes and prefabs are overwritten, materials and settings are created once.
    // TODO(art): replace meshes with models
    public static class DicePrefabGenerator
    {
        private const string MenuPath = "Whiskers/Dice/Generate Dice Prefabs";

        private const string MeshFolder = "Assets/Art/Dice/Meshes";
        private const string MaterialFolder = "Assets/Art/Dice/Materials";
        private const string SpriteFolder = "Assets/Art/Dice/Sprites";
        private const string PrefabFolder = "Assets/Prefabs/Dice";
        private const string SettingsFolder = "Assets/Settings/Dice";

        private const string RegularMaterialPath = MaterialFolder + "/DiceRegular.mat";
        private const string CheeseMaterialPath = MaterialFolder + "/DiceCheese.mat";
        private const string HoleSpritePath = SpriteFolder + "/DiceHole.png";
        private const string SettingsPath = SettingsFolder + "/DiceVisualSettings.asset";

        private const string DieNameFormat = "Die_{0}";
        private const string AnchorsRootName = "GlyphAnchors";
        private const string AnchorNameFormat = "Face{0:00}_Slot{1}";

        private const string LitShaderName = "Universal Render Pipeline/Lit";
        private const string SmoothnessProperty = "_Smoothness";
        private const float RegularSmoothness = 0.6f;
        private const float CheeseSmoothness = 0.15f;

        private const int HoleTextureSize = 128;
        private const float HoleEdgeSoftness = 0.04f;

        private static readonly Color RegularBody = new Color(0.95f, 0.93f, 0.88f);
        private static readonly Color RegularGlyph = new Color(0.1f, 0.1f, 0.12f);
        private static readonly Color RegularRedBody = new Color(0.75f, 0.12f, 0.12f);
        private static readonly Color RegularRedGlyph = new Color(0.85f, 0.1f, 0.1f);
        private static readonly Color RegularYellowBody = new Color(0.95f, 0.8f, 0.2f);
        private static readonly Color RegularYellowGlyph = new Color(0.85f, 0.6f, 0f);

        private static readonly Color CheeseBody = new Color(1f, 0.82f, 0.35f);
        private static readonly Color CheeseHole = new Color(0.55f, 0.35f, 0.1f);
        private static readonly Color CheeseRedBody = new Color(0.7f, 0.1f, 0.1f);
        private static readonly Color CheeseRedGlyph = new Color(0.8f, 0.1f, 0.05f);
        private static readonly Color CheeseYellowBody = new Color(1f, 0.9f, 0.4f);
        private static readonly Color CheeseYellowGlyph = new Color(0.9f, 0.65f, 0f);

        [MenuItem(MenuPath)]
        public static void GenerateAll()
        {
            EnsureFolder(MeshFolder);
            EnsureFolder(MaterialFolder);
            EnsureFolder(SpriteFolder);
            EnsureFolder(PrefabFolder);
            EnsureFolder(SettingsFolder);

            Material regularMaterial = GetOrCreateMaterial(RegularMaterialPath, RegularSmoothness);
            Material cheeseMaterial = GetOrCreateMaterial(CheeseMaterialPath, CheeseSmoothness);
            Sprite holeSprite = GetOrCreateHoleSprite();
            DiceVisualSettings settings = GetOrCreateSettings(regularMaterial, cheeseMaterial, holeSprite);

            foreach (DiceSize size in (DiceSize[])Enum.GetValues(typeof(DiceSize)))
            {
                GeneratePrefab(size, settings, regularMaterial);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Dice prefabs generated in {PrefabFolder}.");
        }

        private static void GeneratePrefab(DiceSize size, DiceVisualSettings settings, Material defaultMaterial)
        {
            DieGeometry geometry = DieGeometryBuilder.Build(size);
            string dieName = string.Format(DieNameFormat, size);
            Mesh mesh = SaveMesh(geometry.Mesh, $"{MeshFolder}/{dieName}.asset", dieName);
            GameObject root = new GameObject(dieName);

            try
            {
                root.AddComponent<MeshFilter>().sharedMesh = mesh;

                MeshRenderer bodyRenderer = root.AddComponent<MeshRenderer>();
                bodyRenderer.sharedMaterial = defaultMaterial;

                // Convex collider ignores the D9 notches
                MeshCollider meshCollider = root.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = mesh;
                meshCollider.convex = true;

                Rigidbody body = root.AddComponent<Rigidbody>();
                body.interpolation = RigidbodyInterpolation.Interpolate;
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

                Transform anchorsRoot = new GameObject(AnchorsRootName).transform;
                anchorsRoot.SetParent(root.transform, false);

                root.AddComponent<DieModel>().Configure(
                    size,
                    geometry.ResultSide,
                    CreateFaceLayouts(geometry, anchorsRoot),
                    new List<Vector3>(geometry.CapNormals));

                root.AddComponent<DieView>().Configure(settings, bodyRenderer);

                PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabFolder}/{dieName}.prefab");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static List<DieFaceLayout> CreateFaceLayouts(DieGeometry geometry, Transform anchorsRoot)
        {
            List<DieFaceLayout> layouts = new List<DieFaceLayout>(geometry.Faces.Count);

            for (int faceIndex = 0; faceIndex < geometry.Faces.Count; faceIndex++)
            {
                DieFaceGeometry face = geometry.Faces[faceIndex];
                List<DieGlyphSlot> slots = new List<DieGlyphSlot>(face.GlyphSlots.Count);

                for (int slotIndex = 0; slotIndex < face.GlyphSlots.Count; slotIndex++)
                {
                    GlyphSlotGeometry slot = face.GlyphSlots[slotIndex];
                    Transform anchor = new GameObject(string.Format(AnchorNameFormat, faceIndex, slotIndex)).transform;
                    anchor.SetParent(anchorsRoot, false);
                    anchor.localPosition = slot.Position;
                    anchor.localRotation = Quaternion.LookRotation(slot.Forward, slot.Up);
                    slots.Add(new DieGlyphSlot(anchor, slot.Size));
                }

                layouts.Add(new DieFaceLayout(face.Normal, slots));
            }

            return layouts;
        }

        private static Mesh SaveMesh(DieMeshData data, string path, string meshName)
        {
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool isNew = mesh == null;

            if (isNew)
            {
                mesh = new Mesh();
            }
            else
            {
                mesh.Clear();
            }

            mesh.name = meshName;
            mesh.SetVertices(data.Vertices);
            mesh.SetNormals(data.Normals);
            mesh.SetUVs(0, data.Uvs);
            mesh.SetTriangles(data.Triangles, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();

            if (isNew)
            {
                AssetDatabase.CreateAsset(mesh, path);
            }
            else
            {
                EditorUtility.SetDirty(mesh);
            }

            return mesh;
        }

        private static Material GetOrCreateMaterial(string path, float smoothness)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material != null)
            {
                return material;
            }

            Shader shader = Shader.Find(LitShaderName);

            if (shader == null)
            {
                throw new InvalidOperationException($"Shader '{LitShaderName}' not found, is URP installed?");
            }

            material = new Material(shader);
            material.SetFloat(SmoothnessProperty, smoothness);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Sprite GetOrCreateHoleSprite()
        {
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(HoleSpritePath);

            if (existing != null)
            {
                return existing;
            }

            const float half = 0.5f;

            Texture2D texture = new Texture2D(HoleTextureSize, HoleTextureSize, TextureFormat.RGBA32, false);
            Vector2 center = Vector2.one * (HoleTextureSize * half);
            float radius = HoleTextureSize * half;
            float softness = HoleTextureSize * HoleEdgeSoftness;

            for (int y = 0; y < HoleTextureSize; y++)
            {
                for (int x = 0; x < HoleTextureSize; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x + half, y + half), center);
                    float alpha = Mathf.Clamp01((radius - distance) / softness);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            File.WriteAllBytes(ToFullPath(HoleSpritePath), texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(HoleSpritePath);

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(HoleSpritePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = HoleTextureSize;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(HoleSpritePath);
        }

        private static DiceVisualSettings GetOrCreateSettings(Material regularMaterial, Material cheeseMaterial, Sprite holeSprite)
        {
            DiceVisualSettings settings = AssetDatabase.LoadAssetAtPath<DiceVisualSettings>(SettingsPath);

            if (settings != null)
            {
                return settings;
            }

            DiceStyle playerStyle = new DiceStyle(regularMaterial, RegularBody, RegularGlyph, false, new List<DiceColorEntry>
            {
                new DiceColorEntry(DiceColor.Red, RegularRedBody, RegularRedGlyph),
                new DiceColorEntry(DiceColor.Yellow, RegularYellowBody, RegularYellowGlyph)
            });

            DiceStyle cheeseStyle = new DiceStyle(cheeseMaterial, CheeseBody, CheeseHole, true, new List<DiceColorEntry>
            {
                new DiceColorEntry(DiceColor.Red, CheeseRedBody, CheeseRedGlyph),
                new DiceColorEntry(DiceColor.Yellow, CheeseYellowBody, CheeseYellowGlyph)
            });

            settings = ScriptableObject.CreateInstance<DiceVisualSettings>();
            settings.InitializeDefaults(playerStyle, cheeseStyle, holeSprite);
            AssetDatabase.CreateAsset(settings, SettingsPath);
            return settings;
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";

                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static string ToFullPath(string assetPath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
        }
    }
}
