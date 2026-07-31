using System.Collections;
using UnityEngine;

//Slow-motion ve haptik tek bir yerde. Time.timeScale'i dagitip birakmak
//tehlikeli: bir yerde geri alinmazsa oyun agir cekimde kalir. Bu yuzden
//butun timeScale islerini buradan geciriyoruz.
public class GameFeel : MonoBehaviour
{
    [SerializeField] private float hitTimeScale = 0.35f;
    [SerializeField] private float hitDuration = 0.12f;
    [SerializeField] private bool useHaptics = true;

    private Coroutine slowMotionRoutine;
    private static GameFeel instance;

    //Sahneye elle obje eklemek gerekmesin diye ilk istekte kendini kuruyor.
    public static GameFeel Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject host = new GameObject("GameFeel");
                instance = host.AddComponent<GameFeel>();
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        //Sahne yeniden yuklendiginde agir cekimde kalmis olabiliriz
        Time.timeScale = 1f;
    }

    public void Hit()
    {
        Vibrate();

        if (slowMotionRoutine != null)
            StopCoroutine(slowMotionRoutine);
        slowMotionRoutine = StartCoroutine(SlowMotionRoutine());
    }

    public void Vibrate()
    {
        if (!useHaptics)
            return;
#if UNITY_ANDROID || UNITY_IOS
        if (!Application.isEditor)
            Handheld.Vibrate();
#endif
    }

    private IEnumerator SlowMotionRoutine()
    {
        Time.timeScale = hitTimeScale;

        //unscaled bekleme sart: normal WaitForSeconds agir cekimden etkilenir
        //ve bekleme suresi kendisi de uzardi.
        yield return new WaitForSecondsRealtime(hitDuration);

        Time.timeScale = 1f;
        slowMotionRoutine = null;
    }

    private void RestoreTimeScale()
    {
        if (slowMotionRoutine != null)
        {
            StopCoroutine(slowMotionRoutine);
            slowMotionRoutine = null;
        }
        Time.timeScale = 1f;
    }

    private void OnEnable()
    {
        GameManager.gameFinishedEvent += RestoreTimeScale;
    }

    private void OnDisable()
    {
        GameManager.gameFinishedEvent -= RestoreTimeScale;

        //Guvenlik agi: obje kapanirsa oyun agir cekimde kalmasin
        Time.timeScale = 1f;
    }
}
