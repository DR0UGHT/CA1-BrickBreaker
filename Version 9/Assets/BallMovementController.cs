using UnityEngine;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using System;

public enum CollisionLayers
{
    Wall = 0,
    Brick = 1,
    Paddle = 2,
    Ball = 3
}
public enum ShapeType
{
    None,
    Circle,
    Box
}
public static class Shape2DMath
{
    public static bool IntersectCircleVsBox(float2 circleCenter, float radius, float4 box)
    {
        float xMin = box.x, yMin = box.y, xMax = box.z, yMax = box.w;
        float closestX = math.clamp(circleCenter.x, xMin, xMax);
        float closestY = math.clamp(circleCenter.y, yMin, yMax);
        float dx = circleCenter.x - closestX;
        float dy = circleCenter.y - closestY;
        return dx * dx + dy * dy <= radius * radius;
    }
    public static bool IntersectCircleVsCircle(float2 a, float ra, float2 b, float rb)
    {
        float2 d = b - a;
        float r = ra + rb;
        return math.dot(d, d) <= r * r;
    }
    public static bool IntersectBoxVsBox(float4 a, float4 b)
    {
        // a.xy = min, a.zw = max
        return !(a.z < b.x || a.x > b.z || a.w < b.y || a.y > b.w);
    }
    public static bool GetCircleVsAABBCollision(float2 origin, float radius, float2 direction, float maxDist, float4 box, out float distance, out float2 hitPoint, out float2 normal)
    {
        float2 min = box.xy;
        float2 max = box.zw;
        float2 expandedMin = min - radius;
        float2 expandedMax = max + radius;

        float2 invDir = new float2(
            1f / (direction.x != 0f ? direction.x : 1e-5f),
            1f / (direction.y != 0f ? direction.y : 1e-5f)
        );

        float2 t1 = (expandedMin - origin) * invDir;
        float2 t2 = (expandedMax - origin) * invDir;

        float2 tMin = math.min(t1, t2);
        float2 tMax = math.max(t1, t2);

        float tEnter = math.max(tMin.x, tMin.y);
        float tExit  = math.min(tMax.x, tMax.y);

        if (tEnter < 0f || tEnter > tExit || tEnter > maxDist)
        {
            distance = 0f;
            hitPoint = float2.zero;
            normal = float2.zero;
            return false;
        }

        distance = tEnter;
        hitPoint = origin + direction * tEnter;

        float2 pt = hitPoint;
        if (math.abs(pt.x - expandedMin.x) < 1e-5f)      normal = new float2(1f, 0f);
        else if (math.abs(pt.x - expandedMax.x) < 1e-5f) normal = new float2(-1f,0f);
        else if (math.abs(pt.y - expandedMin.y) < 1e-5f) normal = new float2(0f, 1f);
        else                                             normal = new float2(0f,-1f);
        return true;
    }
    public static bool GetCircleVsCircleCollision(float2 origin, float radius, float2 direction, float maxDist, Circle2D target, out float distance, out float2 hitPoint, out float2 normal)
    {
        float2 rel = target.currentPosition - origin;
        float r = radius + target.radius;

        float proj = math.dot(rel, direction);
        float2 perp = rel - proj * direction;
        float closestSq = math.dot(perp, perp);

        if (closestSq > r * r)
        {
            distance = 0f;
            hitPoint = float2.zero;
            normal = float2.zero;
            return false;
        }

        float thc = math.sqrt(r * r - closestSq);
        float t0  = proj - thc;

        if (t0 < 0f || t0 > maxDist)
        {
            distance = 0f;
            hitPoint = float2.zero;
            normal = float2.zero;
            return false;
        }

        distance = t0;
        hitPoint = origin + direction * t0;
        normal = math.normalize(hitPoint - target.currentPosition);
        return true;
    }
    public static float2 Normalize(float2 v)
    {
        float len = math.length(v);
        return len > 0f ? v / len : float2.zero;
    }
}
public struct BallData
{
    public float4 color;
    public float2 currentVelocity;
    public float timeToSkip;
    public CollisionInfo hit;
    public int nextHitShapeIndex;
    public Shape2D ball;
    public BallData(float4 color, Shape2D circle)
    {
        currentVelocity = new();
        timeToSkip = 0.0f;
        this.color = color;
        hit = CollisionInfo.None;
        nextHitShapeIndex = -1;
        ball = circle;
    }

