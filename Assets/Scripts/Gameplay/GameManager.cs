//baba
using UnityEngine;
using System;

public class GameManager : MonoBehaviour
{
    public static event Action gameStartedEvent;

    //Oyunu DURDURAN olay. Artik sadece basarisizlikta ateslenir; level
    //tamamlaninca oyun devam ettigi icin burada isi yok.
    public static event Action gameFinishedEvent;

    //Level bitti ama oyun devam ediyor: LevelManager siradakini kurar,
    //picker yoluna devam eder.
    public static event Action levelCompletedEvent;

    public static event Action gameFailedEvent;

    public void StartGame()
    {
        gameStartedEvent?.Invoke();
    }
    public void LevelCompleted()
    {
        levelCompletedEvent?.Invoke();
    }
    public void GameFailed()
    {
        gameFailedEvent?.Invoke();
        gameFinishedEvent?.Invoke();
    }
    private void OnEnable()
    {
        BallCollecterPlatform.collecterFailedEvent += GameFailed;
        PickerMovement.movedToNextStartEvent += LevelCompleted;
    }
    private void OnDisable()
    {
        BallCollecterPlatform.collecterFailedEvent -= GameFailed;
        PickerMovement.movedToNextStartEvent -= LevelCompleted;
    }
}
