using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [SerializeField]  Transform targetTransform;
    [SerializeField] private float followSpeed = 2f;

    [Header("Sarsinti")]
    [SerializeField] private float impactShakeStrength = 0.18f;
    [SerializeField] private float impactShakeDuration = 0.25f;
    [SerializeField] private float levelCompleteShakeStrength = 0.1f;
    [SerializeField] private float levelCompleteShakeDuration = 0.4f;

    private float zOffset = 0f;
    private float targetZPos = 0f;
    private bool isOffsetCalculated = false;
    private bool canFollow = false;

    //Sarsintiyi transform'un uzerine ekleyemeyiz: her karede pozisyonu tekrar
    //okuyup yazdigimiz icin sapma birikir ve kamera kayar. Temel konumu ayri
    //tutup her karede "temel + sarsinti" yaziyoruz.
    private float baseX;
    private float baseY;
    private Vector3 shakeOffset;
    private float shakeTimer;
    private float shakeDuration;
    private float shakeStrength;

    private void Awake()
    {
        baseX = transform.position.x;
        baseY = transform.position.y;
        targetZPos = transform.position.z;
    }

    private void LateUpdate()
    {
        UpdateShake();

        if (canFollow)
        {
            targetZPos = Mathf.Lerp(targetZPos, targetTransform.position.z + zOffset,
                Time.deltaTime * followSpeed);
        }

        transform.position = new Vector3(
            baseX + shakeOffset.x,
            baseY + shakeOffset.y,
            targetZPos + shakeOffset.z);
    }

    public void Shake(float strength, float duration)
    {
        //devam eden daha guclu bir sarsintiyi zayifi ezmesin
        if (shakeTimer > 0f && strength < shakeStrength)
            return;

        shakeStrength = strength;
        shakeDuration = Mathf.Max(0.01f, duration);
        shakeTimer = shakeDuration;
    }

    private void UpdateShake()
    {
        if (shakeTimer <= 0f)
        {
            shakeOffset = Vector3.zero;
            return;
        }

        //unscaled: agir cekimde sarsinti da agirlasmasin, ikisi birbirini tamamlasin
        shakeTimer -= Time.unscaledDeltaTime;

        float fade = Mathf.Clamp01(shakeTimer / shakeDuration);
        shakeOffset = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f),
            0f) * (shakeStrength * fade);
    }

    private void ShakeOnImpact()
    {
        Shake(impactShakeStrength, impactShakeDuration);
    }

    private void ShakeOnLevelComplete()
    {
        Shake(levelCompleteShakeStrength, levelCompleteShakeDuration);
    }

    private void CalculateZOffset()
    {
        zOffset = transform.position.z - targetTransform.position.z;
        targetZPos = transform.position.z;
    }
    private void StartFollowing()
    {
        if (!isOffsetCalculated)
        {
            CalculateZOffset();
            isOffsetCalculated = true;
        }
        canFollow = true;
    }
    private void StopFollowing()
    {
        canFollow = false;
    }
    private void OnEnable()
    {
        GameManager.gameStartedEvent += StartFollowing;
        GameManager.gameFinishedEvent += StopFollowing;
        GameManager.levelCompletedEvent += ShakeOnLevelComplete;
        PickerPhysicsCallbacks.hittedBallCollecterEvent += ShakeOnImpact;
    }
    private void OnDisable()
    {
        GameManager.gameStartedEvent -= StartFollowing;
        GameManager.gameFinishedEvent -= StopFollowing;
        GameManager.levelCompletedEvent -= ShakeOnLevelComplete;
        PickerPhysicsCallbacks.hittedBallCollecterEvent -= ShakeOnImpact;
    }
}
