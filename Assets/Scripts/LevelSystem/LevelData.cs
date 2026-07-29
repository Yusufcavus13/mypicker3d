using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class StageData
{
    public int spawnedBallCount = 10;   
    public int targetBallCount = 10;   
    public Color platformColor = Color.white;
    public float platformLength = 1f;  
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
