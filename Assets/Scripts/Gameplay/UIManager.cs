using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject gameMenu;
    [SerializeField] private GameObject gameEndMenu;
    [SerializeField] private GameObject winMenu;
    [SerializeField] private GameObject failMenu;
    [SerializeField] private Text levelText;
    [SerializeField] private GameManager gameManager;

    private void Start()
    {
        mainMenu.SetActive(false);
        gameEndMenu.SetActive(false);
        winMenu.SetActive(false);
        failMenu.SetActive(false);
        gameMenu.SetActive(true);
        UpdateLevelText();
        StartCoroutine(BeginGameNextFrame());
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
    private void OpenWinUI()
    {
        gameEndMenu.SetActive(true);
        winMenu.SetActive(true);
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
        GameManager.gameSuccessedEvent += OpenWinUI;
    }
    private void OnDisable()
    {
        GameManager.gameStartedEvent -= OpenGameUI;
        GameManager.gameFinishedEvent -= CloseGameUI;
        GameManager.gameFailedEvent -= OpenFailUI;
        GameManager.gameSuccessedEvent -= OpenWinUI;
    }
}
