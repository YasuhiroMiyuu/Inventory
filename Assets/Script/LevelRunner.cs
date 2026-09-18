using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace CaseFit
{
    [Serializable] public class LevelEvent : UnityEvent<LevelDefinition> { }
    [Serializable] public class FloatEvent : UnityEvent<float> { }
    [Serializable] public class IntEvent : UnityEvent<int> { }

    public class LevelRunner : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] InventoryCase inventoryCase;
        [SerializeField] TrayArea tray;
        [SerializeField] DragController dragController;

        [Header("Levels")]
        [SerializeField] List<LevelDefinition> levels = new();
        [SerializeField] int startIndex;
        [SerializeField] bool autoStart = true;
        [SerializeField] bool autoBegin = true;

        [Header("Item Spawning")]
        [SerializeField] string itemLayerName = "InventoryItem";
        [SerializeField] float itemThickness = 0.3f;

        [Header("Options")]
        [SerializeField] bool chillMode;

        [Header("Ending")]
        [SerializeField] string winSceneName = "Win";
        [SerializeField] float winSceneDelay = 2.5f;

        [Header("Events")]
        public LevelEvent onLevelLoaded = new();
        public FloatEvent onTimeChanged = new();
        public IntEvent onMovesChanged = new();
        public IntEvent onRemainingChanged = new();
        public IntEvent onLevelCleared = new();
        public UnityEvent onLevelFailed = new();
        public UnityEvent onStuck = new();
        public UnityEvent onGameCompleted = new();

        public int CurrentIndex { get; private set; }
        public float TimeLeft { get; private set; }
        public bool Running { get; private set; }
        public LevelDefinition CurrentLevel =>
            CurrentIndex >= 0 && CurrentIndex < levels.Count ? levels[CurrentIndex] : null;

        public bool ChillMode
        {
            get => chillMode;
            set => chillMode = value;
        }

        public int TotalStars
        {
            get
            {
                int total = 0;
                foreach (int value in starsPerLevel) total += value;
                return total;
            }
        }

        public int MaxStars => levels.Count * 3;
        public bool IsLastLevel => CurrentIndex >= levels.Count - 1;
        public int Moves => dragController != null ? dragController.Moves : 0;
        public int Remaining => RemainingCount();

        int[] starsPerLevel = new int[0];
        readonly List<ItemView> spawnedViews = new();
        readonly List<ItemInstance> spawnedItems = new();
        bool stuckReported;

        void OnEnable()
        {
            if (dragController != null) dragController.Changed += HandleChanged;
        }

        void OnDisable()
        {
            if (dragController != null) dragController.Changed -= HandleChanged;
        }

        void Start()
        {
            if (!autoStart)
            {
                Debug.LogWarning("[Case Fit] LevelRunner: 'Auto Start' is off, so no level is loaded. Tick it, or call LoadLevel() yourself.", this);
                return;
            }
            LoadLevel(startIndex);
        }

        void Update()
        {
            if (!Running || chillMode) return;

            TimeLeft -= Time.deltaTime;
            if (TimeLeft <= 0f)
            {
                TimeLeft = 0f;
                Fail();
            }
            onTimeChanged?.Invoke(TimeLeft);
        }

        public bool ValidateReferences()
        {
            bool ok = true;

            if (inventoryCase == null)
            {
                Debug.LogError("[Case Fit] LevelRunner: field 'Inventory Case' is empty. Drag the Case object into it.", this);
                ok = false;
            }
            else if (inventoryCase.GetComponent<GridLayout3D>() == null)
            {
                Debug.LogError("[Case Fit] LevelRunner: the Case object has no GridLayout3D component.", inventoryCase);
                ok = false;
            }

            if (tray == null)
            {
                Debug.LogError("[Case Fit] LevelRunner: field 'Tray' is empty. Drag the Tray object into it.", this);
                ok = false;
            }

            if (dragController == null)
            {
                Debug.LogError("[Case Fit] LevelRunner: field 'Drag Controller' is empty.", this);
                ok = false;
            }

            if (levels.Count == 0)
            {
                Debug.LogError("[Case Fit] LevelRunner: the 'Levels' list is empty. Add at least one Level Definition.", this);
                ok = false;
            }

            for (int i = 0; i < levels.Count; i++)
            {
                if (levels[i] != null) continue;
                Debug.LogError($"[Case Fit] LevelRunner: 'Levels' element {i} is empty (None). Drag a Level Definition asset into it.", this);
                ok = false;
            }

            return ok;
        }

        public void LoadLevel(int index)
        {
            if (!ValidateReferences()) return;

            if (starsPerLevel.Length != levels.Count) starsPerLevel = new int[levels.Count];

            CurrentIndex = Mathf.Clamp(index, 0, levels.Count - 1);
            LevelDefinition level = levels[CurrentIndex];

            Running = false;
            stuckReported = false;
            dragController.CancelCarry();
            dragController.ResetMoves();
            dragController.InputEnabled = false;

            ClearSpawned();
            inventoryCase.Build(level.gridSize);
            SpawnItems(level);

            TimeLeft = level.timeLimit;
            onLevelLoaded?.Invoke(level);
            onTimeChanged?.Invoke(TimeLeft);
            onMovesChanged?.Invoke(0);
            onRemainingChanged?.Invoke(spawnedItems.Count);

            if (autoBegin)
            {
                BeginLevel();
            }
            else
            {
                Debug.LogWarning("[Case Fit] LevelRunner: 'Auto Begin' is off, so the timer stays paused. Tick it, or call BeginLevel() from a button.", this);
            }
        }

        public void BeginLevel()
        {
            Running = true;
            dragController.InputEnabled = true;

            if (chillMode)
                Debug.Log("[Case Fit] Chill mode is ON - the timer will not count down. Untick it to run the clock.", this);
            else
                Debug.Log($"[Case Fit] Level {CurrentIndex + 1} started. Timer: {TimeLeft:0} s", this);
        }

        public void ReloadLevel() => LoadLevel(CurrentIndex);

        public void NextLevel()
        {
            if (CurrentIndex + 1 < levels.Count) LoadLevel(CurrentIndex + 1);
        }

        public void SetChillMode(bool value)
        {
            chillMode = value;
            onTimeChanged?.Invoke(TimeLeft);
        }

        public void ClearCase()
        {
            foreach (ItemView view in spawnedViews)
            {
                if (view == null || !view.Instance.IsPlaced) continue;
                inventoryCase.Take(view.Instance);
                tray.Add(view);
            }
            stuckReported = false;
            HandleChanged();
        }

        void SpawnItems(LevelDefinition level)
        {
            GridLayout3D layout = inventoryCase.Layout;
            int layer = LayerMask.NameToLayer(itemLayerName);
            tray.Clear();

            foreach (LevelDefinition.Entry entry in level.items)
            {
                if (entry == null || entry.item == null) continue;

                for (int n = 0; n < Mathf.Max(1, entry.count); n++)
                {
                    ItemInstance instance = new() { Definition = entry.item };

                    GameObject root = new(entry.item.displayName);
                    root.transform.SetParent(tray.transform, false);
                    if (layer >= 0) root.layer = layer;

                    BoxCollider box = root.AddComponent<BoxCollider>();
                    box.size = new Vector3(
                        entry.item.width * layout.StepX - layout.spacing.x,
                        entry.item.height * layout.StepY - layout.spacing.y,
                        itemThickness);

                    GameObject visual = CreateVisual(entry.item, root.transform, box.size);
                    if (layer >= 0) SetLayerRecursively(visual, layer);

                    ItemView view = root.AddComponent<ItemView>();
                    view.Bind(instance);

                    spawnedViews.Add(view);
                    spawnedItems.Add(instance);
                    tray.Add(view);
                }
            }
        }

        // Builds the model for one item and lines it up with its grid footprint.
        // The model's own pivot is ignored: what gets centred is the middle of its
        // geometry, so corner pivots and off-centre pivots both behave.
        GameObject CreateVisual(ItemDefinition item, Transform parent, Vector3 footprint)
        {
            if (item.prefab == null)
            {
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(cube.GetComponent<Collider>());
                cube.transform.SetParent(parent, false);
                cube.transform.localPosition = Vector3.zero;
                cube.transform.localScale = new Vector3(
                    footprint.x * 0.9f, footprint.y * 0.9f, itemThickness * 0.8f);
                return cube;
            }

            GameObject visual = Instantiate(item.prefab, parent);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.Euler(item.prefabEuler);
            visual.transform.localScale = Vector3.one;

            float scale = Mathf.Max(0.0001f, item.prefabScale);

            if (item.fitToFootprint && TryMeasure(parent, visual, out Bounds unit))
            {
                float fit = float.MaxValue;
                if (unit.size.x > 0.0001f) fit = Mathf.Min(fit, footprint.x / unit.size.x);
                if (unit.size.y > 0.0001f) fit = Mathf.Min(fit, footprint.y / unit.size.y);

                if (fit < float.MaxValue) scale *= fit * item.fitMargin;
            }

            visual.transform.localScale = Vector3.one * Mathf.Max(0.0001f, scale);

            if (item.autoCenter && TryMeasure(parent, visual, out Bounds placed))
                visual.transform.localPosition = -placed.center;

            visual.transform.localPosition += item.prefabOffset;
            return visual;
        }

        // Measures the model's geometry in the item root's own space, so a tilted
        // case or a rotated tray does not throw the numbers off.
        static bool TryMeasure(Transform space, GameObject target, out Bounds bounds)
        {
            bounds = new Bounds();
            bool any = false;

            foreach (MeshFilter filter in target.GetComponentsInChildren<MeshFilter>(true))
                if (filter.sharedMesh != null)
                    Accumulate(space, filter.transform, filter.sharedMesh.bounds, ref bounds, ref any);

            foreach (SkinnedMeshRenderer skin in target.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (skin.sharedMesh != null)
                    Accumulate(space, skin.transform, skin.sharedMesh.bounds, ref bounds, ref any);

            return any;
        }

        static void Accumulate(Transform space, Transform source, Bounds local, ref Bounds bounds, ref bool any)
        {
            Vector3 center = local.center;
            Vector3 extents = local.extents;

            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = center + new Vector3(
                    (i & 1) == 0 ? -extents.x : extents.x,
                    (i & 2) == 0 ? -extents.y : extents.y,
                    (i & 4) == 0 ? -extents.z : extents.z);

                Vector3 point = space.InverseTransformPoint(source.TransformPoint(corner));

                if (!any)
                {
                    bounds = new Bounds(point, Vector3.zero);
                    any = true;
                }
                else
                {
                    bounds.Encapsulate(point);
                }
            }
        }

        void ClearSpawned()
        {
            foreach (ItemView view in spawnedViews)
                if (view != null) Destroy(view.gameObject);

            spawnedViews.Clear();
            spawnedItems.Clear();
            tray.Clear();
        }

        void HandleChanged()
        {
            onMovesChanged?.Invoke(dragController.Moves);
            onRemainingChanged?.Invoke(RemainingCount());

            if (!Running) return;

            if (RemainingCount() == 0)
            {
                Complete();
                return;
            }

            if (dragController.Carried != null) return;

            if (!AnyItemCanBePlaced())
            {
                if (stuckReported) return;
                stuckReported = true;
                onStuck?.Invoke();
            }
            else
            {
                stuckReported = false;
            }
        }

        int RemainingCount()
        {
            int remaining = 0;
            foreach (ItemInstance item in spawnedItems)
                if (!item.IsPlaced) remaining++;
            return remaining;
        }

        bool AnyItemCanBePlaced()
        {
            foreach (ItemInstance item in spawnedItems)
            {
                if (item.IsPlaced) continue;
                if (inventoryCase.Grid.HasAnyValidPlacement(item)) return true;
            }
            return false;
        }

        void Complete()
        {
            Running = false;
            dragController.InputEnabled = false;

            int stars = CalculateStars();
            if (CurrentIndex >= 0 && CurrentIndex < starsPerLevel.Length)
                starsPerLevel[CurrentIndex] = Mathf.Max(starsPerLevel[CurrentIndex], stars);

            onLevelCleared?.Invoke(stars);

            if (!IsLastLevel) return;

            PlayerPrefs.SetInt("CaseFit_TotalStars", TotalStars);
            PlayerPrefs.SetInt("CaseFit_MaxStars", MaxStars);
            PlayerPrefs.SetString("CaseFit_GameScene", SceneManager.GetActiveScene().name);
            PlayerPrefs.Save();

            onGameCompleted?.Invoke();

            if (!string.IsNullOrEmpty(winSceneName))
                Invoke(nameof(LoadWinScene), Mathf.Max(0f, winSceneDelay));
        }

        public void LoadWinScene()
        {
            if (string.IsNullOrEmpty(winSceneName)) return;

            if (!Application.CanStreamedLevelBeLoaded(winSceneName))
            {
                Debug.LogError($"[Case Fit] Scene '{winSceneName}' is not in the build list. " +
                               "Add it in File > Build Profiles > Scene List.", this);
                return;
            }

            SceneManager.LoadScene(winSceneName);
        }

        void Fail()
        {
            Running = false;
            dragController.CancelCarry();
            dragController.InputEnabled = false;
            onLevelFailed?.Invoke();
        }

        public int CalculateStars()
        {
            LevelDefinition level = CurrentLevel;
            if (level == null) return 1;

            bool movesOk = dragController.Moves <= level.parMoves;
            bool timeOk = TimeLeft >= level.timeLimit * 0.4f;

            if (chillMode) return movesOk ? 2 : 1;
            if (movesOk && timeOk) return 3;
            if (movesOk || TimeLeft >= level.timeLimit * 0.15f) return 2;
            return 1;
        }

        static void SetLayerRecursively(GameObject target, int layer)
        {
            target.layer = layer;
            foreach (Transform child in target.transform)
                SetLayerRecursively(child.gameObject, layer);
        }
    }
}
