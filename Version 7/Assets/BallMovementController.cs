using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BallData
{
    public Vector2 currentVelocity;
    public float timeToSkip;
    public Color color;
    public bool draw;
    public CollisionInfo hit;
    public Circle2D circle;
    public BallData(bool makeWhite = false)
    {
        currentVelocity = new();
        timeToSkip = 0.0f;
        if(makeWhite)
            color = new Vector4(0.95f, 0.95f, 0.95f, 1.0f);
        else
            color = new Vector4(Random.Range(0, 0.95f), Random.Range(0, 0.95f), Random.Range(0, 0.95f), 1.0f);
        draw = true;
        hit = null;
    }

    public bool CheckForNextCollision(Vector2 circleCenter, float radius, Vector2 dir, Shape2D[] shapesToCheck, out float distance, float maxDistToCheck)
    {
        float checkedDist = radius;
        Vector2 posToCheck = circleCenter;
        Vector2 moveAmount = radius * dir;
        while (checkedDist < maxDistToCheck)
        {
            for (int i = 0; i < shapesToCheck.Length; i++)
            {
                if (shapesToCheck[i].Intersects(circle))
                {
                    distance = checkedDist;
                    return true;
                }
            }

            posToCheck += moveAmount;
            checkedDist += radius;
        }

        distance = maxDistToCheck;
        return false;
    }
}

public class CollisionInfo {
    public Vector2 normal;
    public Vector2 hitPoint;
    public Shape2D lastHit;

    public CollisionInfo()
    {
        normal = Vector2.zero;
        hitPoint = Vector2.zero;
        lastHit = null;
    }

    public CollisionInfo(Vector2 nor, Vector2 hit, Shape2D las)
    {
        normal = nor;
        hitPoint = hit;
        lastHit = las;
    }
}

public abstract class Shape2D
{
    public int layer;
    public bool draw;

    public abstract bool Intersects(Shape2D other);
    public abstract bool IntersectsWithCircle(Circle2D circle);
    public abstract bool IntersectsWithBox(Box2D box);

    public abstract bool GetCollisionInfo(Shape2D other, out Vector2 hitPoint, out Vector2 normal);
    public abstract bool GetCollisionInfoWithCircle(Circle2D circle, out Vector2 hitPoint, out Vector2 normal);
    public abstract bool GetCollisionInfoWithBox(Box2D circle, out Vector2 hitPoint, out Vector2 normal);
}


public class Box2D : Shape2D
{
    public Rect boxShape;

    public Box2D(Rect _boxShape)
    {
        boxShape = _boxShape;
        draw = true;
    }

    public override bool Intersects(Shape2D other)
    {
        return other.IntersectsWithBox(this);
    }
    public override bool IntersectsWithCircle(Circle2D circle)
    {
        Vector2 circleCenter = circle.currentPosition;

        float closestX = Mathf.Clamp(circleCenter.x, boxShape.xMin, boxShape.xMax);
        float closestY = Mathf.Clamp(circleCenter.y, boxShape.yMin, boxShape.yMax);

        float dx = circleCenter.x - closestX;
        float dy = circleCenter.y - closestY;
        float distanceSquared = dx * dx + dy * dy;

        return distanceSquared <= circle.radius * circle.radius;
    }
    public override bool IntersectsWithBox(Box2D box)
    {
        Rect a = boxShape;
        Rect b = box.boxShape;
        return !(a.xMax < b.xMin || a.xMin > b.xMax || a.yMax < b.yMin || a.yMin > b.yMax);
    }



    public override bool GetCollisionInfo(Shape2D other, out Vector2 hitPoint, out Vector2 normal)
    {
        return other.GetCollisionInfoWithBox(this, out hitPoint, out normal);
    }

