using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [SerializeField] private AudioSource[] popSources;
    [SerializeField] private float minPitch = 0.9f;
    [SerializeField] private float maxPitch = 1.1f;
    private int nextIndex = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void PlayPop()
    {
        if (popSources == null || popSources.Length == 0)
            return;

        //round-robin: use the next source so overlapping pops don't cut each other off
        AudioSource source = popSources[nextIndex];
        nextIndex = (nextIndex + 1) % popSources.Length;

        if (source == null)
            return;

        //tiny random pitch so repeated pops don't sound robotic
        source.pitch = Random.Range(minPitch, maxPitch);
        source.Play();
    }
}
