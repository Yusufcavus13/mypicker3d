using UnityEngine;

//Yolun bir kenarina yerlesen donen engel. Picker icinden gecemez, acik
//taraftan dolanmak zorunda kalir. Ayni zamanda isaretleyici gorevi goruyor:
//PickerMovement onunu kesen seyin engel olup olmadigini bu bilesenle anliyor.
public class Obstacle : MonoBehaviour
{
    [SerializeField] private Transform rotatingBody;
    [SerializeField] private Transform baseBody;
    [SerializeField] private BoxCollider blockCollider;
    [SerializeField] private float rotationSpeed = 70f;
    [SerializeField] private float baseHeight = 0.14f;

    public float CenterX { get; private set; }
    public float HalfWidth { get; private set; }

    //Donen govde KARE tabanli. Y ekseninde donen kare bir kutunun x kaplamasi
    //en fazla kosegeni kadar olur; govdenin kenarini width/sqrt(2) yaparsak
    //dondugu hicbir acida width'i asmaz. Boylece gecis boslugu sabit kalir ve
    //uretimdeki adalet hesabi bozulmaz.
    public void Configure(float centerX, float localZ, float surfaceY, float width, float height)
    {
        CenterX = centerX;
        HalfWidth = width * 0.5f;

        //Root'u ASLA olcekleme: donen cocuga esit olmayan olcek gecerse
        //dondugunde egrilir (shear).
        transform.localScale = Vector3.one;
        transform.localPosition = new Vector3(centerX, surfaceY, localZ);

        if (blockCollider != null)
        {
            //z derinligi de width: govde donerken o yonde de o kadar yer kapliyor
            blockCollider.center = new Vector3(0f, height * 0.5f, 0f);
            blockCollider.size = new Vector3(width, height, width);
        }

        if (rotatingBody != null)
        {
            float bodySide = width / Mathf.Sqrt(2f);
            rotatingBody.localPosition = new Vector3(0f, (height * 0.5f) + baseHeight, 0f);
            //x ve z olcegi ESIT: Y ekseninde donerken egrilme olmuyor
            rotatingBody.localScale = new Vector3(bodySide, height, bodySide);
        }

        if (baseBody != null)
        {
            baseBody.localPosition = new Vector3(0f, baseHeight * 0.5f, 0f);
            baseBody.localScale = new Vector3(width, baseHeight, width);
        }
    }

    private void Update()
    {
        if (rotatingBody != null)
            rotatingBody.Rotate(0f, rotationSpeed * Time.deltaTime, 0f, Space.Self);
    }
}
