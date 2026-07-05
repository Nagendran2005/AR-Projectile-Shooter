using TMPro;
using UnityEngine;
using System.Collections;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    [Header("UI")]
    [SerializeField] private TMP_Text scoreText;

    [Header("Animation")]
    [SerializeField] private float animationDuration = 0.25f;

    private int score = 0;

    private Coroutine animationRoutine;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        UpdateScoreText();
    }

    public void AddScore(int amount)
    {
        score += amount;

        UpdateScoreText();

        if (animationRoutine != null)
            StopCoroutine(animationRoutine);

        animationRoutine = StartCoroutine(PopAnimation());
    }

    void UpdateScoreText()
    {
        scoreText.text = score.ToString();
    }

    IEnumerator PopAnimation()
    {
        Vector3 originalScale = scoreText.transform.localScale;

        Vector3 targetScale = originalScale * 1.25f;

        float timer = 0;

        while (timer < animationDuration)
        {
            timer += Time.deltaTime;

            float t = timer / animationDuration;

            scoreText.transform.localScale =
                Vector3.Lerp(originalScale, targetScale, t);

            yield return null;
        }

        timer = 0;

        while (timer < animationDuration)
        {
            timer += Time.deltaTime;

            float t = timer / animationDuration;

            scoreText.transform.localScale =
                Vector3.Lerp(targetScale, originalScale, t);

            yield return null;
        }

        scoreText.transform.localScale = originalScale;
    }

    public int GetScore()
    {
        return score;
    }

    public void ResetScore()
    {
        score = 0;
        UpdateScoreText();
    }
}