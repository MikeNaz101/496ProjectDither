using System.Collections.Generic;
//using Unity.VisualScripting;
using UnityEngine;
using Unity.AI.Navigation;
using static UnityEditor.Experimental.GraphView.GraphView;

public class RoomGenerator2 : MonoBehaviour
{
    public NavMeshSurface navMeshSurface;
    private int numRooms = 15;
    private List<Vector3> doorPositions = new List<Vector3>();
    private List<Vector3> doors = new List<Vector3>(); // Store doors
    private List<Bounds> roomBounds = new List<Bounds>();
    //private List<> doors = new List<Vector3>();
    private Vector2 roomSizeMinMax = new Vector2(10, 25);
    private Vector2 bigRoomSize = new Vector2(200, 200);
    public GameObject wallPrefab, doorPrefab, WindowPrefab;
    public GameObject angelPrefab, dogPrefab, foodBowlPrefab, playerPrefab;
    private GameObject playerInstance;

    // Helper class to represent a connection between two doors (an edge in our graph)
    private class DoorConnection
    {
        public int DoorIndexA;
        public int DoorIndexB;
        public float Distance;

        public DoorConnection(int a, int b, float distance)
        {
            DoorIndexA = a;
            DoorIndexB = b;
            Distance = distance;
        }
    }

