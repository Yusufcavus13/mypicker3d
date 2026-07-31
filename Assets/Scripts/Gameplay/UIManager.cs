using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject gameMenu;
    [SerializeField] private GameObject gameEndMenu;
    [SerializeField] private GameObject winMenu;
    [SerializeField] private GameObject failMenu;
    [SerializeField] private Text levelText;
    [SerializeField] private Text levelCompleteText;
    [SerializeField] private float levelCompleteHoldTime = 1.1f;
    [SerializeField] private GameManager gameManager;

    //Sahne basarisizlikta yeniden yukleniyor. Ana menu her yuklemede degil,
    //sadece uygulama ilk acildiginda cikmali - static alan sahne yuklemeleri
    //arasinda yasadigi icin bayrak gorevi goruyor.
    private static bool mainMenuShown;

    private Tween levelTextTween;
    private Tween bannerTween;
    private Coroutine levelCompleteRoutine;

    private void Start()
    {
        gameEndMenu.SetActive(false);
        winMenu.SetActive(false);
        failMenu.SetActive(false);

        UpdateLevelText();

        if (mainMenuShown)
        {
            mainMenu.SetActive(false);
            gameMenu.SetActive(true);
            StartCoroutine(BeginGameNextFrame());
            return;
        }

        mainMenuShown = true;
        ShowMainMenu();
    }

    //Sahnede MainMenu'nun cocuklari da kapali kaydedilmis; sadece parent'i
    //acmak yetmiyor, hepsini aciyoruz.
    private void ShowMainMenu()
    {
        gameMenu.SetActive(false);
        mainMenu.SetActive(true);

        foreach (Transform child in mainMenu.transform)
            child.gameObject.SetActive(true);
    }

    private IEnumerator BeginGameNextFrame()
    {
        yield return null;
        if (gameManager == null)
            gameManager = FindAnyObjectByType<GameManager>();
        gameManager?.StartGame();
    }

    private void OpenGameUI()
    {
        UpdateLevelText();
        mainMenu.SetActive(false);
        gameMenu.SetActive(true);
    }
    private void CloseGameUI()
    {
        gameMenu.SetActive(false);
        gameEndMenu.SetActive(true);
    }

    //Level bitti ama oyun devam ediyor: menu ACMIYORUZ. Kutlama, kisa sureli
    //bir yazi ve kameranin sarsintisiyla veriliyor.
    private void OnLevelCompleted()
    {
        UpdateLevelText();

        if (levelText != null)
        {
            levelTextTween?.Kill(true);
            levelTextTween = levelText.transform
                .DOPunchScale(Vector3.one * 0.45f, 0.55f, 5, 0.6f);
        }

        ShowLevelCompleteBanner();
    }

    private void ShowLevelCompleteBanner()
    {
        if (levelCompleteText == null)
            return;

        //ust uste level bitirilirse onceki gosterim yarida kalsin
        if (levelCompleteRoutine != null)
            StopCoroutine(levelCompleteRoutine);

        levelCompleteRoutine = StartCoroutine(LevelCompleteBannerRoutine());
    }

    private IEnumerator LevelCompleteBannerRoutine()
    {
        Transform banner = levelCompleteText.transform;

        levelCompleteText.gameObject.SetActive(true);
        banner.localScale = Vector3.zero;

        bannerTween?.Kill(true);
        //OutBack: hedefi bir miktar asip geri oturuyor, "zipladi" hissi veriyor
        bannerTween = banner.DOScale(1f, 0.35f).SetEase(Ease.OutBack).SetUpdate(true);

        //agir cekim sirasinda bekleme uzamasin
        yield return new WaitForSecondsRealtime(levelCompleteHoldTime);

        bannerTween?.Kill(true);
        bannerTween = banner.DOScale(0f, 0.22f).SetEase(Ease.InBack).SetUpdate(true)
            .OnComplete(() => levelCompleteText.gameObject.SetActive(false));

        levelCompleteRoutine = null;
    }

    private void OpenFailUI()
    {
        gameEndMenu.SetActive(true);
        winMenu.SetActive(false);
        failMenu.SetActive(true);
    }
    private void UpdateLevelText()
    {
        levelText.text = "Level " + (PlayerPrefs.GetInt("Level",0)+1).ToString();
    }
    private void OnEnable()
    {
        GameManager.gameStartedEvent += OpenGameUI;
        GameManager.gameFinishedEvent += CloseGameUI;
        GameManager.gameFailedEvent += OpenFailUI;
        GameManager.levelCompletedEvent += OnLevelCompleted;
    }
    private void OnDisable()
    {
        GameManager.gameStartedEvent -= OpenGameUI;
        GameManager.gameFinishedEvent -= CloseGameUI;
        GameManager.gameFailedEvent -= OpenFailUI;
        GameManager.levelCompletedEvent -= OnLevelCompleted;
    }
}
