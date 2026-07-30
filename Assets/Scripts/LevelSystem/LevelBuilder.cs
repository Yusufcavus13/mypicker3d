using UnityEngine;

public class LevelBuilder : MonoBehaviour
{
    [SerializeField] private Transform stagesParent;
    [SerializeField] private GameObject stagePrefab;
    [SerializeField] private Renderer startCubeRenderer;
    [SerializeField] private Renderer endCubeRenderer;
    [SerializeField] private Transform endRoad;
    [SerializeField] private LevelData previewLevelData;

    private static MaterialPropertyBlock mpb;

    public void Build(LevelData levelData)
    {
        if (levelData == null || levelData.StageCount == 0)
        {
            Debug.LogError($"[LevelBuilder] {name}: LevelData bos, level kurulamadi.", this);
            return;
        }
        if (stagesParent == null || stagePrefab == null)
        {
            Debug.LogError($"[LevelBuilder] {name}: stagesParent ya da stagePrefab bagli degil.", this);
            return;
        }

        ClearStages();

        float stageStartZ = 0f;
        for (int i = 0; i < levelData.stages.Count; i++)
        {
            StageData stageData = levelData.stages[i];

            GameObject stageObj = Instantiate(stagePrefab, stagesParent, false);
            stageObj.name = "Stage" + (i + 1);

            if (!stageObj.TryGetComponent(out Stage stage))
            {
                Debug.LogError($"[LevelBuilder] {stagePrefab.name} uzerinde Stage bileseni yok.", this);
                continue;
            }

            stage.SetStagePosZ(stageStartZ);
            stage.ApplyData(stageData, i);

            stageStartZ += (stageData.platformLength * 5f) + 5f;
        }

        SetCubeColor(startCubeRenderer, levelData.stages[0].platformColor);
        SetCubeColor(endCubeRenderer, levelData.stages[levelData.StageCount - 1].platformColor);

        if (endRoad != null)
        {
            endRoad.localPosition = new Vector3(endRoad.localPosition.x, endRoad.localPosition.y,
                2.5f + levelData.GetLevelLength());
        }
    }

    private void ClearStages()
    {
        while (stagesParent.childCount > 0)
        {
            Transform child = stagesParent.GetChild(0);
            child.SetParent(null); //childCount hemen dussun, yoksa dongu bitmez

            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
    }

    private void SetCubeColor(Renderer cubeRenderer, Color color)
    {
        if (cubeRenderer == null)
            return;
        if (mpb == null)
            mpb = new MaterialPropertyBlock();

        cubeRenderer.GetPropertyBlock(mpb);
        mpb.SetColor("_AlbedoColor", color);
        mpb.SetColor("_BaseColor", color);
        mpb.SetColor("_Color", color);
        cubeRenderer.SetPropertyBlock(mpb);
    }

    [ContextMenu("Build Preview")]
    private void BuildPreview()
    {
        Build(previewLevelData);
    }
}
