using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation;
using System.CodeDom.Compiler;

public class RoomGenerator3 : MonoBehaviour
{
    public NavMeshSurface navMeshSurface;
    private int numRooms = 5;
    private int maxDoorsPerRoom = 4;
    private bool[][] status;
    public int[,] roomDoors;
    //private List<Vector3> doors = new List<Vector3>(); // Store doors
    //private List<Bounds> corridorBounds = new List<Bounds>();
    private List<List<Vector3>> existingCorridors = new List<List<Vector3>>(); // List of existing corridors (each corridor is a list of Vector3 points)
    private List<Bounds> CorridorSegmentBounds = new List<Bounds>();
    private Vector2 roomSizeMinMax = new Vector2(12, 28);
    private Vector2 bigRoomSize = new Vector2(200, 200);
    public GameObject wallPrefab, doorPrefab, WindowPrefab;
    public GameObject angelPrefab, dogPrefab, foodBowlPrefab, playerPrefab;
    private GameObject playerInstance;
    //private Vector3 roomsSpacingBuff = new Vector3(15, 0, 15);
    
    public class Room
    {
        public Bounds roomBounds;
        public Vector3 position;

        public Room(Bounds bounds, Vector3 pos)
        {
            roomBounds = bounds;
            position = pos;
        }
    }

    public class Door
    {
        public Vector3 position;
        public bool isConnected;
        public int direction;
        public Room associatedRoom; // Direct reference

        public Door(Vector3 pos, bool connected, int dir, Room room)
        {
            position = pos;
            isConnected = connected;
            direction = dir;
            associatedRoom = room;
        }
    }
    private List<Room> rooms = new List<Room>();
    private List<Door> doors = new List<Door>();

    void Start()
    {
        GenerateRooms();
        CreateCorridors();
        BakeNavMesh();
    }

    /*void GenerateBigRoom()
    {
        GameObject bigRoom = new GameObject("BigRoom");
        GenerateWalls(bigRoom, Vector3.zero, bigRoomSize.x, bigRoomSize.y, 10);
    }*/

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
                if (!IsOverlapping(newRoomBounds, rooms))
                {
                    //roomBounds.Add(newRoomBounds);
                    roomPlaced = true;
                    GameObject room = new GameObject("Room_" + i);
                    room.transform.position = new Vector3(posX, 0, posZ);

                    // Create a Room object
                    Room newRoom = new Room(newRoomBounds, room.transform.position);
                    // Add the Room object to the list
                    rooms.Add(newRoom);
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
        
        roomDoors = new int[rooms.Count, maxDoorsPerRoom];

        for (int i = 0; i < rooms.Count; i++)
        {
            for (int j = 0; j < maxDoorsPerRoom; j++)
            {
                roomDoors[i, j] = j; // Store the door index in the 2D array
                // Check if a door exists for this room and if it lies within the room bounds
                /*if (j < doors.Count && rooms[i].roomBounds.Contains(doors[j].position))
                {
                    roomDoors[i, j] = j; // Store the door index in the 2D array
                }
                else
                {
                    roomDoors[i, j] = -1; // No door assigned (use -1 or another indicator)
                }*/
            }
        }
    }

    // Checks if a new room bounds overlaps with any existing room bounds, considering a minimum spacing.
    private bool IsOverlapping(Bounds newBounds, List<Room> existingRooms)
    {
        float minSpacing = 10f; // Minimum distance between rooms.

        foreach (Room room in existingRooms)
        {
            // Create a copy of the existing room's bounds.
            Bounds expandedBounds = room.roomBounds;

            // Expand the bounds by half the minimum spacing in each direction.
            expandedBounds.Expand(minSpacing);

            if (expandedBounds.Intersects(newBounds))
            {
                return true; // Overlaps with expanded bounds, so it's too close.
            }
        }
        return false; // No overlaps found.
    }
    
