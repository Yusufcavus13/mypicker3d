using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;
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
        AnimateCounter(collectedCount);
    }

    private int displayedCount;
    private Tween countTween;
    private Tween punchTween;

    //Sayac aniden ziplamak yerine sayarak cikiyor, yazi da bir "punch" atiyor.
    //Toplar seri halde geldigi icin onceki tween'leri OLDURMEK sart: yoksa
    //ust uste binip yaziyi buyutup birakirlar.
    private void AnimateCounter(int newCount)
    {
        if (collecedStatusText == null)
            return;

        countTween?.Kill();
        countTween = DOTween.To(() => displayedCount, value =>
        {
            displayedCount = value;
            SetCollectedText(value + " / " + collectLimit);
        }, newCount, 0.25f).SetEase(Ease.OutQuad);

        punchTween?.Kill(true);
        punchTween = collecedStatusText.transform
            .DOPunchScale(Vector3.one * 0.22f, 0.18f, 6, 0.7f);
    }
    public void SetCollectLimit(int newLimit)
    {
        collectLimit = newLimit;
        displayedCount = 0;
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
