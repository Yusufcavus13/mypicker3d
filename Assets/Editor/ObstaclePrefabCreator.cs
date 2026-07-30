using UnityEditor;
using UnityEngine;

/// <summary>
/// Engel prefabini primitiflerden kurar ve Obstacle bileseninin referanslarini
/// otomatik baglar. Elle kurup collider/bilesen unutma riskini ortadan kaldiriyor.
///
/// Yapisi:
///   Obstacle          -> Obstacle + BoxCollider (gorunum yok, sadece carpisma)
///     Base            -> silindir taban, sabit
///     RotatingBody    -> kare tabanli govde, Y ekseninde doner
///       Blade L / R   -> yanlarda iki kanat, donusu gorunur kilar
///       Cap           -> tepe silindiri
/// </summary>
public static class ObstaclePrefabCreator
{
    private const string PrefabPath = "Assets/Prefabs/Obstacle.prefab";
    private const string BodyMaterialPath = "Assets/Materials/ObstacleMaterial.mat";
    private const string TrimMaterialPath = "Assets/Materials/ObstacleTrimMaterial.mat";

    [MenuItem("Tools/Picker3D/Engel Prefabi Olustur")]
    public static void CreateObstaclePrefab()
    {
        Material bodyMaterial = GetOrCreateMaterial(BodyMaterialPath, new Color(0.86f, 0.16f, 0.22f));
        Material trimMaterial = GetOrCreateMaterial(TrimMaterialPath, new Color(0.16f, 0.17f, 0.22f));

        GameObject root = new GameObject("Obstacle");
        try
        {
            BoxCollider blockCollider = root.AddComponent<BoxCollider>();
            blockCollider.isTrigger = false;

            //sabit taban
            GameObject baseBody = CreatePart(PrimitiveType.Cylinder, "Base", root.transform, trimMaterial);

            //donen govde
            GameObject rotating = CreatePart(PrimitiveType.Cube, "RotatingBody", root.transform, bodyMaterial);

            //Kanatlar donusun gozle gorulmesini sagliyor. KURAL: donen govdenin
            //uzerindeki hicbir parcanin yerel kose yaricapi 0.707'yi asamaz
            //(kupun kendi kose yaricapi). Asarsa donerken gecis bosluguna tasar
            //ve uretimdeki adalet hesabi bozulur.
            //Bu kanatlar: hypot(0.5 + 0.2/2, 0.7/2) = 0.695  -> sinirin altinda
            GameObject leftBlade = CreatePart(PrimitiveType.Cube, "BladeL", rotating.transform, trimMaterial);
            leftBlade.transform.localPosition = new Vector3(-0.5f, 0f, 0f);
            leftBlade.transform.localScale = new Vector3(0.2f, 0.42f, 0.7f);

            GameObject rightBlade = CreatePart(PrimitiveType.Cube, "BladeR", rotating.transform, trimMaterial);
            rightBlade.transform.localPosition = new Vector3(0.5f, 0f, 0f);
            rightBlade.transform.localScale = new Vector3(0.2f, 0.42f, 0.7f);

            //tepe halkasi
            GameObject cap = CreatePart(PrimitiveType.Cylinder, "Cap", rotating.transform, trimMaterial);
            cap.transform.localPosition = new Vector3(0f, 0.54f, 0f);
            cap.transform.localScale = new Vector3(1.15f, 0.08f, 1.15f);

            //kanatlarin ve tepenin collider'i olmasin: carpisma root'taki kutudan
            foreach (Collider childCollider in root.GetComponentsInChildren<Collider>(true))
            {
                if (childCollider != blockCollider)
                    Object.DestroyImmediate(childCollider);
            }

            Obstacle obstacle = root.AddComponent<Obstacle>();
            SerializedObject obstacleObject = new SerializedObject(obstacle);
            obstacleObject.FindProperty("rotatingBody").objectReferenceValue = rotating.transform;
            obstacleObject.FindProperty("baseBody").objectReferenceValue = baseBody.transform;
            obstacleObject.FindProperty("blockCollider").objectReferenceValue = blockCollider;
            obstacleObject.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[ObstaclePrefabCreator] {PrefabPath} olusturuldu. " +
                      "RoadPlatform prefabindaki Obstacle Prefab alanina surukle.");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static GameObject CreatePart(PrimitiveType type, string name, Transform parent, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.SetParent(parent, false);

        if (part.TryGetComponent(out MeshRenderer meshRenderer))
            meshRenderer.sharedMaterial = material;

        return part;
    }

    private static Material GetOrCreateMaterial(string path, Color color)
    {
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
            return existing;

        //projedeki shader'i ornek aliyoruz ki gorunum diger objelerle tutarli olsun
        Material sample = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BallMaterial.mat");
        Shader shader = sample != null ? sample.shader : Shader.Find("Universal Render Pipeline/Lit");

        Material material = new Material(shader);
        material.SetColor("_AlbedoColor", color);
        material.SetColor("_BaseColor", color);
        material.SetColor("_Color", color);

        AssetDatabase.CreateAsset(material, path);
        return material;
    }
}
