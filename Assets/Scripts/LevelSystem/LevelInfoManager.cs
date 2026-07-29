using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

//ExecuteAlways: baslangic/bitis kupleri de MaterialPropertyBlock ile boyaniyor.
//MPB sahneye kaydedilmediginden, renkleri sahne her acildiginda yeniden uyguluyoruz.
[ExecuteAlways]
public class LevelInfoManager : MonoBehaviour
{
    [SerializeField] private Transform stagesParent;
    [SerializeField] private List<GameObject> stages;
    [SerializeField] private GameObject stagePrefab;
    [SerializeField] private Renderer startCubeRenderer;
    [SerializeField] private Renderer endCubeRenderer;
    [SerializeField] private GameObject endCube;
    [SerializeField] private Material platformMat;

    private int stageCount = 0;
    public int GetStagesCount()
    {
        stageCount = stagesParent.childCount;
        return stageCount;
    }
#if UNITY_EDITOR
    //--- editor-only level authoring: called from LevelDesigner, never at runtime ---
    public void SetStagesCount(int newStageCount)
    {
        newStageCount = Mathf.Max(1, newStageCount); //at least one stage must remain

        int currentCount = GetStagesCount();
        if (newStageCount == currentCount)
            return;

        if (newStageCount > currentCount)
            AddStages(newStageCount - currentCount);
        else
            RemoveStages(currentCount - newStageCount);

        stageCount = GetStagesCount(); //read back the real count
    }

    private void AddStages(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            GameObject newStage = (GameObject)PrefabUtility.InstantiatePrefab(stagePrefab);
            newStage.transform.SetParent(stagesParent);
            newStage.transform.position = stagesParent.transform.position;

            stages.Add(newStage);
        }
        EditorUtility.SetDirty(this);
    }

    private void RemoveStages(int amount)
    {
        amount = Mathf.Min(amount, stages.Count - 1); //never remove the last stage
        if (amount <= 0)
        {
            Debug.LogWarning("[LevelInfoManager] Son durak silinemez, en az 1 durak kalmali.", this);
            return;
        }

        //resolve the real prefab path before touching anything
        GameObject source = PrefabUtility.GetCorrespondingObjectFromOriginalSource(gameObject);
        if (source == null)
        {
            Debug.LogError("[LevelInfoManager] Prefab kaynagi bulunamadi - silme iptal edildi.", this);
            return;
        }

        string prefabPath = AssetDatabase.GetAssetPath(source);
        if (string.IsNullOrEmpty(prefabPath))
        {
            Debug.LogError("[LevelInfoManager] Prefab yolu okunamadi - silme iptal edildi.", this);
            return;
        }

        //unpack is required to be able to delete children of a prefab instance
        PrefabUtility.UnpackPrefabInstance(gameObject, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);

        for (int i = 0; i < amount; i++)
        {
            int last = stages.Count - 1;
            GameObject stageWillRemove = stages[last];
            stages.RemoveAt(last);
            if (stageWillRemove != null)
                DestroyImmediate(stageWillRemove);
        }

        //overwrite the same asset: the file is never deleted so the GUID survives
        PrefabUtility.SaveAsPrefabAssetAndConnect(gameObject, prefabPath, InteractionMode.AutomatedAction);
        AssetDatabase.SaveAssets();
    }
#endif

    private static MaterialPropertyBlock mpb;

    private void SetColorsOfStartEnd()
    {
        //MaterialPropertyBlock ile boyuyoruz: yeni materyal URETMEDEN renk veriyoruz.
        //Eski hali (new Material + .material) prefab'a kirik referans kaydedip
        //baslangic/bitis kuplerini MAGENTA (pembe) yapiyordu. Artik yapmaz.
        if (stages == null || stages.Count == 0)
            return;

        Stage firstStage = stages[0] != null ? stages[0].GetComponent<Stage>() : null;
        Stage lastStage = stages[stages.Count - 1] != null ? stages[stages.Count - 1].GetComponent<Stage>() : null;
        if (firstStage == null || lastStage == null)
            return;

        if (mpb == null)
            mpb = new MaterialPropertyBlock();

        ApplyColor(startCubeRenderer, firstStage.PlatformColor);
        ApplyColor(endCubeRenderer, lastStage.PlatformColor);
    }

    private void ApplyColor(Renderer renderer, Color color)
    {
        if (renderer == null)
            return;
        renderer.GetPropertyBlock(mpb);
        //_AlbedoColor = MK Toon shader'inin ana renk ozelligi (_BaseColor DEGIL)
        mpb.SetColor("_AlbedoColor", color);
        mpb.SetColor("_BaseColor", color);
        mpb.SetColor("_Color", color);
        renderer.SetPropertyBlock(mpb);
    }
    public void SetupStartEndObjects()
    {
        SetColorsOfStartEnd();
        float endCubeZPos = 2.5f;
        foreach (var stage in stages)
        {
            endCubeZPos += (stage.gameObject.GetComponent<Stage>().PlatformLength * 5f) + 5f;
        }
        endCube.transform.localPosition = new Vector3(endCube.transform.localPosition.x,
            endCube.transform.localPosition.y, endCubeZPos);
    }
    public void UpdateStagesInfo()
    {
        foreach (GameObject stage in stages)
        {
            stage.GetComponent<Stage>().SetupStage();
        }
        CalcStagesPositions();
    }
    private void CalcStagesPositions()
    {
        float lastPlatformLength = 0f;
        for (int i = 0; i < stages.Count; i++)
        {
            Stage curStage = stages[i].GetComponent<Stage>();
            if (i == 0)
            {
                curStage.SetStagePosZ(0f);
                lastPlatformLength = ((curStage.PlatformLength) * 5f) + 5f;
            }
            else
            {
                curStage.SetStagePosZ(lastPlatformLength);
                float localStartPosZ = lastPlatformLength + ((curStage.PlatformLength) * 5f) + 5f;
                lastPlatformLength = localStartPosZ;
            }
        }
    }
    public float GetLevelLength()
    {
        float levelLength = 0f;
        foreach (var s in stages)
        {
            levelLength += (s.GetComponent<Stage>().PlatformLength * 5f) + 5f;
        }
        return levelLength;
    }
    public Stage GetStage(int index)
    {
        return stages[index].gameObject.GetComponent<Stage>();
    }
    private void OnEnable()
    {
        LevelManager.levelLoadedEvent += SetColorsOfStartEnd;
        SetColorsOfStartEnd(); //edit modunda da renkler kaybolmasin
    }
    private void OnDisable()
    {
        LevelManager.levelLoadedEvent -= SetColorsOfStartEnd;
    }
}
