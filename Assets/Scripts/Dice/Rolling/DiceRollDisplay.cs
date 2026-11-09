using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using Whiskers.Core.Events;

namespace Whiskers.Dice.Rolling
{
    // Shows the dice camera picture on the board, also the throw area.
    // Area = SpriteRenderer Size, the picture keeps the tray proportions.
    // RenderTexture can't be a sprite, so it's copied every frame.
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class DiceRollDisplay : MonoBehaviour
    {
        private const float CenterPivot = 0.5f;
        private const uint NoExtrude = 0;
        private const string TextureName = "DiceRollDisplayTexture";

        private static readonly Vector2 DefaultAreaSize = new Vector2(8f, 8f);

        private SpriteRenderer spriteRenderer;
        private RenderTexture source;
        private Texture2D target;
        private Sprite sprite;
        private Vector2 areaSize;
        private float pictureAspect;

        // Local units, centered
        public Rect PictureRect
        {
            get
            {
                Vector2 area = Application.isPlaying ? areaSize : Renderer.size;
                Vector2 size = FitInside(area, pictureAspect > 0f ? pictureAspect : area.x / area.y);
                return new Rect(-size * CenterPivot, size);
            }
        }

        private SpriteRenderer Renderer => spriteRenderer != null ? spriteRenderer : spriteRenderer = GetComponent<SpriteRenderer>();

        private void Awake()
        {
            EnsureSlicedDrawMode();
            areaSize = Renderer.size;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<DiceRollStageReadyEvent>(OnStageReady);

            if (DiceRollStage.Instance != null)
            {
                Show(DiceRollStage.Instance);
            }
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<DiceRollStageReadyEvent>(OnStageReady);
        }

        private void OnDestroy()
        {
            ReleaseTarget();
        }

        private void LateUpdate()
        {
            if (source != null && target != null && source.IsCreated())
            {
                Graphics.CopyTexture(source, target);
            }
        }

        // False if outside the picture
        public bool TryGetNormalizedPoint(Vector2 screenPosition, Camera boardCamera, out Vector2 normalized)
        {
            normalized = GetNormalizedPoint(screenPosition, boardCamera);
            return normalized.x >= 0f && normalized.x <= 1f && normalized.y >= 0f && normalized.y <= 1f;
        }

        // Not clamped
        public Vector2 GetNormalizedPoint(Vector2 screenPosition, Camera boardCamera)
        {
            float depth = boardCamera.WorldToScreenPoint(transform.position).z;
            Vector3 world = boardCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, depth));
            Vector3 local = transform.InverseTransformPoint(world);
            Rect picture = PictureRect;

            return new Vector2((local.x - picture.xMin) / picture.width, (local.y - picture.yMin) / picture.height);
        }

        public Vector3 NormalizedToWorld(Vector2 normalized)
        {
            Rect picture = PictureRect;
            return transform.TransformPoint(picture.min + Vector2.Scale(normalized, picture.size));
        }

        private void Reset()
        {
            Renderer.drawMode = SpriteDrawMode.Sliced;
            Renderer.size = DefaultAreaSize;
        }

        private void OnStageReady(DiceRollStageReadyEvent stageReady)
        {
            Show(stageReady.Stage);
        }

        private void Show(DiceRollStage stage)
        {
            if ((SystemInfo.copyTextureSupport & CopyTextureSupport.RTToTexture) == 0)
            {
                Debug.LogError("This platform cannot copy render textures into textures, the dice display stays empty.", this);
                return;
            }

            ReleaseTarget();

            source = stage.RollCamera.Output;
            pictureAspect = stage.Tray.Aspect;
            target = new Texture2D(source.width, source.height, DiceRollCamera.OutputColorFormat, TextureCreationFlags.None)
            {
                name = TextureName
            };

            sprite = Sprite.Create(
                target,
                new Rect(0f, 0f, source.width, source.height),
                Vector2.one * CenterPivot,
                source.height,
                NoExtrude,
                SpriteMeshType.FullRect);

            Renderer.sprite = sprite;
            Renderer.size = PictureRect.size;
        }

        private static Vector2 FitInside(Vector2 area, float aspect)
        {
            return area.x / area.y > aspect
                ? new Vector2(area.y * aspect, area.y)
                : new Vector2(area.x, area.x / aspect);
        }

        // Simple mode has no Size
        private void EnsureSlicedDrawMode()
        {
            if (Renderer.drawMode != SpriteDrawMode.Simple)
            {
                return;
            }

            Vector2 visibleSize = Renderer.sprite != null ? (Vector2)Renderer.sprite.bounds.size : DefaultAreaSize;
            Renderer.drawMode = SpriteDrawMode.Sliced;
            Renderer.size = visibleSize;
        }

        private void ReleaseTarget()
        {
            if (sprite != null)
            {
                Destroy(sprite);
            }

            if (target != null)
            {
                Destroy(target);
            }

            sprite = null;
            target = null;
            source = null;
        }

        private void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(Vector3.zero, Application.isPlaying ? areaSize : Renderer.size);
        }
    }
}
