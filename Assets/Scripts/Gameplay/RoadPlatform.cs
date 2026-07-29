using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class RoadPlatform : MonoBehaviour
{
    [SerializeField] private Transform myRoadTransform;
    [SerializeField] private Renderer platformRenderer;
    [SerializeField] private GameObject ballPrefab;
    [SerializeField] private Transform ballsParent;
    [SerializeField] private float ballSpacing = 0.35f;    //gruptaki toplarin arasi (top capi 0.3)
    [SerializeField] private float ballAreaPadding = 0.7f; //yolun onunde/arkasinda birakilan bosluk
    [SerializeField] private int minGroupSize = 2;         //bir kumede en az kac top
    [SerializeField] private int maxGroupSize = 5;         //bir kumede en fazla kac top
    //Picker x ekseninde -1.5 / +1.5 arasinda hareket eder, agzinin yarisi da 0.76,
    //yani en fazla 2.26'ya uzanabilir. Yayilma bunu GECMEMELI, yoksa top toplanamaz.
    [SerializeField] private float ballSpreadWidth = 4f;

    private static MaterialPropertyBlock mpb;

    public void SetPlatformColor(Color newColor)
    {
        //MaterialPropertyBlock: yeni materyal URETMEDEN rengi degistirir.
        //Boylece prefab'a kirik materyal kaydedilmez (magenta sorunu biter).
        if (platformRenderer == null)
            return;
        if (mpb == null)
            mpb = new MaterialPropertyBlock();

        platformRenderer.GetPropertyBlock(mpb);
        //_AlbedoColor = MK Toon shader'inin ana renk ozelligi (_BaseColor DEGIL)
        mpb.SetColor("_AlbedoColor", newColor);
        mpb.SetColor("_BaseColor", newColor);
        mpb.SetColor("_Color", newColor);
        platformRenderer.SetPropertyBlock(mpb);
    }

    public void CalcPlatformLength(float platformLength)
    {
        myRoadTransform.localScale = new Vector3(myRoadTransform.localScale.x,
            myRoadTransform.localScale.y, platformLength);
        myRoadTransform.localPosition = new Vector3(myRoadTransform.localPosition.x, myRoadTransform.localPosition.y,
             transform.localPosition.z + ((platformLength - 1f) * 2.5f));

    }
    public void SpawnBalls(int amount)
    {
        //BallParent'i yolun merkezine hizaliyoruz; toplar bundan sonra LOCAL
        //konumla yerlestiriliyor, yani dunya koordinatina hic bakmiyoruz.
        ballsParent.localPosition = new Vector3(myRoadTransform.localPosition.x,
                ballsParent.localPosition.y, myRoadTransform.localPosition.z);

        //once hepsini kapat, sonra ihtiyac kadarini acip yerlestir
        for (int i = 0; i < ballsParent.childCount; i++)
        {
            ballsParent.GetChild(i).gameObject.SetActive(false);
        }

        List<Vector3> positions = BuildBallPositions(amount);

        for (int i = 0; i < positions.Count; i++)
        {
            if (i < ballsParent.childCount)
            {
                //var olan topu tekrar kullan
                ballsParent.GetChild(i).gameObject.SetActive(true);
                PlaceBall(ballsParent.GetChild(i), positions[i]);
            }
            else
            {
                //yeterli top yok, yenisini uret
#if UNITY_EDITOR
                //editorde tasarlarken prefab baglantisi korunsun, oyun calisirken normal Instantiate
                GameObject spawnedBall = Application.isPlaying
                    ? Instantiate(ballPrefab)
                    : (GameObject)PrefabUtility.InstantiatePrefab(ballPrefab);
#else
                GameObject spawnedBall = Instantiate(ballPrefab);
#endif
                spawnedBall.transform.SetParent(ballsParent, false);
                PlaceBall(spawnedBall.transform, positions[i]);
            }
        }

        //Transform'a yazdiklarimizi fizik motoruna da bildiriyoruz; yoksa motor
        //bir sonraki adimda kendi bildigi eski pozu geri yaziyor.
        if (Application.isPlaying)
            Physics.SyncTransforms();
    }

    //Rigidbody'nin pozu transform'dan ayri tutulur. Sadece transform'u yazarsak
    //fizik motoru topu bir sonraki adimda eski yerine geri cekiyor; bu yuzden
    //Rigidbody'yi de elle hizaliyor ve birikmis hizi sifirliyoruz.
    private void PlaceBall(Transform ball, Vector3 localPosition)
    {
        ball.localPosition = localPosition;

        Rigidbody ballRb = ball.GetComponent<Rigidbody>();
        if (ballRb == null)
            return;

        ballRb.position = ball.position;
        ballRb.rotation = ball.rotation;
        ballRb.linearVelocity = Vector3.zero;
        ballRb.angularVelocity = Vector3.zero;
    }

    //Toplari 2-5'lik kucuk kumelere bolup yol boyunca serpiyoruz. Her kume
    //rastgele bir sekil (dik dizi, yan dizi, kucuk blok) ve rastgele bir yan
    //konum aliyor; boylece her level farkli bir dagilimla cikiyor.
    private List<Vector3> BuildBallPositions(int amount)
    {
        List<Vector3> positions = new List<Vector3>(Mathf.Max(0, amount));
        if (amount <= 0)
            return positions;

        Vector2 roadSize = GetRoadSize();

        float halfSpread = Mathf.Min(ballSpreadWidth * 0.5f, Mathf.Max(0f, (roadSize.x * 0.5f) - ballAreaPadding));
        float halfLength = Mathf.Max(0f, (roadSize.y * 0.5f) - ballAreaPadding);

        List<int> groupSizes = SplitIntoGroups(amount);

        //her kumeye yol boyunca esit bir dilim ayirip dilimin icinde oynatiyoruz
        float slot = (halfLength * 2f) / groupSizes.Count;

        for (int g = 0; g < groupSizes.Count; g++)
        {
            int size = groupSizes[g];

            //kume dilimine kac sira sigiyorsa o kadar derin olsun, kalani yana
            //dagilsin; boylece kalabalik levellerde kumeler ust uste binmez
            int maxRows = Mathf.Max(1, Mathf.FloorToInt(Mathf.Max(0f, slot - ballSpacing) / ballSpacing) + 1);
            int minColumns = Mathf.Clamp(Mathf.CeilToInt(size / (float)maxRows), 1, size);
            int columns = Mathf.Clamp(Random.Range(minColumns, Mathf.Max(minColumns, 3) + 1), 1, size);
            int rows = Mathf.CeilToInt(size / (float)columns);

            float groupHalfWidth = (columns - 1) * ballSpacing * 0.5f;
            float groupHalfDepth = (rows - 1) * ballSpacing * 0.5f;

            //kume yolun disina ve picker'in erisemeyecegi yere tasmasin.
            //Tek tek toplari degil KUMEYI sigdiriyoruz, yoksa uctaki toplar
            //ayni noktaya kirpilip ust uste binerdi.
            float limitX = Mathf.Max(0f, halfSpread - groupHalfWidth);
            float centerX = Random.Range(-limitX, limitX);

            //dilimin kenarinda bir top capi pay: yan yana dusen kumeler carpismasin
            float slotCenter = -halfLength + (slot * (g + 0.5f));
            float slotPlay = Mathf.Max(0f, (slot * 0.5f) - groupHalfDepth - (ballSpacing * 0.5f));
            float limitZ = Mathf.Max(0f, halfLength - groupHalfDepth);
            float centerZ = Mathf.Clamp(slotCenter + Random.Range(-slotPlay, slotPlay), -limitZ, limitZ);

            for (int i = 0; i < size; i++)
            {
                positions.Add(new Vector3(
                    centerX + (((i % columns) - ((columns - 1) * 0.5f)) * ballSpacing),
                    0f,
                    centerZ + (((i / columns) - ((rows - 1) * 0.5f)) * ballSpacing)));
            }
        }

        return positions;
    }

    //Yolun olcusu SADECE localScale'den hesaplaniyor. Renderer.bounds ve localBounds
    //cizime bagli oldugu icin yeni Instantiate edilmis objede yanlis deger donduruyor,
    //bu da butun toplarin tek noktaya yiginlanmasina yol aciyordu. localScale ise
    //serialize edilmis veri: her zaman, her karede dogru.
    private Vector2 GetRoadSize()
    {
        if (myRoadTransform == null)
            return new Vector2(5f, 5f);

        //yol kupunun kendi olcegi (5x1x5) x Road'un olcegi (z = platformLength)
        Vector3 cubeScale = platformRenderer != null ? platformRenderer.transform.localScale : Vector3.one;
        Vector3 roadScale = myRoadTransform.localScale;

        return new Vector2(cubeScale.x * roadScale.x, cubeScale.z * roadScale.z);
    }

    private List<int> SplitIntoGroups(int amount)
    {
        List<int> groupSizes = new List<int>();
        int minSize = Mathf.Max(1, minGroupSize);
        int maxSize = Mathf.Max(minSize, maxGroupSize);
        int remaining = amount;

        while (remaining > 0)
        {
            int size = Mathf.Min(Random.Range(minSize, maxSize + 1), remaining);
            if (remaining - size < minSize) //geride tek basina kalan top olmasin
                size = remaining;

            groupSizes.Add(size);
            remaining -= size;
        }

        return groupSizes;
    }
}
