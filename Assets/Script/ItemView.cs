using UnityEngine;

namespace CaseFit
{
    public class ItemView : MonoBehaviour
    {
        [Header("Highlight")]
        public Color normalColor = Color.white;
        public Color carriedColor = new(1f, 0.83f, 0.32f);
        public string colorProperty = "_BaseColor";

        public ItemInstance Instance { get; private set; }

        Renderer[] renderers;
        MaterialPropertyBlock block;
        int colorId;

        public void Bind(ItemInstance instance)
        {
            Instance = instance;
            renderers = GetComponentsInChildren<Renderer>(true);
            colorId = Shader.PropertyToID(colorProperty);
            block = new MaterialPropertyBlock();
            ApplyOrientation();
            SetHighlight(false);
        }

        public void ApplyOrientation()
        {
            bool rotated = Instance != null && Instance.Rotated;
            transform.localRotation = Quaternion.Euler(0f, 0f, rotated ? -90f : 0f);
        }

        public void SetHighlight(bool carried)
        {
            if (renderers == null) return;
            block ??= new MaterialPropertyBlock();
            Color color = carried ? carriedColor : normalColor;
            foreach (Renderer target in renderers)
            {
                if (target == null) continue;
                target.GetPropertyBlock(block);
                block.SetColor(colorId, color);
                target.SetPropertyBlock(block);
            }
        }
    }
}
