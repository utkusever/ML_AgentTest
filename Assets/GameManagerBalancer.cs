using TMPro;
using UnityEngine;

public class GameManagerBalancer : MonoBehaviour
{
    [Header("References")] public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timerText;
    public Balancer balancer;
    private int score;
    private float timer;
    private Vector3 spawnLocation = new Vector3(0f, 0f, 0.65f);
    private float scoreCooldown = 0;

    void Start()
    {
        GameReset();
    }

    public void GameReset()
    {
        balancer.EndEpisode();

        //reset score
        score = 0;

        //reset timer
        timer = 0f;

        //reset scoreCD
        scoreCooldown = 0;
    }

    private void Update()
    {
        //decrement counter, update timer text, and reset game if time runs out
        timer += Time.deltaTime;
        timerText.text = "Timer: " + timer.ToString("f0");
        scoreCooldown += Time.deltaTime;
        if (scoreCooldown >= 1)
        {
            scoreCooldown = 0;
            IncrementScore();
        }
    }


    void UpdateScoreText()
    {
        scoreText.text = "Score: " + score;
    }

    public void IncrementScore()
    {
        score++;
        UpdateScoreText();
    }
}