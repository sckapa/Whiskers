using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using Whiskers.UI;
using Object = UnityEngine.Object;

namespace Whiskers.Editor
{
    // Builds the main menu described in Art/UI/MainMenu/HANDOFF.md.
    // Safe to re-run: import settings, layout and button setup are reapplied every time, and the
    // buttons MainMenuScreen references are updated in place. Font assets are created once,
    // delete them to regenerate.
    public static class MainMenuUIBuilder
    {
        private const string MenuPath = "Whiskers/UI/Build Main Menu";

        private const string ScenePath = "Assets/Scenes/MainMenu.unity";
        private const string SpriteFolder = "Assets/Art/UI/MainMenu";
        private const string FontFolder = "Assets/Art/UI/Fonts";
        private const string RegularFontName = "Silkscreen-Regular";
        private const string BoldFontName = "Silkscreen-Bold";
        private const string FontAssetSuffix = " Bitmap";

        // One art pixel is 8 canvas units with the canvas reference PPU at 100
        private const float ArtPixelsPerUnit = 12.5f;
        private const float BackdropPixelsPerUnit = 100f;

        // Silkscreen's em is 8 font pixels, so sampling at 8 gives one texel per font pixel
        private const int FontSamplingSize = 8;
        private const int FontAtlasPadding = 1;
        private const int FontAtlasSize = 256;
        private const string FontCharacters =
            " ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789.,:;!?'\"-+/()%&#*";

        private const string BackdropName = "Backdrop";
        private const string LogoName = "Logo";
        private const string RibbonName = "Ribbon";
        private const string RibbonLabelShadowName = "RibbonLabelShadow";
        private const string RibbonLabelName = "RibbonLabel";
        private const string PanelName = "Panel";
        private const string CursorName = "DieCursor";
        private const string VersionName = "Version";
        private const string ButtonLabelName = "Text (TMP)";

        private const string RibbonText = "RIG-MA-ROLL";
        private const string NewRunText = "NEW RUN";
        private const string ExitText = "EXIT";
        private const string VersionText = "V1.0";

        private const float LargeFontSize = 64f;
        private const float SmallFontSize = 32f;

        // Layout in canvas units, top-left origin, y down. Matches the handoff table.
        private static readonly Rect CanvasRect = new Rect(0f, 0f, 1920f, 1080f);
        private static readonly Rect LogoRect = new Rect(448f, 136f, 1024f, 144f);
        private static readonly Rect RibbonRect = new Rect(600f, 296f, 720f, 88f);
        private static readonly Rect PanelRect = new Rect(520f, 432f, 880f, 400f);
        private static readonly Rect NewRunRect = new Rect(680f, 496f, 560f, 128f);
        private static readonly Rect ExitRect = new Rect(680f, 640f, 560f, 128f);
        private static readonly Rect CursorRect = new Rect(584f, 512f, 72f, 80f);

        private const float CursorOffsetBelowButtonTop = 16f;
        private const float RibbonLabelInkTop = 320f;
        private const float RibbonShadowDrop = 8f;
        private const float ButtonLabelInkOffset = 32f;
        private const float ButtonLabelPressedDrop = 16f;
        private const float VersionInkRight = 1780f;
        private const float VersionInkTop = 1040f;

        // Title bob: heights in art pixels, one entry per step
        private static readonly int[] TitleBobPattern = { 0, 1, 2, 0 };
        private const float TitleBobStepSeconds = 0.2f;
        private const float ArtPixelSize = 8f;
        private const int TitleRibbonDelaySteps = 0;

        // Ink sizes from the reference, used to check the font assets render on the pixel grid
        private static readonly Vector2 RibbonInkSize = new Vector2(552f, 40f);
        private static readonly Vector2 NewRunInkSize = new Vector2(368f, 40f);
        private static readonly Vector2 ExitInkSize = new Vector2(176f, 40f);
        private static readonly Vector2 VersionInkSize = new Vector2(76f, 20f);
        private const float InkTolerance = 0.01f;

