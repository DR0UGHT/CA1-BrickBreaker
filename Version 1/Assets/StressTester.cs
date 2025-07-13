using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StressTester : MonoBehaviour
{
    [SerializeField] private GameObject ballPrefab;
    [SerializeField] private int numberOfBalls = 0;
    [SerializeField] private float timeWindow = 5f;
    [SerializeField] private int ballsToAdd = 1;
    private int ballsToAddNum = 1;
    private readonly Queue<float> frameTimes = new();
    private float totalTime = 0f;
    public float AverageFPS { get; private set; } = 0f;
    private CreateBrickBreakerLayout sam;
    [SerializeField] private int totalRunsForAverage;

    [SerializeField] private List<int> runResults = new();
    [SerializeField] private float delay = 0.0f;

    void Start()
    {
        delay = 0.0f;
        sam = FindFirstObjectByType<CreateBrickBreakerLayout>();
        runResults = new();
    }

    void Update()
    {
        if (delay > 0.0f)
        {
            delay -= Time.deltaTime;
            return;
        }

        float deltaTime = Time.unscaledDeltaTime;

        frameTimes.Enqueue(deltaTime);
        totalTime += deltaTime;

        // Remove frames outside the time window
        while (totalTime > timeWindow && frameTimes.Count > 0)
        {
            totalTime -= frameTimes.Dequeue();
        }

        // Calculate average FPS
        if (frameTimes.Count > 0)
        {
            float averageDelta = totalTime / frameTimes.Count;
            AverageFPS = 1f / averageDelta;
        }

        if (Time.unscaledTime % timeWindow < deltaTime && Time.time >= timeWindow && frameTimes.Count > 50)
        {
            if (AverageFPS < 30f)
            {
                Debug.LogWarning($"Average FPS is low, the total number of balls is {numberOfBalls}.");
                delay = 5.0f;
                runResults.Add(numberOfBalls);
                numberOfBalls = 0;
                GameObject.FindGameObjectsWithTag("Ball").ToList().ForEach(x => Destroy(x));
                // sam.RelayBricks();

                if (runResults.Count == totalRunsForAverage)
                {
                    Debug.LogAssertion($"The average amount of balls that could spawn were -> {Mathf.RoundToInt(runResults.Sum() / runResults.Count)}");
                    Time.timeScale = 0.0f;
                    Destroy(this);
                }

                return;
            }
        }

        if (Time.unscaledTime % 1f < deltaTime)
        {
            ballsToAddNum = Mathf.RoundToInt(Mathf.Lerp(1, ballsToAdd, AverageFPS / 60.0f));
            
            SpawnBall(ballsToAddNum);
        }
    }

    private void SpawnBall(int amount)
    {
        for (int i = 0; i < ballsToAddNum; ++i)
        {
            GameObject ball = Instantiate(ballPrefab, ballPrefab.transform.position, Quaternion.identity, ballPrefab.transform.parent);
            ball.SetActive(true);
            ball.tag = "Ball";
            ball.GetComponent<Ball>().SendInRandomDirection();
            ++numberOfBalls;
            ball.GetComponent<UnityEngine.UI.Image>().color = new Color(Random.value, Random.value, Random.value, 1f);
        }
    }
}
