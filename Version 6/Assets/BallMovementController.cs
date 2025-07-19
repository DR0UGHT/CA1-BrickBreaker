using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallData
{
    public Vector2 currentVelocity;
    public Vector2 currentPosition;
    public float timeToSkip;
    public Color color;
    public bool draw;
    public BallData()
    {
        currentVelocity = new();
        timeToSkip = 0.0f;
        color = Random.ColorHSV();
        draw = true;
    }
}

public class BallMovementController : MonoBehaviour
{

    [SerializeField] private float ballRadius = 0.09f;
    [SerializeField] private float speed = 5f;
    private List<BallData> balls;
    [SerializeField] int ballCount = 0;
    private LayerMask collisionMask;
    public enum CollisionType
    {
        Wall = 7,
        Brick = 6,
        Paddle = 8,
        Ball = 3
    }
    const int instanceCountMax = 1023;

    //GPU circle draw mesh
    [SerializeField] private Material circleMaterial;
    private MaterialPropertyBlock propertyBlock;
    private Mesh circleMesh;
    private Matrix4x4[] matrices;
    private Vector4[] colors;

    void Start()
    {
        collisionMask = ~LayerMask.GetMask("Ball");
        balls = new()
        {
            new()
        };
        ++ballCount;
        // SendInRandomDirection(balls[0]);
        balls[0].currentPosition = new Vector2(0, 0f);
        SendInDirection(balls[0], new Vector2(1, 0.05f));

        CreateCircleMesh();

        matrices = new Matrix4x4[instanceCountMax];
        Vector3 scale = Vector3.one * (22.5f * ballRadius);
        for (int i = 0; i < instanceCountMax; ++i)
        {
            Matrix4x4 m = Matrix4x4.identity;
            m.m00 = m.m11 = scale.x;
            matrices[i] = m;
        }
        colors = new Vector4[instanceCountMax];

        propertyBlock = new MaterialPropertyBlock();
        propertyBlock.SetVectorArray("_InstColor", colors);
    }

    void CreateCircleMesh()
    {
        circleMesh = new Mesh();

        Vector3[] vertices = new Vector3[4]
        {
            new (-ballRadius/2, -ballRadius/2, 0),
            new (ballRadius/2, -ballRadius/2, 0),
            new (-ballRadius/2, ballRadius/2, 0),
            new (ballRadius/2, ballRadius/2, 0)
        };
        circleMesh.vertices = vertices;

        Vector2[] uv = new Vector2[4]
        {
            new (0, 0),
            new (1, 0),
            new (0, 1),
            new (1, 1)
        };
        circleMesh.uv = uv;

        int[] triangles = new int[6]
        {
            0, 2, 1,
            2, 3, 1
        };
        circleMesh.triangles = triangles;
        circleMesh.hideFlags = HideFlags.DontSave;
        circleMesh.RecalculateNormals();
    }

    public void SpawnNewBalls(int count)
    {
        for (int i = 0; i < count; ++i)
        {
            if (ballCount < balls.Count)
            {
                BallData ball = balls[ballCount - 1];

                ball.draw = true;
                ball.currentPosition = balls[0].currentPosition;

                SendInRandomDirection(ball);
            }
            else
            {
                BallData newBall = new()
                {
                    currentPosition = balls[0].currentPosition
                };
                balls.Add(newBall);
                SendInRandomDirection(newBall);
            }

            ++ballCount;
        }
        propertyBlock.SetVectorArray("_InstColor", colors);
    }

    public void ResetBalls()
    {
        for (int i = 0; i < ballCount; ++i)
        {
            var ball = balls[i];
            ball.timeToSkip = 0;

            if (i == 0) continue;
            ball.draw = false;
        }

        balls[0].currentPosition = Vector2.zero;
        ballCount = 1;
        SendInRandomDirection(balls[0]);
    }

    void Update()
    {
        DrawBalls();
    }

