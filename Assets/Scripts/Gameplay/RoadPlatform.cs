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
    //Picker x ekseninde laneLimit kadar hareket eder, agzinin yarisi da 0.76,
    //yani en fazla laneLimit + 0.76'ya uzanabilir. Yayilma bunu GECMEMELI.
    [SerializeField] private float ballSpreadWidth = 4f;
    [SerializeField] private float bigBallScale = 2.2f;    //buyuk toplarin olcek carpani
    [SerializeField] private int bigBallValue = 5;          //buyuk top kac top sayilir
    [SerializeField] private Color normalBallColor = Color.black;
    [SerializeField] private Color bigBallColor = new Color(1f, 0.72f, 0.1f); //altin: "degerli" demek
    [SerializeField] private float ballPackingFactor = 0.75f; //yolun ne kadarina yayilsin (1 = tamami)


    private struct BallPlacement
    {
        public Vector3 localPosition;
        public bool isBig;
    }

    [Header("Engeller")]
    [SerializeField] private GameObject obstaclePrefab;
    [SerializeField] private Transform obstaclesParent;
    [SerializeField] private float obstacleWidth = 1.6f;
    [SerializeField] private float obstacleHeight = 1.2f;
    //Duvarlar x = +-2.443'te ve 0.1 kalinliginda. Bu pay olmadan engelin dis
    //kenari duvarin icine giriyor ve gomulu gorunuyor.
    [SerializeField] private float obstacleWallInset = 0.2f;
    //Picker olculeri PickerMovement ile ORTAK: ayri tutuldugunda birini
    //degistirip digerini unutmak oyunu sessizce adaletsiz yapiyordu.
    [SerializeField] private PickerSettings pickerSettings;

    private struct BallGroup
    {
        public int size;
        public bool isBig;
    }

    //Yolda sirayla gelen duraklar: ya bir top kumesi ya bir engel.
    //Ikisi ayni zincirde, cunku picker ikisinin arasinda da yol almak zorunda.
    private struct SlotItem
    {
        public bool isObstacle;
        public bool obstacleOnLeft;
        public BallGroup group;
    }

    private struct ObstaclePlacement
    {
        public float centerX;
        public float localZ;
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
    public void SpawnBalls(int amount, int bigBallCount, float spreadWidthOverride, int obstacleCount)
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

        List<ObstaclePlacement> obstacles = new List<ObstaclePlacement>();
        List<BallPlacement> placements = BuildBallPositions(amount, bigBallCount, spreadWidthOverride,
            obstacleCount, obstacles);
        SpawnObstacles(obstacles);

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

    private void SpawnObstacles(List<ObstaclePlacement> obstacles)
    {
        if (obstaclePrefab == null)
        {
            if (obstacles.Count > 0)
                Debug.LogWarning($"[RoadPlatform] {name}: obstaclePrefab bagli degil, engeller atlandi.", this);
            return;
        }

        Transform parent = GetObstaclesParent();

        //BallParent gibi yolun merkezine hizaliyoruz; y'yi 0 birakiyoruz ki
        //engelin local y'si dogrudan yuzey yuksekligi olsun
        parent.localPosition = new Vector3(myRoadTransform.localPosition.x, 0f,
            myRoadTransform.localPosition.z);

        //havuz disiplini: once hepsini kapat
        for (int i = 0; i < parent.childCount; i++)
            parent.GetChild(i).gameObject.SetActive(false);

        //engeller yolun yuzeyine oturmali: Road'un y'si + kupun yariyuksekligi
        float cubeHeight = platformRenderer != null ? platformRenderer.transform.localScale.y : 1f;
        float surfaceY = myRoadTransform.localPosition.y
            + (myRoadTransform.localScale.y * cubeHeight * 0.5f);

        for (int i = 0; i < obstacles.Count; i++)
        {
            Transform obstacleTransform;
            if (i < parent.childCount)
            {
                obstacleTransform = parent.GetChild(i);
                obstacleTransform.gameObject.SetActive(true);
            }
            else
            {
#if UNITY_EDITOR
                GameObject spawned = Application.isPlaying
                    ? Instantiate(obstaclePrefab)
                    : (GameObject)PrefabUtility.InstantiatePrefab(obstaclePrefab);
#else
                GameObject spawned = Instantiate(obstaclePrefab);
#endif
                spawned.transform.SetParent(parent, false);
                obstacleTransform = spawned.transform;
            }

            if (!obstacleTransform.TryGetComponent(out Obstacle obstacle))
            {
                Debug.LogError($"[RoadPlatform] {obstaclePrefab.name} uzerinde Obstacle bileseni yok.", this);
                return;
            }

            obstacle.Configure(obstacles[i].centerX, obstacles[i].localZ, surfaceY,
                obstacleWidth, obstacleHeight);
        }
    }

    //Prefaba yeni bir cocuk eklemek zorunda kalmamak icin gerekirse kendimiz uretiyoruz.
    private Transform GetObstaclesParent()
    {
        if (obstaclesParent != null)
            return obstaclesParent;

        Transform existing = transform.Find("ObstaclesParent");
        if (existing != null)
        {
            obstaclesParent = existing;
            return obstaclesParent;
        }

        GameObject created = new GameObject("ObstaclesParent");
        created.transform.SetParent(transform, false);
        obstaclesParent = created.transform;
        return obstaclesParent;
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
    private List<BallPlacement> BuildBallPositions(int amount, int bigBallCount, float spreadWidthOverride,
        int obstacleCount, List<ObstaclePlacement> obstacleResults)
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

        List<SlotItem> slots = BuildSlots(amount, bigRemaining, obstacleCount);

        //her durak yol boyunca esit bir dilim aliyor; engeller de kendi diliminde
        float slot = (halfLength * 2f) / slots.Count;
        float baseDiameter = ballPrefab != null ? ballPrefab.transform.localScale.x : 0.3f;

        float previousCenterX = 0f;
        float previousCenterZ = -halfLength - (Settings.forwardSpeed * 0.5f); //picker yola girmeden once ortada

        for (int g = 0; g < slots.Count; g++)
        {
            float itemSlotCenter = -halfLength + (slot * (g + 0.5f));

            if (slots[g].isObstacle)
            {
                PlaceObstacleSlot(slots[g].obstacleOnLeft, itemSlotCenter, roadSize,
                    ref previousCenterX, ref previousCenterZ, obstacleResults);
                continue;
            }

            BallGroup group = slots[g].group;
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
            float slotCenter = itemSlotCenter;
            float slotPlay = Mathf.Max(0f, (slot * 0.5f) - groupHalfDepth - ballRadius);
            float limitZ = Mathf.Max(0f, halfLength - groupHalfDepth - ballRadius);
            float centerZ = Mathf.Clamp(slotCenter + Random.Range(-slotPlay, slotPlay), -limitZ, limitZ);

            //ERISILEBILIRLIK: picker onceki kumeden buraya gelirken ne kadar yol
            //alabilir? Kumeyi o pencerenin icine kirpiyoruz, boylece hicbir top
            //"sansa bagli olarak" ulasilamaz hale gelmiyor. Zorluk sikilıktan gelir.
            float forwardGap = Mathf.Max(0.01f, centerZ - previousCenterZ);
            float travelWindow = Settings.lateralSpeed * (forwardGap / Mathf.Max(0.01f, Settings.forwardSpeed));
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

    //Engeli duvara yasliyoruz, geciş boslugu acik tarafta kaliyor. Boslugun
    //ORTASI picker'in gidebilecegi bir x olmak zorunda, yoksa level kilitlenir.
    //Ayrica bu boslugu erisilebilirlik zincirine bir durak olarak ekliyoruz:
    //picker onceki kumeden buraya, buradan sonraki kumeye yetismek zorunda.
    private void PlaceObstacleSlot(bool onLeft, float slotCenterZ, Vector2 roadSize,
        ref float previousCenterX, ref float previousCenterZ, List<ObstaclePlacement> results)
    {
        //duvarin ic yuzu: engel buraya yaslanacak, icine gommeyecek
        float roadHalfWidth = (roadSize.x * 0.5f) - obstacleWallInset;
        float width = Mathf.Min(obstacleWidth, Mathf.Max(0.2f, (roadHalfWidth * 2f) - (Settings.halfWidth * 2f) - 0.2f));

        //duvara yasli engelin ic kenari
        float innerEdge = onLeft ? -roadHalfWidth + width : roadHalfWidth - width;

        //picker'in merkezi bu araliktan gecebilir
        float gapMin = onLeft ? innerEdge + Settings.halfWidth : -Settings.laneLimit;
        float gapMax = onLeft ? Settings.laneLimit : innerEdge - Settings.halfWidth;
        gapMin = Mathf.Max(gapMin, -Settings.laneLimit);
        gapMax = Mathf.Min(gapMax, Settings.laneLimit);

        //Bosluk kapaniyorsa engel bu yola sigmiyor: hic koymuyoruz.
        //Sessizce gecmek yerine kaydediyorum ki tasarimda fark edilsin.
        if (gapMin > gapMax)
        {
            Debug.LogWarning($"[RoadPlatform] {name}: engel yola sigmadi (genislik {width:F2}), atlandi.", this);
            return;
        }

        //picker onceki duraktan buraya ne kadar yana gidebilir
        float forwardGap = Mathf.Max(0.01f, slotCenterZ - previousCenterZ);
        float travelWindow = Settings.lateralSpeed * (forwardGap / Mathf.Max(0.01f, Settings.forwardSpeed));

        float reachMin = Mathf.Max(gapMin, previousCenterX - travelWindow);
        float reachMax = Mathf.Min(gapMax, previousCenterX + travelWindow);

        //Yetisemiyorsa bosluga en yakin noktayi hedefliyoruz; engel yine de
        //gecilebilir kalsin diye bosluk araligindan disari cikmiyoruz.
        float passX = reachMin <= reachMax
            ? Random.Range(reachMin, reachMax)
            : Mathf.Clamp(previousCenterX, gapMin, gapMax);

        results.Add(new ObstaclePlacement
        {
            centerX = onLeft ? -roadHalfWidth + (width * 0.5f) : roadHalfWidth - (width * 0.5f),
            localZ = slotCenterZ
        });

        //zincir bu bosluktan devam ediyor
        previousCenterX = passX;
        previousCenterZ = slotCenterZ;
    }

    //PickerSettings atanmamissa oyun cokmesin: varsayilanlarla devam.
    private PickerSettings Settings
    {
        get
        {
            if (pickerSettings == null)
            {
                Debug.LogError($"[RoadPlatform] {name}: PickerSettings atanmamis, varsayilanlar kullaniliyor.", this);
                pickerSettings = ScriptableObject.CreateInstance<PickerSettings>();
            }
            return pickerSettings;
        }
    }

    //Duraklari hazirla: top kumeleri + engeller, karisik sirada.
    private List<SlotItem> BuildSlots(int amount, int bigBallCount, int obstacleCount)
    {
        List<SlotItem> slots = new List<SlotItem>();

        foreach (BallGroup group in SplitIntoGroups(amount, bigBallCount))
            slots.Add(new SlotItem { isObstacle = false, group = group });

        //engeller donusumlu olarak sag/sol duvara yaslanir
        int obstacles = Mathf.Max(0, obstacleCount);
        for (int i = 0; i < obstacles; i++)
            slots.Add(new SlotItem { isObstacle = true, obstacleOnLeft = i % 2 == 0 });

        //Engelleri araya serpiyoruz. Ilk durak engel olmasin: picker yola
        //girer girmez duvara tosladigini hissetmesin.
        for (int i = slots.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (slots[i], slots[j]) = (slots[j], slots[i]);
        }
        for (int i = 1; i < slots.Count; i++)
        {
            if (!slots[0].isObstacle)
                break;
            (slots[0], slots[i]) = (slots[i], slots[0]);
        }

        return slots;
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
