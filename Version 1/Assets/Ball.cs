using UnityEngine;

public class Ball : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    Vector2 currentVelocity = Vector2.zero;
    private Rigidbody2D rb;

    void Awake()
    {
        SetupRigidBody();
    }

    void Start()
    {
        SendInRandomDirection();
    }


    private void SetupRigidBody()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.isKinematic = false;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.gravityScale = 0;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Brick"))
        {
            Destroy(collision.collider.gameObject);
        }
    }

    void BrickBounce(RaycastHit2D brick)
    {
        Vector2 inDirection = currentVelocity.normalized;
        Vector2 normal = brick.normal;
        currentVelocity = Vector2.Reflect(inDirection, normal) * speed;

        transform.position = brick.point + normal * transform.GetComponent<Collider2D>().bounds.extents.x;
    }

    public void SendInRandomDirection()
    {
        Vector2 initialDirection = new Vector2(
            Mathf.Cos(Random.Range(0f, Mathf.PI * 0.6f + Mathf.PI * 0.2f)),
            Mathf.Sin(Random.Range(0f, Mathf.PI * 0.6f + Mathf.PI * 0.2f))
        ).normalized;

        rb.velocity = initialDirection * speed;
    }
}

