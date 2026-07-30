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
    [SerializeField] private float ballSpacing = 0.5f;     //gruptaki toplarin arasi
    [SerializeField] private float ballAreaPadding = 0.7f; //yolun ONUNDE/ARKASINDA birakilan bosluk (z)
    [SerializeField] private float ballEdgeMargin = 0.3f;  //duvarlara birakilan bosluk (x)
    [SerializeField] private int minGroupSize = 2;         //bir kumede en az kac top
    [SerializeField] private int maxGroupSize = 5;         //bir kumede en fazla kac top
    //Picker x ekseninde -1.5 / +1.5 arasinda hareket eder, agzinin yarisi da 0.76,
    //yani en fazla 2.26'ya uzanabilir. Yayilma bunu GECMEMELI, yoksa top toplanamaz.
    [SerializeField] private float ballSpreadWidth = 4f;
    [SerializeField] private float bigBallScale = 2.2f;    //buyuk toplarin olcek carpani
    [SerializeField] private int bigBallValue = 5;          //buyuk top kac top sayilir
    [SerializeField] private Color normalBallColor = Color.black;
    [SerializeField] private Color bigBallColor = new Color(1f, 0.72f, 0.1f); //altin: "degerli" demek
    [SerializeField] private float ballPackingFactor = 0.75f; //yolun ne kadarina yayilsin (1 = tamami)

    //Picker'in hizlari. Uretim bunlara bakarak "bu kumeye yetisilebilir mi"
    //hesabi yapiyor; PickerMovement'taki degerlerle AYNI olmali.
    [SerializeField] private float pickerForwardSpeed = 5f;
    [SerializeField] private float pickerLateralSpeed = 9f;

    private struct BallPlacement
    {
        public Vector3 localPosition;
        public bool isBig;
    }

    private struct BallGroup
    {
        public int size;
        public bool isBig;
    }

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
    public void SpawnBalls(int amount, int bigBallCount, float spreadWidthOverride)
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

        List<BallPlacement> placements = BuildBallPositions(amount, bigBallCount, spreadWidthOverride);

        for (int i = 0; i < placements.Count; i++)
        {
            if (i < ballsParent.childCount)
            {
                //var olan topu tekrar kullan
                ballsParent.GetChild(i).gameObject.SetActive(true);
                PlaceBall(ballsParent.GetChild(i), placements[i]);
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
                PlaceBall(spawnedBall.transform, placements[i]);
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
    private void PlaceBall(Transform ball, BallPlacement placement)
    {
        ball.localPosition = placement.localPosition;

        //olcek, renk ve deger her seferinde YAZILIR: havuzdan gelen top onceki
        //kullanimindan buyuk ya da altin renkli kalmis olabilir
        Vector3 baseScale = ballPrefab.transform.localScale;
        ball.localScale = placement.isBig ? baseScale * bigBallScale : baseScale;

        SetBallColor(ball, placement.isBig ? bigBallColor : normalBallColor);

        if (ball.TryGetComponent(out Ball ballComponent))
            ballComponent.SetValue(placement.isBig ? bigBallValue : 1);

        if (!ball.TryGetComponent(out Rigidbody ballRb))
            return;

        ballRb.position = ball.position;
        ballRb.rotation = ball.rotation;
        ballRb.linearVelocity = Vector3.zero;
        ballRb.angularVelocity = Vector3.zero;
    }

    //Topun rengini MaterialPropertyBlock ile veriyoruz: yeni materyal uretmeden,
    //tek tek topa ozel renk. Platformlarda da ayni yontem kullaniliyor.
    private void SetBallColor(Transform ball, Color color)
    {
        if (!ball.TryGetComponent(out Renderer ballRenderer))
            return;
        if (mpb == null)
            mpb = new MaterialPropertyBlock();

        ballRenderer.GetPropertyBlock(mpb);
        //_AlbedoColor = MK Toon shader'inin ana renk ozelligi
        mpb.SetColor("_AlbedoColor", color);
        mpb.SetColor("_BaseColor", color);
        mpb.SetColor("_Color", color);
        ballRenderer.SetPropertyBlock(mpb);
    }

    //Toplari 2-5'lik kucuk kumelere bolup yol boyunca serpiyoruz. Her kume
    //rastgele bir sekil (dik dizi, yan dizi, kucuk blok) ve rastgele bir yan
    //konum aliyor; boylece her level farkli bir dagilimla cikiyor.
    private List<BallPlacement> BuildBallPositions(int amount, int bigBallCount, float spreadWidthOverride)
    {
        List<BallPlacement> positions = new List<BallPlacement>(Mathf.Max(0, amount));
        if (amount <= 0)
            return positions;

        Vector2 roadSize = GetRoadSize();

        //durak kendi yayilma genisligini soyleyebilir; 0 ise RoadPlatform'un varsayilani
        float spread = spreadWidthOverride > 0f ? spreadWidthOverride : ballSpreadWidth;
        int bigRemaining = Mathf.Clamp(bigBallCount, 0, amount);

        //X'te tavan duvar, Z'de tavan yolun uzunlugu. Ayri marj kullanmak sart:
        //ikisine ayni padding'i verirsek yayilma ayari tavana carpip etkisiz kaliyor.
        float halfSpread = Mathf.Min(spread * 0.5f, Mathf.Max(0f, (roadSize.x * 0.5f) - ballEdgeMargin));
        float halfLength = Mathf.Max(0f, (roadSize.y * 0.5f) - ballAreaPadding) * Mathf.Clamp01(ballPackingFactor);

        List<BallGroup> groups = SplitIntoGroups(amount, bigRemaining);

        //her kumeye yol boyunca esit bir dilim ayirip dilimin icinde oynatiyoruz
        float slot = (halfLength * 2f) / groups.Count;
        float baseDiameter = ballPrefab != null ? ballPrefab.transform.localScale.x : 0.3f;

        float previousCenterX = 0f;
        float previousCenterZ = -halfLength - (pickerForwardSpeed * 0.5f); //picker yola girmeden once ortada

        for (int g = 0; g < groups.Count; g++)
        {
            BallGroup group = groups[g];
            int size = group.size;

            //bu kumedeki toplarin capi: buyuk toplar cok daha genis yer kapliyor
            float ballDiameter = baseDiameter * (group.isBig ? bigBallScale : 1f);
            float ballRadius = ballDiameter * 0.5f;
            float spacing = Mathf.Max(ballSpacing, ballDiameter + 0.05f);

            //kume dilimine kac sira sigiyorsa o kadar derin olsun, kalani yana
            //dagilsin; boylece kalabalik levellerde kumeler ust uste binmez
            int maxRows = Mathf.Max(1, Mathf.FloorToInt(Mathf.Max(0f, slot - spacing) / spacing) + 1);
            int minColumns = Mathf.Clamp(Mathf.CeilToInt(size / (float)maxRows), 1, size);
            int columns = Mathf.Clamp(Random.Range(minColumns, Mathf.Max(minColumns, 3) + 1), 1, size);
            int rows = Mathf.CeilToInt(size / (float)columns);

            float groupHalfWidth = (columns - 1) * spacing * 0.5f;
            float groupHalfDepth = (rows - 1) * spacing * 0.5f;

            //kume yolun disina ve picker'in erisemeyecegi yere tasmasin.
            //Tek tek toplari degil KUMEYI sigdiriyoruz, yoksa uctaki toplar
            //ayni noktaya kirpilip ust uste binerdi.
            float limitX = Mathf.Max(0f, halfSpread - groupHalfWidth - ballRadius);

            //dilimin kenarinda topun yaricapi kadar pay: yan yana dusen kumeler carpismasin
            float slotCenter = -halfLength + (slot * (g + 0.5f));
            float slotPlay = Mathf.Max(0f, (slot * 0.5f) - groupHalfDepth - ballRadius);
            float limitZ = Mathf.Max(0f, halfLength - groupHalfDepth - ballRadius);
            float centerZ = Mathf.Clamp(slotCenter + Random.Range(-slotPlay, slotPlay), -limitZ, limitZ);

            //ERISILEBILIRLIK: picker onceki kumeden buraya gelirken ne kadar yol
            //alabilir? Kumeyi o pencerenin icine kirpiyoruz, boylece hicbir top
            //"sansa bagli olarak" ulasilamaz hale gelmiyor. Zorluk sikilıktan gelir.
            float forwardGap = Mathf.Max(0.01f, centerZ - previousCenterZ);
            float travelWindow = pickerLateralSpeed * (forwardGap / Mathf.Max(0.01f, pickerForwardSpeed));
            float minX = Mathf.Max(-limitX, previousCenterX - travelWindow);
            float maxX = Mathf.Min(limitX, previousCenterX + travelWindow);
            float centerX = minX <= maxX
                ? Random.Range(minX, maxX)
                : Mathf.Clamp(previousCenterX, -limitX, limitX);

            previousCenterX = centerX;
            previousCenterZ = centerZ;

            for (int i = 0; i < size; i++)
            {
                positions.Add(new BallPlacement
                {
                    localPosition = new Vector3(
                        centerX + (((i % columns) - ((columns - 1) * 0.5f)) * spacing),
                        0f,
                        centerZ + (((i / columns) - ((rows - 1) * 0.5f)) * spacing)),
                    isBig = group.isBig
                });
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

    //Buyuk toplar KENDI BASINA kume oluyor: 2.2 kat buyuk olduklari icin baska
    //bir topla ayni kumede yan yana gelirlerse ic ice girerler.
    private List<BallGroup> SplitIntoGroups(int amount, int bigBallCount)
    {
        List<BallGroup> groups = new List<BallGroup>();

        int bigCount = Mathf.Clamp(bigBallCount, 0, amount);
        for (int i = 0; i < bigCount; i++)
            groups.Add(new BallGroup { size = 1, isBig = true });

        int minSize = Mathf.Max(1, minGroupSize);
        int maxSize = Mathf.Max(minSize, maxGroupSize);
        int remaining = amount - bigCount;

        while (remaining > 0)
        {
            int size = Mathf.Min(Random.Range(minSize, maxSize + 1), remaining);
            if (remaining - size < minSize) //geride tek basina kalan top olmasin
                size = remaining;

            groups.Add(new BallGroup { size = size, isBig = false });
            remaining -= size;
        }

        //buyuk toplar hep yolun basinda toplanmasin
        for (int i = groups.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (groups[i], groups[j]) = (groups[j], groups[i]);
        }

        return groups;
    }
}