    public readonly bool CheckForIntersects(Shape2D[] collisions, out CollisionInfo collision)
    {
        for (int ii = 0; ii < collisions.Length; ++ii)
        {
            if (!collisions[ii].draw || !collisions[ii].Intersects(ball)) continue;

            collisions[ii].GetCollisionInfo(ball, out float2 hitPoint, out float2 outNormal);

            collision = new CollisionInfo(
                outNormal,
                hitPoint,
                collisions[ii].layer
            );
            return true;
        }
        collision = CollisionInfo.None;
        return false;
    }
    public readonly bool CheckForIntersect(in Shape2D collisions, out CollisionInfo collision)
    {
        if (!collisions.draw || collisions.Intersects(ball))
        {
            collision = CollisionInfo.None;
            return false;
        }
        
        collisions.GetCollisionInfo(ball, out float2 hitPoint, out float2 outNormal);

        collision = new CollisionInfo(
            outNormal,
            hitPoint,
            collisions.layer
        );
        return true;
        
    }
}
public struct CollisionInfo
{
    public float2 normal;
    public float2 hitPoint;
    public int hitLayer;
    public bool isValid;

    public CollisionInfo(float2 nor, float2 hit, int layer)
    {
        normal = nor;
        hitPoint = hit;
        hitLayer = layer;
        isValid = true;
    }
    public static readonly CollisionInfo None = new()
    {
        normal = float2.zero,
        hitPoint = float2.zero,
        hitLayer = -1,
        isValid = false
    };
    public readonly bool IsValid => isValid;
}
public struct Shape2D
{
    public ShapeType shapeType;
    public Circle2D  C;
    public Box2D     B;
    public int layer;
    public bool draw;

    public void CreateCircle(float2 position, float radius, int layer, bool draw)
    {
        shapeType = ShapeType.Circle;
        C = new(position, radius);
        this.draw = draw;
        this.layer = layer;
    }

    public void CreateSquare(float4 boxShape, int layer, bool draw)
    {
        shapeType = ShapeType.Box;
        B = new(boxShape);
        this.draw = draw;
        this.layer = layer;
    }
    public static readonly Shape2D Null = new() { shapeType = ShapeType.None };

