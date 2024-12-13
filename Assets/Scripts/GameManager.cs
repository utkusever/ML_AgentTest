using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("References")] public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timerText;
    public PlayerController playerController;
    public GameObject targetPrefab;

    [HideInInspector] public GameObject currentTarget;
    private int score;
    private float timer;
    private Vector3 spawnLocation = new Vector3(0f, 0f, 0.65f);

    void Start()
    {
        GameReset();
    }

    public void GameReset()
    {
        playerController.EndEpisode();

        SpawnTarget();
        playerController.Reset();

        //reset score
        score = 0;
        UpdateScoreText();

        //reset timer
        timer = 10f;
    }

    private void Update()
    {
        //decrement counter, update timer text, and reset game if time runs out
        timer -= Time.deltaTime;
        timerText.text = "Timer: " + timer.ToString("f0");

        if (timer <= 0f)
        {
            GameReset();
        }
    }


    public void SpawnTarget()
    {
        if (currentTarget)
        {
            Destroy(currentTarget);
        }

        //get a random y value and spawn the target (flower) at that height
        spawnLocation.y = Random.Range(1f, 5f);
        playerController.targetYPosition = spawnLocation.y;
        currentTarget = Instantiate(targetPrefab, transform.position + spawnLocation, Quaternion.identity);
    }

    public void TargetHit()
    {
        IncrementScore();
        SpawnTarget();
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