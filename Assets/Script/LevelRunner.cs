using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

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

        [Header("Events")]
        public LevelEvent onLevelLoaded = new();
        public FloatEvent onTimeChanged = new();
        public IntEvent onMovesChanged = new();
        public IntEvent onRemainingChanged = new();
        public IntEvent onLevelCleared = new();
        public UnityEvent onLevelFailed = new();
        public UnityEvent onStuck = new();

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
            if (autoStart) LoadLevel(startIndex);
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

        public void LoadLevel(int index)
        {
            if (levels.Count == 0) return;
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

            if (autoBegin) BeginLevel();
        }

        public void BeginLevel()
        {
            Running = true;
            dragController.InputEnabled = true;
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

                    GameObject visual;
                    if (entry.item.prefab != null)
                    {
                        visual = Instantiate(entry.item.prefab, root.transform);
                        visual.transform.localRotation = Quaternion.Euler(entry.item.prefabEuler);
                        visual.transform.localScale = Vector3.one * entry.item.prefabScale;
                    }
                    else
                    {
                        visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        Destroy(visual.GetComponent<Collider>());
                        visual.transform.SetParent(root.transform, false);
                        visual.transform.localScale = new Vector3(
                            box.size.x * 0.9f, box.size.y * 0.9f, itemThickness * 0.8f);
                    }

                    visual.transform.localPosition = Vector3.zero;
                    if (layer >= 0) SetLayerRecursively(visual, layer);

                    ItemView view = root.AddComponent<ItemView>();
                    view.Bind(instance);

                    spawnedViews.Add(view);
                    spawnedItems.Add(instance);
                    tray.Add(view);
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
            onLevelCleared?.Invoke(CalculateStars());
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