    public readonly bool Intersects(in Shape2D other)
    {
        switch (shapeType)
        {
            case ShapeType.Circle:
                if (other.shapeType == ShapeType.Circle)
                {
                    return Shape2DMath.IntersectCircleVsCircle(
                        a: C.currentPosition,
                        ra: C.radius,
                        b: other.C.currentPosition,
                        rb: other.C.radius
                    );
                }
                else if (other.shapeType == ShapeType.Box)
                {
                    return Shape2DMath.IntersectCircleVsBox(
                        circleCenter: C.currentPosition,
                        radius: C.radius,
                        box: other.B.boxShape
                    );
                }
                break;
            case ShapeType.Box:
                if (other.shapeType == ShapeType.Circle)
                {
                    return Shape2DMath.IntersectCircleVsBox(
                        circleCenter: other.C.currentPosition,
                        radius: other.C.radius,
                        box: B.boxShape
                    );
                }
                else if (other.shapeType == ShapeType.Box)
                {
                    return Shape2DMath.IntersectBoxVsBox(
                        a: B.boxShape,
                        b: other.B.boxShape
                    );
                }
                break;
        }
        return false;
    }
    public readonly bool GetCollisionInfo(in Shape2D other, out float2 hitPoint, out float2 normal)
    {
        if (shapeType == ShapeType.Box && other.shapeType == ShapeType.Box)
            return GetBoxVsBoxInfo(B.boxShape, other.B.boxShape, out hitPoint, out normal);

        if (shapeType == ShapeType.Circle && other.shapeType == ShapeType.Box)
            return GetCircleVsBoxInfo(C.currentPosition, C.radius, other.B.boxShape, out hitPoint, out normal);

        if (shapeType == ShapeType.Box && other.shapeType == ShapeType.Circle)
        {
            bool ok = GetCircleVsBoxInfo(other.C.currentPosition, other.C.radius, B.boxShape, out hitPoint, out normal);
            normal = -normal;
            return ok;
        }

        if (shapeType == ShapeType.Circle && other.shapeType == ShapeType.Circle)
        {
            return Shape2DMath.GetCircleVsCircleCollision(
                C.currentPosition,
                C.radius,
                math.normalize(other.C.currentPosition - C.currentPosition),
                float.MaxValue,
                other.C,
                out _,
                out hitPoint,
                out normal
            );
        }

        hitPoint = float2.zero;
        normal = float2.zero;
        return false;
    }
    static bool GetBoxVsBoxInfo(float4 a, float4 b, out float2 hitPoint, out float2 normal)
    {
        float xMinA = a.x, yMinA = a.y, xMaxA = a.z, yMaxA = a.w;
        float xMinB = b.x, yMinB = b.y, xMaxB = b.z, yMaxB = b.w;

        float overlapX = math.min(xMaxA, xMaxB) - math.max(xMinA, xMinB);
        float overlapY = math.min(yMaxA, yMaxB) - math.max(yMinA, yMinB);

        float2 centerA = new ((xMinA + xMaxA) * 0.5f, (yMinA + yMaxA) * 0.5f);
        float2 centerB = new ((xMinB + xMaxB) * 0.5f, (yMinB + yMaxB) * 0.5f);

        if (overlapX < overlapY)
        {
            bool isLeft = centerA.x < centerB.x;
            normal = isLeft ? new float2(-1, 0) : new float2(1, 0);
            float x = isLeft ? xMaxA : xMinA;
            hitPoint = new float2(
                x,
                (math.max(yMinA, yMinB) + math.min(yMaxA, yMaxB)) * 0.5f
            );
        }
        else
        {
            bool isDown = centerA.y < centerB.y;
            normal = isDown ? new float2(0, -1) : new float2(0, 1);
            float y = isDown ? yMaxA : yMinA;
            hitPoint = new float2(
                (math.max(xMinA, xMinB) + math.min(xMaxA, xMaxB)) * 0.5f,
                y
            );
        }

        return true;
    }
    static bool GetCircleVsBoxInfo(float2 circleCenter, float radius, float4 box, out float2 hitPoint, out float2 normal)
    {
        float2 min = box.xy;
        float2 max = box.zw;

        float2 closest = new (
            math.clamp(circleCenter.x, min.x, max.x),
            math.clamp(circleCenter.y, min.y, max.y)
        );

        float2 delta = circleCenter - closest;
        if (math.lengthsq(delta) > 1e-6f)
        {
            normal = math.normalize(delta);
            hitPoint = closest;
            return true;
        }

        float leftDist = math.abs(circleCenter.x - min.x);
        float rightDist = math.abs(circleCenter.x - max.x);
        float bottomDist = math.abs(circleCenter.y - min.y);
        float topDist = math.abs(circleCenter.y - max.y);

        float minDist = math.min(math.min(leftDist, rightDist),
                                 math.min(bottomDist, topDist));

        if (minDist == leftDist)
        {
            normal = new float2(-1, 0);
            hitPoint = new float2(min.x, circleCenter.y);
        }
        else if (minDist == rightDist)
        {
            normal = new float2(1, 0);
            hitPoint = new float2(max.x, circleCenter.y);
        }
        else if (minDist == bottomDist)
        {
            normal = new float2(0, -1);
            hitPoint = new float2(circleCenter.x, min.y);
        }
        else
        {
            normal = new float2(0, 1);
            hitPoint = new float2(circleCenter.x, max.y);
        }

        return true;
    }
    public float DistanceToCompare(in Shape2D other)
    {
        float2 a = shapeType == ShapeType.Circle ? C.currentPosition :
                   shapeType == ShapeType.Box ? B.Center :
                                                   new float2(0, 0);
        float2 b = other.shapeType == ShapeType.Circle ? other.C.currentPosition :
                   other.shapeType == ShapeType.Box ? other.B.Center :
                                                   new float2(0, 0);

        float2 d = b - a;
        return math.dot(d, d);
    }
}
public struct Box2D
{
    // box.xy = (xMin, yMin), box.zw = (xMax, yMax)
    public float4 boxShape;
    public float2 Center => (boxShape.xy + boxShape.zw) * 0.5f;
    public float2 Size => boxShape.zw - boxShape.xy;
    public Box2D(float4 boxShape)
    {
        this.boxShape = boxShape;
    }
}
public struct Circle2D
{
    public float2 currentPosition;
    public float radius;

