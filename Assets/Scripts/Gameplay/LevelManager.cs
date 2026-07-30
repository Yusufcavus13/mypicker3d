using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class LevelManager : MonoBehaviour
{
    [SerializeField] private GameObject levelSkeletonPrefab;
    [SerializeField] private LevelData[] levels;

    private LevelData currentLevelData;

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

        //tasarlanan leveller sirayla oynanir, hepsi bitince rastgele devam eder
        int completedLevel = PlayerPrefs.GetInt("Level", 0);
        int currentLevelIndex;
        int nextLevelIndex;

        if (completedLevel < levels.Length)
        {
            currentLevelIndex = completedLevel;
            nextLevelIndex = completedLevel + 1 < levels.Length
                ? completedLevel + 1
                : GetSavedIndex("NextLevelIndex", currentLevelIndex); //son level: sonraki artik rastgele
        }
        else
        {
            currentLevelIndex = GetSavedIndex("CurrentLevelIndex", -1);
            nextLevelIndex = GetSavedIndex("NextLevelIndex", currentLevelIndex);
        }

        for (int i = 0; i < levels.Length; i++)
        {
            if (levels[i] == null)
                Debug.LogError($"[LevelManager] Levels dizisinde {i}. sira BOS. Inspector'dan LevelData ata.", this);
        }

        currentLevelData = levels[currentLevelIndex];
        BuildLevel(currentLevelData, transform.position, currentLevelIndex);

        Vector3 nextLevelSpawnPos = new Vector3(transform.position.x, transform.position.y,
            transform.position.z + GetCurrentLevelLength());
        BuildLevel(levels[nextLevelIndex], nextLevelSpawnPos, nextLevelIndex);

        levelLoadedEvent?.Invoke();
    }
    private void BuildLevel(LevelData levelData, Vector3 position, int levelIndex)
    {
        if (levelData == null)
        {
            Debug.LogError($"[LevelManager] Levels dizisinin {levelIndex}. sirasi bos, level kurulamadi.", this);
            return;
        }

        GameObject levelObj = Instantiate(levelSkeletonPrefab, position, Quaternion.identity, transform);
        levelObj.name = levelData.name;

        if (!levelObj.TryGetComponent(out LevelBuilder levelBuilder))
        {
            Debug.LogError($"[LevelManager] {levelSkeletonPrefab.name} uzerinde LevelBuilder yok.", this);
            return;
        }

        levelBuilder.Build(levelData);
    }
    public float GetCurrentLevelLength()
    {
        return currentLevelData != null ? currentLevelData.GetLevelLength() + 20f : 0f;
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
        GameManager.gameSuccessedEvent += IncreaseLevel;
    }
    private void OnDisable()
    {
        GameManager.gameSuccessedEvent -= IncreaseLevel;
    }
}
