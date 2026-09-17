using UnityEngine;

namespace CaseFit
{
    [ExecuteAlways]
    public class CaseFitStaging : MonoBehaviour
    {
        public enum TrayPlacement
        {
            Below,
            Right,
            Left
        }

        [Header("References")]
        [SerializeField] GridLayout3D layout;
        [SerializeField] TrayArea tray;
        [SerializeField] CaseFrame frame;
        [SerializeField] Camera gameCamera;

        [Header("Tray Placement")]
        [SerializeField] TrayPlacement placement = TrayPlacement.Below;
        [SerializeField] float trayMargin = 1.1f;
        [SerializeField] int columnsWhenBelow = 7;
        [SerializeField] int columnsWhenBeside = 3;

        [Header("Camera Framing")]
        [SerializeField] bool frameCamera = true;
        [SerializeField] float viewPadding = 1.2f;
        [SerializeField] float minDistance = 5f;
        [SerializeField] float followSpeed = 6f;

        Vector3 targetCameraPosition;
        bool hasTarget;

        void OnEnable()
        {
            hasTarget = false;
            Apply();
        }

        void OnValidate() => hasTarget = false;

        void LateUpdate()
        {
            Apply();

            if (!frameCamera || gameCamera == null || !hasTarget) return;

            if (Application.isPlaying && followSpeed > 0f)
            {
                gameCamera.transform.position = Vector3.Lerp(
                    gameCamera.transform.position, targetCameraPosition, Time.deltaTime * followSpeed);
            }
            else
            {
                gameCamera.transform.position = targetCameraPosition;
            }

            gameCamera.transform.rotation = layout.transform.rotation;
        }

        [ContextMenu("Apply")]
        public void Apply()
        {
            if (layout == null) layout = GetComponentInChildren<GridLayout3D>();
            if (layout == null || tray == null) return;

            Vector2 total = layout.TotalSize;
            float rim = frame != null ? frame.rimWidth : 0f;

            Vector2 caseCenter = layout.Origin + new Vector3(total.x * 0.5f, -total.y * 0.5f, 0f);
            float caseHalfW = total.x * 0.5f + rim;
            float caseHalfH = total.y * 0.5f + rim;

            int wantedColumns = placement == TrayPlacement.Below ? columnsWhenBelow : columnsWhenBeside;
            if (tray.columns != wantedColumns)
            {
                tray.columns = wantedColumns;
                tray.Rebuild();
            }

            int rows = CountTrayRows();
            float trayW = tray.columns * tray.StepX - tray.spacing.x;
            float trayH = rows * tray.StepY - tray.spacing.y;

            Vector2 trayOrigin;
            switch (placement)
            {
                case TrayPlacement.Right:
                    trayOrigin = new Vector2(
                        caseCenter.x + caseHalfW + trayMargin + trayW * 0.5f,
                        caseCenter.y + trayH * 0.5f);
                    break;
                case TrayPlacement.Left:
                    trayOrigin = new Vector2(
                        caseCenter.x - caseHalfW - trayMargin - trayW * 0.5f,
                        caseCenter.y + trayH * 0.5f);
                    break;
                default:
                    trayOrigin = new Vector2(caseCenter.x, caseCenter.y - caseHalfH - trayMargin);
                    break;
            }

            tray.transform.SetPositionAndRotation(
                layout.transform.TransformPoint(new Vector3(trayOrigin.x, trayOrigin.y, 0f)),
                layout.transform.rotation);

            if (!frameCamera || gameCamera == null) return;

            Vector2 trayCenter = new(trayOrigin.x, trayOrigin.y - trayH * 0.5f);

            float minX = Mathf.Min(caseCenter.x - caseHalfW, trayCenter.x - trayW * 0.5f);
            float maxX = Mathf.Max(caseCenter.x + caseHalfW, trayCenter.x + trayW * 0.5f);
            float minY = Mathf.Min(caseCenter.y - caseHalfH, trayCenter.y - trayH * 0.5f);
            float maxY = Mathf.Max(caseCenter.y + caseHalfH, trayCenter.y + trayH * 0.5f);

            Vector2 viewCenter = new((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
            float halfW = (maxX - minX) * 0.5f + viewPadding;
            float halfH = (maxY - minY) * 0.5f + viewPadding;
            float aspect = gameCamera.aspect > 0.01f ? gameCamera.aspect : 16f / 9f;

            if (gameCamera.orthographic)
            {
                gameCamera.orthographicSize = Mathf.Max(halfH, halfW / aspect);
                targetCameraPosition = layout.transform.TransformPoint(
                    new Vector3(viewCenter.x, viewCenter.y, -Mathf.Max(minDistance, 10f)));
            }
            else
            {
                float halfFov = gameCamera.fieldOfView * 0.5f * Mathf.Deg2Rad;
                float tan = Mathf.Tan(halfFov);
                float distance = Mathf.Max(halfH / tan, halfW / (tan * aspect));
                distance = Mathf.Max(distance, minDistance);
                targetCameraPosition = layout.transform.TransformPoint(
                    new Vector3(viewCenter.x, viewCenter.y, -distance));
            }

            hasTarget = true;
        }

        int CountTrayRows()
        {
            int column = 0;
            int rowTop = 0;
            int rowHeight = 0;

            foreach (ItemView view in tray.Members)
            {
                if (view == null || view.Instance == null) continue;

                int width = view.Instance.Width;
                int height = view.Instance.Height;

                if (column + width > tray.columns && column > 0)
                {
                    rowTop += Mathf.Max(1, rowHeight);
                    column = 0;
                    rowHeight = 0;
                }

                column += width;
                rowHeight = Mathf.Max(rowHeight, height);
            }

            return Mathf.Max(1, rowTop + Mathf.Max(1, rowHeight));
        }
    }
}
