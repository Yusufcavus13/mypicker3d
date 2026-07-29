using UnityEngine;

public class PickerAnimation : MonoBehaviour
{
    [SerializeField] private Animator myAnim;

    private void Awake()
    {
        if (myAnim == null)
            myAnim = GetComponent<Animator>();
    }

    private void PlayCelebrate()
    {
        if (myAnim != null)
            myAnim.SetTrigger("Celebrate");
    }
    
    private void OnEnable()
    {
        PickerPhysicsCallbacks.hittedBallCollecterEvent += PlayCelebrate;
    }
    private void OnDisable()
    {
        PickerPhysicsCallbacks.hittedBallCollecterEvent -= PlayCelebrate;
    }
}
