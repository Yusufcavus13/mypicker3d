using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;


public static class LevelDataConverter
{
    private const string LevelPrefabsFolder = "Assets/Prefabs/Levels/SavedLevels";
    private const string LevelDataFolder = "Assets/LevelData";

    [MenuItem("Tools/Picker3D/Prefab Levelleri LevelData'ya Cevir")]
    public static void ConvertAll()
    {
        if (!AssetDatabase.IsValidFolder(LevelDataFolder))
            AssetDatabase.CreateFolder("Assets", "LevelData");

        //isme gore siralayalim ki Level1, Level2, ... sirasi bozulmasin
        List<string> prefabPaths = AssetDatabase.FindAssets("t:Prefab", new[] { LevelPrefabsFolder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .OrderBy(path => path)
            .ToList();

        int converted = 0;

        foreach (string prefabPath in prefabPaths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                continue;

            //Stage bileseninin kendisini hiyerarsi sirasiyla okuyoruz
            Stage[] stages = prefab.GetComponentsInChildren<Stage>(true);
            if (stages.Length == 0)
            {
                Debug.LogWarning($"[LevelDataConverter] {prefabPath} icinde durak bulunamadi, atlandi.");
                continue;
            }

            string assetPath = $"{LevelDataFolder}/{prefab.name}.asset";

            //Ayni isimde asset varsa yenisini uretmek yerine icerigini guncelliyoruz,
            //boylece daha once bagladigin referanslar (GUID) kopmuyor.
            LevelData levelData = AssetDatabase.LoadAssetAtPath<LevelData>(assetPath);
            bool isNew = levelData == null;
            if (isNew)
                levelData = ScriptableObject.CreateInstance<LevelData>();

            levelData.stages = stages.Select(stage => new StageData
            {
                spawnedBallCount = stage.SpawnedBallCount,
                targetBallCount = stage.TargetBallCount,
                platformColor = stage.PlatformColor,
                platformLength = stage.PlatformLength
            }).ToList();

            if (isNew)
                AssetDatabase.CreateAsset(levelData, assetPath);
            else
                EditorUtility.SetDirty(levelData);

            Debug.Log($"[LevelDataConverter] {prefab.name}: {levelData.stages.Count} durak -> {assetPath}");
            converted++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[LevelDataConverter] Tamamlandi. {converted} level {LevelDataFolder} klasorune yazildi.");
    }
}