    public override bool GetCollisionInfoWithBox(Box2D other, out Vector2 hitPoint, out Vector2 normal)
    {
        if (!other.Intersects(this))
        {
            hitPoint = Vector2.zero;
            normal = Vector2.zero;
            return false;
        }

        Rect a = boxShape;
        Rect b = other.boxShape;

        float overlapX = Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin);
        float overlapY = Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin);

        if (overlapX < overlapY)
        {
            normal = a.center.x < b.center.x ? Vector2.left : Vector2.right;
            float x = normal == Vector2.left ? a.xMax : a.xMin;
            hitPoint = new Vector2(x, (Mathf.Max(a.yMin, b.yMin) + Mathf.Min(a.yMax, b.yMax)) / 2f);
        }
        else
        {
            normal = a.center.y < b.center.y ? Vector2.down : Vector2.up;
            float y = normal == Vector2.down ? a.yMax : a.yMin;
            hitPoint = new Vector2((Mathf.Max(a.xMin, b.xMin) + Mathf.Min(a.xMax, b.xMax)) / 2f, y);
        }

        return true;
    }
    public override bool GetCollisionInfoWithCircle(Circle2D other, out Vector2 hitPoint, out Vector2 normal)
    {
        if (!Intersects(other))
        {
            hitPoint = Vector2.zero;
            normal = Vector2.zero;
            return false;
        }

        Vector2 circleCenter = other.currentPosition;
        float radius = other.radius;
        Rect box = boxShape;

        float closestX = Mathf.Clamp(circleCenter.x, box.xMin, box.xMax);
        float closestY = Mathf.Clamp(circleCenter.y, box.yMin, box.yMax);
        Vector2 closestPoint = new Vector2(closestX, closestY);

        Vector2 toCenter = circleCenter - closestPoint;
        float distanceSq = toCenter.sqrMagnitude;

        if (distanceSq > Mathf.Epsilon)
        {
            normal = toCenter.normalized;
            hitPoint = closestPoint;
            return true;
        }

        float left = Mathf.Abs(circleCenter.x - box.xMin);
        float right = Mathf.Abs(circleCenter.x - box.xMax);
        float bottom = Mathf.Abs(circleCenter.y - box.yMin);
        float top = Mathf.Abs(circleCenter.y - box.yMax);

        float min = Mathf.Min(left, right, bottom, top);

        normal = min == left ? Vector2.left :
                 min == right ? Vector2.right :
                 min == bottom ? Vector2.down :
                                 Vector2.up;

        hitPoint = min == left ? new Vector2(circleCenter.x - radius, circleCenter.y) :
                   min == right ? new Vector2(circleCenter.x + radius, circleCenter.y) :
                   min == bottom ? new Vector2(circleCenter.x, circleCenter.y - radius) :
                                   new Vector2(circleCenter.x, circleCenter.y + radius);

        return true;
    }
}
public class Circle2D : Shape2D
{
    public Vector2 currentPosition;
    public float radius;

    public Circle2D(Vector2 curpos, float rad)
    {
        currentPosition = curpos;
        radius = rad;
    }

    public override bool Intersects(Shape2D other)
    {
        return other.IntersectsWithCircle(this);
    }
    public override bool IntersectsWithCircle(Circle2D circle)
    {

        Vector2 circleA = currentPosition;
        Vector2 circleB = circle.currentPosition;
        float dx = circleB.x - circleA.x;
        float dy = circleB.y - circleA.y;
        float distanceSq = dx * dx + dy * dy;
        float radiusSum = circle.radius + radius;

        return distanceSq <= radiusSum * radiusSum;
    }
    public override bool IntersectsWithBox(Box2D box)
    {
        Vector2 circleCenter = currentPosition;
        Rect boxShape = box.boxShape;
        float closestX = Mathf.Clamp(circleCenter.x, boxShape.xMin, boxShape.xMax);
        float closestY = Mathf.Clamp(circleCenter.y, boxShape.yMin, boxShape.yMax);

        float dx = circleCenter.x - closestX;
        float dy = circleCenter.y - closestY;
        float distanceSquared = dx * dx + dy * dy;

        return distanceSquared <= radius * radius;
    }

