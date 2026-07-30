using System;
using UnityEngine;
using DG.Tweening;

public class PickerMovement : MonoBehaviour
{
    [SerializeField] private Rigidbody myRb;
    [SerializeField] private float horiztontalSpeed = 10f;
    [SerializeField] private float verticalSpeed = 10f;
    [SerializeField] private float dragSensitivity = 3f;
    //Yana hareketin ust hizi (birim/sn). Bu sinir olmadan oyuncu seridi bir
    //karede kat ediyor ve toplarin nereye yayildigi hic onemli olmuyor.
    //Oyunun zorlugunu belirleyen asil ayar bu.
    //DIKKAT: RoadPlatform'daki pickerLateralSpeed ile ayni olmali.
    [SerializeField] private float maxLateralSpeed = 9f;
    public static event Action movedToNextStartEvent;

    private bool canMove = false;
    private bool canRun = false;
    private float keyboardInput;   // -1 / 0 / +1  (yön)
    private float dragDelta;       // birikmiş sürükleme (mesafe)
    private Vector3 mousePosition;

    private void Update()
    {
        if (!canMove)
            return;

        keyboardInput = Input.GetAxisRaw("Horizontal");

        if (Input.GetMouseButtonDown(0))
        {
            mousePosition = Input.mousePosition;
        }
        else if (Input.GetMouseButton(0))
        {
            float pixelDelta = Input.mousePosition.x - mousePosition.x;
            dragDelta += (pixelDelta / Screen.width) * dragSensitivity;   // ÜZERİNE YAZMA, BİRİKTİR
            mousePosition = Input.mousePosition;
        }
    }

    private void FixedUpdate()
    {
        if (!canMove)
            return;

        // Klavye: yön → mesafeye çevir (zamanla çarp)
        float horizontalMove = keyboardInput * horiztontalSpeed * Time.fixedDeltaTime;

        // Sürükleme: zaten mesafe, olduğu gibi ekle
        horizontalMove += dragDelta;
        dragDelta = 0f;              // TÜKETTİK — sıfırla

        // Yana hareketi hız sınırına kırp: şeridi anlık kat etmek mümkün olmasın
        float maxStep = maxLateralSpeed * Time.fixedDeltaTime;
        horizontalMove = Mathf.Clamp(horizontalMove, -maxStep, maxStep);

        float verticalMove = canRun ? verticalSpeed * Time.fixedDeltaTime : 0f;

        Vector3 pos = myRb.position;     // transform.position DEĞİL
        myRb.MovePosition(new Vector3(
            Mathf.Clamp(pos.x + horizontalMove, -1.5f, 1.5f),
            pos.y,
            pos.z + verticalMove));
    }
    private void EnableMovement()
    {
        canMove = true;
        canRun = true;
    }
    private void DisableMovement()
    {
        canMove = false;
        canRun = false;
    }
    private void DisableVerticalMovement()
    {
        canRun = false;
    }
    private void EnableVerticalMovement()
    {
        canRun = true;
    }
    private void MoveToNextLevelStartPos()
    {
        DisableMovement();
        float curLevellength = LevelManager.Instance.GetCurrentLevelLength();
        Vector3 targetPos = new Vector3(0, transform.position.y, curLevellength-10f);
        transform.DOMove(targetPos, 2f).OnComplete(() =>
        {
            movedToNextStartEvent?.Invoke();
        });
    }
    private void OnEnable()
    {
        GameManager.gameStartedEvent += EnableMovement;
        GameManager.gameFinishedEvent += DisableMovement;
        PickerPhysicsCallbacks.hittedBallCollecterEvent += DisableVerticalMovement;
        PickerPhysicsCallbacks.hittedLevelEndEvent += MoveToNextLevelStartPos;
        BallCollecterPlatform.collecterSuccessEvent += EnableVerticalMovement;
    }
    private void OnDisable()
    {
        GameManager.gameStartedEvent -= EnableMovement;
        GameManager.gameFinishedEvent -= DisableMovement;
        PickerPhysicsCallbacks.hittedBallCollecterEvent -= DisableVerticalMovement;
        PickerPhysicsCallbacks.hittedLevelEndEvent -= MoveToNextLevelStartPos;
        BallCollecterPlatform.collecterSuccessEvent -= EnableVerticalMovement;
    }
}