    void DrawBalls() {
        int drawn = 0;
        while (drawn < ballCount)
        {
            int count = Mathf.Min(instanceCountMax, ballCount - drawn);

            for (int i = 0; i < count; ++i)
            {

                BallData ball = balls[drawn + i];
                if (!ball.draw) break;

                Matrix4x4 m = matrices[i];
                m.m03 = ball.currentPosition.x;
                m.m13 = ball.currentPosition.y;
                matrices[i] = m;

                colors[i] = ball.color;
            }
            
            Graphics.RenderMeshInstanced(
                rparams: new RenderParams(circleMaterial) { matProps = propertyBlock },
                mesh: circleMesh,
                submeshIndex: 0,
                instanceData: matrices,
                instanceCount: count
            );

            drawn += count;
        }
    }

    void FixedUpdate()
    {
        float moveDistance = speed * Time.fixedDeltaTime;
        RaycastHit2D[] res = new RaycastHit2D[1];
        float minimumSkipDist = Mathf.Max(ballRadius * 2, 0.1f);
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
                distance: 5, // Distance to check
                layerMask: collisionMask // Exclude Ball layer
            );
            
            if (res[0].distance > ballRadius + moveDistance && res[0].distance < minimumSkipDist)
            {
                MoveBall(currentBall, moveDistance, velNorm);
                continue; // No collision detected, exit early
            }
            else if (hits == 0 || res[0].distance > minimumSkipDist)
            {
                currentBall.timeToSkip = (res[0].distance - minimumSkipDist) / speed;
                MoveBall(currentBall, moveDistance, velNorm);
                continue;
            }

            switch (res[0].transform.gameObject.layer)
            {
                case (int)CollisionType.Wall:
                    WallBounce(currentBall, res[0]);
                    break;
                case (int)CollisionType.Brick:
                    BrickBounce(currentBall, res[0]);
                    StartCoroutine(DestroyAfterPhysics(res[0].transform.gameObject));
                    break;
                case (int)CollisionType.Paddle:
                    PaddleBounce(currentBall, res[0]);
                    break;
                default:
                    Debug.Log("Ball collided with: " + res[0].transform.name);
                    break;
            }
        }
    }

    void MoveBall(BallData ball, float distance, Vector2 dir)
    {
        ball.currentPosition += distance * dir;
    }

    void WallBounce(BallData ball, RaycastHit2D hit)
    {
        Vector2 inDirection = ball.currentVelocity.normalized;
        Vector2 normal = hit.normal;
        ball.currentVelocity = Vector2.Reflect(inDirection, normal) * speed;

        // Move the ball to the point of collision, slightly off the surface
        ball.currentPosition = hit.point + normal * ballRadius;
    }

    void BrickBounce(BallData ball, RaycastHit2D hit)
    {
        // Get the normal of the collision
        Vector2 inDirection = ball.currentVelocity.normalized;
        Vector2 normal = hit.normal;
        ball.currentVelocity = Vector2.Reflect(inDirection, normal) * speed;

        ball.currentPosition = hit.point + normal * ballRadius;
    }

    void PaddleBounce(BallData ball, RaycastHit2D hit)
    {
        Vector2 normal = hit.normal;
        float paddleCenter = hit.transform.position.x;
        float hitPoint = hit.point.x;
        float offset = (hitPoint - paddleCenter) / hit.transform.GetComponent<RectTransform>().sizeDelta.x;

        ball.currentVelocity = (Vector2.Reflect(ball.currentVelocity, normal) +
                               new Vector2(offset * 0.5f, 0)).normalized * speed;
        ball.currentPosition = hit.point + normal * ballRadius;
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

    public void SendInDirection(BallData ball, Vector2 dir)
    {
        ball.currentVelocity = dir.normalized * speed;
    }
    
    public int GetBallCount() { return ballCount; }

    void OnDrawGizmos()
    {
        // return;
        if (balls == null) return;

        Gizmos.color = Color.red;

        foreach (var ball in balls)
        {
            if (ball == null) continue;

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
