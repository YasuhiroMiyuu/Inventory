using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CaseFit
{
    public class DragController : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] Camera gameCamera;
        [SerializeField] InventoryCase inventoryCase;
        [SerializeField] TrayArea tray;

        [Header("Picking")]
        [SerializeField] LayerMask itemLayers = ~0;
        [SerializeField] float rayDistance = 200f;
        [SerializeField] float clickThreshold = 8f;

        [Header("Ghost")]
        [SerializeField] Transform ghost;
        [SerializeField] Renderer ghostRenderer;
        [SerializeField] Color validColor = new(0.56f, 0.75f, 0.4f, 0.5f);
        [SerializeField] Color invalidColor = new(0.79f, 0.31f, 0.19f, 0.5f);
        [SerializeField] string ghostColorProperty = "_BaseColor";

        [Header("Offsets")]
        [SerializeField] Vector3 carryOffset = new(0f, 0f, -0.45f);
        [SerializeField] Vector3 ghostOffset = new(0f, 0f, -0.02f);

        public event Action Changed;
        public event Action Rejected;

        public bool InputEnabled { get; set; } = true;
        public int Moves { get; private set; }
        public ItemView Carried => carried;

        ItemView carried;
        bool sticky;
        Vector2 pressPosition;
        int hoverColumn;
        int hoverRow;
        bool hoverValid;
        MaterialPropertyBlock ghostBlock;
        int ghostColorId;

        void Awake()
        {
            if (gameCamera == null) gameCamera = Camera.main;
            ghostBlock = new MaterialPropertyBlock();
            ghostColorId = Shader.PropertyToID(ghostColorProperty);
            ShowGhost(false);
        }

        public void ResetMoves()
        {
            Moves = 0;
        }

        public void CancelCarry()
        {
            if (carried != null) ReturnCarriedToTray();
        }

        void Update()
        {
            if (!InputEnabled || gameCamera == null || inventoryCase == null || Mouse.current == null) return;

            Vector2 screen = Mouse.current.position.ReadValue();
            Ray ray = gameCamera.ScreenPointToRay(screen);
            bool pressed = Mouse.current.leftButton.wasPressedThisFrame;
            bool released = Mouse.current.leftButton.wasReleasedThisFrame;

            if (carried == null)
            {
                if (pressed) TryPickUp(ray, screen);
                return;
            }

            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) RotateCarried();
            if (Mouse.current.rightButton.wasPressedThisFrame) RotateCarried();
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                ReturnCarriedToTray();
                return;
            }

            UpdateCarry(ray);

            if (sticky)
            {
                if (pressed) Drop();
            }
            else if (released)
            {
                if (Vector2.Distance(screen, pressPosition) < clickThreshold) sticky = true;
                else Drop();
            }
        }

        void TryPickUp(Ray ray, Vector2 screen)
        {
            if (!Physics.Raycast(ray, out RaycastHit hit, rayDistance, itemLayers)) return;

            ItemView view = hit.collider.GetComponentInParent<ItemView>();
            if (view == null || view.Instance == null) return;

            carried = view;
            sticky = false;
            pressPosition = screen;

            inventoryCase.Take(view.Instance);
            if (tray != null) tray.Remove(view);
            view.transform.SetParent(inventoryCase.ItemRoot, true);
            view.SetHighlight(true);
            ShowGhost(true);
            Changed?.Invoke();
        }

        void UpdateCarry(Ray ray)
        {
            GridLayout3D layout = inventoryCase.Layout;
            if (!layout.RaycastSurface(ray, out Vector3 worldPoint)) return;

            Vector3 local = layout.transform.InverseTransformPoint(worldPoint);
            carried.transform.localPosition = local + carryOffset;

            ItemInstance item = carried.Instance;
            layout.LocalToCellForFootprint(local, item.Width, item.Height, out hoverColumn, out hoverRow);
            hoverValid = inventoryCase.Grid != null && inventoryCase.Grid.CanPlace(item, hoverColumn, hoverRow);
            UpdateGhost();
        }

        void RotateCarried()
        {
            if (carried == null || !carried.Instance.Definition.canRotate) return;
            carried.Instance.Rotated = !carried.Instance.Rotated;
            carried.ApplyOrientation();
        }

        void Drop()
        {
            ItemView view = carried;
            carried = null;
            sticky = false;
            ShowGhost(false);

            if (hoverValid && inventoryCase.TryPlace(view, hoverColumn, hoverRow))
            {
                Moves++;
            }
            else
            {
                SendToTray(view);
                Rejected?.Invoke();
            }

            view.SetHighlight(false);
            Changed?.Invoke();
        }

        void ReturnCarriedToTray()
        {
            ItemView view = carried;
            carried = null;
            sticky = false;
            ShowGhost(false);
            SendToTray(view);
            view.SetHighlight(false);
            Changed?.Invoke();
        }

        void SendToTray(ItemView view)
        {
            inventoryCase.Take(view.Instance);
            if (tray != null) tray.Add(view);
        }

        void UpdateGhost()
        {
            if (ghost == null || carried == null) return;

            GridLayout3D layout = inventoryCase.Layout;
            ItemInstance item = carried.Instance;

            ghost.localPosition = layout.CellToLocal(hoverColumn, hoverRow, item.Width, item.Height) + ghostOffset;
            ghost.localRotation = Quaternion.identity;
            ghost.localScale = layout.SizeOfFootprint(item.Width, item.Height);

            if (ghostRenderer == null) return;
            ghostBlock ??= new MaterialPropertyBlock();
            ghostRenderer.GetPropertyBlock(ghostBlock);
            ghostBlock.SetColor(ghostColorId, hoverValid ? validColor : invalidColor);
            ghostRenderer.SetPropertyBlock(ghostBlock);
        }

        void ShowGhost(bool visible)
        {
            if (ghost != null) ghost.gameObject.SetActive(visible);
        }
    }
}