        private static readonly Color32 Cream = new Color32(0xF1, 0xE6, 0xC4, 0xFF);
        private static readonly Color32 RibbonShadow = new Color32(0x8E, 0x2B, 0x2B, 0xFF);
        private static readonly Color32 ButtonInk = new Color32(0x15, 0x0C, 0x19, 0xFF);

        private static readonly Vector2 CenterAnchor = new Vector2(0.5f, 0.5f);
        private static readonly Vector2 TopLeftPivot = new Vector2(0f, 1f);

        private enum InkAlign
        {
            Center,
            Right
        }

        private sealed class MenuSprites
        {
            public Sprite Backdrop;
            public Sprite Logo;
            public Sprite Ribbon;
            public Sprite Panel;
            public Sprite ButtonIdle;
            public Sprite ButtonSelected;
            public Sprite ButtonPressed;
            public Sprite ButtonDisabled;
            public Sprite Cursor;
        }

        [MenuItem(MenuPath)]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            try
            {
                MenuSprites sprites = ImportSprites();
                TMP_FontAsset regularFont = GetOrCreateFontAsset(RegularFontName);
                TMP_FontAsset boldFont = GetOrCreateFontAsset(BoldFontName);

                Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                FindMenuButtons(scene, out Button newRunButton, out Button exitButton);

                BuildCanvas(newRunButton, exitButton, sprites, regularFont, boldFont);
                ConfigureEventSystem(scene, newRunButton);

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
                Debug.Log($"Main menu built in {ScenePath}.");
            }
            catch (InvalidOperationException exception)
            {
                Debug.LogError($"Main menu build stopped: {exception.Message}");
            }
        }

        private static MenuSprites ImportSprites()
        {
            Vector4 noBorder = Vector4.zero;

            // spriteBorder is (left, bottom, right, top)
            Vector4 panelBorder = new Vector4(8f, 8f, 8f, 8f);
            Vector4 buttonBorder = new Vector4(6f, 8f, 6f, 6f);
            Vector4 ribbonBorder = new Vector4(7f, 0f, 7f, 0f);

            return new MenuSprites
            {
                Backdrop = ImportSprite("felt-table-map", BackdropPixelsPerUnit, noBorder),
                Logo = ImportSprite("logo_whiskers", ArtPixelsPerUnit, noBorder),
                Ribbon = ImportSprite("ribbon_red", ArtPixelsPerUnit, ribbonBorder),
                Panel = ImportSprite("panel_sign", ArtPixelsPerUnit, panelBorder),
                ButtonIdle = ImportSprite("btn_idle", ArtPixelsPerUnit, buttonBorder),
                ButtonSelected = ImportSprite("btn_selected", ArtPixelsPerUnit, buttonBorder),
                ButtonPressed = ImportSprite("btn_pressed", ArtPixelsPerUnit, buttonBorder),
                ButtonDisabled = ImportSprite("btn_disabled", ArtPixelsPerUnit, buttonBorder),
                Cursor = ImportSprite("cursor_die", ArtPixelsPerUnit, noBorder)
            };
        }

        private static Sprite ImportSprite(string fileName, float pixelsPerUnit, Vector4 border)
        {
            string path = $"{SpriteFolder}/{fileName}.png";
            TextureImporter importer = LoadImporter<TextureImporter>(path);

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spritePixelsPerUnit = pixelsPerUnit;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteBorder = border;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

            if (sprite == null)
            {
                throw new InvalidOperationException($"{path} did not import as a sprite.");
            }

            return sprite;
        }

        private static TMP_FontAsset GetOrCreateFontAsset(string fontName)
        {
            string assetPath = $"{FontFolder}/{fontName}{FontAssetSuffix}.asset";
            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);

            if (fontAsset == null)
            {
                fontAsset = CreateFontAsset(fontName, assetPath);
            }