    void Start()
    {
        GenerateBigRoom();
        GenerateRooms();
        //ConnectRooms();
        GenerateCorridors();  // Call the revised corridor generation
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
                    //room.layer = LayerMask.NameToLayer("Room");
                    Debug.Log("HEre");
                }
            }
            if (i == 0)
            {
                playerInstance = Instantiate(playerPrefab, new Vector3(roomBounds[0].center.x, 0.5f, roomBounds[0].center.z), Quaternion.identity);
                playerInstance.name = "Player";
                NiPlayerMovement playerScript = playerInstance.GetComponent<NiPlayerMovement>();
                roomPlaced = false;
            }
            else if (i == numRooms / 2)
            {
                // get the 4 corner positions for the dog
                Vector3[] corners = new Vector3[]
                {
                    new Vector3(roomBounds[numRooms / 2].min.x, 0, roomBounds[numRooms / 2].min.z), // Bottom-left
                    new Vector3(roomBounds[numRooms / 2].max.x, 0, roomBounds[numRooms / 2].min.z), // Bottom-right
                    new Vector3(roomBounds[numRooms / 2].min.x, 0, roomBounds[numRooms / 2].max.z), // Top-left
                    new Vector3(roomBounds[numRooms / 2].max.x, 0, roomBounds[numRooms / 2].max.z)  // Top-right
                };
                GameObject dogInstance = Instantiate(dogPrefab, new Vector3(roomBounds[numRooms / 2].center.x, 0.5f, roomBounds[numRooms / 2].center.z), Quaternion.identity);
                dogInstance.name = "Dog";
                DogStateManager dogScript = dogInstance.GetComponent<DogStateManager>();
                dogScript.corners = corners;
                // Spawn the food bowl at a random point inside the room
                float bowlX = Random.Range(roomBounds[numRooms / 2].min.x, roomBounds[numRooms / 2].max.x);
                float bowlZ = Random.Range(roomBounds[numRooms / 2].min.z, roomBounds[numRooms / 2].max.z);
                GameObject foodBowlInstance = Instantiate(foodBowlPrefab, new Vector3(bowlX, 0.5f, bowlZ), Quaternion.identity);
                foodBowlInstance.name = "FoodBowl";
                dogScript.foodBowl = foodBowlInstance;
                dogScript.player = playerInstance;
            }
            else if (i == numRooms - 2)
            {
                GameObject angelInstance = Instantiate(angelPrefab, new Vector3(roomBounds[numRooms - 2].center.x, 0.5f, roomBounds[numRooms - 2].center.z), Quaternion.identity);
                angelInstance.name = "Angel";
                MikesWeepingAngel angelScript = angelInstance.GetComponent<MikesWeepingAngel>();
                angelScript.playerCam = playerInstance.GetComponentInChildren<Camera>();
                roomPlaced = false;
            }
        }
        GenerateCorridors();
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
        //GameObject door[]
        float doorWidth = 1.5f;
        float doorHeight = scale.y / 4; // 1/4th of total wall height
        float wallThickness = 0.1f;
        float topWallHeight = scale.y - doorHeight; // The remaining 3/4 height

        bool isHorizontal = scale.x > scale.z; // Check if the wall runs along the X-axis

        Vector3 leftWallPos, rightWallPos, topWallPos;
        Vector3 leftWallScale, rightWallScale, topWallScale;
        Vector3 doorPos;

        if (isHorizontal)
        {
            float halfRemainingWidth = (scale.x - doorWidth) / 2;

            leftWallPos = position + new Vector3(-doorWidth / 2 - halfRemainingWidth / 2, 0, 0);
            rightWallPos = position + new Vector3(doorWidth / 2 + halfRemainingWidth / 2, 0, 0);
            topWallPos = position + new Vector3(0, position.y / 4, 0);
            doorPos = new Vector3(position.x, 0, position.z); // Door is centered on the wall

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
            doorPos = new Vector3(position.x, 0, position.z); // Door is centered on the wall.

            leftWallScale = new Vector3(wallThickness, scale.y, halfRemainingDepth);
            rightWallScale = new Vector3(wallThickness, scale.y, halfRemainingDepth);
            topWallScale = new Vector3(wallThickness, topWallHeight, doorWidth);
        }
        doors.Add(doorPos);
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
            //doors.Add(new Vector3(leftWallPos.x, 0, rightWallPos.z));
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

    private void CreateOrthogonalCorridor(Vector3 door1Pos, Vector3 door2Pos)
    {
        float corridorWidth = 3f;
        float corridorHeight = 7f;
        float wallThickness = 0.2f;
        float halfWidth = corridorWidth / 2f;

        // 1. Determine door directions.
        Vector3 door1Direction = GetDoorDirection(door1Pos);
        Vector3 door2Direction = GetDoorDirection(door2Pos);

        // 2. Calculate start and end offset points.
        Vector3 startPoint = door1Pos + door1Direction * halfWidth;
        Vector3 endPoint = door2Pos + door2Direction * halfWidth;

        // 3. Pathfinding with offsets and parallel wall handling.
        List<Vector3> pathPoints = new List<Vector3>();
        pathPoints.Add(startPoint);

        // Check if the initial direction is parallel to the target door's wall.
        if (Vector3.Dot(door1Direction, door2Direction) < -0.9f) // Opposite directions (parallel walls)
        {
            // Special handling for parallel walls.

            // First, move along the initial direction (away from door1's wall).
            Vector3 intermediate1 = startPoint + door1Direction * halfWidth;
            pathPoints.Add(intermediate1);

            // Determine the direction to move towards the endPoint.
            Vector3 towardsEndPoint = (endPoint - intermediate1).normalized;

            // Move parallel to the target wall until aligned with endPoint on one axis.
            Vector3 intermediate2;
            if (Mathf.Abs(door2Direction.x) > 0.1f) // door2 faces along X
            {
                intermediate2 = intermediate1 + new Vector3(0, 0, endPoint.z - intermediate1.z); // Align Z
                if (!IsValidSegment(intermediate1, intermediate2, corridorWidth)) //check for valid path
                {
                    intermediate2 = intermediate1 + new Vector3(endPoint.x - intermediate1.x, 0, 0); //if invalid use x
                }
            }
            else // door2 faces along Z
            {
                intermediate2 = intermediate1 + new Vector3(endPoint.x - intermediate1.x, 0, 0); // Align X
                if (!IsValidSegment(intermediate1, intermediate2, corridorWidth))//check for valid path
                {
                    intermediate2 = intermediate1 + new Vector3(0, 0, endPoint.z - intermediate1.z); //if invalid use z

                }
            }

            pathPoints.Add(intermediate2);

            // Final segment to endPoint (already offset).
            // No need for intermediate3; connect directly.

        }
        else
        {
            // Regular handling (non-parallel walls):  One or two turns.

            Vector3 intermediate1 = new Vector3(endPoint.x, 0, startPoint.z);
            Vector3 intermediate2 = endPoint;


            if (intermediate1 != startPoint && intermediate1 != endPoint)
            {
                if (IsValidSegment(startPoint, intermediate1, corridorWidth))
                {
                    pathPoints.Add(intermediate1);

                }


            }

            if (intermediate2 != startPoint && intermediate2 != intermediate1)
            {
                if (IsValidSegment(intermediate1, intermediate2, corridorWidth))
                {
                    pathPoints.Add(intermediate2);
                }

            }

        }

        pathPoints.Add(endPoint);

        // 4. Build the corridor segments.
        for (int i = 0; i < pathPoints.Count - 1; i++)
        {
            CreateCorridorSegment(pathPoints[i], pathPoints[i + 1], corridorWidth, corridorHeight, wallThickness);
        }
    }

    private bool IsValidSegment(Vector3 start, Vector3 end, float corridorWidth)
    {
        // Calculate the center and size of the segment.
        Vector3 center = (start + end) / 2;
        Vector3 direction = (end - start).normalized;
        float length = Vector3.Distance(start, end);
        Vector3 size;

        // Determine size based on orientation.  Add extra width for overlap check.
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.z)) // Horizontal
        {
            size = new Vector3(length, 1f, corridorWidth + 1f);  // +1f for tolerance
        }
        else // Vertical
        {
            size = new Vector3(corridorWidth + 1f, 1f, length);  // +1f for tolerance
        }


        Bounds segmentBounds = new Bounds(center, size);

        // Check against ALL room bounds.
        foreach (Bounds roomBound in roomBounds)
        {
            if (segmentBounds.Intersects(roomBound))
            {
                return false; // Overlaps a room, so it's invalid.
            }
        }

        return true; // No overlaps found.
    }


    // Helper function to get door direction based on surrounding walls.
    private Vector3 GetDoorDirection(Vector3 doorPos)
    {
        // A small offset to check for walls.  Adjust as needed.
        float checkDistance = 0.6f;


        // Check for walls in each direction (+X, -X, +Z, -Z).
        if (Physics.Raycast(doorPos, Vector3.right, checkDistance, LayerMask.GetMask("NotWalkable")))
        {
            return Vector3.left; // Wall to the right, so door faces left.
        }
        if (Physics.Raycast(doorPos, Vector3.left, checkDistance, LayerMask.GetMask("NotWalkable")))
        {
            return Vector3.right; // Wall to the left, so door faces right.
        }
        if (Physics.Raycast(doorPos, Vector3.forward, checkDistance, LayerMask.GetMask("NotWalkable")))
        {
            return Vector3.back; // Wall in front, so door faces back.
        }
        if (Physics.Raycast(doorPos, Vector3.back, checkDistance, LayerMask.GetMask("NotWalkable")))
        {
            return Vector3.forward; // Wall behind, so door faces forward.
        }

        // If no wall is detected, assume it faces forward (you might need a better default).
        Debug.LogWarning("Could not determine door direction.  Assuming forward.");
        return Vector3.forward;
    }

    private void CreateCorridorSegment(Vector3 startPos, Vector3 endPos, float width, float height, float thickness)
    {
        // Calculate the direction and length of this segment.
        Vector3 direction = (endPos - startPos).normalized;
        float segmentLength = Vector3.Distance(startPos, endPos);

        // Calculate the center of the segment.
        Vector3 segmentCenter = (startPos + endPos) / 2;

        // Determine the orientation (horizontal or vertical).
        bool isHorizontal = Mathf.Abs(direction.x) > Mathf.Abs(direction.z);

        // Create floor.
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.transform.position = segmentCenter + new Vector3(0, -height / 2, 0);
        floor.transform.localScale = isHorizontal
            ? new Vector3(segmentLength, thickness, width)
            : new Vector3(width, thickness, segmentLength);

        // Create ceiling.
        GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ceiling.transform.position = segmentCenter + new Vector3(0, height / 2, 0);
        ceiling.transform.localScale = floor.transform.localScale;

        // Create walls.
        GameObject wall1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall1.layer = LayerMask.NameToLayer("NotWalkable");

        GameObject wall2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall2.layer = LayerMask.NameToLayer("NotWalkable");

        if (isHorizontal)
        {
            wall1.transform.position = segmentCenter + new Vector3(0, 0, width / 2);
            wall2.transform.position = segmentCenter + new Vector3(0, 0, -width / 2);
            wall1.transform.localScale = new Vector3(segmentLength, height, thickness);
            wall2.transform.localScale = new Vector3(segmentLength, height, thickness);
        }
        else
        {
            wall1.transform.position = segmentCenter + new Vector3(width / 2, 0, 0);
            wall2.transform.position = segmentCenter + new Vector3(-width / 2, 0, 0);
            wall1.transform.localScale = new Vector3(thickness, height, segmentLength);
            wall2.transform.localScale = new Vector3(thickness, height, segmentLength);
        }
    }

    private void GenerateCorridors()
    {
        if (doors.Count == 0) return;

        List<DoorConnection> allConnections = new List<DoorConnection>();

        // 1. Create all possible door-to-door connections between DIFFERENT rooms.
        for (int i = 0; i < doors.Count; i++)
        {
            for (int j = i + 1; j < doors.Count; j++)
            {
                int roomA = GetRoomIndex(doors[i]);
                int roomB = GetRoomIndex(doors[j]);
                if (roomA != roomB && roomA != -1 && roomB != -1) // Different rooms and valid indices.
                {
                    float distance = Vector3.Distance(doors[i], doors[j]);
                    allConnections.Add(new DoorConnection(i, j, distance));
                }
            }
        }

        // 2. Sort connections by distance (shortest first).
        allConnections.Sort((a, b) => a.Distance.CompareTo(b.Distance));

        // 3. Iteratively connect doors, respecting the one-connection-per-door rule.
        List<DoorConnection> selectedConnections = new List<DoorConnection>();
        HashSet<int> connectedDoors = new HashSet<int>(); // Track connected *doors*.

        foreach (DoorConnection connection in allConnections)
        {
            // Check if *neither* door in this connection has already been connected.
            if (!connectedDoors.Contains(connection.DoorIndexA) && !connectedDoors.Contains(connection.DoorIndexB))
            {
                selectedConnections.Add(connection);
                connectedDoors.Add(connection.DoorIndexA);
                connectedDoors.Add(connection.DoorIndexB);
            }
        }

        // 4. Create the corridors.
        foreach (DoorConnection connection in selectedConnections)
        {
            CreateOrthogonalCorridor(doors[connection.DoorIndexA], doors[connection.DoorIndexB]);
        }
    }



    private int GetRoomIndex(Vector3 doorPosition)
    {
        for (int i = 0; i < roomBounds.Count; i++)
        {
            // Expand the bounds slightly to account for doors on the edges.  Adjust the value as needed.
            Bounds expandedBounds = roomBounds[i];
            expandedBounds.Expand(new Vector3(0.6f, 0, 0.6f)); //Expand by the door width

            if (expandedBounds.Contains(doorPosition))
            {
                return i;
            }
        }
        return -1; // Door is not within any room (shouldn't happen, but handle it)
    }
}
    /*private void CreateCorridorTunnel(Vector3 door1Pos, Vector3 door2Pos)
    {
        //Debug.Log("create door");
        Vector3 direction = (door2Pos - door1Pos).normalized;
        float corridorLength = Vector3.Distance(door1Pos, door2Pos);

        float corridorWidth = 3f;  // Keep a consistent width
        float corridorHeight = 7f; // Keep a consistent height
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
        leftWall.layer = LayerMask.NameToLayer("NotWalkable");
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
        rightWall.layer = LayerMask.NameToLayer("NotWalkable");
        rightWall.transform.position = corridorCenter + new Vector3(
            -direction.z * corridorWidth / 2, 0, direction.x * corridorWidth / 2
        );
        rightWall.transform.localScale = leftWall.transform.localScale;
    }*/
