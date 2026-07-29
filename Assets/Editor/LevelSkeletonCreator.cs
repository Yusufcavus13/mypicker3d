using UnityEditor;
using UnityEngine;

/// <summary>
/// Mevcut bir level prefabindan duraklari cikarip "bos level iskeleti" uretir ve
/// uzerindeki LevelBuilder'in referanslarini otomatik baglar.
/// Kaynak prefaba dokunmaz, kopyasi uzerinde calisir.
/// </summary>
public static class LevelSkeletonCreator
{
    private const string SourcePrefabPath = "Assets/Prefabs/Levels/SavedLevels/Level1.prefab";
    private const string SkeletonPrefabPath = "Assets/Prefabs/Levels/LevelSkeleton.prefab";

    [MenuItem("Tools/Picker3D/Bos Level Iskeleti Olustur")]
    public static void CreateSkeleton()
    {
        GameObject contents = PrefabUtility.LoadPrefabContents(SourcePrefabPath);
        if (contents == null)
        {
            Debug.LogError($"[LevelSkeletonCreator] {SourcePrefabPath} acilamadi.");
            return;
        }

        try
        {
            LevelInfoManager info = contents.GetComponent<LevelInfoManager>();
            if (info == null)
            {
                Debug.LogError("[LevelSkeletonCreator] Kaynak prefabta LevelInfoManager yok.");
                return;
            }

            //eski bilesenin bagladigi referanslari okuyoruz, elle surukleme derdi olmasin
            SerializedObject infoObject = new SerializedObject(info);
            Transform stagesParent = GetReference<Transform>(infoObject, "stagesParent");
            GameObject stagePrefab = GetReference<GameObject>(infoObject, "stagePrefab");
            Renderer startCubeRenderer = GetReference<Renderer>(infoObject, "startCubeRenderer");
            Renderer endCubeRenderer = GetReference<Renderer>(infoObject, "endCubeRenderer");
            GameObject endRoad = GetReference<GameObject>(infoObject, "endCube");

            if (stagesParent == null || stagePrefab == null)
            {
                Debug.LogError("[LevelSkeletonCreator] stagesParent ya da stagePrefab okunamadi.");
                return;
            }

            int removedStages = stagesParent.childCount;
            while (stagesParent.childCount > 0)
            {
                Transform child = stagesParent.GetChild(0);
                child.SetParent(null);
                Object.DestroyImmediate(child.gameObject);
            }

            Object.DestroyImmediate(info, true);

            LevelBuilder builder = contents.GetComponent<LevelBuilder>();
            if (builder == null)
                builder = contents.AddComponent<LevelBuilder>();

            SerializedObject builderObject = new SerializedObject(builder);
            SetReference(builderObject, "stagesParent", stagesParent);
            SetReference(builderObject, "stagePrefab", stagePrefab);
            SetReference(builderObject, "startCubeRenderer", startCubeRenderer);
            SetReference(builderObject, "endCubeRenderer", endCubeRenderer);
            SetReference(builderObject, "endRoad", endRoad != null ? endRoad.transform : null);
            builderObject.ApplyModifiedPropertiesWithoutUndo();

            contents.name = "LevelSkeleton";
            PrefabUtility.SaveAsPrefabAsset(contents, SkeletonPrefabPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[LevelSkeletonCreator] {SkeletonPrefabPath} olusturuldu. " +
                      $"{removedStages} durak cikarildi, LevelBuilder baglandi.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    private static T GetReference<T>(SerializedObject source, string propertyName) where T : Object
    {
        SerializedProperty property = source.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogWarning($"[LevelSkeletonCreator] '{propertyName}' alani bulunamadi.");
            return null;
        }
        return property.objectReferenceValue as T;
    }

    private static void SetReference(SerializedObject target, string propertyName, Object value)
    {
        SerializedProperty property = target.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogWarning($"[LevelSkeletonCreator] LevelBuilder'da '{propertyName}' alani yok.");
            return;
        }
        property.objectReferenceValue = value;
    }
}
