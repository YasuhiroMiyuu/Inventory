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
                { "Pistol", MakeItem("Pistol", "ปืนพก", 2, 2) },
                { "AmmoBox", MakeItem("AmmoBox", "กล่องกระสุน", 2, 1) },
                { "Herb", MakeItem("Herb", "สมุนไพร", 1, 1) },
                { "Shell", MakeItem("Shell", "ลูกซองเปลือย", 1, 1) },
                { "Smg", MakeItem("Smg", "ปืนกลมือ", 3, 1) },
                { "Crowbar", MakeItem("Crowbar", "ชะแลง", 3, 1) },
                { "AmmoBelt", MakeItem("AmmoBelt", "แถบกระสุน", 3, 1) },
                { "Torch", MakeItem("Torch", "ไฟฉาย", 3, 1) },
                { "Medkit", MakeItem("Medkit", "ชุดปฐมพยาบาล", 2, 2) },
                { "Rifle", MakeItem("Rifle", "ไรเฟิลยาว", 3, 1) },
                { "Shotgun", MakeItem("Shotgun", "ลูกซองยาว", 3, 1) },
                { "AmmoCrate", MakeItem("AmmoCrate", "ลังกระสุน", 2, 2) },
                { "FuelCan", MakeItem("FuelCan", "ถังเชื้อเพลิง", 2, 2) },
            };

            MakeLevel("Level_01", "บ้านร้างหลังแรก", 3, 60f, 6,
                "ของ 5 ชิ้น ช่องว่าง 9 ช่อง พอดีเป๊ะ ไม่มีที่ให้พลาด",
                new (ItemDefinition, int)[]
                {
                    (items["Pistol"], 1),
                    (items["AmmoBox"], 1),
                    (items["Herb"], 2),
                    (items["Shell"], 1),
                });

            MakeLevel("Level_02", "โกดังริมทะเลสาบ", 4, 120f, 7,
                "ของยาว 4 ชิ้น กับกล่องใหญ่ 1 ใบ ไม่มีชิ้นเล็กให้อุดรูเลย ลองคิดใหม่ว่ากล่องใหญ่ไม่จำเป็นต้องอยู่ที่มุม",
                new (ItemDefinition, int)[]
                {
                    (items["Smg"], 1),
                    (items["Crowbar"], 1),
                    (items["AmmoBelt"], 1),
                    (items["Torch"], 1),
                    (items["Medkit"], 1),
                });

            MakeLevel("Level_03", "คลังใต้ปราสาท", 5, 210f, 10,
                "ของยาว 3 ชิ้น กล่องสี่เหลี่ยม 4 ใบ ทั้งกระเป๋ามีทางลงอยู่แค่ไม่กี่ทาง",
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
