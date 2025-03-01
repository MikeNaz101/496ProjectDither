using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Unity.AI.Navigation;
using static UnityEditor.Experimental.GraphView.GraphView;

public class ProceduralRoomGenerator : MonoBehaviour
{
    public NavMeshSurface navMeshSurface;
    private int numRooms = 30;
    private List<Vector3> doorPositions = new List<Vector3>();
    //private List<Vector3> doorSizes = new List<Vector3>(); // Store door size
    private List<Bounds> roomBounds = new List<Bounds>();
    private Vector2 roomSizeMinMax = new Vector2(10, 25);
    private Vector2 bigRoomSize = new Vector2(200, 200);
    public GameObject wallPrefab, doorPrefab, WindowPrefab;
    void Start()
    {
        GenerateBigRoom();
        GenerateRooms();
        //ConnectRooms();
        BakeNavMesh();
    }

    void Update()
    {

    }
    void GenerateBigRoom()
    {
        GameObject bigRoom = new GameObject("BigRoom");
        GenerateWalls(bigRoom, Vector3.zero, bigRoomSize.x, bigRoomSize.y, 10);
    }
    public void GenerateRooms()
    {
        for (int i = 0; i < numRooms; i++)
        {
            bool roomPlaced = false;
            int maxAttempts = 200; // Avoid infinite loops
            int attempts = 0;
            while (!roomPlaced && attempts < maxAttempts)
            {
                Debug.Log("Attemps = " + attempts);
                attempts++;
                float roomWidth = Random.Range(roomSizeMinMax.x, roomSizeMinMax.y);
                float roomHeight = Random.Range(roomSizeMinMax.x, roomSizeMinMax.y);

                float posX = Random.Range(-bigRoomSize.x / 2 + roomWidth / 2, bigRoomSize.x / 2 - roomWidth / 2);
                float posZ = Random.Range(-bigRoomSize.y / 2 + roomWidth / 2, bigRoomSize.y / 2 - roomWidth / 2);

                Bounds newRoomBounds = new Bounds(new Vector3(posX, 0, posZ), new Vector3(roomWidth, 1, roomHeight));
                if (!IsOverlapping(newRoomBounds, roomBounds))
                {
                    roomBounds.Add(newRoomBounds);
                    //roomCenters.Add(newRoomBounds.center);
                    roomPlaced = true;
                    GameObject room = new GameObject("Room_" + i);
                    room.transform.position = new Vector3(posX, 0, posZ);
                    GenerateWalls(room, room.transform.position, roomWidth, roomHeight, 20);
                    // Assign the "Room" layer to the generated room
                    room.layer = LayerMask.NameToLayer("Room");
                    Debug.Log("HEre");
                }
            }
            if (i == 0)
            {
                GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                player.name = "Player";
                player.transform.position = new Vector3(1, 0.5f, 1);
                player.AddComponent<Rigidbody>().freezeRotation = true;
                //player.AddComponent<PlayerController>();

            }
            roomPlaced = false;
        }
        //GenerateCorridors();
    }
    private bool IsOverlapping(Bounds newBounds, List<Bounds> existingBounds)
    {
        foreach (Bounds bounds in existingBounds)
        {
            if (bounds.Intersects(newBounds))
            {
                return true;
            }
        }
        return false;
    }
    private void GenerateWalls(GameObject parent, Vector3 position, float width, float height, float wallHeight)
    {
        Vector3[] wallPosition =
        {
            new Vector3(position.x, wallHeight / 2, position.z + height / 2),
            new Vector3(position.x, wallHeight / 2, position.z - height / 2),
            new Vector3(position.x + width / 2, wallHeight / 2,  position.z),
            new Vector3(position.x - width / 2, wallHeight / 2,  position.z)
        };

        Vector3[] wallScales = {
            new Vector3(width, wallHeight, 0.1f),
            new Vector3(width, wallHeight, 0.1f),
            new Vector3(0.1f, wallHeight, height),
            new Vector3(0.1f, wallHeight, height)
        };
        // Select two walls to have doors
        int doorWall1 = Random.Range(0, 4);
        int doorWall2;
        do { doorWall2 = Random.Range(0, 4); } while (doorWall2 == doorWall1);

        for (int i = 0; i < 4; i++)
        {
            if (i == doorWall1 || i == doorWall2)
            {
                GenerateWallWithDoor(parent, wallPosition[i], wallScales[i]);
            }
            else
            {
                GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.layer = LayerMask.NameToLayer("NotWalkable");
                wall.transform.position = wallPosition[i];
                wall.transform.localScale = wallScales[i];
                wall.transform.parent = parent.transform;
            }
        }

        //GenerateDoorAndWindows(parent, position, width, height, wallHeight);

    }
    private void GenerateWallWithDoor(GameObject parent, Vector3 position, Vector3 scale)
    {
        float doorWidth = 1.5f;
        float doorHeight = scale.y / 4; // 1/4th of total wall height
        float wallThickness = 0.1f;
        float topWallHeight = scale.y - doorHeight; // The remaining 3/4 height

        bool isHorizontal = scale.x > scale.z; // Check if the wall runs along the X-axis

        Vector3 leftWallPos, rightWallPos, topWallPos;
        Vector3 leftWallScale, rightWallScale, topWallScale;

        if (isHorizontal)
        {
            float halfRemainingWidth = (scale.x - doorWidth) / 2;

            leftWallPos = position + new Vector3(-doorWidth / 2 - halfRemainingWidth / 2, 0, 0);
            rightWallPos = position + new Vector3(doorWidth / 2 + halfRemainingWidth / 2, 0, 0);
            topWallPos = position + new Vector3(0, position.y/4, 0);

            leftWallScale = new Vector3(halfRemainingWidth, scale.y, wallThickness);
            rightWallScale = new Vector3(halfRemainingWidth, scale.y, wallThickness);
            topWallScale = new Vector3(doorWidth, topWallHeight, wallThickness);
        }
        else
        {
            float halfRemainingDepth = (scale.z - doorWidth) / 2;

            leftWallPos = position + new Vector3(0, 0, -doorWidth / 2 - halfRemainingDepth / 2);
            rightWallPos = position + new Vector3(0, 0, doorWidth / 2 + halfRemainingDepth / 2);
            topWallPos = position + new Vector3(0, position.y / 4, 0);

            leftWallScale = new Vector3(wallThickness, scale.y, halfRemainingDepth);
            rightWallScale = new Vector3(wallThickness, scale.y, halfRemainingDepth);
            topWallScale = new Vector3(wallThickness, topWallHeight, doorWidth);
        }

        // Create left part of the wall
        if (leftWallScale.x > 0 && leftWallScale.z > 0)
        {
            GameObject leftWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftWall.layer = LayerMask.NameToLayer("NotWalkable");
            leftWall.transform.position = leftWallPos;
            leftWall.transform.localScale = leftWallScale;
            leftWall.transform.parent = parent.transform;
        }

        // Create right part of the wall
        if (rightWallScale.x > 0 && rightWallScale.z > 0)
        {
            GameObject rightWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightWall.layer = LayerMask.NameToLayer("NotWalkable");
            rightWall.transform.position = rightWallPos;
            rightWall.transform.localScale = rightWallScale;
            rightWall.transform.parent = parent.transform;
        }

        // Create the top connector wall (correctly placed)
        GameObject topWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        topWall.transform.position = topWallPos;
        topWall.transform.localScale = topWallScale;
        topWall.transform.parent = parent.transform;
    }

