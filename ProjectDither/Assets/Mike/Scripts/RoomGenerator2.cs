using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation;

public class RoomGenerator2 : MonoBehaviour
{
    public NavMeshSurface navMeshSurface;
    private int numRooms = 2;
    private bool[][] status;
    public int[][] roomDoors;
    //private List<Vector3> doors = new List<Vector3>(); // Store doors
    private List<Bounds> roomBounds = new List<Bounds>();
    private List<Bounds> CorridorSegmentBounds = new List<Bounds>();
    private Vector2 roomSizeMinMax = new Vector2(12, 28);
    private Vector2 bigRoomSize = new Vector2(200, 200);
    public GameObject wallPrefab, doorPrefab, WindowPrefab;
    public GameObject angelPrefab, dogPrefab, foodBowlPrefab, playerPrefab;
    private GameObject playerInstance;
    private Vector3 roomsSpacingBuff = new Vector3(15, 0, 15);

    void InitializeStatus(int numRooms)
    {
        status = new bool[numRooms][]; // Initialize outer array

        for (int i = 0; i < numRooms; i++)
        {
            status[i] = new bool[4]; // Initialize inner arrays
        }
    }
    void InitializeRoomDoors(int numRooms)
    {
        roomDoors = new int[numRooms][]; // Initialize outer array

        for (int i = 0; i < numRooms; i++)
        {
            roomDoors[i] = new int[4]; // Initialize inner arrays
        }
    }
    public struct DoorData
    {
        //public int[][] roomDoors;
        public Vector3 position;
        public int facingDirection; // 1=North, 2=East, 3=South, 4=West
        public bool isConnected;

