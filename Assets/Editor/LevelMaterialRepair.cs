using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Magenta (pembe) level tamiri.
///
/// Eski kod stage renklerini "new Material(...)" ile veriyordu. Bu materyaller
/// diske kaydedilmeyen kopyalar oldugu icin, level prefab'a uygulandiginda
/// renderer'in materyali BOS (None) kaliyor ve Unity onu magenta cizyor.
/// Bu arac hem bos kalan materyalleri kaynak prefab'tan geri getirir, hem de
/// sahnede duran "kopya materyal" override'larini temizler; boylece bir dahaki
/// "Apply To Prefab" sorunu tekrar geri getirmez.
/// </summary>
public static class LevelMaterialRepair
{
    private const string LevelsFolder = "Assets/Prefabs/Levels";

    [MenuItem("Tools/Picker3D/Fix Magenta Materials")]
    public static void FixAll()
    {
        int sceneFixes = FixOpenScenes();
        int prefabFixes = FixLevelPrefabs();

        Debug.Log($"[LevelMaterialRepair] Tamamlandi. Sahnede {sceneFixes}, prefab'larda {prefabFixes} renderer duzeltildi." +
                  (sceneFixes > 0 ? " Sahneyi kaydetmeyi unutma (Ctrl/Cmd+S)." : ""));
    }

    private static int FixOpenScenes()
    {
        int fixedCount = 0;

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded)
                continue;

            int before = fixedCount;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                fixedCount += RepairRenderers(root);
            }

            if (fixedCount > before)
                EditorSceneManager.MarkSceneDirty(scene);
        }

        return fixedCount;
    }

    private static int FixLevelPrefabs()
    {
        int fixedCount = 0;
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { LevelsFolder });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                int repaired = RepairRenderers(contents);
                if (repaired > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(contents, path);
                    fixedCount += repaired;
                    Debug.Log($"[LevelMaterialRepair] {path}: {repaired} renderer duzeltildi.");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        if (fixedCount > 0)
            AssetDatabase.SaveAssets();

        return fixedCount;
    }

    private static int RepairRenderers(GameObject root)
    {
        int repaired = 0;

        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (!NeedsRepair(renderer))
                continue;

            //once prefab override'ini geri al: dogru materyal kaynak prefab'ta duruyor
            if (PrefabUtility.IsPartOfPrefabInstance(renderer))
            {
                SerializedObject so = new SerializedObject(renderer);
                SerializedProperty materialsProp = so.FindProperty("m_Materials");
                if (materialsProp != null)
                    PrefabUtility.RevertPropertyOverride(materialsProp, InteractionMode.AutomatedAction);
            }

            //override yoksa (ya da hala bossa) kaynak prefab'tan kopyala
            if (NeedsRepair(renderer))
            {
                Renderer source = PrefabUtility.GetCorrespondingObjectFromSource(renderer);
                if (source == null || NeedsRepair(source))
                {
                    Debug.LogWarning($"[LevelMaterialRepair] {GetPath(renderer.transform)} icin materyal bulunamadi, elle atanmali.", renderer);
                    continue;
                }

                renderer.sharedMaterials = source.sharedMaterials;
            }

            EditorUtility.SetDirty(renderer);
            repaired++;
        }

        return repaired;
    }

    private static bool NeedsRepair(Renderer renderer)
    {
        Material[] materials = renderer.sharedMaterials;
        if (materials == null || materials.Length == 0)
            return true;

        foreach (Material material in materials)
        {
            //null  -> magenta cizilir
            //asset olmayan -> sahneye gomulu kopya, prefab'a uygulaninca null olur
            if (material == null || !EditorUtility.IsPersistent(material))
                return true;
        }

        return false;
    }

    private static string GetPath(Transform transform)
    {
        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }
        return path;
    }
}
