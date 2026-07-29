using UnityEngine;

public class Ball : MonoBehaviour
{
    [SerializeField] private GameObject explosionEfectPrefab;
    [SerializeField] private Rigidbody myRb;
    [SerializeField] private float forcePower = 7f;
    [SerializeField] private float upwardRatio = 3f; //ileri bilesene gore yukari bilesen
    private bool isInside = false;

    public void SetStatus(bool _isInside)
    {
        isInside = _isInside;
    }
    
    public void Explode(Material platformMat)
    {
        if (explosionEfectPrefab != null)
        {
            GameObject spawnedEffectObj = Instantiate(explosionEfectPrefab, transform.position, Quaternion.identity);
            ParticleSystemRenderer psr = spawnedEffectObj.GetComponent<ParticleSystemRenderer>();
            if (psr == null)
                psr = spawnedEffectObj.GetComponentInChildren<ParticleSystemRenderer>();

            if (psr != null && platformMat != null)
                psr.material = platformMat;

            ParticleSystem particleSystem = spawnedEffectObj.GetComponent<ParticleSystem>();
            if (particleSystem == null)
                particleSystem = spawnedEffectObj.GetComponentInChildren<ParticleSystem>();

            particleSystem?.Play();
            Destroy(spawnedEffectObj, 3f);
        }

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
        PickerPhysicsCallbacks.hittedBallCollecterEvent += CheckIsInside;
    }
    private void OnDisable()
    {
        PickerPhysicsCallbacks.hittedBallCollecterEvent -= CheckIsInside;
    }
}
