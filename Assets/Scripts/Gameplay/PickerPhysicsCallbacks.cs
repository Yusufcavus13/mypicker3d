// Picker duyu organı 
using UnityEngine;
using System;

public class PickerPhysicsCallbacks : MonoBehaviour
{
    public static event Action hittedBallCollecterEvent;
    public static event Action hittedLevelEndEvent;
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("BallCollecter"))
        {
            other.gameObject.tag = "Untagged";

            //GetComponentInParent'in Try- versiyonu yok, null kontrolu elle
            BallCollecterPlatform ballCollecterPlatform = other.gameObject.GetComponentInParent<BallCollecterPlatform>();
            if (ballCollecterPlatform == null)
            {
                Debug.LogError($"[Picker] {other.name} uzerinde BallCollecterPlatform bulunamadi.", other);
                return;
            }

            ballCollecterPlatform.CheckCollecterStatus();
            other.gameObject.SetActive(false);

            //carpma ani: kisa agir cekim + titresim (kamera sarsintisi olaya abone)
            GameFeel.Instance.Hit();

            hittedBallCollecterEvent?.Invoke();
        }
        if (other.gameObject.CompareTag("LevelEnd"))
        {
            other.gameObject.tag = "Untagged";
            hittedLevelEndEvent?.Invoke();
        }
        if (other.gameObject.CompareTag("Ball") && other.TryGetComponent(out Ball enteringBall))
        {
            enteringBall.SetStatus(true);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Ball") && other.TryGetComponent(out Ball exitingBall))
        {
            exitingBall.SetStatus(false);
        }
    }
}
