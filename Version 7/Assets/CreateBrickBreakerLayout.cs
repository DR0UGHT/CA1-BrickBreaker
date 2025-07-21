using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class CreateBrickBreakerLayout : MonoBehaviour
{
    //The number of bricks to create in the layout
    [SerializeField] int numberOfBricks = 10;

    //Reference to the canvas
    private Transform canvasTransform;


    void Start()
    {
        canvasTransform = GameObject.Find("Canvas").transform;

        LayoutBricks(CalculatePyramidShape(numberOfBricks));
    }

    int CalculatePyramidShape(int _numberOfBricks)
    {
        return (int)(Mathf.Sqrt(1 + 8 * _numberOfBricks) - 1) / 2;
    }

    void LayoutBricks(int rows)
    {
        Vector2 screenSize = new(
            Screen.width,
            Screen.height
        );


        float brickWidth = screenSize.x / rows * 0.9f;
        float baseHeight = screenSize.y / rows;
        //Adjust vertical placement of bricks by removing half a brick to center the pyramid
        float brickHeight = (baseHeight - (baseHeight / rows)) / 2.0f;

        float startX = 0f, startY = 0f;

        //Create template brick to reuse
        GameObject brick = new("BrickTemplate");
        brick.AddComponent<Image>();
        brick.AddComponent<BoxCollider2D>();
        brick.GetComponent<BoxCollider2D>().size = new Vector2(brickWidth * 0.9f, brickHeight * 0.9f);

        //90% of size to allow for spacing
        brick.GetComponent<RectTransform>().sizeDelta = new Vector2(brickWidth * 0.9f, brickHeight * 0.9f);
        brick.tag = "Brick";

        for (int row = 0; row < rows; ++row)
        {
            for (int col = 0; col <= row; ++col)
            {
                float x = startX - (brickWidth * row / 2f) + (col * brickWidth);
                float y = startY + (row * brickHeight) + (brickHeight / 2f);

                GameObject newBrick = Instantiate(brick, canvasTransform);
                newBrick.name = $"Brick_{row}_{col}";
                newBrick.tag = "Brick"; 
                newBrick.transform.localPosition = new Vector3(x, y, 0f);
            }
        }

        Destroy(brick); // Clean up template
    }

    public void RelayBricks()
    {
        GameObject.FindGameObjectsWithTag("Brick").ToList().ForEach(x => Destroy(x));

        LayoutBricks(numberOfBricks);
    }
}