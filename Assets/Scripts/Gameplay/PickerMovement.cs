using System;
using UnityEngine;
using DG.Tweening;

public class PickerMovement : MonoBehaviour
{
    [SerializeField] private Rigidbody myRb;
    //Hiz ve olculer artik burada degil: RoadPlatform da ayni sayilara ihtiyac
    //duydugu icin ortak bir varlikta tutuluyor.
    [SerializeField] private PickerSettings settings;
    [SerializeField] private float horiztontalSpeed = 10f;
    [SerializeField] private float dragSensitivity = 3f;

    [Header("Engel carpismasi")]
    //Picker'in Rigidbody'si kinematic, yani hicbir collider onu durduramaz.
    //O yuzden ilerlemeden ONCE onunu tariyoruz ve engel varsa ileri hareketi
    //kesiyoruz. Deterministik kaliyor, fizige birakmadan.
    [SerializeField] private Vector3 blockCheckSize = new Vector3(1.9f, 1f, 0.4f);
    [SerializeField] private float blockCheckForwardOffset = 0.9f;

    private readonly Collider[] blockBuffer = new Collider[8];
    public static event Action movedToNextStartEvent;

    private bool canMove = false;
    private bool canRun = false;
    private float keyboardInput;   // -1 / 0 / +1  (yön)
    private float dragDelta;       // birikmiş sürükleme (mesafe)
    private Vector3 mousePosition;

    private void Awake()
    {
        if (settings == null)
        {
            Debug.LogError("[PickerMovement] PickerSettings atanmamis, varsayilanlar kullaniliyor.", this);
            settings = ScriptableObject.CreateInstance<PickerSettings>();
        }
    }

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
        float maxStep = settings.lateralSpeed * Time.fixedDeltaTime;
        horizontalMove = Mathf.Clamp(horizontalMove, -maxStep, maxStep);

        float verticalMove = canRun ? settings.forwardSpeed * Time.fixedDeltaTime : 0f;

        // Engel varsa ileri gitme; oyuncu yana kayarak bosluga gecmek zorunda
        if (verticalMove > 0f && IsObstacleAhead(myRb.position, horizontalMove))
            verticalMove = 0f;

        Vector3 pos = myRb.position;     // transform.position DEĞİL
        myRb.MovePosition(new Vector3(
            Mathf.Clamp(pos.x + horizontalMove, -settings.laneLimit, settings.laneLimit),
            pos.y,
            pos.z + verticalMove));
    }
    //Picker'in hemen onunde engel var mi? OverlapBoxNonAlloc kullaniyoruz:
    //onceden ayrilmis tampon sayesinde her karede cop uretmiyor.
    private bool IsObstacleAhead(Vector3 position, float horizontalMove)
    {
        //yana kaymayi da hesaba katiyoruz, yoksa bosluga girerken kendini bloke ediyor
        Vector3 center = position + new Vector3(horizontalMove, 0f, blockCheckForwardOffset);

        int count = Physics.OverlapBoxNonAlloc(center, blockCheckSize * 0.5f, blockBuffer,
            Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            if (blockBuffer[i] == null)
                continue;
            if (blockBuffer[i].GetComponentInParent<Obstacle>() != null)
                return true;
        }
        return false;
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

        //Leveller artik 0'dan baslamiyor; hedefi LevelManager veriyor.
        Vector3 targetPos = new Vector3(0f, transform.position.y,
            LevelManager.Instance.GetNextLevelStartZ());

        transform.DOMove(targetPos, 2f).OnComplete(OnArrivedAtNextLevel);
    }

    private void OnArrivedAtNextLevel()
    {
        //DOTween transform'u tasidi ama Rigidbody'nin kendi pozu ayri tutuluyor.
        //Hizalamazsak fizik bir sonraki adimda picker'i eski yerine geri cekiyor.
        myRb.position = transform.position;
        myRb.rotation = transform.rotation;
        Physics.SyncTransforms();

        movedToNextStartEvent?.Invoke();
    }
    private void OnEnable()
    {
        GameManager.gameStartedEvent += EnableMovement;
        GameManager.gameFinishedEvent += DisableMovement;
        //level bitti ama oyun devam ediyor: yeni levelde kosmaya devam
        GameManager.levelCompletedEvent += EnableMovement;
        PickerPhysicsCallbacks.hittedBallCollecterEvent += DisableVerticalMovement;
        PickerPhysicsCallbacks.hittedLevelEndEvent += MoveToNextLevelStartPos;
        BallCollecterPlatform.collecterSuccessEvent += EnableVerticalMovement;
    }
    private void OnDisable()
    {
        GameManager.gameStartedEvent -= EnableMovement;
        GameManager.gameFinishedEvent -= DisableMovement;
        GameManager.levelCompletedEvent -= EnableMovement;
        PickerPhysicsCallbacks.hittedBallCollecterEvent -= DisableVerticalMovement;
        PickerPhysicsCallbacks.hittedLevelEndEvent -= MoveToNextLevelStartPos;
        BallCollecterPlatform.collecterSuccessEvent -= EnableVerticalMovement;
    }
}