        public DoorData(Vector3 pos, int dir)
        {
            position = pos;
            facingDirection = dir;
            isConnected = false; // Initialize to not connected
        }
    }
    private List<DoorData> doors = new List<DoorData>();
    private class DoorConnection  // Helper class for connections
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
        GenerateRooms();
        GenerateCorridors();
        BakeNavMesh();
    }

    void GenerateBigRoom()
    {
        GameObject bigRoom = new GameObject("BigRoom");
        GenerateWalls(bigRoom, Vector3.zero, bigRoomSize.x, bigRoomSize.y, 10);
    }

    // Generates multiple rooms within the defined bounds, avoiding overlaps.
    public void GenerateRooms()
    {
        //(Same as before - no changes needed here)
        for (int i = 0; i < numRooms; i++)
        {
            bool roomPlaced = false;
            int maxAttempts = 200; // Avoid infinite loops
            int attempts = 0;
            while (!roomPlaced && attempts < maxAttempts)
            {
                attempts++;
                float roomWidth = Random.Range(roomSizeMinMax.x, roomSizeMinMax.y);
                float roomHeight = Random.Range(roomSizeMinMax.x, roomSizeMinMax.y);

                float posX = Random.Range(-bigRoomSize.x / 2 + roomWidth / 2, bigRoomSize.x / 2 - roomWidth / 2); //Uses bigRoomSize for placement bounds, as in original.
                float posZ = Random.Range(-bigRoomSize.y / 2 + roomWidth / 2, bigRoomSize.y / 2 - roomWidth / 2);

                Bounds newRoomBounds = new Bounds(new Vector3(posX, 0, posZ), new Vector3(roomWidth, 1, roomHeight));
                if (!IsOverlapping(newRoomBounds, roomBounds))
                {
                    roomBounds.Add(newRoomBounds);
                    roomPlaced = true;
                    GameObject room = new GameObject("Room_" + i);
                    room.transform.position = new Vector3(posX, 0, posZ);
                    GenerateWalls(room, room.transform.position, roomWidth, roomHeight, 20);
                }
            }
            //Object Spawns
            /*if (i == 0)
            {
                playerInstance = Instantiate(playerPrefab, new Vector3(roomBounds[0].center.x, 0.5f, roomBounds[0].center.z), Quaternion.identity);
                playerInstance.name = "Player";
                NiPlayerMovement playerScript = playerInstance.GetComponent<NiPlayerMovement>(); //Gets the script, but doesn't do anything with it. Kept as in original code.
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
            }*/
        }
    }

    // Checks if a new room bounds overlaps with any existing room bounds, considering a minimum spacing.
    private bool IsOverlapping(Bounds newBounds, List<Bounds> existingBounds)
    {
        float minSpacing = 10f; // The minimum distance between rooms.

        foreach (Bounds bounds in existingBounds)
        {
            // Create a copy of the existing bounds.  We MUST work with a copy,
            // otherwise we'll permanently modify the original room bounds!
            Bounds expandedBounds = bounds;

            // Expand the bounds by half the minimum spacing in each direction.
            // We use half because the expansion happens on *both* sides of the bounds.
            expandedBounds.Expand(roomsSpacingBuff);

            if (expandedBounds.Intersects(newBounds))
            {
                return true; // Overlaps with expanded bounds, so it's too close.
            }
        }
        return false; // No overlaps found.
    }

    // Generates walls for a room, including two walls with doors.
    private void GenerateWalls(GameObject parent, Vector3 position, float width, float height, float wallHeight)
    {
        //(Same as before)
        Vector3[] wallPositions =
         {
            new Vector3(position.x, wallHeight / 2, position.z + height / 2),  // North
            new Vector3(position.x, wallHeight / 2, position.z - height / 2),  // South
            new Vector3(position.x + width / 2, wallHeight / 2,  position.z), // West
            new Vector3(position.x - width / 2, wallHeight / 2,  position.z)  // East
        };

        Vector3[] wallScales = {
            new Vector3(width, wallHeight, 0.1f), // North/South
            new Vector3(width, wallHeight, 0.1f), // North/South
            new Vector3(0.1f, wallHeight, height),// West/East
            new Vector3(0.1f, wallHeight, height) // West/East
        };
        // Select two walls to have doors
        /*int doorWall1 = Random.Range(0, 4);
        int doorWall2;
        do { doorWall2 = Random.Range(0, 4); } while (doorWall2 == doorWall1);
        */
        for (int i = 0; i < 4; i++)
        {
            GenerateWallWithDoor(parent, wallPositions[i], wallScales[i]);
            
            /*if (i == doorWall1 || i == doorWall2)
            {
                GenerateWallWithDoor(parent, wallPositions[i], wallScales[i]);
            }
            else
            {
                GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.layer = LayerMask.NameToLayer("NotWalkable");
                wall.transform.position = wallPositions[i];
                wall.transform.localScale = wallScales[i];
                wall.transform.parent = parent.transform;
            }*/
        }
    }

    // Generates a wall section with a door opening and adds door data to the list.
    private void GenerateWallWithDoor(GameObject parent, Vector3 position, Vector3 scale)
    {
        float doorWidth = 3f;  // Increased door width
        float doorHeight = scale.y / 4;
        float wallThickness = 0.1f;
        float topWallHeight = scale.y - doorHeight;

        bool isHorizontal = scale.x > scale.z;

        Vector3 leftWallPos, rightWallPos, topWallPos, doorPos;
        Vector3 leftWallScale, rightWallScale, topWallScale;

        if (isHorizontal)
        {
            float halfRemainingWidth = (scale.x - doorWidth) / 2;
            leftWallPos = position + new Vector3(-doorWidth / 2 - halfRemainingWidth / 2, 0, 0);
            rightWallPos = position + new Vector3(doorWidth / 2 + halfRemainingWidth / 2, 0, 0);
            topWallPos = position + new Vector3(0, position.y / 4, 0);
            doorPos = new Vector3(position.x, 0, position.z);
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
            doorPos = new Vector3(position.x, 0, position.z);
            leftWallScale = new Vector3(wallThickness, scale.y, halfRemainingDepth);
            rightWallScale = new Vector3(wallThickness, scale.y, halfRemainingDepth);
            topWallScale = new Vector3(wallThickness, topWallHeight, doorWidth);
        }

        int facingDir = GetDoorDirection(doorPos); // doorPos is calculated *inside* GenerateWallWithDoor
        doors.Add(new DoorData(doorPos, facingDir));

        // Create walls
        GameObject leftWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftWall.layer = LayerMask.NameToLayer("NotWalkable");
        leftWall.transform.position = leftWallPos;
        leftWall.transform.localScale = leftWallScale;
        leftWall.transform.parent = parent.transform;

        GameObject rightWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightWall.layer = LayerMask.NameToLayer("NotWalkable");
        rightWall.transform.position = rightWallPos;
        rightWall.transform.localScale = rightWallScale;
        rightWall.transform.parent = parent.transform;

        GameObject topWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        topWall.transform.position = topWallPos;
        topWall.transform.localScale = topWallScale;
        topWall.transform.parent = parent.transform;
    }

    // Builds the navigation mesh after rooms and corridors are generated.
    void BakeNavMesh()
    {
        navMeshSurface.BuildNavMesh();
    }

    // --- Corridor Generation ---

    // Determines the facing direction of a door (North, East, South, or West).
    private int GetDoorDirection(Vector3 doorPosition)
    {
        int roomIndex = GetRoomIndex(doorPosition);
        if (roomIndex == -1)
        {
            Debug.LogError("Door is not within any room!");
            return 0; // Invalid direction
        }

        Bounds roomBound = roomBounds[roomIndex];
        string relativeDirection = GetRelativeDirection(doorPosition, roomBound.center);

        switch (relativeDirection)
        {
            case "North": return 1;
            case "East": return 2;
            case "South": return 3;
            case "West": return 4;
            default:
                Debug.LogError("Invalid door direction: " + relativeDirection);
                return 0; // Invalid direction
        }
    }

    // Determines the relative direction (North, East, South, West) of a target position from a reference position.
    string GetRelativeDirection(Vector3 targetPosition, Vector3 referencePosition)
    {
        //(Same as before)
        Vector3 directionVector = targetPosition - referencePosition;

        float x = directionVector.x;
        float z = directionVector.z;

        if (Mathf.Abs(x) > Mathf.Abs(z)) // Prioritize horizontal direction
        {
            return x < 0 ? "East" : "West";
        }
        else  // Prioritize vertical direction
        {
            return z > 0 ? "North" : "South";
        }
    }

    // Creates a corridor between two doors, handling different orientations.
    private void CreateDirectionalCorridor(Vector3 door1Pos, Vector3 door2Pos, int direction1, int direction2)
    {
        float corridorWidth = 3f;
        float corridorHeight = 7f;
        float wallThickness = 0.2f;

        int directionDifference = Mathf.Abs(direction1 - direction2);
        /*if (directionDifference > 2)
        {
            directionDifference = 4 - directionDifference;  // Wrap around
        }*/

        // Offset start and end points based on door direction
        Vector3 startPoint = door1Pos + GetDirectionVector(direction1) * (corridorWidth / 2f);
        Vector3 endPoint = door2Pos + GetDirectionVector(direction2) * (corridorWidth / 2f);

        List<Vector3> pathPoints = new List<Vector3>() { startPoint };


        if (direction1 == direction2)
        {
            // Same direction: 2 or 4 turns.
            if (direction1 == 1 || direction1 == 3) // North or South facing doors
            {
                //try using z
                Vector3 intermediate1 = new Vector3(startPoint.x, 0, (startPoint.z + endPoint.z) / 2);
                Vector3 intermediate2 = new Vector3(endPoint.x, 0, (startPoint.z + endPoint.z) / 2);
                if (IsValidSegment(startPoint, intermediate1, corridorWidth) && IsValidSegment(intermediate1, intermediate2, corridorWidth) && IsValidSegment(intermediate2, endPoint, corridorWidth))
                {
                    pathPoints.Add(intermediate1);
                    pathPoints.Add(intermediate2);
                    pathPoints.Add(endPoint);
                }
                else // if that doesnt work, try using x.
                {
                    intermediate1 = new Vector3((startPoint.x + endPoint.x) / 2, 0, startPoint.z);
                    intermediate2 = new Vector3((startPoint.x + endPoint.x) / 2, 0, endPoint.z);
                    if (IsValidSegment(startPoint, intermediate1, corridorWidth) && IsValidSegment(intermediate1, intermediate2, corridorWidth) && IsValidSegment(intermediate2, endPoint, corridorWidth))
                    {
                        pathPoints.Add(intermediate1);
                        pathPoints.Add(intermediate2);
                        pathPoints.Add(endPoint);
                    }
                    else //if that doesnt work try to add more points.
                    {
                        // Calculate midpoints for the extended path
                        Vector3 midPoint1 = startPoint + GetDirectionVector(direction1) * corridorWidth;
                        Vector3 midPoint2 = endPoint + GetDirectionVector(direction2) * corridorWidth;

                        intermediate1 = new Vector3(midPoint1.x, 0, midPoint2.z);
                        intermediate2 = new Vector3(endPoint.x, 0, midPoint2.z);

                        //check if valid segments and add them
                        if (IsValidSegment(startPoint, midPoint1, corridorWidth))
                        {
                            pathPoints.Add(midPoint1);
                        }
                        if (IsValidSegment(midPoint1, intermediate1, corridorWidth))
                        {
                            pathPoints.Add(intermediate1);
                        }
                        if (IsValidSegment(intermediate1, midPoint2, corridorWidth))
                        {
                            pathPoints.Add(midPoint2);
                        }
                        if (IsValidSegment(midPoint2, endPoint, corridorWidth))
                        {
                            pathPoints.Add(endPoint);
                        }
                    }
                }
            }
            else // Doors are facing East or West
            {
                //try using x
                Vector3 intermediate1 = new Vector3((startPoint.x + endPoint.x) / 2, 0, startPoint.z);
                Vector3 intermediate2 = new Vector3((startPoint.x + endPoint.x) / 2, 0, endPoint.z);
                if (IsValidSegment(startPoint, intermediate1, corridorWidth) && IsValidSegment(intermediate1, intermediate2, corridorWidth) && IsValidSegment(intermediate2, endPoint, corridorWidth))
                {
                    pathPoints.Add(intermediate1);
                    pathPoints.Add(intermediate2);
                    pathPoints.Add(endPoint);
                }
                else // if that doesn't work, try using z.
                {
                    intermediate1 = new Vector3(startPoint.x, 0, (startPoint.z + endPoint.z) / 2);
                    intermediate2 = new Vector3(endPoint.x, 0, (startPoint.z + endPoint.z) / 2);
                    if (IsValidSegment(startPoint, intermediate1, corridorWidth) && IsValidSegment(intermediate1, intermediate2, corridorWidth) && IsValidSegment(intermediate2, endPoint, corridorWidth))
                    {
                        pathPoints.Add(intermediate1);
                        pathPoints.Add(intermediate2);
                        pathPoints.Add(endPoint);
                    }

                    else  //if that doesnt work try to add more points.
                    {
                        // Calculate midpoints for the extended path
                        Vector3 midPoint1 = startPoint + GetDirectionVector(direction1) * corridorWidth;
                        Vector3 midPoint2 = endPoint + GetDirectionVector(direction2) * corridorWidth;

                        intermediate1 = new Vector3(midPoint2.x, 0, midPoint1.z);
                        intermediate2 = new Vector3(midPoint2.x, 0, endPoint.z);

                        //check if valid segments and add them
                        if (IsValidSegment(startPoint, midPoint1, corridorWidth))
                        {
                            pathPoints.Add(midPoint1);
                        }
                        if (IsValidSegment(midPoint1, intermediate1, corridorWidth))
                        {
                            pathPoints.Add(intermediate1);
                        }
                        if (IsValidSegment(intermediate1, midPoint2, corridorWidth))
                        {
                            pathPoints.Add(midPoint2);
                        }
                        if (IsValidSegment(midPoint2, endPoint, corridorWidth))
                        {
                            pathPoints.Add(endPoint);
                        }

                    }
                }
            }
        }
        else if (directionDifference == 1 || directionDifference == 3)
        {
            // Adjacent directions: 1 or 3 turns.

            // One-turn solution (L-shape)
            Vector3 intermediate1;
            if (direction1 == 1 || direction1 == 3) // Start door is N/S
            {
                intermediate1 = new Vector3(startPoint.x, 0, endPoint.z);
            }
            else // Start door is E/W
            {
                intermediate1 = new Vector3(endPoint.x, 0, startPoint.z);
            }

            if (IsValidSegment(startPoint, intermediate1, corridorWidth) && IsValidSegment(intermediate1, endPoint, corridorWidth))
            {
                pathPoints.Add(intermediate1);
                pathPoints.Add(endPoint);
            }
            else
            {
                // Three-turn solution.  Extend both start and end points.

                Vector3 midPoint1 = startPoint + GetDirectionVector(direction1) * corridorWidth;
                Vector3 midPoint2 = endPoint + GetDirectionVector(direction2) * corridorWidth;

                // Two intermediate points to connect the extended lines.
                if (direction1 == 1 || direction1 == 3)
                {
                    intermediate1 = new Vector3(midPoint1.x, 0, midPoint2.z);
                }
                else
                {
                    intermediate1 = new Vector3(midPoint2.x, 0, midPoint1.z);
                }


                if (IsValidSegment(startPoint, midPoint1, corridorWidth))
                {
                    pathPoints.Add(midPoint1);
                }
                if (IsValidSegment(midPoint1, intermediate1, corridorWidth))
                {
                    pathPoints.Add(intermediate1);
                }
                if (IsValidSegment(intermediate1, midPoint2, corridorWidth))
                {
                    pathPoints.Add(midPoint2);
                }
                if (IsValidSegment(midPoint2, endPoint, corridorWidth))
                {
                    pathPoints.Add(endPoint);
                }
            }
        }
        else if (directionDifference == 2)
        {
            // Opposite directions: 0, 2, or 4 turns

            // Zero-turn solution (straight line).
            if (IsValidSegment(startPoint, endPoint, corridorWidth))
            {
                pathPoints.Add(endPoint);
            }
            else
            {
                // Two-turn solution.  Try both possible intermediate points.
                Vector3 intermediate1;
                if (direction1 == 1 || direction1 == 3) // Start door is N/S
                {
                    intermediate1 = new Vector3(startPoint.x, 0, (startPoint.z + endPoint.z) / 2);
                    Vector3 intermediate2 = new Vector3(endPoint.x, 0, (startPoint.z + endPoint.z) / 2);
                    if (IsValidSegment(startPoint, intermediate1, corridorWidth) && IsValidSegment(intermediate1, intermediate2, corridorWidth) && IsValidSegment(intermediate2, endPoint, corridorWidth))
                    {
                        pathPoints.Add(intermediate1);
                        pathPoints.Add(intermediate2);
                        pathPoints.Add(endPoint);
                    }
                    else
                    {
                        intermediate1 = new Vector3((startPoint.x + endPoint.x) / 2, 0, startPoint.z);
                        intermediate2 = new Vector3((startPoint.x + endPoint.x) / 2, 0, endPoint.z);
                        if (IsValidSegment(startPoint, intermediate1, corridorWidth) && IsValidSegment(intermediate1, intermediate2, corridorWidth) && IsValidSegment(intermediate2, endPoint, corridorWidth))
                        {
                            pathPoints.Add(intermediate1);
                            pathPoints.Add(intermediate2);
                            pathPoints.Add(endPoint);

                        }
                        else
                        {
                            //four turn solution
                            Vector3 midPoint1 = startPoint + GetDirectionVector(direction1) * corridorWidth;
                            Vector3 midPoint2 = endPoint + GetDirectionVector(direction2) * corridorWidth;
                            intermediate1 = new Vector3(midPoint1.x, 0, midPoint2.z);

                            if (IsValidSegment(startPoint, midPoint1, corridorWidth))
                            {
                                pathPoints.Add(midPoint1);
                            }
                            if (IsValidSegment(midPoint1, intermediate1, corridorWidth))
                            {
                                pathPoints.Add(intermediate1);
                            }
                            if (IsValidSegment(intermediate1, midPoint2, corridorWidth))
                            {
                                pathPoints.Add(midPoint2);
                            }
                            if (IsValidSegment(midPoint2, endPoint, corridorWidth))
                            {
                                pathPoints.Add(endPoint);
                            }

                        }
                    }

                }
                else // Start door is E/W
                {
                    intermediate1 = new Vector3((startPoint.x + endPoint.x) / 2, 0, startPoint.z);
                    Vector3 intermediate2 = new Vector3((startPoint.x + endPoint.x) / 2, 0, endPoint.z);
                    if (IsValidSegment(startPoint, intermediate1, corridorWidth) && IsValidSegment(intermediate1, intermediate2, corridorWidth) && IsValidSegment(intermediate2, endPoint, corridorWidth))
                    {
                        pathPoints.Add(intermediate1);
                        pathPoints.Add(intermediate2);
                        pathPoints.Add(endPoint);
                    }
                    else
                    {
                        intermediate1 = new Vector3(startPoint.x, 0, (startPoint.z + endPoint.z) / 2);
                        intermediate2 = new Vector3(endPoint.x, 0, (startPoint.z + endPoint.z) / 2);
                        if (IsValidSegment(startPoint, intermediate1, corridorWidth) && IsValidSegment(intermediate1, intermediate2, corridorWidth) && IsValidSegment(intermediate2, endPoint, corridorWidth))
                        {
                            pathPoints.Add(intermediate1);
                            pathPoints.Add(intermediate2);
                            pathPoints.Add(endPoint);
                        }
                        else
                        {
                            //four turn solution
                            Vector3 midPoint1 = startPoint + GetDirectionVector(direction1) * corridorWidth;
                            Vector3 midPoint2 = endPoint + GetDirectionVector(direction2) * corridorWidth;

                            intermediate1 = new Vector3(midPoint2.x, 0, midPoint1.z);


                            if (IsValidSegment(startPoint, midPoint1, corridorWidth))
                            {
                                pathPoints.Add(midPoint1);
                            }
                            if (IsValidSegment(midPoint1, intermediate1, corridorWidth))
                            {
                                pathPoints.Add(intermediate1);
                            }
                            if (IsValidSegment(intermediate1, midPoint2, corridorWidth))
                            {
                                pathPoints.Add(midPoint2);
                            }
                            if (IsValidSegment(midPoint2, endPoint, corridorWidth))
                            {
                                pathPoints.Add(endPoint);
                            }

                        }
                    }
                }
            }
        }


        // Build the corridor segments based on pathPoints.
        for (int i = 0; i < pathPoints.Count - 1; i++)
        {
            if(i == 0)
            {
                switch (direction1)
                {
                    case 1: CreateCorridorSegment(pathPoints[i]+ new Vector3(roomsSpacingBuff.x,0,0), pathPoints[i + 1], corridorWidth, corridorHeight, wallThickness);
                    break;
                    case 2: CreateCorridorSegment(pathPoints[i]+ new Vector3(0,0,roomsSpacingBuff.z), pathPoints[i + 1], corridorWidth, corridorHeight, wallThickness);
                    break;
                    case 3: CreateCorridorSegment(pathPoints[i] - new Vector3(roomsSpacingBuff.x,0,0), pathPoints[i + 1], corridorWidth, corridorHeight, wallThickness);
                    break;
                    default: CreateCorridorSegment(pathPoints[i] - new Vector3(0,0,roomsSpacingBuff.z), pathPoints[i + 1], corridorWidth, corridorHeight, wallThickness);
                    break;
                    
                }
            }
            else if(i == pathPoints.Count - 2)
            {
                switch (direction2)
                {
                    case 1: CreateCorridorSegment(pathPoints[i]- new Vector3(roomsSpacingBuff.x,0,0), pathPoints[i + 1], corridorWidth, corridorHeight, wallThickness);
                    break;
                    case 2: CreateCorridorSegment(pathPoints[i]- new Vector3(0,0,roomsSpacingBuff.z), pathPoints[i + 1], corridorWidth, corridorHeight, wallThickness);
                    break;
                    case 3: CreateCorridorSegment(pathPoints[i]+ new Vector3(roomsSpacingBuff.x,0,0), pathPoints[i + 1], corridorWidth, corridorHeight, wallThickness);
                    break;
                    default: CreateCorridorSegment(pathPoints[i]+ new Vector3(0,0,roomsSpacingBuff.z), pathPoints[i + 1], corridorWidth, corridorHeight, wallThickness);
                    break;
                    
                }
            }
            else
            {
                CreateCorridorSegment(pathPoints[i], pathPoints[i + 1], corridorWidth, corridorHeight, wallThickness);
            }
            
        }
    }

    // Returns a unit vector representing a given direction (1=North, 2=East, 3=South, 4=West).
    private Vector3 GetDirectionVector(int direction)
    {
        switch (direction)
        {
            case 1: return Vector3.forward;  // North
            case 2: return Vector3.right;    // East
            case 3: return Vector3.back;     // South
            case 4: return Vector3.left;     // West
            default: return Vector3.zero;
        }
    }

    // Creates a single segment of a corridor (floor, ceiling, and walls).
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
        // Store Corridor Bounds
        Bounds segmentBounds = new Bounds(segmentCenter, isHorizontal ? new Vector3(segmentLength, height, width) : new Vector3(width, height, segmentLength));
        CorridorSegmentBounds.Add(segmentBounds);
    }

    // Checks if a corridor segment is valid (doesn't overlap with any rooms).
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

        // Check against other corridor segments ---
        foreach (Bounds otherSegmentBounds in CorridorSegmentBounds)
        {
            if (segmentBounds.Intersects(otherSegmentBounds))
            {
                return false; // Overlaps another corridor segment.
            }
        }

        return true; // No overlaps found.
    }

    // Generates corridors between rooms, connecting doors based on proximity and avoiding overlaps.
    private void GenerateCorridors()
    {
        if (doors.Count == 0) return;

        List<DoorConnection> allConnections = new List<DoorConnection>();

        // Create all possible door-to-door connections between DIFFERENT rooms.
        for (int i = 0; i < doors.Count; i++)
        {
            for (int j = i + 1; j < doors.Count; j++)
            {
                int roomA = GetRoomIndex(doors[i].position);
                int roomB = GetRoomIndex(doors[j].position);
                if (roomA != roomB && roomA != -1 && roomB != -1) // Different rooms
                {
                    float distance = Vector3.Distance(doors[i].position, doors[j].position);
                    allConnections.Add(new DoorConnection(i, j, distance));
                }
            }
        }

        // Sort connections by distance (shortest first).
        allConnections.Sort((a, b) => a.Distance.CompareTo(b.Distance));

        // Iteratively connect doors.
        List<DoorConnection> selectedConnections = new List<DoorConnection>();
        HashSet<int> connectedDoors = new HashSet<int>();

        foreach (DoorConnection connection in allConnections)
        {
            // Access facingDirection using .facingDirection
            if (!doors[connection.DoorIndexA].isConnected && !doors[connection.DoorIndexB].isConnected)
            {
                selectedConnections.Add(connection);

                // Mark doors as connected using the struct!
                DoorData doorA = doors[connection.DoorIndexA];
                doorA.isConnected = true;
                doors[connection.DoorIndexA] = doorA;  // Reassign the modified struct

                DoorData doorB = doors[connection.DoorIndexB];
                doorB.isConnected = true;
                doors[connection.DoorIndexB] = doorB;

            }
        }

        // Create the corridors.
        foreach (DoorConnection connection in selectedConnections)
        {
            // Now you can directly access .facingDirection
            CreateDirectionalCorridor(doors[connection.DoorIndexA].position, doors[connection.DoorIndexB].position,
                                     doors[connection.DoorIndexA].facingDirection, doors[connection.DoorIndexB].facingDirection);
        }
    }

    // Gets the index of the room that contains a given door position.
    private int GetRoomIndex(Vector3 doorPosition)
    {
        for (int i = 0; i < roomBounds.Count; i++)
        {
            // Expand the bounds slightly to account for doors on the edges.
            Bounds expandedBounds = roomBounds[i];
            expandedBounds.Expand(new Vector3(3f, 0, 3f)); //Expand by the door width + extra for connections

            if (expandedBounds.Contains(doorPosition))
            {
                return i;
            }
        }
        return -1;
    }
}