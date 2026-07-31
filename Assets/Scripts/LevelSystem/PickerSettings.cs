using UnityEngine;

//Picker'in olculeri iki yerde birden lazim:
//  PickerMovement -> hareketi uygular
//  RoadPlatform   -> "picker buraya yetisebilir mi" hesabini yapar
//
//Bu sayilar ayri ayri tutuldugunda birini degistirip digerini unutmak
//oyunu SESSIZCE adaletsiz yapiyordu: uretim picker'in yetisebilecegini
//varsayip toplari oraya koyuyor, picker yetisemiyordu.
//Tek kaynak olsun diye ScriptableObject'e tasindi.
[CreateAssetMenu(fileName = "PickerSettings", menuName = "Picker3D/Picker Settings")]
public class PickerSettings : ScriptableObject
{
    [Tooltip("Ileri hiz (birim/sn)")]
    public float forwardSpeed = 5f;

    [Tooltip("Yana hareketin ust hizi (birim/sn). Oyunun zorluk dugmesi.")]
    public float lateralSpeed = 9f;

    [Tooltip("Picker merkezinin gidebilecegi en uc x degeri")]
    public float laneLimit = 1.5f;

    [Tooltip("Picker'in yarim genisligi (kollar dahil)")]
    public float halfWidth = 0.95f;
}
