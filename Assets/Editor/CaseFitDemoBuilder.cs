using System.Collections.Generic;
using System.IO;
using CaseFit;
using UnityEditor;
using UnityEngine;

namespace CaseFitEditor
{
    public static class CaseFitDemoBuilder
    {
        const string Root = "Assets/CaseFitDemo";
        const string ItemFolder = Root + "/Items";
        const string LevelFolder = Root + "/Levels";

        [MenuItem("Tools/Case Fit/Create Demo Levels")]
        public static void CreateDemoAssets()
        {
            EnsureFolder("Assets", "CaseFitDemo");
            EnsureFolder(Root, "Items");
            EnsureFolder(Root, "Levels");

            Dictionary<string, ItemDefinition> items = new()
            {
                { "Pistol", MakeItem("Pistol", "Handgun", 2, 2) },
                { "AmmoBox", MakeItem("AmmoBox", "Ammo Box", 2, 1) },
                { "Herb", MakeItem("Herb", "Green Herb", 1, 1) },
                { "Shell", MakeItem("Shell", "Shotgun Shells", 1, 1) },
                { "Smg", MakeItem("Smg", "SMG", 3, 1) },
                { "Crowbar", MakeItem("Crowbar", "Crowbar", 3, 1) },
                { "AmmoBelt", MakeItem("AmmoBelt", "Ammo Belt", 3, 1) },
                { "Torch", MakeItem("Torch", "Flashlight", 3, 1) },
                { "Medkit", MakeItem("Medkit", "First Aid Kit", 2, 2) },
                { "Rifle", MakeItem("Rifle", "Rifle", 3, 1) },
                { "Shotgun", MakeItem("Shotgun", "Shotgun", 3, 1) },
                { "AmmoCrate", MakeItem("AmmoCrate", "Ammo Crate", 2, 2) },
                { "FuelCan", MakeItem("FuelCan", "Fuel Can", 2, 2) },
            };

            MakeLevel("Level_01", "Abandoned House", 3, 60f, 6,
                "Five items, nine slots. An exact fit - there is no room for a wasted cell.",
                new (ItemDefinition, int)[]
                {
                    (items["Pistol"], 1),
                    (items["AmmoBox"], 1),
                    (items["Herb"], 2),
                    (items["Shell"], 1),
                });

            MakeLevel("Level_02", "Lakeside Warehouse", 4, 120f, 7,
                "Four long items and one big box. Nothing small to plug the gaps - the big box does not have to sit in a corner.",
                new (ItemDefinition, int)[]
                {
                    (items["Smg"], 1),
                    (items["Crowbar"], 1),
                    (items["AmmoBelt"], 1),
                    (items["Torch"], 1),
                    (items["Medkit"], 1),
                });

            MakeLevel("Level_03", "Castle Storeroom", 5, 210f, 10,
                "Three long items and four square ones. The whole case has only a handful of valid packings.",
                new (ItemDefinition, int)[]
                {
                    (items["Rifle"], 1),
                    (items["Shotgun"], 1),
                    (items["Crowbar"], 1),
                    (items["Medkit"], 1),
                    (items["Pistol"], 1),
                    (items["AmmoCrate"], 1),
                    (items["FuelCan"], 1),
                });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Case Fit] Demo items and levels created under " + Root);
        }

        static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder(Path.Combine(parent, child).Replace('\\', '/')))
                AssetDatabase.CreateFolder(parent, child);
        }

        static ItemDefinition MakeItem(string assetName, string displayName, int width, int height)
        {
            string path = $"{ItemFolder}/{assetName}.asset";
            ItemDefinition asset = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<ItemDefinition>();
                AssetDatabase.CreateAsset(asset, path);
            }
            asset.displayName = displayName;
            asset.width = width;
            asset.height = height;
            asset.canRotate = width != height;
            EditorUtility.SetDirty(asset);
            return asset;
        }

        static void MakeLevel(string assetName, string title, int gridSize, float timeLimit, int parMoves,
                              string brief, (ItemDefinition item, int count)[] entries)
        {
            string path = $"{LevelFolder}/{assetName}.asset";
            LevelDefinition asset = AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<LevelDefinition>();
                AssetDatabase.CreateAsset(asset, path);
            }
            asset.title = title;
            asset.brief = brief;
            asset.gridSize = gridSize;
            asset.timeLimit = timeLimit;
            asset.parMoves = parMoves;
            asset.items = new List<LevelDefinition.Entry>();
            foreach ((ItemDefinition item, int count) in entries)
                asset.items.Add(new LevelDefinition.Entry { item = item, count = count });

            if (!asset.IsExactFit)
                Debug.LogError($"[Case Fit] {assetName} is not an exact fit: {asset.TotalItemArea} / {asset.CellCount}");

            EditorUtility.SetDirty(asset);
        }
    }
}
