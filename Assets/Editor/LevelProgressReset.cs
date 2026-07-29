using UnityEditor;
using UnityEngine;

public static class LevelProgressReset
{
    [MenuItem("Tools/Picker3D/Level Ilerlemesini Sifirla")]
    public static void ResetProgress()
    {
        PlayerPrefs.DeleteKey("Level");
        PlayerPrefs.DeleteKey("CurrentLevelIndex");
        PlayerPrefs.DeleteKey("NextLevelIndex");
        PlayerPrefs.DeleteKey("IsLevelsSelected"); //eski sistemden kalan anahtar
        PlayerPrefs.Save();

        Debug.Log("[LevelProgressReset] Ilerleme sifirlandi, oyun 1. levelden baslayacak.");
    }
}
