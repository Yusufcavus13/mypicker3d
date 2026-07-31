using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

//Patlama efektleri icin object pool.
//Bir toplayici bosalinca 12-18 top AYNI KAREDE patliyordu: o kadar Instantiate
//ve 3 saniye sonra o kadar Destroy. Mobilde gorulen takilma tam olarak bu
//desenden geliyor. Havuzla ilk seferden sonra yeni obje uretilmiyor.
public class ExplosionEffectPool : MonoBehaviour
{
    [SerializeField] private int prewarmCount = 20;
    [SerializeField] private int maxSize = 60;
    [SerializeField] private float releaseAfter = 3f;

    private ObjectPool<ParticleSystem> pool;
    private GameObject effectPrefab;

    private static ExplosionEffectPool instance;

    //Sahneye elle obje eklemek gerekmesin diye ilk istekte kendini kuruyor.
    public static ExplosionEffectPool Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject host = new GameObject("ExplosionEffectPool");
                instance = host.AddComponent<ExplosionEffectPool>();
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
    }

    //Havuzu ONCEDEN kurmak icin. Ilk patlamayi beklersek prewarm tam da
    //kacinmak istedigimiz anda, toplayici bosalirken calisirdi.
    public void Prepare(GameObject prefab)
    {
        EnsurePool(prefab);
    }

    public void Play(GameObject prefab, Vector3 position, Material overrideMaterial)
    {
        if (prefab == null)
            return;

        EnsurePool(prefab);
        if (pool == null)
            return;

        ParticleSystem effect = pool.Get();
        if (effect == null)
            return;

        effect.transform.position = position;

        if (overrideMaterial != null && effect.TryGetComponent(out ParticleSystemRenderer psr))
            psr.material = overrideMaterial;

        effect.Play(true);
        StartCoroutine(ReleaseAfterDelay(effect));
    }

    private void EnsurePool(GameObject prefab)
    {
        if (pool != null)
        {
            if (effectPrefab != prefab)
                Debug.LogWarning("[ExplosionEffectPool] Farkli bir efekt prefabi geldi, havuz ilkine gore kuruldu.", this);
            return;
        }

        effectPrefab = prefab;
        pool = new ObjectPool<ParticleSystem>(
            createFunc: CreateEffect,
            actionOnGet: OnGet,
            actionOnRelease: OnRelease,
            actionOnDestroy: OnDestroyEffect,
            collectionCheck: true,
            defaultCapacity: prewarmCount,
            maxSize: maxSize);

        Prewarm();
    }

    private ParticleSystem CreateEffect()
    {
        GameObject spawned = Instantiate(effectPrefab, transform);
        ParticleSystem effect = spawned.GetComponent<ParticleSystem>();
        if (effect == null)
            effect = spawned.GetComponentInChildren<ParticleSystem>();

        if (effect == null)
        {
            Debug.LogError($"[ExplosionEffectPool] {effectPrefab.name} uzerinde ParticleSystem yok.", this);
            Destroy(spawned);
            return null;
        }

        spawned.SetActive(false);
        return effect;
    }

    private void OnGet(ParticleSystem effect)
    {
        if (effect != null)
            effect.gameObject.SetActive(true);
    }

    //ALTIN KURAL: havuza donen obje eski durumunu tasir. Durdurup temizlemezsek
    //sonraki kullanimda onceki patlamanin yarisi ekranda basliyor.
    private void OnRelease(ParticleSystem effect)
    {
        if (effect == null)
            return;

        effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        effect.Clear(true);
        effect.gameObject.SetActive(false);
    }

    private void OnDestroyEffect(ParticleSystem effect)
    {
        if (effect != null)
            Destroy(effect.gameObject);
    }

    //defaultCapacity obje URETMEZ, sadece ic yiginin kapasitesini ayirir.
    //Ilk patlamada takilma olmasin diye elle dolduruyoruz.
    private void Prewarm()
    {
        ParticleSystem[] warm = new ParticleSystem[prewarmCount];
        for (int i = 0; i < prewarmCount; i++)
            warm[i] = pool.Get();

        for (int i = 0; i < prewarmCount; i++)
        {
            if (warm[i] != null)
                pool.Release(warm[i]);
        }
    }

    private IEnumerator ReleaseAfterDelay(ParticleSystem effect)
    {
        yield return new WaitForSeconds(releaseAfter);

        if (effect != null && pool != null)
            pool.Release(effect);
    }
}
