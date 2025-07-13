using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BallData
{
    public Vector2 currentVelocity;
    public Vector2 currentPosition;
    public Transform ballTransform;
    public Transform lastCollisionTransform;
    public RaycastHit2D hit;

    public BallData(Transform _ballTransform)
    {
        ballTransform = _ballTransform;
        currentPosition = ballTransform.position;
        currentVelocity = new();
        lastCollisionTransform = null;
        hit = default;
    }

    public void UpdatePosition()
    {
        if(ballTransform != null) ballTransform.position = currentPosition;
    }
}

public class BallMovementController : MonoBehaviour
{

    [SerializeField] private float ballRadius = 0.09f;
    [SerializeField] private float speed = 5f;
    private List<BallData> balls;
    private int ballCount = 0;
    private GameObject ballPrefab;

    void Start()
    {
        ballPrefab = GameObject.Find("Canvas/Ball");
        balls = new()
        {
            new(ballPrefab.transform)
        };
        ++ballCount;
        SendInRandomDirection(balls[0]);
    }
    public void SpawnNewBall()
    {
        GameObject ball = Instantiate(ballPrefab, ballPrefab.transform.position, Quaternion.identity, ballPrefab.transform.parent);
        ball.tag = "Ball";
        ++ballCount;
        ball.GetComponent<SpriteRenderer>().color = new Color(Random.value, Random.value, Random.value, 1f);
        BallData newBall = new(ball.transform);
        balls.Add(newBall);
        SendInRandomDirection(newBall);
    }

    public void ResetBalls()
    {
        for (int i = 1; i < ballCount; ++i)
        {
            var ball = balls[i];
            Destroy(ball.ballTransform.gameObject);
        }

        balls.Clear();
        balls.Add(new(ballPrefab.transform));
        ballCount = 1;
        SendInRandomDirection(balls[0]);
    }
    void FixedUpdate()
    {
        for (int i = 0; i < ballCount; ++i)
        {
            var currentBall = balls[i];

            float moveDistance = currentBall.currentVelocity.magnitude * Time.fixedDeltaTime;
            Vector2 velNorm = currentBall.currentVelocity.normalized;
            currentBall.hit = Physics2D.CircleCast(
                origin: currentBall.currentPosition,      // Start point
                radius: ballRadius, // Radius of the circle
                direction: velNorm,            // Movement direction (normalized)
                distance: moveDistance, // Distance to check
                layerMask: ~LayerMask.GetMask("Ball") // Exclude Ball layer
            );

            if (currentBall.hit.collider == null || currentBall.hit.transform == currentBall.lastCollisionTransform)
            {
                currentBall.currentPosition += moveDistance * velNorm;
                currentBall.UpdatePosition();
                continue; // No collision detected, exit early
            }

            currentBall.lastCollisionTransform = currentBall.hit.transform;
            // print("Collision with: " + hit.transform.name);

            switch (currentBall.hit.transform.tag)
            {
                case "Wall":
                    WallBounce(currentBall);
                    break;
                case "Brick":
                    BrickBounce(currentBall);
                    StartCoroutine(DestroyAfterPhysics(currentBall.hit.transform.gameObject));
                    break;
                case "Paddle":
                    PaddleBounce(currentBall);
                    break;
                default:
                    Debug.Log("Ball collided with: " + currentBall.hit.transform.name);
                    break;
            }
        }
    }


    void WallBounce(BallData ball)
    {
        Vector2 inDirection = ball.currentVelocity.normalized;
        Vector2 normal = ball.hit.normal;
        ball.currentVelocity = Vector2.Reflect(inDirection, normal) * speed;

        // Move the ball to the point of collision, slightly off the surface
        ball.currentPosition = ball.hit.point + normal * ballRadius;
    }

    void BrickBounce(BallData ball)
    {
        // Get the normal of the collision
        Vector2 inDirection = ball.currentVelocity.normalized;
        Vector2 normal = ball.hit.normal;
        ball.currentVelocity = Vector2.Reflect(inDirection, normal) * speed;

        ball.currentPosition = ball.hit.point + normal * ballRadius;
    }

    void PaddleBounce(BallData ball)
    {
        Vector2 normal = ball.hit.normal;
        float paddleCenter = ball.hit.transform.position.x;
        float hitPoint = ball.hit.point.x;
        float offset = (hitPoint - paddleCenter) / ball.hit.transform.GetComponent<RectTransform>().sizeDelta.x;

        ball.currentVelocity = (Vector2.Reflect(ball.currentVelocity, normal) +
                               new Vector2(offset * 0.5f, 0)).normalized * speed;
        ball.currentPosition = ball.hit.point + normal * ballRadius;
    }

    IEnumerator DestroyAfterPhysics(GameObject obj)
    {
        yield return new WaitForFixedUpdate(); // Wait until physics update finishes
        Destroy(obj);
    }

    public void SendInRandomDirection(BallData ball)
    {
        Vector2 initialDirection = new Vector2(
            Mathf.Cos(Random.Range(0f, Mathf.PI * 0.6f + Mathf.PI * 0.2f)),
            Mathf.Sin(Random.Range(0f, Mathf.PI * 0.6f + Mathf.PI * 0.2f))
        ).normalized;

        ball.currentVelocity = initialDirection * speed;
    }
    
    public int GetBallCount(){ return ballCount; }
}