            ApplyPointFilter(fontAsset);
            return fontAsset;
        }

        private static TMP_FontAsset CreateFontAsset(string fontName, string assetPath)
        {
            string fontPath = $"{FontFolder}/{fontName}.ttf";
            LoadImporter<TrueTypeFontImporter>(fontPath);
            Font font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);

            // Raster is 1-bit, no anti-aliasing. Created dynamic so glyphs can be added,
            // then frozen so the atlas never regrows and loses its filter mode at runtime.
            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
                font,
                FontSamplingSize,
                FontAtlasPadding,
                GlyphRenderMode.RASTER,
                FontAtlasSize,
                FontAtlasSize,
                AtlasPopulationMode.Dynamic,
                false);

            if (fontAsset == null)
            {
                throw new InvalidOperationException($"TextMesh Pro could not create a font asset from {fontPath}.");
            }

            fontAsset.name = Path.GetFileNameWithoutExtension(assetPath);
            AssetDatabase.CreateAsset(fontAsset, assetPath);

            // Atlas and material must live inside the font asset or they are lost on reload
            Texture2D atlas = fontAsset.atlasTexture;
            atlas.name = $"{fontAsset.name} Atlas";
            AssetDatabase.AddObjectToAsset(atlas, fontAsset);

            Material material = fontAsset.material;
            material.name = $"{fontAsset.name} Material";
            AssetDatabase.AddObjectToAsset(material, fontAsset);

            if (!fontAsset.TryAddCharacters(FontCharacters, out string missing))
            {
                Debug.LogWarning($"{fontName} has no glyphs for: {missing}");
            }

            fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;

            EditorUtility.SetDirty(atlas);
            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            return fontAsset;
        }

        private static void ApplyPointFilter(TMP_FontAsset fontAsset)
        {
            foreach (Texture2D atlas in fontAsset.atlasTextures)
            {
                if (atlas != null && atlas.filterMode != FilterMode.Point)
                {
                    atlas.filterMode = FilterMode.Point;
                    EditorUtility.SetDirty(atlas);
                }
            }
        }

        private static T LoadImporter<T>(string path) where T : AssetImporter
        {
            T importer = AssetImporter.GetAtPath(path) as T;

            if (importer == null)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                importer = AssetImporter.GetAtPath(path) as T;
            }

            if (importer == null)
            {
                throw new InvalidOperationException($"{path} is missing. Copy it from Art/UI/MainMenu.");
            }

            return importer;
        }

        // Reuses the buttons MainMenuScreen already points at so its references survive
        private static void FindMenuButtons(Scene scene, out Button newRunButton, out Button exitButton)
        {
            MainMenuScreen screen = FindInScene<MainMenuScreen>(scene);

            if (screen == null)
            {
                throw new InvalidOperationException($"No MainMenuScreen in {ScenePath}.");
            }

            SerializedObject serializedScreen = new SerializedObject(screen);
            newRunButton = serializedScreen.FindProperty("newRunButton").objectReferenceValue as Button;
            exitButton = serializedScreen.FindProperty("exitButton").objectReferenceValue as Button;

            if (newRunButton == null || exitButton == null)
            {
                throw new InvalidOperationException("MainMenuScreen is missing its New Run or Exit button reference.");
            }

            if (newRunButton.transform.parent != exitButton.transform.parent)
            {
                throw new InvalidOperationException("NewRunButton and ExitButton must share a parent.");
            }

            if (newRunButton.GetComponentInParent<Canvas>(true) == null)
            {
                throw new InvalidOperationException("The menu buttons are not under a Canvas.");
            }
        }

        private static void BuildCanvas(
            Button newRunButton,
            Button exitButton,
            MenuSprites sprites,
            TMP_FontAsset regularFont,
            TMP_FontAsset boldFont)
        {
            Canvas canvas = newRunButton.GetComponentInParent<Canvas>(true).rootCanvas;
            ConfigureScaler(canvas);
            Transform root = canvas.transform;

            Image backdrop = GetOrCreateImage(root, BackdropName, CanvasRect, sprites.Backdrop, Image.Type.Simple);
            Image logo = GetOrCreateImage(root, LogoName, LogoRect, sprites.Logo, Image.Type.Simple);
            Image ribbon = GetOrCreateImage(root, RibbonName, RibbonRect, sprites.Ribbon, Image.Type.Sliced);

            TextMeshProUGUI ribbonShadow = GetOrCreateText(root, RibbonLabelShadowName);
            ConfigureText(ribbonShadow, RibbonText, boldFont, LargeFontSize, RibbonShadow, HorizontalAlignmentOptions.Center);
            PlaceByInk(ribbonShadow, CanvasRect, InkAlign.Center, CanvasRect.center.x,
                RibbonLabelInkTop + RibbonShadowDrop, RibbonInkSize);

            TextMeshProUGUI ribbonLabel = GetOrCreateText(root, RibbonLabelName);
            ConfigureText(ribbonLabel, RibbonText, boldFont, LargeFontSize, Cream, HorizontalAlignmentOptions.Center);
            PlaceByInk(ribbonLabel, CanvasRect, InkAlign.Center, CanvasRect.center.x, RibbonLabelInkTop, RibbonInkSize);

            // Logo bobs first, the ribbon and its text follow a step behind like a hanging sign
            AddTitleBob(logo.gameObject, 0);
            AddTitleBob(ribbon.gameObject, TitleRibbonDelaySteps);
            AddTitleBob(ribbonShadow.gameObject, TitleRibbonDelaySteps);
            AddTitleBob(ribbonLabel.gameObject, TitleRibbonDelaySteps);

            Image panel = GetOrCreateImage(root, PanelName, PanelRect, sprites.Panel, Image.Type.Sliced);

            RectTransform container = (RectTransform)newRunButton.transform.parent;
            ConfigureContainer(container);

            ConfigureButton(newRunButton, NewRunRect, NewRunText, NewRunInkSize, null, exitButton, sprites, boldFont);
            ConfigureButton(exitButton, ExitRect, ExitText, ExitInkSize, newRunButton, null, sprites, boldFont);
            newRunButton.transform.SetSiblingIndex(0);
            exitButton.transform.SetSiblingIndex(1);

            Image cursor = GetOrCreateImage(root, CursorName, CursorRect, sprites.Cursor, Image.Type.Simple);
            GetOrAdd<SelectionCursor>(cursor.gameObject).Configure(
                cursor,
                new Selectable[] { newRunButton, exitButton },
                CursorOffsetBelowButtonTop);

            TextMeshProUGUI version = GetOrCreateText(root, VersionName);
            ConfigureText(version, VersionText, regularFont, SmallFontSize, Cream, HorizontalAlignmentOptions.Right);
            PlaceByInk(version, CanvasRect, InkAlign.Right, VersionInkRight, VersionInkTop, VersionInkSize);

            // Draw order, back to front
            Transform[] order =
            {
                backdrop.transform, logo.transform, ribbon.transform, ribbonShadow.transform,
                ribbonLabel.transform, panel.transform, container, cursor.transform, version.transform
            };

            for (int i = 0; i < order.Length; i++)
            {
                order[i].SetSiblingIndex(i);
            }
        }

        private static void AddTitleBob(GameObject target, int delaySteps)
        {
            GetOrAdd<PixelBob>(target).Configure(
                (int[])TitleBobPattern.Clone(),
                TitleBobStepSeconds,
                ArtPixelSize,
                delaySteps);
        }

        private static void ConfigureScaler(Canvas canvas)
        {
            CanvasScaler scaler = GetOrAdd<CanvasScaler>(canvas.gameObject);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = CanvasRect.size;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;
        }

        // The old layout group and translucent box would fight the handoff positions
        private static void ConfigureContainer(RectTransform container)
        {
            foreach (LayoutGroup layoutGroup in container.GetComponents<LayoutGroup>())
            {
                Object.DestroyImmediate(layoutGroup);
            }

            foreach (ContentSizeFitter fitter in container.GetComponents<ContentSizeFitter>())
            {
                Object.DestroyImmediate(fitter);
            }

            Image image = container.GetComponent<Image>();

            if (image != null)
            {
                Object.DestroyImmediate(image);
            }

            // Fills the canvas so child rects use canvas coordinates
            container.anchorMin = Vector2.zero;
            container.anchorMax = Vector2.one;
            container.pivot = CenterAnchor;
            container.offsetMin = Vector2.zero;
            container.offsetMax = Vector2.zero;
            container.localScale = Vector3.one;
        }

        private static void ConfigureButton(
            Button button,
            Rect rect,
            string labelText,
            Vector2 expectedInk,
            Selectable selectOnUp,
            Selectable selectOnDown,
            MenuSprites sprites,
            TMP_FontAsset boldFont)
        {
            SetRect((RectTransform)button.transform, rect, CanvasRect);

            Image image = GetOrAdd<Image>(button.gameObject);
            ConfigureImage(image, sprites.ButtonIdle, Image.Type.Sliced);
            image.raycastTarget = true;

            // Highlighted uses the idle sprite on purpose. Hover already selects, so the only time
            // a button is highlighted but not selected is when the mouse rests on it while the
            // keyboard moves away, and showing gold there would light two buttons at once.
            button.targetGraphic = image;
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState
            {
                highlightedSprite = sprites.ButtonIdle,
                selectedSprite = sprites.ButtonSelected,
                pressedSprite = sprites.ButtonPressed,
                disabledSprite = sprites.ButtonDisabled
            };
            button.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                wrapAround = false,
                selectOnUp = selectOnUp,
                selectOnDown = selectOnDown
            };

            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);

            if (label == null)
            {
                label = GetOrCreateText(button.transform, ButtonLabelName);
            }

            ConfigureText(label, labelText, boldFont, LargeFontSize, ButtonInk, HorizontalAlignmentOptions.Center);
            PlaceByInk(label, rect, InkAlign.Center, rect.center.x, rect.y + ButtonLabelInkOffset, expectedInk);

            GetOrAdd<SelectOnHover>(button.gameObject);
            GetOrAdd<PressedLabelOffset>(button.gameObject).Configure(
                label.rectTransform,
                new Vector2(0f, -ButtonLabelPressedDrop));
        }

        private static void ConfigureEventSystem(Scene scene, Button firstSelected)
        {
            EventSystem eventSystem = FindInScene<EventSystem>(scene);

            if (eventSystem == null)
            {
                Debug.LogWarning($"No EventSystem in {ScenePath}, New Run will not start selected.");
                return;
            }

            eventSystem.firstSelectedGameObject = firstSelected.gameObject;

            // Clicking the table would otherwise clear the selection, hiding the die and
            // leaving keyboard and gamepad with nothing to move from
            InputSystemUIInputModule inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();

            if (inputModule != null)
            {
                inputModule.deselectOnBackgroundClick = false;
            }
        }

        private static Image GetOrCreateImage(Transform parent, string name, Rect rect, Sprite sprite, Image.Type type)
        {
            RectTransform rectTransform = GetOrCreateChild(parent, name);
            SetRect(rectTransform, rect, CanvasRect);

            Image image = GetOrAdd<Image>(rectTransform.gameObject);
            ConfigureImage(image, sprite, type);
            image.raycastTarget = false;
            return image;
        }

        private static void ConfigureImage(Image image, Sprite sprite, Image.Type type)
        {
            image.sprite = sprite;
            image.type = type;
            image.fillCenter = true;
            image.preserveAspect = false;
            image.useSpriteMesh = false;
            image.pixelsPerUnitMultiplier = 1f;
            image.color = Color.white;
        }

        private static TextMeshProUGUI GetOrCreateText(Transform parent, string name)
        {
            RectTransform rectTransform = GetOrCreateChild(parent, name);
            return GetOrAdd<TextMeshProUGUI>(rectTransform.gameObject);
        }

        private static void ConfigureText(
            TextMeshProUGUI text,
            string value,
            TMP_FontAsset font,
            float fontSize,
            Color32 color,
            HorizontalAlignmentOptions alignment)
        {
            text.text = value;
            text.font = font;
            text.fontSharedMaterial = font.material;
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Normal;
            text.enableAutoSizing = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.horizontalAlignment = alignment;
            text.verticalAlignment = VerticalAlignmentOptions.Top;
            text.characterSpacing = 0f;
            text.wordSpacing = 0f;
            text.margin = Vector4.zero;
            text.extraPadding = false;
            text.richText = false;
            text.color = color;
            text.raycastTarget = false;
        }

        // Positions by rendered glyph pixels rather than by rect. Silkscreen's ascender is off
        // the pixel grid and right alignment includes the last glyph's side bearing, so rect
        // maths alone lands the ink up to a pixel away from the reference.
        private static void PlaceByInk(
            TextMeshProUGUI text,
            Rect parentRect,
            InkAlign align,
            float inkX,
            float inkTop,
            Vector2 expectedInk)
        {
            float boxWidth = parentRect.width;
            float boxHeight = text.fontSize * 2f;
            float boxX = align == InkAlign.Center ? inkX - boxWidth * 0.5f : inkX - boxWidth;
            Rect box = new Rect(boxX, inkTop, boxWidth, boxHeight);

            SetRect(text.rectTransform, box, parentRect);
            Rect ink = MeasureInk(text, box);

            float measuredX = align == InkAlign.Center ? ink.center.x : ink.xMax;
            box.x += inkX - measuredX;
            box.y += inkTop - ink.y;
            SetRect(text.rectTransform, box, parentRect);

            if (Mathf.Abs(ink.width - expectedInk.x) > InkTolerance || Mathf.Abs(ink.height - expectedInk.y) > InkTolerance)
            {
                Debug.LogWarning(
                    $"\"{text.text}\" ink is {ink.width}x{ink.height}, the reference has {expectedInk.x}x{expectedInk.y}. " +
                    "Check the font asset sampling size and render mode.");
            }
        }

        // Visible glyph bounds in the parent's top-left space, given the text's current box
        private static Rect MeasureInk(TextMeshProUGUI text, Rect box)
        {
            text.ForceMeshUpdate(true, true);
            TMP_TextInfo textInfo = text.textInfo;

            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            bool hasInk = false;

            for (int i = 0; i < textInfo.characterCount; i++)
            {
                TMP_CharacterInfo character = textInfo.characterInfo[i];

                if (!character.isVisible)
                {
                    continue;
                }

                // The character quad includes shader padding on every side, so take its centre
                // and the glyph's own size to get the visible pixels
                Vector2 center = (character.bottomLeft + character.topRight) * 0.5f;
                GlyphMetrics metrics = character.textElement.glyph.metrics;
                Vector2 halfSize = new Vector2(metrics.width, metrics.height) * (character.scale * 0.5f);

                hasInk = true;
                min = Vector2.Min(min, center - halfSize);
                max = Vector2.Max(max, center + halfSize);
            }

            if (!hasInk)
            {
                throw new InvalidOperationException($"\"{text.text}\" rendered no glyphs with {text.font.name}.");
            }

            // Text geometry is relative to the top-left pivot, y up
            return new Rect(box.x + min.x, box.y - max.y, max.x - min.x, max.y - min.y);
        }

        // Rect is in the same top-left space as parentRect. Anchored to the parent's centre so
        // the layout stays centred if the canvas is not 16:9.
        private static void SetRect(RectTransform rectTransform, Rect rect, Rect parentRect)
        {
            rectTransform.anchorMin = CenterAnchor;
            rectTransform.anchorMax = CenterAnchor;
            rectTransform.pivot = TopLeftPivot;
            rectTransform.sizeDelta = rect.size;
            rectTransform.anchoredPosition = new Vector2(
                rect.x - parentRect.center.x,
                parentRect.center.y - rect.y);
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = Vector3.one;
        }

        private static RectTransform GetOrCreateChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);

            if (child == null)
            {
                GameObject gameObject = new GameObject(name, typeof(RectTransform));
                gameObject.layer = parent.gameObject.layer;
                gameObject.transform.SetParent(parent, false);
                child = gameObject.transform;
            }

            return (RectTransform)child;
        }

        private static T GetOrAdd<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T component = root.GetComponentInChildren<T>(true);

                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }
    }
}
