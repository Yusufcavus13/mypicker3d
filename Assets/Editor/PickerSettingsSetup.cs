using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// PickerSettings varligini uretir ve hem sahnedeki PickerMovement'a hem de
/// RoadPlatform prefabina baglar.
///
/// Elle baglamak riskli: ikisinden birini unutmak, tam da bu varligin cozmek
/// icin var oldugu "sayilar ayristi" hatasini geri getirir.
/// </summary>
public static class PickerSettingsSetup
{
    private const string SettingsPath = "Assets/Settings/PickerSettings.asset";
    private const string RoadPlatformPath = "Assets/Prefabs/RoadPlatform.prefab";

    [MenuItem("Tools/Picker3D/Picker Ayarlarini Kur ve Bagla")]
    public static void SetupAndWire()
    {
        PickerSettings settings = GetOrCreateSettings();

        int wired = 0;
        wired += WireScene(settings) ? 1 : 0;
        wired += WireRoadPlatform(settings) ? 1 : 0;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[PickerSettingsSetup] {SettingsPath} hazir, {wired} yerde baglandi. " +
                  "Sahne degistiyse kaydetmeyi unutma (Cmd+S).");
    }

    private static PickerSettings GetOrCreateSettings()
    {
        PickerSettings existing = AssetDatabase.LoadAssetAtPath<PickerSettings>(SettingsPath);
        if (existing != null)
            return existing;

        if (!AssetDatabase.IsValidFolder("Assets/Settings"))
            AssetDatabase.CreateFolder("Assets", "Settings");

        PickerSettings settings = ScriptableObject.CreateInstance<PickerSettings>();
        AssetDatabase.CreateAsset(settings, SettingsPath);
        return settings;
    }

    private static bool WireScene(PickerSettings settings)
    {
        Scene scene = SceneManager.GetActiveScene();
        bool wired = false;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (PickerMovement picker in root.GetComponentsInChildren<PickerMovement>(true))
            {
                if (AssignField(picker, "settings", settings))
                    wired = true;
            }
        }

        if (wired)
            EditorSceneManager.MarkSceneDirty(scene);
        else
            Debug.LogWarning($"[PickerSettingsSetup] Acik sahnede ({scene.name}) PickerMovement bulunamadi.");

        return wired;
    }

    private static bool WireRoadPlatform(PickerSettings settings)
    {
        GameObject contents = PrefabUtility.LoadPrefabContents(RoadPlatformPath);
        if (contents == null)
        {
            Debug.LogError($"[PickerSettingsSetup] {RoadPlatformPath} acilamadi.");
            return false;
        }

        try
        {
            bool wired = false;
            foreach (RoadPlatform road in contents.GetComponentsInChildren<RoadPlatform>(true))
            {
                if (AssignField(road, "pickerSettings", settings))
                    wired = true;
            }

            if (wired)
                PrefabUtility.SaveAsPrefabAsset(contents, RoadPlatformPath);
            else
                Debug.LogWarning($"[PickerSettingsSetup] {RoadPlatformPath} icinde RoadPlatform bulunamadi.");

            return wired;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    //Alanlar private; Inspector'in kendi API'siyle yaziyoruz.
    private static bool AssignField(Object target, string fieldName, Object value)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(fieldName);
        if (property == null)
        {
            Debug.LogWarning($"[PickerSettingsSetup] {target.GetType().Name} icinde '{fieldName}' alani yok.");
            return false;
        }

        property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
        return true;
    }
}
