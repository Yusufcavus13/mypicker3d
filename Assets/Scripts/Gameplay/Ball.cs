using UnityEngine;

public class Ball : MonoBehaviour
{
    [SerializeField] private GameObject explosionEfectPrefab;
    [SerializeField] private Rigidbody myRb;
    [SerializeField] private float forcePower = 7f;
    [SerializeField] private float upwardRatio = 3f; //ileri bilesene gore yukari bilesen
    private bool isInside = false;
    private int value = 1;

    public int Value
    {
        get { return value; }
    }

    public void SetValue(int newValue)
    {
        value = Mathf.Max(1, newValue);
    }

    public void SetStatus(bool _isInside)
    {
        isInside = _isInside;
    }
    
    public void Explode(Material platformMat)
    {
        //Efekt artik havuzdan geliyor: bir toplayici bosalinca 15 tane
        //Instantiate + 15 Destroy yerine sifir tahsis.
        ExplosionEffectPool.Instance.Play(explosionEfectPrefab, transform.position, platformMat);

        SoundManager.Instance?.PlayPop();

        Destroy(gameObject);
    }
    private void CheckIsInside()
    {
        if (isInside)
        {
            ThrowForward();
        }
    }
    private void ThrowForward()
    {
        //yukari-ileri kavis: toplayicinin kenarina takilmadan icine dussun
        Vector3 forceDirection = (Vector3.up * upwardRatio + Vector3.forward).normalized;

        myRb.linearVelocity = Vector3.zero;
        myRb.angularVelocity = Vector3.zero;
        myRb.AddForce(forceDirection * forcePower, ForceMode.Impulse);
    }

    private void OnEnable()
    {
        //Havuz level kurulurken hazirlansin: ilk patlama anina denk gelmesin.
        //EnsurePool ilkinden sonra hemen donuyor, tekrar maliyeti yok.
        if (explosionEfectPrefab != null && Application.isPlaying)
            ExplosionEffectPool.Instance.Prepare(explosionEfectPrefab);

        PickerPhysicsCallbacks.hittedBallCollecterEvent += CheckIsInside;
    }
    private void OnDisable()
    {
        PickerPhysicsCallbacks.hittedBallCollecterEvent -= CheckIsInside;
    }
}