    public Circle2D(float2 curpos, float rad)
    {
        currentPosition = curpos;
        radius = rad;
    }

    public readonly bool GetEarliestCircleCollision(float2 direction, float maxDistance, Shape2D[] shapes, out float distToHit, out float2 hitPoint, out float2 normal, out int hitShapeIndex)
    {
        distToHit = maxDistance;
        hitPoint = float2.zero;
        normal = float2.zero;
        hitShapeIndex = -1;

        float2 dirNorm = math.normalize(direction);
        bool hitFound = false;

        for (int i = 0; i < shapes.Length; ++i)
        {
            Shape2D shape = shapes[i];
            bool hit = false;
            float d = 0f;
            float2 hp = float2.zero;
            float2 n = float2.zero;

            if (shape.shapeType == ShapeType.Circle)
            {
                hit = Shape2DMath.GetCircleVsCircleCollision(
                    origin: currentPosition,
                    radius: radius,
                    direction: dirNorm,
                    maxDist: maxDistance,
                    target: shape.C,
                    out d, out hp, out n
                );
            }
            else if (shape.shapeType == ShapeType.Box)
            {
                hit = Shape2DMath.GetCircleVsAABBCollision(
                    origin: currentPosition,
                    radius: radius,
                    direction: dirNorm,
                    maxDist: maxDistance,
                    box: shape.B.boxShape,
                    out d, out hp, out n
                );
            }

            if (!hit)
                continue;

            if (d < 0f)
                d = 0f;

            if (d < distToHit)
            {
                distToHit = d;
                hitPoint = hp;
                normal = n;
                hitShapeIndex = i;
                hitFound = true;
            }
        }

        return hitFound;
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
    private NativeList<BallData> balls;
    [SerializeField] int ballCount = 0;

    private Buffer ballsBuffer;
    private Buffer wallsBuffer;
    private Shape2D[] walls;
    private Matrix4x4 baseMatrix;
    [SerializeField] private Material material;
    private float moveDistance;

    void Start()
    {
        float s = 22.5f * ballRadius;
        baseMatrix = Matrix4x4.TRS(
            Vector3.zero,
            Quaternion.identity,
            new Vector3(s, s, 1f)
        );

        moveDistance = speed * Time.fixedDeltaTime;
        balls = new NativeList<BallData>(Allocator.Persistent);
        float4 color = new(UnityEngine.Random.value * 0.95f, UnityEngine.Random.value * 0.95f, UnityEngine.Random.value * 0.95f, 1.0f);
        Shape2D newBallShape = new();
        newBallShape.CreateCircle(float2.zero, ballRadius, (int)CollisionLayers.Ball, true);
        BallData newBall = new(
            color: color,
            circle: newBallShape
        );
        balls.Add(newBall);

        SendInDirection(0, new float2(1, 0.05f));
        ballCount = 1;

        walls = new Shape2D[4];

        float screenHeightHalf = Camera.main.orthographicSize;
        float screenWidthHalf = screenHeightHalf * (Screen.width / (float)Screen.height);

        float screenWidth = screenWidthHalf * 2f;
        float screenHeight = screenHeightHalf * 2f;

        float wallThickness = screenWidth / 80f;

        // Left wall: xMin=-screenWidthHalf, yMin=-screenHeightHalf, xMax=xMin+thickness, yMax=+screenHeightHalf
        walls[0] = new();
        walls[0].CreateSquare(new float4(
            -screenWidthHalf,
            -screenHeightHalf,
            -screenWidthHalf + wallThickness,
            screenHeightHalf
        ), (int)CollisionLayers.Wall, true);

        // Right wall: xMin=screenWidthHalf-thickness, yMin=-screenHeightHalf, xMax=screenWidthHalf, yMax=+screenHeightHalf
        walls[1] = new();
        walls[1].CreateSquare(new float4(
            screenWidthHalf - wallThickness,
            -screenHeightHalf,
            screenWidthHalf,
            screenHeightHalf
        ), (int)CollisionLayers.Wall, true);

        // Bottom wall: xMin=-screenWidthHalf, yMin=-screenHeightHalf, xMax=+screenWidthHalf, yMax=yMin+thickness
        walls[2] = new();
        walls[2].CreateSquare(new float4(
            -screenWidthHalf,
            -screenHeightHalf,
            screenWidthHalf,
            -screenHeightHalf + wallThickness
        ), (int)CollisionLayers.Wall, true);

        // Top wall: xMin=-screenWidthHalf, yMin=screenHeightHalf-thickness, xMax=+screenWidthHalf, yMax=screenHeightHalf
        walls[3] = new();
        walls[3].CreateSquare(new float4(
            -screenWidthHalf,
            screenHeightHalf - wallThickness,
            screenWidthHalf,
            screenHeightHalf
        ), (int)CollisionLayers.Wall, true);

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

        for (int i = 0; i < walls.Length; ++i)
        {
            float2 center = walls[i].B.Center;
            Vector3 cent = new(center.x, center.y, 1);
            float2 size = walls[i].B.Size;
            Vector3 siz = new(size.x, size.y, 1);
            wallsBuffer.matrices[i] = Matrix4x4.TRS(cent, Quaternion.identity, siz);
            wallsBuffer.colors[i] = Color.white;

            walls[i].layer = (int)CollisionLayers.Wall;
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
    void CreateQuadMesh()
    {
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
            if (ballCount < balls.Length)
            {
                BallData ball = balls[ballCount - 1];

                ball.ball.draw = true;
                ball.ball.C.currentPosition = balls[0].ball.C.currentPosition;
                ball.timeToSkip = Time.deltaTime;
                balls[ballCount - 1] = ball;
                int idx = ballCount++;
                SendInRandomDirection(idx);
            }
            else
            {
                float4 color = new(UnityEngine.Random.value * 0.95f, UnityEngine.Random.value * 0.95f, UnityEngine.Random.value * 0.95f, 1.0f);
                Shape2D newBallShape = new();
                newBallShape.CreateCircle(balls[0].ball.C.currentPosition, ballRadius, (int)CollisionLayers.Ball, true);
                BallData newBall = new(
                    color: color,
                    circle: newBallShape
                );

                balls.Add(newBall);
                int idx = ballCount++;
                SendInRandomDirection(idx);
            }

        }
        ballsBuffer.propertyBlock.SetVectorArray("_InstColor", ballsBuffer.colors);
    }
    public void ResetBalls()
    {
        for (int i = 1; i < ballCount; ++i)
        {
            BallData ball = balls[i];
            ball.timeToSkip = 0;
            ball.ball.draw = false;
            balls[i] = ball;
        }
        BallData ballOne = balls[0];
        ballOne.ball.C.currentPosition = float2.zero;
        ballOne.timeToSkip = 0;
        balls[0] = ballOne;
        ballCount = 1;
        SendInRandomDirection(0);
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
        int maxCount = Buffer.instanceCountMax;

        while (drawn < ballCount)
        {
            int count = Mathf.Min(maxCount, ballCount - drawn);
            int instCount = 0;
            for (int i = 0; i < count; ++i)
            {
                var ball = balls[drawn + i];
                if (!ball.ball.draw) continue;

                Matrix4x4 m = baseMatrix;
                m.m03 = ball.ball.C.currentPosition.x;
                m.m13 = ball.ball.C.currentPosition.y;

                ballsBuffer.matrices[instCount] = m;
                ballsBuffer.colors[instCount] = ball.color;

                instCount++;
            }

            ballsBuffer.propertyBlock.SetVectorArray("_InstColor", ballsBuffer.colors);
            Graphics.RenderMeshInstanced(
                rparams: new RenderParams(ballsBuffer.mat) { matProps = ballsBuffer.propertyBlock },
                mesh: ballsBuffer.mesh,
                submeshIndex: 0,
                instanceData: ballsBuffer.matrices,
                instanceCount: instCount
            );

            drawn += count;
        }
    }
    void FixedUpdate()
    {
        for (int i = 0; i < ballCount; ++i)
        {
            BallData currentBall = balls[i];
            float2 velNorm = Shape2DMath.Normalize(currentBall.currentVelocity);

            if (currentBall.timeToSkip > 0)
            {
                currentBall.timeToSkip -= Time.fixedDeltaTime;
                balls[i] = currentBall;

                float remainingDistance = math.max(0f, currentBall.timeToSkip * speed);
                float travelThisFrame = math.min(moveDistance, remainingDistance);
                MoveBall(i, travelThisFrame, velNorm);
                continue;
            }

            MoveBall(i, moveDistance, velNorm);
            currentBall = balls[i];
            
            if (currentBall.nextHitShapeIndex == -1 &&
                currentBall.ball.C.GetEarliestCircleCollision(velNorm, 15, walls, out float distToHit, out float2 _, out float2 _, out int hitShapeIndex))
            {
                if (hitShapeIndex == -1)
                {
                    currentBall.timeToSkip = (15 - ballRadius) / speed;
                    balls[i] = currentBall;
                    continue;
                }
                if (distToHit > ballRadius)
                {
                    currentBall.timeToSkip = (distToHit - ballRadius) / speed;
                    currentBall.nextHitShapeIndex = hitShapeIndex;
                    balls[i] = currentBall;
                    continue;
                }
            }

            CollisionInfo collisionz;
            if (currentBall.nextHitShapeIndex != -1)
            {
                var shape = walls[currentBall.nextHitShapeIndex];
                if (!currentBall.CheckForIntersect(shape, out collisionz) || !collisionz.IsValid)
                {
                    continue;
                }
            }
            else
            {
                if (!currentBall.CheckForIntersects(walls, out collisionz) || !collisionz.IsValid)
                {
                    continue;
                }
            }

            currentBall.hit = collisionz;

            switch (currentBall.hit.hitLayer)
            {
                case (int)CollisionLayers.Wall:
                    WallBounce(ref currentBall);
                    break;
                case (int)CollisionLayers.Brick:
                    BrickBounce(ref currentBall);
                    break;
                case (int)CollisionLayers.Paddle:
                    PaddleBounce(ref currentBall);
                    break;
                default:
                    break;
            }

            currentBall.hit = CollisionInfo.None;
            currentBall.nextHitShapeIndex = -1;
            balls[i] = currentBall;
        }
    }

    void MoveBall(int ballIndex, float distance, float2 dir)
    {
        BallData ball = balls[ballIndex];
        ball.ball.C.currentPosition += distance * dir;
        balls[ballIndex] = ball;
    }
    void WallBounce(ref BallData ball)
    {
        float2 inDirection = math.normalizesafe(ball.currentVelocity);
        float2 normal = ball.hit.normal;
        ball.currentVelocity = math.reflect(inDirection, normal) * speed;

        ball.ball.C.currentPosition = ball.hit.hitPoint - normal * ballRadius;
    }
    void BrickBounce(ref BallData ball)
    {
        float2 inDirection = math.normalizesafe(ball.currentVelocity);
        float2 normal = ball.hit.normal;
        ball.currentVelocity = math.reflect(inDirection, normal) * speed;

        ball.ball.C.currentPosition = ball.hit.hitPoint - normal * ballRadius;
    }
    void PaddleBounce(ref BallData ball)
    {
        float2 normal = ball.hit.normal;
        // float paddleCenter = ball.hit.lastHit.
        // float hitPoint = hit.point.x;
        // float offset = (hitPoint - paddleCenter) / hit.transform.GetComponent<RectTransform>().sizeDelta.x;

        ball.currentVelocity = math.reflect(math.normalizesafe(ball.currentVelocity), normal) * speed;
        ball.ball.C.currentPosition = ball.hit.hitPoint - normal * ballRadius;
    }
    public void SendInRandomDirection(int ballIndex)
    {
        float2 initialDirection = math.normalize(new float2(
            Mathf.Cos(UnityEngine.Random.Range(0f, Mathf.PI * 0.6f + Mathf.PI * 0.2f)),
            Mathf.Sin(UnityEngine.Random.Range(0f, Mathf.PI * 0.6f + Mathf.PI * 0.2f))
        ));

        BallData ball = balls[ballIndex];
        ball.currentVelocity = initialDirection * speed;
        balls[ballIndex] = ball;
    }
    public void SendInDirection(int ballIndex, float2 dir)
    {
        BallData ball = balls[ballIndex];
        ball.currentVelocity = math.normalize(dir) * speed;
        balls[ballIndex] = ball;
    }
    public int GetBallCount() { return ballCount; }

    void OnDestroy() {
        if (balls.IsCreated) balls.Dispose();
    }
}
