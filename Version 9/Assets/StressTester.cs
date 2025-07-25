using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StressTester : MonoBehaviour
{
    [SerializeField] private BallMovementController bmc;
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
        bmc = FindFirstObjectByType<BallMovementController>();
        runResults = new();
    }

    void Update()
    {
        if (delay > 0.0f || ballsToAdd == 0)
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
                int numberOfBalls = bmc.GetBallCount();
                Debug.LogWarning($"Average FPS is low, the total number of balls is {numberOfBalls}.");
                delay = 5.0f;
                runResults.Add(numberOfBalls);
                bmc.ResetBalls();
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
            bmc.SpawnNewBalls(ballsToAddNum);
        }
    }
}