    void BakeNavMesh()
    {
        navMeshSurface.BuildNavMesh();
    }
}




/*private void GenerateCorridors()
{
    List<(Vector3, Vector3)> corridors = new List<(Vector3, Vector3)>();

    HashSet<int> connectedDoors = new HashSet<int>();
    connectedDoors.Add(0); // Start from the first door

    while (connectedDoors.Count < doorPositions.Count)
    {
        int closestDoorA = -1;
        int closestDoorB = -1;
        float minDistance = float.MaxValue;

        foreach (int doorA in connectedDoors)
        {
            for (int doorB = 0; doorB < doorPositions.Count; doorB++)
            {
                if (connectedDoors.Contains(doorB)) continue;

                float distance = Vector3.Distance(doorPositions[doorA], doorPositions[doorB]);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestDoorA = doorA;
                    closestDoorB = doorB;
                }
            }
        }

        if (closestDoorA != -1 && closestDoorB != -1)
        {
            connectedDoors.Add(closestDoorB);
            corridors.Add((doorPositions[closestDoorA], doorPositions[closestDoorB]));
        }
    }

    // Generate tunnels between doors
    foreach (var (doorA, doorB) in corridors)
    {
        CreateCorridorTunnel(doorA, doorB);
    }
}


// Connect rooms with corridors
private void ConnectRooms()
{
    for (int i = 0; i < doorPositions.Count - 1; i++)
    {
        Vector3 door1 = doorPositions[i];
        Vector3 door2 = doorPositions[i + 1];
        CreateCorridorTunnel(door1, door2); // Create a tunnel between doors
    }
}

private void CreateCorridorTunnel(Vector3 door1Pos, Vector3 door2Pos)
{
    Vector3 direction = (door2Pos - door1Pos).normalized;
    float corridorLength = Vector3.Distance(door1Pos, door2Pos);

    float corridorWidth = 1.5f;  // Keep a consistent width
    float corridorHeight = 2.0f; // Keep a consistent height
    float wallThickness = 0.2f;

    Vector3 corridorCenter = (door1Pos + door2Pos) / 2;

    // Create floor
    GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
    floor.transform.position = corridorCenter + new Vector3(0, -corridorHeight / 2, 0);
    floor.transform.localScale = new Vector3(
        Mathf.Abs(direction.x) > Mathf.Abs(direction.z) ? corridorLength : corridorWidth,
        wallThickness,
        Mathf.Abs(direction.z) > Mathf.Abs(direction.x) ? corridorLength : corridorWidth
    );

    // Create ceiling
    GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
    ceiling.transform.position = corridorCenter + new Vector3(0, corridorHeight / 2, 0);
    ceiling.transform.localScale = floor.transform.localScale;

    // Left Wall
    GameObject leftWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
    leftWall.transform.position = corridorCenter + new Vector3(
        direction.z * corridorWidth / 2, 0, -direction.x * corridorWidth / 2
    );
    leftWall.transform.localScale = new Vector3(
        Mathf.Abs(direction.x) > Mathf.Abs(direction.z) ? corridorLength : wallThickness,
        corridorHeight,
        Mathf.Abs(direction.z) > Mathf.Abs(direction.x) ? corridorLength : wallThickness
    );

    // Right Wall
    GameObject rightWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
    rightWall.transform.position = corridorCenter + new Vector3(
        -direction.z * corridorWidth / 2, 0, direction.x * corridorWidth / 2
    );
    rightWall.transform.localScale = leftWall.transform.localScale;
}*/