    public override bool GetCollisionInfo(Shape2D other, out Vector2 hitPoint, out Vector2 normal)
    {
        return other.GetCollisionInfoWithCircle(this, out hitPoint, out normal);
    }
    public override bool GetCollisionInfoWithBox(Box2D other, out Vector2 hitPoint, out Vector2 normal)
    {
        if (!other.Intersects(this))
        {
            hitPoint = Vector2.zero;
            normal = Vector2.zero;
            return false;
        }

        Vector2 circleCenter = currentPosition;
        Rect box = other.boxShape;

        float closestX = Mathf.Clamp(circleCenter.x, box.xMin, box.xMax);
        float closestY = Mathf.Clamp(circleCenter.y, box.yMin, box.yMax);
        Vector2 closestPoint = new Vector2(closestX, closestY);

        Vector2 toCircle = circleCenter - closestPoint;
        float distanceSq = toCircle.sqrMagnitude;

        if (distanceSq > Mathf.Epsilon)
        {
            normal = toCircle.normalized;
            hitPoint = closestPoint;
            return true;
        }

        float left = Mathf.Abs(circleCenter.x - box.xMin);
        float right = Mathf.Abs(circleCenter.x - box.xMax);
        float bottom = Mathf.Abs(circleCenter.y - box.yMin);
        float top = Mathf.Abs(circleCenter.y - box.yMax);

        float min = Mathf.Min(left, right, bottom, top);

        normal = min == left ? Vector2.left :
                 min == right ? Vector2.right :
                 min == bottom ? Vector2.down :
                                 Vector2.up;

        hitPoint = min == left ? new Vector2(box.xMin, circleCenter.y) :
                   min == right ? new Vector2(box.xMax, circleCenter.y) :
                   min == bottom ? new Vector2(circleCenter.x, box.yMin) :
                                   new Vector2(circleCenter.x, box.yMax);

        return true;
    }

    public override bool GetCollisionInfoWithCircle(Circle2D other, out Vector2 hitPoint, out Vector2 normal)
    {
        Vector2 delta = currentPosition - other.currentPosition;
        float distanceSq = delta.sqrMagnitude;
        float combinedRadius = radius + other.radius;

        if (distanceSq > combinedRadius * combinedRadius)
        {
            hitPoint = Vector2.zero;
            normal = Vector2.zero;
            return false;
        }

        if (distanceSq < Mathf.Epsilon)
        {
            normal = Vector2.up;
            hitPoint = currentPosition + normal * radius;
            return true;
        }

        float distance = Mathf.Sqrt(distanceSq);
        normal = delta / distance;

        hitPoint = other.currentPosition + normal * other.radius;

        return true;
    }

}

public class Buffer
{
    public const int instanceCountMax = 1023;

    public Material mat;
    public MaterialPropertyBlock propertyBlock;
    public Mesh mesh;
    public Matrix4x4[] matrices;
    public Vector4[] colors;