    /*private bool IsOverlapping(Bounds newBounds, List<Bounds> existingBounds)
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
    }*/
    private bool IsCorridorOverlapping(List<Vector3> corridorPath, List<Room> existingRooms, List<List<Vector3>> existCorridors, float spacingBuffer)
    {
        // Check against existing rooms
        foreach (Room room in existingRooms)
        {
            Bounds expandedBounds = room.roomBounds;
            expandedBounds.Expand(spacingBuffer);

            foreach (Vector3 point in corridorPath)
            {
                if (expandedBounds.Contains(point))
                {
                    return true; // Overlaps a room
                }
            }
        }

        // Check against existing corridors
        foreach (List<Vector3> corridor in existCorridors)
        {
            foreach (Vector3 corridorPoint in corridor)
            {
                foreach (Vector3 newCorridorPoint in corridorPath)
                {
                    if (Vector3.Distance(corridorPoint, newCorridorPoint) < spacingBuffer)
                    {
                        return true; // Too close to an existing corridor
                    }
                }
            }
        }

        return false; // No overlaps found, safe to build
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
        doors.Add(new Door(doorPos, false, facingDir, rooms[rooms.Count-1]));

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

        Bounds roomBound = rooms[rooms.Count-1].roomBounds;
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

    private void CreateCorridors()
    {
        //This list is going to store the points in which the corridor takes to reach the end door. 
        //this will be used to test overlapping and then draw out the corridors if not overlapping.
        List<Vector3> currentPathPoints = new List<Vector3>(); 
        List<Vector3> pathPoints = new List<Vector3>(); 

        for(int i = 0; i<rooms.Count; i++)
        {
            for(int j = i+1; j<rooms.Count; j++)
            {
                for(int k = 0; k<maxDoorsPerRoom; k++)
                {
                    Debug.Log("roomDoors[i][k] = " + roomDoors[i,k]);
                    Debug.Log("roomDoors[j,(k+2)%4] = " + roomDoors[j,(k+2)%4]);
                    Door startingPoint = doors[roomDoors[i,k]];
                    Door endingPoint = doors[roomDoors[j,(k+2)%4]];
                    currentPathPoints.Add(startingPoint.position);
                    float vertDirection = startingPoint.position.z - endingPoint.position.z;
                    float horizDirection = startingPoint.position.x - endingPoint.position.x;

                    //This is to draw out from the starting door.
                    if (!startingPoint.isConnected && !endingPoint.isConnected)
                    {
                        if(startingPoint.direction == 3) // if starting door faces south (down)
                        {
                            currentPathPoints.Add(startingPoint.position - new Vector3(0,0,10f));
                        }
                        else if(startingPoint.direction == 2 || startingPoint.direction == 4) // if starting door faces East/Right or West/Left
                        {
                            currentPathPoints.Add(startingPoint.position + new Vector3(10*((startingPoint.direction-2)-1),0,0));
                        }
                        else // if starting door faces North (Up)
                        {
                            currentPathPoints.Add(startingPoint.position + new Vector3(0,0,10f));
                        }

                        
                        if(vertDirection > 0) // if the ending door is below the current door (down)
                        {
                            //if starting door faces south (down/3) the second point in the path
                            // will be down until it reaches the same z-level as the ending door
                            if(startingPoint.direction == 3)
                            {
                                currentPathPoints.Add(currentPathPoints[currentPathPoints.Count-1] - (new Vector3(0,0,currentPathPoints[currentPathPoints.Count-1].z) - new Vector3(0,0,endingPoint.position.z)));
                                if(horizDirection < 0) // ending door is to the Right/East/2
                                {
                                    currentPathPoints.Add(currentPathPoints[currentPathPoints.Count-1] + (new Vector3(currentPathPoints[currentPathPoints.Count-1].x,0,0) - new Vector3(endingPoint.position.x,0,0)));
                                    //currentPathPoints.Add(currentPathPoints[currentPathPoints.Count-1] + new Vector3(endingPoint.position.x,0,0));
                                    //pathPoints.Add(startingPoint.position + new Vector3(10*((startingPoint.direction-2)-1),0,0));
                                }
                                else
                                {
                                    //currentPathPoints.Add(currentPathPoints[currentPathPoints.Count-1] - (new Vector3(0,0,currentPathPoints[currentPathPoints.Count-1].z) - new Vector3(0,0,endingPoint.position.z)));
                                    currentPathPoints.Add(currentPathPoints[currentPathPoints.Count-1] + (new Vector3(currentPathPoints[currentPathPoints.Count-1].x,0,0) - new Vector3(endingPoint.position.x,0,0)));
                                }
                            }
                            else if(startingPoint.direction == 2) //if starting door faces East (right/2)
                            {
                                if(horizDirection < 0) // ending door is to the Right/East/2
                                {
                                    currentPathPoints.Add(currentPathPoints[currentPathPoints.Count-1] + (new Vector3(currentPathPoints[currentPathPoints.Count-1].x,0,0) - new Vector3(endingPoint.position.x,0,0)));
                                    //currentPathPoints.Add(currentPathPoints[currentPathPoints.Count-1] + new Vector3(endingPoint.position.x,0,0));
                                    //pathPoints.Add(startingPoint.position + new Vector3(10*((startingPoint.direction-2)-1),0,0));
                                }
                                else
                                {
                                    currentPathPoints.Add(currentPathPoints[currentPathPoints.Count-1] - (new Vector3(0,0,currentPathPoints[currentPathPoints.Count-1].z) - new Vector3(0,0,endingPoint.position.z)));
                                    currentPathPoints.Add(currentPathPoints[currentPathPoints.Count-1] + (new Vector3(currentPathPoints[currentPathPoints.Count-1].x,0,0) - new Vector3(endingPoint.position.x,0,0)));
                                }
                            }
                            else if(startingPoint.direction == 4)
                            {
                                if(horizDirection < 0) // ending door is to the Right/East/2
                                {
                                    currentPathPoints.Add(currentPathPoints[currentPathPoints.Count-1] - (new Vector3(0,0,currentPathPoints[currentPathPoints.Count-1].z) - new Vector3(0,0,endingPoint.position.z)));
                                    currentPathPoints.Add(currentPathPoints[currentPathPoints.Count-1] + (new Vector3(currentPathPoints[currentPathPoints.Count-1].x,0,0) - new Vector3(endingPoint.position.x,0,0)));
                                    
                                    //currentPathPoints.Add(currentPathPoints[currentPathPoints.Count-1] + (new Vector3(currentPathPoints[currentPathPoints.Count-1].x,0,0) - new Vector3(endingPoint.position.x,0,0)));
                                    
                                }
                                else
                                {
                                    currentPathPoints.Add(currentPathPoints[currentPathPoints.Count-1] + (new Vector3(currentPathPoints[currentPathPoints.Count-1].x,0,0) - new Vector3(endingPoint.position.x,0,0)));
                                    
                                    //currentPathPoints.Add(currentPathPoints[currentPathPoints.Count-1] - (new Vector3(0,0,currentPathPoints[currentPathPoints.Count-1].z) - new Vector3(0,0,endingPoint.position.z)));
                                    //currentPathPoints.Add(currentPathPoints[currentPathPoints.Count-1] + (new Vector3(currentPathPoints[currentPathPoints.Count-1].x,0,0) - new Vector3(endingPoint.position.x,0,0)));
                                }
                            }
                            else
                            {
                                if(horizDirection < 0) // ending door is to the Right/East/2
                                {
                                    currentPathPoints.Add(currentPathPoints[currentPathPoints.Count-1] + (new Vector3(currentPathPoints[currentPathPoints.Count-1].x,0,0) - new Vector3(endingPoint.position.x + 3,0,0)));
                                    currentPathPoints.Add(currentPathPoints[currentPathPoints.Count-1] - (new Vector3(0,0,currentPathPoints[currentPathPoints.Count-1].z) - new Vector3(0,0,endingPoint.position.z)));
                                    currentPathPoints.Add(currentPathPoints[currentPathPoints.Count-1] + (new Vector3(currentPathPoints[currentPathPoints.Count-1].x,0,0) - new Vector3(endingPoint.position.x,0,0)));
                                    //currentPathPoints.Add(currentPathPoints[currentPathPoints.Count-1] + new Vector3(endingPoint.position.x,0,0));
                                    //pathPoints.Add(startingPoint.position + new Vector3(10*((startingPoint.direction-2)-1),0,0));
                                }
                                else
                                {
                                    currentPathPoints.Add(currentPathPoints[currentPathPoints.Count-1] + (new Vector3(currentPathPoints[currentPathPoints.Count-1].x,0,0) - new Vector3(endingPoint.position.x + 3,0,0)));
                                    currentPathPoints.Add(currentPathPoints[currentPathPoints.Count-1] - (new Vector3(0,0,currentPathPoints[currentPathPoints.Count-1].z) - new Vector3(0,0,endingPoint.position.z)));
                                    currentPathPoints.Add(currentPathPoints[currentPathPoints.Count-1] + (new Vector3(currentPathPoints[currentPathPoints.Count-1].x,0,0) - new Vector3(endingPoint.position.x,0,0)));
                                    //currentPathPoints.Add(currentPathPoints[currentPathPoints.Count-1] - (new Vector3(0,0,currentPathPoints[currentPathPoints.Count-1].z) - new Vector3(0,0,endingPoint.position.z)));
                                    //currentPathPoints.Add(currentPathPoints[currentPathPoints.Count-1] + (new Vector3(currentPathPoints[currentPathPoints.Count-1].x,0,0) - new Vector3(endingPoint.position.x,0,0)));
                                }
                            }
                        }
                        else // if the ending door is above the current door (up)
                        {
                            //if starting door faces south (down/3) the second point in the path
                            // will be left or right until it reaches the same x-level as the ending door
                            if(startingPoint.direction == 3)
                            {
                                if(horizDirection < 0) // ending door is to the Right/East/2
                                {
                                    currentPathPoints.Add(currentPathPoints[currentPathPoints.Count-1] + (new Vector3(currentPathPoints[currentPathPoints.Count-1].x,0,0) - new Vector3(endingPoint.position.x + 3,0,0)));
                                    currentPathPoints.Add(currentPathPoints[currentPathPoints.Count-1] - (new Vector3(0,0,currentPathPoints[currentPathPoints.Count-1].z) - new Vector3(0,0,endingPoint.position.z)));
                                    currentPathPoints.Add(currentPathPoints[currentPathPoints.Count-1] + (new Vector3(currentPathPoints[currentPathPoints.Count-1].x,0,0) - new Vector3(endingPoint.position.x,0,0)));
                                    //currentPathPoints.Add(currentPathPoints[currentPathPoints.Count-1] + new Vector3(endingPoint.position.x,0,0));
                                    //pathPoints.Add(startingPoint.position + new Vector3(10*((startingPoint.direction-2)-1),0,0));
                                }
                                else
                                {
                                    currentPathPoints.Add(currentPathPoints[currentPathPoints.Count-1] + (new Vector3(currentPathPoints[currentPathPoints.Count-1].x,0,0) - new Vector3(endingPoint.position.x + 3,0,0)));
                                    currentPathPoints.Add(currentPathPoints[currentPathPoints.Count-1] - (new Vector3(0,0,currentPathPoints[currentPathPoints.Count-1].z) - new Vector3(0,0,endingPoint.position.z)));
                                    currentPathPoints.Add(currentPathPoints[currentPathPoints.Count-1] + (new Vector3(currentPathPoints[currentPathPoints.Count-1].x,0,0) - new Vector3(endingPoint.position.x,0,0)));
                                    //currentPathPoints.Add(currentPathPoints[currentPathPoints.Count-1] - (new Vector3(0,0,currentPathPoints[currentPathPoints.Count-1].z) - new Vector3(0,0,endingPoint.position.z)));
                                    //currentPathPoints.Add(currentPathPoints[currentPathPoints.Count-1] + (new Vector3(currentPathPoints[currentPathPoints.Count-1].x,0,0) - new Vector3(endingPoint.position.x,0,0)));
                                }
                            }
                            else if(startingPoint.direction == 2) //if starting door faces East (right/2)
                            {
                                if(horizDirection < 0) // ending door is to the Right/East/2
                                {
                                    currentPathPoints.Add(currentPathPoints[currentPathPoints.Count-1] + (new Vector3(currentPathPoints[currentPathPoints.Count-1].x,0,0) - new Vector3(endingPoint.position.x,0,0)));
                                    //currentPathPoints.Add(currentPathPoints[currentPathPoints.Count-1] + new Vector3(endingPoint.position.x,0,0));
                                    //pathPoints.Add(startingPoint.position + new Vector3(10*((startingPoint.direction-2)-1),0,0));
                                }
                                else
                                {
                                    currentPathPoints.Add(currentPathPoints[currentPathPoints.Count-1] - (new Vector3(0,0,currentPathPoints[currentPathPoints.Count-1].z) - new Vector3(0,0,endingPoint.position.z)));
                                    currentPathPoints.Add(currentPathPoints[currentPathPoints.Count-1] + (new Vector3(currentPathPoints[currentPathPoints.Count-1].x,0,0) - new Vector3(endingPoint.position.x,0,0)));
                                }
                            }
                            else if(startingPoint.direction == 4)
                            {
                                if(horizDirection < 0) // ending door is to the Right/East/2
                                {
                                    currentPathPoints.Add(currentPathPoints[currentPathPoints.Count-1] - (new Vector3(0,0,currentPathPoints[currentPathPoints.Count-1].z) - new Vector3(0,0,endingPoint.position.z)));
                                    currentPathPoints.Add(currentPathPoints[currentPathPoints.Count-1] + (new Vector3(currentPathPoints[currentPathPoints.Count-1].x,0,0) - new Vector3(endingPoint.position.x,0,0)));
                                    
                                    //currentPathPoints.Add(currentPathPoints[currentPathPoints.Count-1] + (new Vector3(currentPathPoints[currentPathPoints.Count-1].x,0,0) - new Vector3(endingPoint.position.x,0,0)));
                                    
                                }
                                else
                                {
                                    currentPathPoints.Add(currentPathPoints[currentPathPoints.Count-1] + (new Vector3(currentPathPoints[currentPathPoints.Count-1].x,0,0) - new Vector3(endingPoint.position.x,0,0)));
                                    
                                    //currentPathPoints.Add(currentPathPoints[currentPathPoints.Count-1] - (new Vector3(0,0,currentPathPoints[currentPathPoints.Count-1].z) - new Vector3(0,0,endingPoint.position.z)));
                                    //currentPathPoints.Add(currentPathPoints[currentPathPoints.Count-1] + (new Vector3(currentPathPoints[currentPathPoints.Count-1].x,0,0) - new Vector3(endingPoint.position.x,0,0)));
                                }
                            }
                            else
                            {
                                currentPathPoints.Add(currentPathPoints[currentPathPoints.Count-1] - (new Vector3(0,0,currentPathPoints[currentPathPoints.Count-1].z) - new Vector3(0,0,endingPoint.position.z)));
                                if(horizDirection < 0) // ending door is to the Right/East/2
                                {
                                    currentPathPoints.Add(currentPathPoints[currentPathPoints.Count-1] + (new Vector3(currentPathPoints[currentPathPoints.Count-1].x,0,0) - new Vector3(endingPoint.position.x,0,0)));
                                    //currentPathPoints.Add(currentPathPoints[currentPathPoints.Count-1] + new Vector3(endingPoint.position.x,0,0));
                                    //pathPoints.Add(startingPoint.position + new Vector3(10*((startingPoint.direction-2)-1),0,0));
                                }
                                else
                                {
                                    //currentPathPoints.Add(currentPathPoints[currentPathPoints.Count-1] - (new Vector3(0,0,currentPathPoints[currentPathPoints.Count-1].z) - new Vector3(0,0,endingPoint.position.z)));
                                    currentPathPoints.Add(currentPathPoints[currentPathPoints.Count-1] + (new Vector3(currentPathPoints[currentPathPoints.Count-1].x,0,0) - new Vector3(endingPoint.position.x,0,0)));
                                }
                            }
                        }
                        existingCorridors.Add(currentPathPoints);
                        if (!IsCorridorOverlapping(currentPathPoints, rooms, existingCorridors, 4))
                        {
                            doors[roomDoors[i, k]].isConnected = true;
                            doors[roomDoors[j, (k+2)%4]].isConnected = true;
                        }
                        else
                        {
                            existingCorridors.RemoveAt(existingCorridors.Count - 1);
                        }
                    }
                    
                }
            }
        }
        BuildCorridorsFromPaths(existingCorridors, 3f, 7f, 0.2f);
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
    
    // Uses the CorridorPaths to create the corridor segments.
    private void BuildCorridorsFromPaths(List<List<Vector3>> existingCorridorPaths, float width, float height, float thickness)
    {
        foreach (List<Vector3> corridorPath in existingCorridorPaths) // Loop through each stored corridor path
        {
            for (int i = 0; i < corridorPath.Count - 1; i++) // Loop through points in the path
            {
                Vector3 startPos = corridorPath[i];
                Vector3 endPos = corridorPath[i + 1];

                CreateCorridorSegment(startPos, endPos, width, height, thickness); // Create corridor segment
            }
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

    // Gets the index of the room that contains a given door position.
    private int GetRoomIndex(Vector3 doorPosition)
    {
        for (int i = 0; i < rooms.Count; i++)
        {
            // Expand the bounds slightly to account for doors on the edges.
            Bounds expandedBounds = rooms[i].roomBounds;
            expandedBounds.Expand(new Vector3(3f, 0, 3f)); //Expand by the door width + extra for connections

            if (expandedBounds.Contains(doorPosition))
            {
                return i;
            }
        }
        return -1;
    }
}