/*private void GenerateWallWithDoor(GameObject parent, Vector3 position, Vector3 scale)
{
    float doorWidth = 1.5f;
    float doorHeight = 2.0f;
    float wallThickness = 0.1f;

    // Split the wall into two parts to create space for the door
    Vector3 leftWallPos = position + new Vector3(-doorWidth / 2, 0, 0);
    Vector3 rightWallPos = position + new Vector3(doorWidth / 2, 0, 0);

    Vector3 leftWallScale = new Vector3(scale.x / 2 - doorWidth / 2, scale.y, wallThickness);
    Vector3 rightWallScale = new Vector3(scale.x / 2 - doorWidth / 2, scale.y, wallThickness);

    // Left part of the wall
    if (leftWallScale.x > 0)
    {
        GameObject leftWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftWall.transform.position = leftWallPos;
        leftWall.transform.localScale = leftWallScale;
        leftWall.transform.parent = parent.transform;
    }

    // Right part of the wall
    if (rightWallScale.x > 0)
    {
        GameObject rightWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightWall.transform.position = rightWallPos;
        rightWall.transform.localScale = rightWallScale;
        rightWall.transform.parent = parent.transform;
    }

    // Create the door
    Vector3 doorPosition = new Vector3(position.x, doorHeight / 2, position.z);
    GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
    door.transform.position = doorPosition;
    door.transform.localScale = new Vector3(doorWidth, doorHeight, wallThickness);
    door.GetComponent<Renderer>().material.color = Color.blue;
    door.transform.parent = parent.transform;
}*/


/*public void GenerateDoorAndWindows(GameObject parent, Vector3 position, float width, float height, float wallHeight)
{
    Vector3 doorPosition = new Vector3(position.x, 1, position.z - height / 2);
    GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
    door.transform.position = doorPosition;
    door.transform.localScale = new Vector3(1, 2, 0.1f);
    door.GetComponent<Renderer>().material.color = Color.blue;
    door.transform.parent = parent.transform;

    Vector3 windowPosition = new Vector3(position.x + width / 2, 1.5f, position.z);
    GameObject window = GameObject.CreatePrimitive(PrimitiveType.Cube);
    window.transform.position = windowPosition;
    window.transform.localScale = new Vector3(0.1f, 1, 1);
    window.GetComponent<Renderer>().material.color = Color.red;
    window.transform.parent = parent.transform;
}
}*/