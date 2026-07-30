using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class LevelManager : MonoBehaviour
{
    [SerializeField] private GameObject levelSkeletonPrefab;
    [SerializeField] private LevelData[] levels;

    private LevelData currentLevelData;
    private LevelData nextLevelData;
    private GameObject currentLevelObject;
    private GameObject nextLevelObject;
    private int currentLevelIndex;
    private int nextLevelIndex;

    //Leveller artik hep 0'dan baslamiyor: kesintisiz gecisde her level bir
    //oncekinin bittigi yerde kuruluyor, o yuzden mevcut levelin baslangicini
    //akilda tutmak zorundayiz.
    private float currentLevelOriginZ;

    public static event Action levelLoadedEvent;
    public static LevelManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        LoadCurrentLevel();
    }
    private void LoadCurrentLevel()
    {
        if (levelSkeletonPrefab == null || levels == null || levels.Length == 0)
        {
            Debug.LogError("[LevelManager] Level iskeleti ya da level listesi bos.", this);
            return;
        }

        for (int i = 0; i < levels.Length; i++)
        {
            if (levels[i] == null)
                Debug.LogError($"[LevelManager] Levels dizisinde {i}. sira BOS. Inspector'dan LevelData ata.", this);
        }

        ResolveIndices(out currentLevelIndex, out nextLevelIndex);
        currentLevelData = levels[currentLevelIndex];
        nextLevelData = levels[nextLevelIndex];

        currentLevelOriginZ = transform.position.z;
        currentLevelObject = BuildLevel(currentLevelData, currentLevelOriginZ, currentLevelIndex);
        nextLevelObject = BuildLevel(nextLevelData, GetNextLevelOriginZ(), nextLevelIndex);

        levelLoadedEvent?.Invoke();
    }

    //Hangi level oynaniyor, hangisi sirada: tasarlanan leveller sirayla,
    //hepsi bitince rastgele devam eder.
    private void ResolveIndices(out int current, out int next)
    {
        int completedLevel = PlayerPrefs.GetInt("Level", 0);

        if (completedLevel < levels.Length)
        {
            current = completedLevel;
            next = completedLevel + 1 < levels.Length
                ? completedLevel + 1
                : GetSavedIndex("NextLevelIndex", current);
        }
        else
        {
            current = GetSavedIndex("CurrentLevelIndex", -1);
            next = GetSavedIndex("NextLevelIndex", current);
        }
    }

    private GameObject BuildLevel(LevelData levelData, float originZ, int levelIndex)
    {
        if (levelData == null)
        {
            Debug.LogError($"[LevelManager] Levels dizisinin {levelIndex}. sirasi bos, level kurulamadi.", this);
            return null;
        }

        Vector3 position = new Vector3(transform.position.x, transform.position.y, originZ);
        GameObject levelObj = Instantiate(levelSkeletonPrefab, position, Quaternion.identity, transform);
        levelObj.name = levelData.name;

        if (!levelObj.TryGetComponent(out LevelBuilder levelBuilder))
        {
            Debug.LogError($"[LevelManager] {levelSkeletonPrefab.name} uzerinde LevelBuilder yok.", this);
            return levelObj;
        }

        levelBuilder.Build(levelData);
        return levelObj;
    }

    //Level bitti: sahneyi yeniden yuklemeden siradakine geciyoruz.
    private void OnLevelCompleted()
    {
        IncreaseLevel();
        AdvanceToNextLevel();
    }

    private void AdvanceToNextLevel()
    {
        //DIKKAT: yeni baslangic ESKI levelin uzunluguna gore hesaplanir,
        //o yuzden currentLevelData'yi degistirmeden once aliyoruz.
        float newOriginZ = GetNextLevelOriginZ();

        if (currentLevelObject != null)
            Destroy(currentLevelObject);

        currentLevelOriginZ = newOriginZ;
        currentLevelData = nextLevelData;
        currentLevelObject = nextLevelObject;
        currentLevelIndex = nextLevelIndex;

        ResolveIndices(out _, out nextLevelIndex);
        nextLevelData = levels[nextLevelIndex];
        nextLevelObject = BuildLevel(nextLevelData, GetNextLevelOriginZ(), nextLevelIndex);

        levelLoadedEvent?.Invoke();
    }

    public float GetCurrentLevelLength()
    {
        return currentLevelData != null ? currentLevelData.GetLevelLength() + 20f : 0f;
    }

    //Siradaki levelin baslangic noktasi (dunya z)
    public float GetNextLevelOriginZ()
    {
        return currentLevelOriginZ + GetCurrentLevelLength();
    }

    //Picker'in level sonunda gidecegi nokta: siradaki levelin baslangic yolu
    public float GetNextLevelStartZ()
    {
        return GetNextLevelOriginZ() - 10f;
    }

    public void ReloadLevel()
    {
        SceneManager.LoadScene(0);
    }
    //rastgele secilen index bir kere secilip kaydedilir: level yeniden
    //yuklenince (fail/retry) oyuncunun karsisina ayni level cikar
    private int GetSavedIndex(string key, int avoidIndex)
    {
        if (PlayerPrefs.HasKey(key))
            return Mathf.Clamp(PlayerPrefs.GetInt(key), 0, levels.Length - 1);

        int index = PickRandomLevelIndex(avoidIndex);
        PlayerPrefs.SetInt(key, index);
        return index;
    }
    private int PickRandomLevelIndex(int avoidIndex)
    {
        if (levels.Length <= 1)
            return 0;

        int index = avoidIndex;
        while (index == avoidIndex) //ayni level ust uste gelmesin
            index = UnityEngine.Random.Range(0, levels.Length);
        return index;
    }
    private void IncreaseLevel()
    {
        PlayerPrefs.SetInt("Level", PlayerPrefs.GetInt("Level", 0) + 1);

        //ileride duran level artik oynanacak level oldu; sonrakini bir sonraki
        //yuklemede yeniden secmek icin kaydi temizliyoruz
        if (!PlayerPrefs.HasKey("NextLevelIndex"))
            return;

        PlayerPrefs.SetInt("CurrentLevelIndex", PlayerPrefs.GetInt("NextLevelIndex"));
        PlayerPrefs.DeleteKey("NextLevelIndex");
    }
    private void OnEnable()
    {
        GameManager.levelCompletedEvent += OnLevelCompleted;
    }
    private void OnDisable()
    {
        GameManager.levelCompletedEvent -= OnLevelCompleted;
    }
}
