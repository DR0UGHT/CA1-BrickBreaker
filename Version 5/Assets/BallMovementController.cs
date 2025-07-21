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
    public float timeToSkip;
    public BallData(Transform _ballTransform)
    {
        ballTransform = _ballTransform;
        currentPosition = ballTransform.position;
        currentVelocity = new();
        lastCollisionTransform = null;
        hit = default;
        timeToSkip = 0.0f;
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
    public LayerMask collisionMask;
    public enum CollisionType
    {
        Wall = 7,
        Brick = 6,
        Paddle = 8,
        Ball = 3
    }

    void Awake()
    {
        collisionMask = ~LayerMask.GetMask("Ball");
        ballPrefab = GameObject.Find("Canvas/Ball");
        balls = new()
        {
            new(ballPrefab.transform)
        };
        ++ballCount;
        balls[0].currentPosition = Vector2.zero;
        balls[0].UpdatePosition();
        SendInRandomDirection(balls[0]);
    }
    public void SpawnNewBall()
    {
        if (ballCount < balls.Count)
        {
            int i = ballCount - 1;

            balls[i].ballTransform.gameObject.SetActive(true);
            balls[i].currentPosition = balls[0].currentPosition;
            balls[i].UpdatePosition();

            SendInRandomDirection(balls[i]);
        }
        else
        {
            GameObject ball = Instantiate(ballPrefab, ballPrefab.transform.position, Quaternion.identity, ballPrefab.transform.parent);
            ball.GetComponent<SpriteRenderer>().color = new Color(Random.value, Random.value, Random.value, 1f);
            BallData newBall = new(ball.transform);
            balls.Add(newBall);
            SendInRandomDirection(newBall);
        }

        ++ballCount;
    }

    public void ResetBalls()
    {
        for (int i = 0; i < ballCount; ++i)
        {
            var ball = balls[i];
            ball.timeToSkip = 0;
            ball.lastCollisionTransform = null;
            ball.hit = default;

            if (i == 0) continue;
            ball.ballTransform.gameObject.SetActive(false);
        }

        ballCount = 1;
        SendInRandomDirection(balls[0]);
        balls[0].currentPosition = Vector2.zero;
        balls[0].timeToSkip = 0;
    }
    void FixedUpdate()
    {
        if (Time.time < 0.1f) return;

        float moveDistance = speed * Time.fixedDeltaTime;
        RaycastHit2D[] res = new RaycastHit2D[1];
        for (int i = 0; i < ballCount; ++i)
        {
            var currentBall = balls[i];
            Vector2 velNorm = currentBall.currentVelocity.normalized;

            if (currentBall.timeToSkip > 0)
            {
                currentBall.timeToSkip -= Time.fixedDeltaTime;
                MoveBall(currentBall, moveDistance, velNorm);
                continue;
            }
            int hits = Physics2D.CircleCastNonAlloc(
                origin: currentBall.currentPosition,      // Start point
                radius: ballRadius, // Radius of the circle
                direction: velNorm,            // Movement direction (normalized)
                results: res,
                distance: 15, // Distance to check
                layerMask: collisionMask // Exclude Ball layer
            );

            if (res[0].distance > ballRadius + moveDistance && res[0].distance < ballRadius * 3)
            {
                MoveBall(currentBall, moveDistance, velNorm);
                continue; // No collision detected, exit early
            }
            else if (hits == 0 || res[0].distance >= ballRadius * 3)
            {
                currentBall.timeToSkip = (res[0].distance - (ballRadius * 3)) / speed;
                MoveBall(currentBall, moveDistance, velNorm);
                continue;
            }

            currentBall.lastCollisionTransform = currentBall.hit.transform;
            currentBall.hit = res[0];
            // print("Collision with: " + hit.transform.name);

            switch (currentBall.hit.transform.gameObject.layer)
            {
                case (int) CollisionType.Wall:
                    WallBounce(currentBall);
                    break;
                case (int) CollisionType.Brick:
                    BrickBounce(currentBall);
                    StartCoroutine(DestroyAfterPhysics(currentBall.hit.transform.gameObject));
                    break;
                case (int) CollisionType.Paddle:
                    PaddleBounce(currentBall);
                    break;
                default:
                    Debug.Log("Ball collided with: " + currentBall.hit.transform.name);
                    break;
            }
        }
    }

    void MoveBall(BallData ball, float distance, Vector2 dir)
    {
        ball.currentPosition += distance * dir;
        ball.UpdatePosition();
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
        ball.timeToSkip = 0;
    }

    public void SendInDirection(BallData ball, Vector2 dir)
    {
        ball.currentVelocity = dir.normalized * speed;
        ball.timeToSkip = 0;
    }
    
    public int GetBallCount() { return ballCount; }

    void OnDrawGizmos()
    {
        return;
        if (balls == null) return;

        Gizmos.color = Color.red;

        foreach (var ball in balls)
        {
            if (ball == null || ball.ballTransform == null) continue;

            Vector2 position = ball.currentPosition;
            Vector2 velocity = ball.currentVelocity;

            if (velocity == Vector2.zero) continue;

            Vector2 direction = velocity.normalized;
            Gizmos.color = Color.red;
            Gizmos.DrawRay(position, direction);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(position, ballRadius);
        }
    }
}
