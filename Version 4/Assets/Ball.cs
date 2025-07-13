using System.Collections;
using UnityEngine;

public class Ball : MonoBehaviour
{
    private Transform lastCollisionTransform = null;
    [SerializeField] private float ballRadius = 0.09f;
    [SerializeField] private float speed = 5f;
    Vector2 currentVelocity = Vector2.zero;

    public bool debugDrawHit;

    void Start()
    {
        SendInRandomDirection();
    }

    void FixedUpdate()
    {
        float moveDistance = currentVelocity.magnitude * Time.fixedDeltaTime;

        RaycastHit2D hit = Physics2D.CircleCast(
            origin: transform.position,      // Start point
            radius: ballRadius, // Radius of the circle
            direction: currentVelocity,            // Movement direction (normalized)
            distance: moveDistance, // Distance to check
            layerMask: ~LayerMask.GetMask("Ball") // Exclude Ball layer
        );

        if (hit.collider == null || hit.transform == lastCollisionTransform)
        {
            transform.Translate(currentVelocity * Time.fixedDeltaTime, Space.World);
            return; // No collision detected, exit early
        }

        lastCollisionTransform = hit.transform;
        // print("Collision with: " + hit.transform.name);

        switch (hit.transform.tag)
        {
            case "Wall":
                WallBounce(hit);
                break;
            case "Brick":
                BrickBounce(hit);
                StartCoroutine(DestroyAfterPhysics(hit.transform.gameObject));
                break;
            case "Paddle":
                PaddleBounce(hit);
                break;
            default:
                Debug.Log("Ball collided with: " + hit.transform.name);
                break;
        }
    }



    void WallBounce(RaycastHit2D hit)
    {
        Vector2 inDirection = currentVelocity.normalized;
        Vector2 normal = hit.normal;
        currentVelocity = Vector2.Reflect(inDirection, normal) * speed;

        // Move the ball to the point of collision, slightly off the surface
        transform.position = hit.point + normal * ballRadius;
    }

    void BrickBounce(RaycastHit2D brick)
    {
        // Get the normal of the collision
        Vector2 inDirection = currentVelocity.normalized;
        Vector2 normal = brick.normal;
        currentVelocity = Vector2.Reflect(inDirection, normal) * speed;

        transform.position = brick.point + normal * ballRadius;
    }

    void PaddleBounce(RaycastHit2D paddle)
    {
        Vector2 normal = paddle.normal;
        float paddleCenter = paddle.transform.position.x;
        float hitPoint = paddle.point.x;
        float offset = (hitPoint - paddleCenter) / paddle.transform.GetComponent<RectTransform>().sizeDelta.x;

        currentVelocity = Vector2.Reflect(currentVelocity, normal);
        currentVelocity.x += offset * 0.5f; // Add horizontal influence based on where the paddle was hit
        currentVelocity = currentVelocity.normalized * speed;

        transform.position = paddle.point + normal * ballRadius;
    }

    IEnumerator DestroyAfterPhysics(GameObject obj)
    {
        yield return new WaitForFixedUpdate(); // Wait until physics update finishes
        Destroy(obj);
    }

    void OnDrawGizmosSelected()
    {
        if (!debugDrawHit) return;

        RaycastHit2D hit = Physics2D.CircleCast(
            origin: transform.position,      // Start point
            radius: ballRadius, // Radius of the circle
            direction: currentVelocity,            // Movement direction (normalized)
            distance: currentVelocity.magnitude * Time.fixedDeltaTime, // Distance to check
            layerMask: ~LayerMask.GetMask("Ball") // Exclude Ball layer
        );
        
        if (hit.collider != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, hit.point);
            Gizmos.DrawWireSphere(hit.point, 0.1f);
            Gizmos.DrawLine(hit.point, hit.point + hit.normal * 0.5f);
        }
        else
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, ballRadius);
        }
    }

    public void SendInRandomDirection()
    {
        Vector2 initialDirection = new Vector2(
            Mathf.Cos(Random.Range(0f, Mathf.PI * 0.6f + Mathf.PI * 0.2f)),
            Mathf.Sin(Random.Range(0f, Mathf.PI * 0.6f + Mathf.PI * 0.2f))
        ).normalized;

        currentVelocity = initialDirection * speed;
    }
}