    public Buffer(Material _mat)
    {
        mat = _mat;
        propertyBlock = new MaterialPropertyBlock();
        matrices = new Matrix4x4[instanceCountMax];
        colors = new Vector4[instanceCountMax];
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

    private Buffer ballsBuffer;
    private Buffer wallsBuffer;
    private Box2D[] walls;
    [SerializeField] private Material material;

    void Start()
    {
        balls = new List<BallData>();
        BallData newBall = new(true)
        {
            circle = new Circle2D(Vector2.zero, ballRadius)
        };
        balls.Add(newBall);

        SendInDirection(balls[0], new Vector2(1, 0.05f));
        ballCount = 1;

        walls = new Box2D[4];

        float screenHeightHalf = Camera.main.orthographicSize;
        float screenWidthHalf = screenHeightHalf * (Screen.width / (float)Screen.height);

        float screenWidth = screenWidthHalf * 2f;
        float screenHeight = screenHeightHalf * 2f;

        float wallThickness = screenWidth / 80f;


        walls[0] = new(new Rect(-screenWidthHalf, -screenHeightHalf, wallThickness, screenHeight));
        walls[1] = new(new Rect(screenWidthHalf - wallThickness, -screenHeightHalf, wallThickness, screenHeight));
        walls[2] = new(new Rect(-screenWidthHalf, -screenHeightHalf, screenWidth, wallThickness));
        walls[3] = new(new Rect(-screenWidthHalf, screenHeightHalf - wallThickness, screenWidth, wallThickness));

        ballsBuffer = new(material)
        {
            matrices = new Matrix4x4[Buffer.instanceCountMax],
            colors = new Vector4[Buffer.instanceCountMax],
            propertyBlock = new MaterialPropertyBlock()
        };

        wallsBuffer = new(material)
        {
            matrices = new Matrix4x4[Buffer.instanceCountMax],
            colors = new Vector4[Buffer.instanceCountMax],
            propertyBlock = new MaterialPropertyBlock()
        };

        Vector3 scale = Vector3.one * (22.5f * ballRadius);

        for (int i = 0; i < Buffer.instanceCountMax; ++i)
        {
            Matrix4x4 m = Matrix4x4.identity;
            m.m00 = m.m11 = scale.x;
            ballsBuffer.matrices[i] = m;
        }

        for (int i = 0; i < walls.Count(); ++i)
        {
            Vector2 center = walls[i].boxShape.center;
            wallsBuffer.matrices[i] = Matrix4x4.TRS(center, Quaternion.identity, walls[i].boxShape.size);
            wallsBuffer.colors[i] = Color.white;

            walls[i].layer = 7;
        }
        ballsBuffer.colors[0] = balls[0].color;
        ballsBuffer.propertyBlock.SetVectorArray("_InstColor", ballsBuffer.colors);
        wallsBuffer.propertyBlock.SetVectorArray("_InstColor", wallsBuffer.colors);
        

        wallsBuffer.colors = wallsBuffer.colors[..walls.Length];
        wallsBuffer.matrices = wallsBuffer.matrices[..walls.Length];

        wallsBuffer.mat = material;
        ballsBuffer.mat = material;

        CreateCircleMesh();
        CreateQuadMesh();

    }
    void CreateCircleMesh()
    {
        ballsBuffer.mesh = new Mesh();

        Vector3[] vertices = new Vector3[4]
        {
            new (-ballRadius/2, -ballRadius/2, 0),
            new (ballRadius/2, -ballRadius/2, 0),
            new (-ballRadius/2, ballRadius/2, 0),
            new (ballRadius/2, ballRadius/2, 0)
        };
        ballsBuffer.mesh.vertices = vertices;

        Vector2[] uv = new Vector2[4]
        {
            new (0, 0),
            new (1, 0),
            new (0, 1),
            new (1, 1)
        };
        ballsBuffer.mesh.uv = uv;

        int[] triangles = new int[6]
        {
            0, 2, 1,
            2, 3, 1
        };
        ballsBuffer.mesh.triangles = triangles;
        ballsBuffer.mesh.hideFlags = HideFlags.DontSave;
        ballsBuffer.mesh.RecalculateNormals();
    }
    void CreateQuadMesh() {
        wallsBuffer.mesh = new Mesh();

        Vector3[] vertices = new Vector3[4]
        {
            new (-1, -1, 0),
            new (1, -1, 0),
            new (-1, 1, 0),
            new (1, 1, 0)
        };
        wallsBuffer.mesh.vertices = vertices;

        Vector2[] uv = new Vector2[4]
        {
            new (0, 0),
            new (1, 0),
            new (0, 1),
            new (1, 1)
        };
        wallsBuffer.mesh.uv = uv;

        int[] triangles = new int[6]
        {
            0, 2, 1,
            2, 3, 1
        };
        wallsBuffer.mesh.triangles = triangles;
        wallsBuffer.mesh.hideFlags = HideFlags.DontSave;
        wallsBuffer.mesh.RecalculateNormals();
    }
    public void SpawnNewBalls(int count)
    {
        for (int i = 0; i < count; ++i)
        {
            if (ballCount < balls.Count)
            {
                BallData ball = balls[ballCount - 1];

                ball.draw = true;
                ball.circle.currentPosition = balls[0].circle.currentPosition;

                SendInRandomDirection(ball);
            }
            else
            {
                BallData newBall = new()
                {
                    circle = new(balls[0].circle.currentPosition, ballRadius)
                };
                balls.Add(newBall);
                SendInRandomDirection(newBall);
            }

            ++ballCount;
        }
        ballsBuffer.propertyBlock.SetVectorArray("_InstColor", ballsBuffer.colors);
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

        balls[0].circle.currentPosition = Vector2.zero;
        ballCount = 1;
        SendInRandomDirection(balls[0]);
    }
    void Update()
    {
        DrawBalls();
        DrawWalls();
    }
    void DrawWalls()
    {
        Graphics.RenderMeshInstanced(
            rparams: new RenderParams(wallsBuffer.mat) { matProps = wallsBuffer.propertyBlock },
            mesh: wallsBuffer.mesh,
            submeshIndex: 0,
            instanceData: wallsBuffer.matrices,
            instanceCount: walls.Length
        );
    }
    void DrawBalls()
    {
        int drawn = 0;
        while (drawn < ballCount)
        {
            int count = Mathf.Min(Buffer.instanceCountMax, ballCount - drawn);

            for (int i = 0; i < count; ++i)
            {

                BallData ball = balls[drawn + i];
                if (!ball.draw) break;

                Matrix4x4 m = ballsBuffer.matrices[i];
                m.m03 = ball.circle.currentPosition.x;
                m.m13 = ball.circle.currentPosition.y;
                ballsBuffer.matrices[i] = m;

                ballsBuffer.colors[i] = ball.color;
            }

            Graphics.RenderMeshInstanced(
                rparams: new RenderParams(ballsBuffer.mat) { matProps = ballsBuffer.propertyBlock },
                mesh: ballsBuffer.mesh,
                submeshIndex: 0,
                instanceData: ballsBuffer.matrices[..count],
                instanceCount: count
            );

            drawn += count;
        }
    }
    void FixedUpdate()
    {
        float moveDistance = speed * Time.fixedDeltaTime;
        for (int i = 0; i < ballCount; ++i)
        {
            var currentBall = balls[i];
            Vector2 velNorm = currentBall.currentVelocity.normalized;
            Vector2 outNormal = Vector2.zero;
            Vector2 hitPoint = Vector2.zero;

            // if (currentBall.timeToSkip > 0)
            // {
            //     currentBall.timeToSkip -= Time.fixedDeltaTime;
            //     MoveBall(currentBall, moveDistance, velNorm);
            //     continue;
            // }

            MoveBall(currentBall, moveDistance, velNorm);

            for (int ii = 0; ii < walls.Length; ++ii)
            {
                if (!walls[ii].draw) continue;

                if (walls[ii].Intersects(currentBall.circle))
                {
                    walls[ii].GetCollisionInfo(currentBall.circle, out hitPoint, out outNormal);

                    currentBall.hit = new CollisionInfo(
                        outNormal,
                        hitPoint,
                        walls[ii]
                    );

                    break;
                }
            }

            if (hitPoint == Vector2.zero && outNormal == Vector2.zero) continue;

            switch (currentBall.hit.lastHit.layer)
            {
                case (int)CollisionType.Wall:
                    WallBounce(currentBall);
                    break;
                case (int)CollisionType.Brick:
                    BrickBounce(currentBall);
                    currentBall.hit.lastHit.draw = false;
                    break;
                case (int)CollisionType.Paddle:
                    PaddleBounce(currentBall);
                    break;
                default:
                    Debug.Log("Ball collided with: " + currentBall.hit.lastHit.layer);
                    break;
            }
        }
    }
    void MoveBall(BallData ball, float distance, Vector2 dir)
    {
        ball.circle.currentPosition += distance * dir;
    }
    void WallBounce(BallData ball)
    {
        Vector2 inDirection = ball.currentVelocity.normalized;
        Vector2 normal = ball.hit.normal;
        ball.currentVelocity = Vector2.Reflect(inDirection, normal) * speed;

        ball.circle.currentPosition = ball.hit.hitPoint + normal * ballRadius;
    }
    void BrickBounce(BallData ball)
    {
        Vector2 inDirection = ball.currentVelocity.normalized;
        Vector2 normal = ball.hit.normal;
        ball.currentVelocity = Vector2.Reflect(inDirection, normal) * speed;

        ball.circle.currentPosition = ball.hit.hitPoint + normal * ballRadius;
    }
    void PaddleBounce(BallData ball)
    {
        Vector2 normal = ball.hit.normal;
        // float paddleCenter = ball.hit.lastHit.
        // float hitPoint = hit.point.x;
        // float offset = (hitPoint - paddleCenter) / hit.transform.GetComponent<RectTransform>().sizeDelta.x;

        ball.currentVelocity = Vector2.Reflect(ball.currentVelocity, normal) * speed;
        ball.circle.currentPosition = ball.hit.hitPoint + normal * ballRadius;
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
        if (balls == null) return;

        Gizmos.color = Color.red;

        foreach (var ball in balls)
        {
            if (ball == null) continue;

            Vector2 position = ball.circle.currentPosition;
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
