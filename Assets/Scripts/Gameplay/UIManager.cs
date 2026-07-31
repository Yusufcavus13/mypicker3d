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
    [SerializeField] private GameManager gameManager;

    //Sahne basarisizlikta yeniden yukleniyor. Ana menu her yuklemede degil,
    //sadece uygulama ilk acildiginda cikmali - static alan sahne yuklemeleri
    //arasinda yasadigi icin bayrak gorevi goruyor.
    private static bool mainMenuShown;

    private Tween levelTextTween;

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

    //Level bitti ama oyun devam ediyor: menu ACMIYORUZ. Kutlama, yazinin
    //kisa bir animasyonu ve kameranin sarsintisiyla veriliyor.
    private void OnLevelCompleted()
    {
        UpdateLevelText();

        if (levelText == null)
            return;

        levelTextTween?.Kill(true);
        levelTextTween = levelText.transform
            .DOPunchScale(Vector3.one * 0.45f, 0.55f, 5, 0.6f);
    }

    private void OpenFailUI()
    {
        gameEndMenu.SetActive(true);
        winMenu.SetActive(false);
        failMenu.SetActive(true);
        HideFailStatusImage();
        StartCoroutine(KeepFailStatusImageHidden());
    }

    private void HideFailStatusImage()
    {
        if (failMenu == null)
            return;
        var status = failMenu.transform.Find("StatusImg");
        if (status != null)
            status.gameObject.SetActive(false);
    }

    private IEnumerator KeepFailStatusImageHidden()
    {
        for (var i = 0; i < 90; i++)
        {
            HideFailStatusImage();
            yield return null;  // animasyonun aktifleştirdiği statusImg bu kod ile kapatılıyor.
        }
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
