using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Whiskers.Dice.Rolling
{
    // Top-down camera over the tray, renders into a texture.
    // Needs the Universal Renderer, not the 2D one.
    // TODO(art): shadows
    [RequireComponent(typeof(Camera))]
    public sealed class DiceRollCamera : MonoBehaviour
    {
        private const int MinTextureHeight = 64;
        // Must match the display texture for CopyTexture
        public const GraphicsFormat OutputColorFormat = GraphicsFormat.R8G8B8A8_SRGB;
        private const GraphicsFormat OutputDepthFormat = GraphicsFormat.D24_UNorm_S8_UInt;
        private const float LookDownPitch = 90f;
        private const float Half = 0.5f;
        private const float ClipMargin = 1f;
        private const float NearClip = 0.01f;
        private const string OutputName = "DiceRollOutput";

        [SerializeField] private DiceTray tray = null;
        [SerializeField, Min(MinTextureHeight)] private int textureHeight = 1080;

        [SerializeField, Min(0f)] private float heightAboveLid = 2f;

        private Camera rollCamera;

        public RenderTexture Output { get; private set; }

        private void Awake()
        {
            rollCamera = GetComponent<Camera>();
            FitToTray();
            CreateOutput();
        }

        private void OnDestroy()
        {
            if (Output == null)
            {
                return;
            }

            rollCamera.targetTexture = null;
            Output.Release();
            Destroy(Output);
        }

        [ContextMenu("Fit To Tray")]
        private void FitToTray()
        {
            Camera targetCamera = rollCamera != null ? rollCamera : GetComponent<Camera>();
            float height = tray.WallHeight + heightAboveLid;

            transform.SetPositionAndRotation(
                tray.transform.TransformPoint(Vector3.up * height),
                tray.transform.rotation * Quaternion.Euler(LookDownPitch, 0f, 0f));

            targetCamera.orthographic = true;
            targetCamera.orthographicSize = tray.FloorSize.y * Half;
            targetCamera.nearClipPlane = NearClip;
            targetCamera.farClipPlane = height + ClipMargin;
            targetCamera.clearFlags = CameraClearFlags.SolidColor;
            targetCamera.backgroundColor = Color.clear;
        }

        private void CreateOutput()
        {
            int width = Mathf.RoundToInt(textureHeight * tray.Aspect);

            Output = new RenderTexture(width, textureHeight, OutputColorFormat, OutputDepthFormat)
            {
                name = OutputName
            };

            Output.Create();
            rollCamera.targetTexture = Output;
            rollCamera.aspect = tray.Aspect;
        }
    }
}
