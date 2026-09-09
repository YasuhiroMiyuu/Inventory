using UnityEngine;

namespace CaseFit
{
    [ExecuteAlways]
    public class CaseFrame : MonoBehaviour
    {
        const string RootName = "__CaseFrame";

        [Header("Grid")]
        public GridLayout3D layout;

        [Header("Shell")]
        public float rimWidth = 0.42f;
        public float rimRise = 0.22f;
        public float floorThickness = 0.16f;
        public float shellDepth = 0.45f;
        public float shellLip = 0.1f;
        public float gapBehindGrid = 0.2f;

        [Header("Details")]
        public bool cornerBrackets = true;
        public float cornerSize = 0.5f;
        public bool latches = true;
        public bool handle = true;
        public float handleWidth = 1.7f;
        [Range(0, 12)] public int rivetsPerSide = 5;
        public float rivetSize = 0.1f;

        [Header("Materials")]
        public Material bodyMaterial;
        public Material metalMaterial;
        public Material interiorMaterial;

        Transform root;
        Vector2 lastSize = new(-1f, -1f);
        bool dirty = true;

        void OnEnable() => dirty = true;

        void OnValidate() => dirty = true;

        void LateUpdate()
        {
            if (layout == null) layout = GetComponentInParent<GridLayout3D>();
            if (layout == null) return;

            Vector2 size = layout.TotalSize;
            if (!dirty && size == lastSize) return;

            lastSize = size;
            dirty = false;
            Rebuild();
        }

        [ContextMenu("Rebuild")]
        public void Rebuild()
        {
            if (layout == null) layout = GetComponentInParent<GridLayout3D>();
            if (layout == null) return;

            EnsureRoot();
            ClearRoot();

            Vector2 total = layout.TotalSize;
            root.localPosition = layout.Origin + new Vector3(total.x * 0.5f, -total.y * 0.5f, 0f);
            root.localRotation = Quaternion.identity;
            root.localScale = Vector3.one;

            float halfW = total.x * 0.5f;
            float halfH = total.y * 0.5f;
            float outerW = total.x + rimWidth * 2f;
            float outerH = total.y + rimWidth * 2f;

            float floorZ = gapBehindGrid + floorThickness * 0.5f;
            float wallDepth = rimRise + gapBehindGrid;
            float wallZ = gapBehindGrid - wallDepth * 0.5f;
            float shellZ = gapBehindGrid + floorThickness + shellDepth * 0.5f;

            Material body = bodyMaterial;
            Material metal = metalMaterial != null ? metalMaterial : bodyMaterial;
            Material interior = interiorMaterial != null ? interiorMaterial : bodyMaterial;

            Box("Shell", new Vector3(0f, 0f, shellZ),
                new Vector3(outerW + shellLip, outerH + shellLip, shellDepth), body);

            Box("Interior", new Vector3(0f, 0f, floorZ),
                new Vector3(outerW - rimWidth, outerH - rimWidth, floorThickness), interior);

            Box("Rim_Top", new Vector3(0f, halfH + rimWidth * 0.5f, wallZ),
                new Vector3(outerW, rimWidth, wallDepth), body);
            Box("Rim_Bottom", new Vector3(0f, -halfH - rimWidth * 0.5f, wallZ),
                new Vector3(outerW, rimWidth, wallDepth), body);
            Box("Rim_Left", new Vector3(-halfW - rimWidth * 0.5f, 0f, wallZ),
                new Vector3(rimWidth, total.y, wallDepth), body);
            Box("Rim_Right", new Vector3(halfW + rimWidth * 0.5f, 0f, wallZ),
                new Vector3(rimWidth, total.y, wallDepth), body);

            if (cornerBrackets)
            {
                float cx = halfW + rimWidth * 0.5f;
                float cy = halfH + rimWidth * 0.5f;
                Vector3 cornerScale = new(cornerSize, cornerSize, wallDepth * 1.25f);
                Box("Corner_TL", new Vector3(-cx, cy, wallZ), cornerScale, metal);
                Box("Corner_TR", new Vector3(cx, cy, wallZ), cornerScale, metal);
                Box("Corner_BL", new Vector3(-cx, -cy, wallZ), cornerScale, metal);
                Box("Corner_BR", new Vector3(cx, -cy, wallZ), cornerScale, metal);
            }

            if (rivetsPerSide > 0)
            {
                float span = total.x * 0.78f;
                float step = rivetsPerSide > 1 ? span / (rivetsPerSide - 1) : 0f;
                Vector3 rivetScale = new(rivetSize, rivetSize, wallDepth * 1.3f);
                for (int i = 0; i < rivetsPerSide; i++)
                {
                    float x = rivetsPerSide > 1 ? -span * 0.5f + step * i : 0f;
                    Box($"Rivet_T{i}", new Vector3(x, halfH + rimWidth * 0.5f, wallZ), rivetScale, metal);
                    Box($"Rivet_B{i}", new Vector3(x, -halfH - rimWidth * 0.5f, wallZ), rivetScale, metal);
                }
            }

            if (latches)
            {
                float lx = total.x * 0.24f;
                float ly = -halfH - rimWidth * 0.5f;
                Vector3 latchScale = new(rimWidth * 1.05f, rimWidth * 0.66f, wallDepth * 1.45f);
                Box("Latch_L", new Vector3(-lx, ly, wallZ), latchScale, metal);
                Box("Latch_R", new Vector3(lx, ly, wallZ), latchScale, metal);
            }

            if (handle)
            {
                float postX = handleWidth * 0.5f;
                float baseY = halfH + rimWidth;
                float postH = rimWidth * 0.8f;
                Vector3 postScale = new(rimWidth * 0.34f, postH, rimWidth * 0.34f);
                Box("Handle_L", new Vector3(-postX, baseY + postH * 0.5f, wallZ), postScale, metal);
                Box("Handle_R", new Vector3(postX, baseY + postH * 0.5f, wallZ), postScale, metal);
                Box("Handle_Bar",
                    new Vector3(0f, baseY + postH, wallZ),
                    new Vector3(handleWidth + rimWidth * 0.34f, rimWidth * 0.3f, rimWidth * 0.34f), metal);
            }
        }

        void EnsureRoot()
        {
            if (root != null) return;

            Transform found = transform.Find(RootName);
            if (found != null)
            {
                root = found;
                return;
            }

            GameObject created = new(RootName);
            created.transform.SetParent(transform, false);
            created.hideFlags = HideFlags.DontSave;
            root = created.transform;
        }

        void ClearRoot()
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                GameObject child = root.GetChild(i).gameObject;
                child.transform.SetParent(null, false);
                Kill(child);
            }
        }

        GameObject Box(string boxName, Vector3 center, Vector3 size, Material material)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = boxName;
            box.hideFlags = HideFlags.DontSave;
            box.layer = gameObject.layer;

            Collider collider = box.GetComponent<Collider>();
            if (collider != null) Kill(collider);

            box.transform.SetParent(root, false);
            box.transform.localPosition = center;
            box.transform.localScale = size;

            if (material != null) box.GetComponent<MeshRenderer>().sharedMaterial = material;
            return box;
        }

        static void Kill(Object target)
        {
            if (Application.isPlaying) Destroy(target);
            else DestroyImmediate(target);
        }
    }
}
