using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// "LEVEL COMPLETE" yazisini sahneye ekler ve UIManager'a baglar.
///
/// Fontu ve rengi mevcut level yazisindan kopyaliyor: boylece yeni yazi
/// projenin gorunumune uyuyor ve font referansini elle bulmak gerekmiyor.
/// Iki kez calistirilirsa yenisini uretmek yerine mevcudu guncelliyor.
/// </summary>
public static class LevelCompleteBannerSetup
{
    private const string ObjectName = "LevelCompleteText";
    private const string BannerMessage = "LEVEL COMPLETE";

    [MenuItem("Tools/Picker3D/Level Complete Yazisini Ekle")]
    public static void CreateBanner()
    {
        Scene scene = SceneManager.GetActiveScene();

        UIManager uiManager = Object.FindAnyObjectByType<UIManager>(FindObjectsInactive.Include);
        if (uiManager == null)
        {
            Debug.LogError($"[LevelCompleteBannerSetup] Acik sahnede ({scene.name}) UIManager bulunamadi.");
            return;
        }

        SerializedObject uiObject = new SerializedObject(uiManager);

        //Stili ornek alacagimiz yazi
        SerializedProperty levelTextProperty = uiObject.FindProperty("levelText");
        Text sample = levelTextProperty != null ? levelTextProperty.objectReferenceValue as Text : null;
        if (sample == null)
        {
            Debug.LogError("[LevelCompleteBannerSetup] UIManager'daki Level Text bagli degil, " +
                           "font ornek alinamadi.");
            return;
        }

        Transform parent = sample.transform.parent;
        Text banner = FindOrCreateBanner(parent);

        StyleBanner(banner, sample);

        //acilista kapali dursun; UIManager level bitince aciyor
        banner.gameObject.SetActive(false);

        SerializedProperty bannerProperty = uiObject.FindProperty("levelCompleteText");
        if (bannerProperty == null)
        {
            Debug.LogError("[LevelCompleteBannerSetup] UIManager'da 'levelCompleteText' alani yok.");
            return;
        }

        bannerProperty.objectReferenceValue = banner;
        uiObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(uiManager);
        EditorSceneManager.MarkSceneDirty(scene);

        Debug.Log($"[LevelCompleteBannerSetup] '{ObjectName}' hazir ve UIManager'a baglandi. " +
                  "Sahneyi kaydetmeyi unutma (Cmd+S).");
    }

    private static Text FindOrCreateBanner(Transform parent)
    {
        Transform existing = parent.Find(ObjectName);
        if (existing != null && existing.TryGetComponent(out Text existingText))
            return existingText;

        GameObject created = new GameObject(ObjectName, typeof(RectTransform));
        created.transform.SetParent(parent, false);
        Undo.RegisterCreatedObjectUndo(created, "Level Complete yazisi");

        return created.AddComponent<Text>();
    }

    private static void StyleBanner(Text banner, Text sample)
    {
        banner.text = BannerMessage;
        banner.font = sample.font;
        banner.fontStyle = FontStyle.Bold;
        banner.color = sample.color;
        banner.alignment = TextAnchor.MiddleCenter;
        banner.horizontalOverflow = HorizontalWrapMode.Overflow;
        banner.verticalOverflow = VerticalWrapMode.Overflow;

        //ornekten belirgin sekilde buyuk olsun ama kontrolsuz buyumesin
        banner.fontSize = Mathf.Clamp(Mathf.RoundToInt(sample.fontSize * 1.6f), 40, 160);

        //tiklamayi engellemesin
        banner.raycastTarget = false;

        RectTransform rect = banner.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, 260f); //ekranin biraz ustu
        rect.sizeDelta = new Vector2(1000f, 220f);
        rect.localScale = Vector3.one;
    }
}
