using UnityEngine;

namespace Whiskers.Dice.Rolling
{
    // Box shared by both sides. X/Z span the floor, Y is up.
    // TODO(art): tray visuals
    [DisallowMultipleComponent]
    public sealed class DiceTray : MonoBehaviour
    {
        private const float MinFloorSide = 1f;
        private const float MinWallHeight = 1f;
        private const float WallThickness = 0.5f;
        private const float Half = 0.5f;
        private const int ColliderCount = 6;

        [SerializeField] private Vector2 floorSize = new Vector2(8f, 8f);

        [SerializeField, Min(MinWallHeight)] private float wallHeight = 6f;

        public Vector2 FloorSize => floorSize;
        public float WallHeight => wallHeight;
        public float Aspect => floorSize.x / floorSize.y;

        // (0, 0) = bottom-left
        public Vector2 ToNormalized(Vector3 worldPosition)
        {
            Vector3 local = transform.InverseTransformPoint(worldPosition);
            return new Vector2(local.x / floorSize.x + Half, local.z / floorSize.y + Half);
        }

        public Vector3 FromNormalized(Vector2 normalized, float heightAboveFloor)
        {
            Vector3 local = new Vector3(
                (normalized.x - Half) * floorSize.x,
                heightAboveFloor,
                (normalized.y - Half) * floorSize.y);

            return transform.TransformPoint(local);
        }

        private void Awake()
        {
            if (!CollidersMatchSize())
            {
                RebuildColliders();
            }
        }

        [ContextMenu("Rebuild Colliders")]
        public void RebuildColliders()
        {
            foreach (BoxCollider existing in GetComponents<BoxCollider>())
            {
                DestroyImmediate(existing);
            }

            float halfWidth = floorSize.x * Half;
            float halfDepth = floorSize.y * Half;
            float halfThickness = WallThickness * Half;
            float wallCenterY = wallHeight * Half;

            // Order matters for CollidersMatchSize
            AddBox(new Vector3(0f, -halfThickness, 0f), FloorBoxSize());
            AddBox(new Vector3(0f, wallHeight + halfThickness, 0f), FloorBoxSize());
            AddBox(new Vector3(-halfWidth - halfThickness, wallCenterY, 0f), SideWallSize());
            AddBox(new Vector3(halfWidth + halfThickness, wallCenterY, 0f), SideWallSize());
            AddBox(new Vector3(0f, wallCenterY, -halfDepth - halfThickness), new Vector3(floorSize.x, wallHeight, WallThickness));
            AddBox(new Vector3(0f, wallCenterY, halfDepth + halfThickness), new Vector3(floorSize.x, wallHeight, WallThickness));
        }

        private void Reset()
        {
            RebuildColliders();
        }

        private void OnValidate()
        {
            floorSize = Vector2.Max(floorSize, Vector2.one * MinFloorSide);

#if UNITY_EDITOR
            // Can't add components inside OnValidate
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null && !Application.isPlaying && !CollidersMatchSize())
                {
                    RebuildColliders();
                }
            };
#endif
        }

        private bool CollidersMatchSize()
        {
            BoxCollider[] boxes = GetComponents<BoxCollider>();

            if (boxes.Length != ColliderCount)
            {
                return false;
            }

            return boxes[0].size == FloorBoxSize() && boxes[2].size == SideWallSize();
        }

        private Vector3 FloorBoxSize()
        {
            return new Vector3(floorSize.x + WallThickness * 2f, WallThickness, floorSize.y + WallThickness * 2f);
        }

        private Vector3 SideWallSize()
        {
            return new Vector3(WallThickness, wallHeight, floorSize.y + WallThickness * 2f);
        }

        private void AddBox(Vector3 center, Vector3 size)
        {
            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            box.center = center;
            box.size = size;
        }

        private void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(Vector3.up * (wallHeight * Half), new Vector3(floorSize.x, wallHeight, floorSize.y));
        }
    }
}
