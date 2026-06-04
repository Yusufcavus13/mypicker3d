using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ball : MonoBehaviour
{
    [SerializeField] private GameObject explosionEfectPrefab;
    [SerializeField] private Renderer myRenderer;
    [SerializeField] private Rigidbody myRb;
    private bool isInside=false;

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
        Vector3 forceDirection = (transform.position + new Vector3(transform.position.x,
            transform.position.y +15f,transform.position.z+5f)).normalized;
        myRb.linearVelocity = Vector3.zero;
        myRb.angularVelocity = Vector3.zero;
        myRb.AddForce(forceDirection*300);
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
