using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class StageData
{
    public int spawnedBallCount = 10;
    public int targetBallCount = 10;
    public Color platformColor = Color.white;
    public float platformLength = 1f;

    //Kacinin buyuk top olacagi. Buyuk top 3 sayar ama daha genise yayilmis
    //olabildigi icin ona gitmek risk demek.
    public int bigBallCount = 0;

    //Toplarin yayilma genisligi. 0 = RoadPlatform'un varsayilanini kullan.
    //Picker en fazla 2.26'ya uzanabildigi icin 4.5'ten buyuk vermek, bazi
    //toplarin hic toplanamamasi anlamina gelir - zorluk ayari tam olarak burada.
    public float ballSpreadWidth = 0f;

    //Yola kac engel konacak. Engeller duvara yaslanir, picker acik taraftan
    //dolanmak zorunda kalir - yani top toplama cizgisini bozmak zorunda.
    public int obstacleCount = 0;
}

[CreateAssetMenu(fileName = "NewLevel", menuName = "Picker3D/Level Data")]
public class LevelData : ScriptableObject
{
    public List<StageData> stages = new List<StageData>();

    public int StageCount
    {
        get { return stages != null ? stages.Count : 0; }
    }

    public float GetLevelLength()
    {
        float levelLength = 0f;
        if (stages == null)
            return levelLength;

        foreach (StageData stage in stages)
        {
            levelLength += (stage.platformLength * 5f) + 5f;
        }
        return levelLength;
    }
}
