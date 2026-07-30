using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class BallCollecterPlatform : MonoBehaviour
{
    [SerializeField]  TMP_Text collecedStatusText;
    [SerializeField]  GameObject ballBlocker;
    [SerializeField]  Renderer upperCubeRenderer;
    [SerializeField]  Animator myAnim;
    private List<GameObject> collectedBalls = new List<GameObject>();
    private int collectedCount = 0;
    private int collectLimit = 0;
    public static event Action collecterSuccessEvent;
    public static event Action collecterFailedEvent;
    [SerializeField] private Material platformMat;
    public void CollactNewBall(GameObject ballObj)
    {
        collectedBalls.Add(ballObj);

        //buyuk toplar birden fazla sayar
        int ballValue = 1;
        if (ballObj.TryGetComponent(out Ball ball))
            ballValue = ball.Value;

        collectedCount += ballValue;
        SetCollectedText(collectedCount.ToString() + " / " + collectLimit.ToString());
    }
    public void SetCollectLimit(int newLimit)
    {
        collectLimit = newLimit;
    }
    public void SetCollectedText(string text)
    {
        if (collecedStatusText != null)
            collecedStatusText.text = text;
    }
    public void SetPosition(float lengthOfRoad)
    {
        transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y,
             ((lengthOfRoad) * 5f));
    }
    private static MaterialPropertyBlock mpb;

    public void SetUpperCubeColor(Color newColor)
    {
        //MaterialPropertyBlock ile boyuyoruz: yeni materyal uretmeden renk veriyoruz.
        //_AlbedoColor = MK Toon shader'inin ana renk ozelligi.
        if (upperCubeRenderer == null)
            return;
        if (mpb == null)
            mpb = new MaterialPropertyBlock();

        upperCubeRenderer.GetPropertyBlock(mpb);
        mpb.SetColor("_AlbedoColor", newColor);
        mpb.SetColor("_BaseColor", newColor);
        mpb.SetColor("_Color", newColor);
        upperCubeRenderer.SetPropertyBlock(mpb);
    }
    public void CheckCollecterStatus()
    {
        StartCoroutine(AnimDelayCaroutine());
    }
    private IEnumerator AnimDelayCaroutine()
    {
        yield return new WaitForSeconds(1.5f);
        ballBlocker.SetActive(true);
        yield return new WaitForSeconds(0.5f);
        Material explodeMat = platformMat;
        if (upperCubeRenderer != null)
            explodeMat = upperCubeRenderer.material;

        foreach (var b in collectedBalls)
        {
            if (b != null && b.TryGetComponent(out Ball ball))
                ball.Explode(explodeMat);
        }
        if (collectedCount >= collectLimit)
        {
            //pass thru
            yield return new WaitForSeconds(1f);
            myAnim.SetTrigger("Close");
            yield return new WaitForSeconds(2f);
            collecterSuccessEvent?.Invoke();
        }
        else
        {
            //fail
            collecterFailedEvent?.Invoke();
        }
    }

}
