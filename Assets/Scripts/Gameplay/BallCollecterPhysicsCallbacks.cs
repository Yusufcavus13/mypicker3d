//Bu da çukurun ağzındaki bir trigger'da duruyor.
//  Görevi tek: "içime bir top düştü mü?"
using UnityEngine;

public class BallCollecterPhysicsCallbacks : MonoBehaviour
{
    [SerializeField] private BallCollecterPlatform ballCollecterPlatform;

    private void Awake()
    {
        if (ballCollecterPlatform == null)
            ballCollecterPlatform = GetComponentInParent<BallCollecterPlatform>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag("Ball") || ballCollecterPlatform == null)
            return;

        other.gameObject.tag = "Untagged";
        ballCollecterPlatform.CollactNewBall(other.gameObject);
    }
}
