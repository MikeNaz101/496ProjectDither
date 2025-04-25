using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using Unity.AI.Navigation; // Required for NavMeshSurface
using System.Linq;
using Random = UnityEngine.Random; // Needed for OrderBy, Any, FindIndex, etc.
//using System;

public class RoomGenerator8 : MonoBehaviour
{
    public event Action<List<Room>> OnRoomsGenerated;


    [Header("Generation Settings")]
    public int numRooms = 8; // How many rooms to generate
    public Vector2 roomSizeMinMax = new Vector2(12, 28); // Min/Max width and depth for rooms
    public Vector2 bigRoomSize = new Vector2(200, 200); // Area within which rooms are placed
    public float minRoomSeparation = 10.0f; // Minimum distance between room edges

    [Header("Corridor Settings")]
    public float corridorWallThickness = 0.1f;
    public float corridorHeight = 7f;
    public float corridorWidth = 5f;
    public float corridorClearance = 2.0f; // Min distance corridors keep from rooms/other corridors/corners
    public float corridorInitialStraightLength = 3.0f; // Distance corridor goes straight from door first/last

    [Header("Prefabs & Components")]
    public NavMeshSurface navMeshSurface; // Assign the NavMesh Surface component here
    public GameObject wallPrefab; // Optional: Not currently used for walls, but keep if needed later
    public GameObject doorPrefab; // Optional: Assign if you want visual doors instantiated
    public GameObject WindowPrefab; // Optional: Not currently used
    // ✨✨ NEW MATERIAL SLOTS ✨✨
    public Material wallMaterial; // <-- Berry nice! For ROOM walls! 🍓
    public Material floorMaterial; // <-- Plum perfect! For ALL floors! 💜
    public Material corridorWallMaterial; // <-- Lime-light! For CORRIDOR walls! 💚

    // Object Spawning Prefabs (Assign if uncommenting spawn logic)
    public GameObject angelPrefab;
    public GameObject dogPrefab;
    public GameObject foodBowlPrefab;
    public GameObject playerPrefab;
    private GameObject playerInstance; // Instance of the player

    // --- Private Generation Data ---
    private List<Room> rooms = new List<Room>();
    private List<Door> allDoors = new List<Door>();
    private List<Bounds> corridorSegmentBounds = new List<Bounds>(); // Store bounds of built corridor segments
    private List<Bounds> corridorCornerBounds = new List<Bounds>(); // Store bounds of built corridor corners

    // --- Helper Class for Path Calculation Result ---
    // NOTE: This might become less useful if the new pathfinder just returns List<Vector3>
    public class PathCalculationResult
    {
        public List<Vector3> Points { get; set; }
        public Vector3 CornerPoint { get; set; } // Relevant for L-Path only
        public bool HasCorner { get; set; } // Relevant for L-Path only

        public PathCalculationResult(List<Vector3> points, Vector3 corner, bool hasCorner)
        {
            Points = points;
            CornerPoint = corner;
            HasCorner = hasCorner;
        }
    }

    // --- Core Class Definitions ---
    public class Room
    {
        public Bounds roomBounds;
        public Vector3 position; // Center position
        public int id;
        public List<Door> doors = new List<Door>(); // Doors belonging to this room

        public Room(Bounds bounds, Vector3 pos, int id)
        {
            roomBounds = bounds;
            position = pos;
            this.id = id;
        }
    }

    public class Door
    {
        public Vector3 position; // Logical position on the wall plane (Y=0)
        public bool isConnected;
        public int direction; // 1:N (+Z), 2:E (+X), 3:S (-Z), 4:W (-X)
        public Room associatedRoom;
        public List<GameObject> WallSegments { get; set; } // References to the opening segments (sides, top)
        public GameObject DoorPrefabInstance { get; set; } // Reference to the instantiated door prefab

        public Door(Vector3 pos, bool connected, int dir, Room room)
        {
            position = pos;
            isConnected = connected;
            direction = dir;
            associatedRoom = room;
            WallSegments = new List<GameObject>(); // Initialize the list
            DoorPrefabInstance = null;
        }
    }

    // --- Unity Methods ---
    void Start()
    {
        Debug.Log("Starting Dungeon Generation...");
        StartCoroutine(GenerateAndBakeSequence());
        Debug.Log("Dungeon Generation Complete.");
    }

    // --- Room Generation ---
    public void GenerateRooms()
    {
        Debug.Log($"Attempting to generate {numRooms} rooms...");
        rooms.Clear(); // Clear previous rooms if regenerating
        allDoors.Clear(); // Clear previous doors
        corridorSegmentBounds.Clear(); // Clear previous corridor data
        corridorCornerBounds.Clear();

        // Destroy previous generation objects if any exist
        foreach (Transform child in transform)
        {
            // Avoid destroying essential scene objects, maybe check names/tags
            if (child.name.StartsWith("Room_") || child.name.StartsWith("Corridor_") || child.name.StartsWith("FilledWall_"))
            {
                Destroy(child.gameObject);
            }
        }

        for (int i = 0; i < numRooms; i++)
        {
            bool roomPlaced = false;
            int maxAttempts = 200;
            int attempts = 0;
            while (!roomPlaced && attempts < maxAttempts)
            {
                attempts++;
                float roomWidth = Random.Range(roomSizeMinMax.x, roomSizeMinMax.y);
                float roomDepth = Random.Range(roomSizeMinMax.x, roomSizeMinMax.y); // Use depth for Z axis
                float posX = Random.Range(-bigRoomSize.x / 2 + roomWidth / 2, bigRoomSize.x / 2 - roomWidth / 2);
                float posZ = Random.Range(-bigRoomSize.y / 2 + roomDepth / 2, bigRoomSize.y / 2 - roomDepth / 2);

                // Ground Y-level is 0
                Bounds newRoomBounds = new Bounds(new Vector3(posX, 0, posZ), new Vector3(roomWidth, 1, roomDepth)); // Use Y=1 for bounds height

                // Use the new minRoomSeparation variable here
                if (!DoesRoomOverlap(newRoomBounds, rooms, minRoomSeparation))
                {
                    roomPlaced = true;
                    GameObject roomGO = new GameObject("Room_" + i);
                    roomGO.transform.position = new Vector3(posX, 0, posZ);
                    roomGO.transform.parent = this.transform; // Parent rooms to this generator object

                    Room newRoom = new Room(newRoomBounds, roomGO.transform.position, i);
                    rooms.Add(newRoom);

                    // Generate walls and associate doors with this new room
                    GenerateWalls(roomGO, newRoom, roomWidth, roomDepth, corridorHeight); // Pass depth
                    Debug.Log($"Placed Room {i} at {newRoom.position}");
                }
            }
            if (!roomPlaced)
            {
                Debug.LogWarning($"Failed to place room {i} after {maxAttempts} attempts.");
            }
        }
        Debug.Log($"Successfully generated {rooms.Count} rooms.");

        // --- Object Spawning (Optional - Uncomment and ensure prefabs are assigned) ---

        if (playerPrefab != null && rooms.Count > 0)
        {
            Vector3 spawnCenter = rooms[0].roomBounds.center;
            playerInstance = Instantiate(playerPrefab, new Vector3(spawnCenter.x, 0.5f, spawnCenter.z), Quaternion.identity);
            playerInstance.name = "Player";
            Debug.Log("Spawned Player in Room 0");
        }

        /*if (dogPrefab != null && foodBowlPrefab != null && playerInstance != null && rooms.Count > numRooms / 2)
        {
            int dogRoomIndex = numRooms / 2;
            if (dogRoomIndex < rooms.Count)
            {
                Room dogRoom = rooms[dogRoomIndex];
                Vector3 dogSpawnPos = dogRoom.roomBounds.center + Vector3.up * 0.5f;
                GameObject dogInstance = Instantiate(dogPrefab, dogSpawnPos, Quaternion.identity);
                dogInstance.name = "Dog";
                Debug.Log($"Spawned Dog in Room {dogRoomIndex}");

                float bowlX = Random.Range(dogRoom.roomBounds.min.x + 1, dogRoom.roomBounds.max.x - 1);
                float bowlZ = Random.Range(dogRoom.roomBounds.min.z + 1, dogRoom.roomBounds.max.z - 1);
                Vector3 bowlPos = new Vector3(bowlX, 0.5f, bowlZ);
                Instantiate(foodBowlPrefab, bowlPos, Quaternion.identity);
                Debug.Log($"Placed Food Bowl in Room {dogRoomIndex}");

                DogStateManager dogScript = dogInstance.GetComponent<DogStateManager>();
                if (dogScript != null)
                {
                    dogScript.foodBowl = foodBowlInstance;
                    dogScript.player = playerInstance;
                }
            }
        }*/

        if (angelPrefab != null && playerInstance != null && rooms.Count > 1)
        {
            int angelRoomIndex = rooms.Count - 2;
            if (angelRoomIndex <= 0 && rooms.Count > 1) angelRoomIndex = 1;

            if (angelRoomIndex >= 0 && angelRoomIndex < rooms.Count)
            {
                Room angelRoom = rooms[angelRoomIndex];
                Vector3 angelSpawnPos = angelRoom.roomBounds.center + Vector3.up * 1.5f;
                GameObject angelInstance = Instantiate(angelPrefab, angelSpawnPos, Quaternion.identity);
                angelInstance.name = "Angel";
                Debug.Log($"Spawned Angel in Room {angelRoomIndex}");

                MikesWeepingAngel angelScript = angelInstance.GetComponent<MikesWeepingAngel>();
                if (angelScript != null)
                {
                    Camera playerCam = playerInstance.GetComponentInChildren<Camera>();
                    if (playerCam != null)
                    {
                        angelScript.playerCam = playerCam;
                    }
                    else
                    {
                        Debug.LogError("Player Camera not found for Angel script!");
                    }
                }
            }
        }

        // Call the OnRoomsGenerated event after rooms are generated
        OnRoomsGenerated?.Invoke(rooms);
    }
    public void GenerateCorridors()
    {
        Debug.Log("Starting Corridor Generation...");
        if (rooms.Count < 2)
        {
            Debug.LogWarning("Not enough rooms to generate corridors.");
            return;
        }

        // Order rooms by X position, then by Z position
        List<Room> orderedRooms = rooms.OrderBy(r => r.position.x).ThenBy(r => r.position.z).ToList();

        // Connect adjacent rooms horizontally
        for (int i = 0; i < orderedRooms.Count - 1; i++)
        {
            if (Mathf.Abs(orderedRooms[i].position.z - orderedRooms[i + 1].position.z) < 10f) // Close enough on Z
            {
                ConnectRooms(orderedRooms[i], orderedRooms[i + 1]);
            }
        }

        // Connect adjacent rooms vertically
        for (int i = 0; i < orderedRooms.Count - 1; i++)
        {
            for (int j = i + 1; j < orderedRooms.Count; j++)
            {
                if (Mathf.Abs(orderedRooms[i].position.x - orderedRooms[j].position.x) < 10f) // Close enough on X
                {
                    ConnectRooms(orderedRooms[i], orderedRooms[j]);
                }
            }
        }
        Debug.Log("Corridor Generation Complete.");
    }
    public void ConnectRooms(Room roomA, Room roomB)
    {
        // Find potential doors
        Door doorA = FindSuitableDoor(roomA, roomB);
        Door doorB = FindSuitableDoor(roomB, roomA);

        if (doorA != null && doorB != null)
        {
            doorA.isConnected = true;
            doorB.isConnected = true;

            // Generate corridor path
            PathCalculationResult pathResult = CalculateCorridorPath(doorA.position, doorB.position);
            List<Vector3> pathPoints = pathResult.Points;

            // Build the corridor (walls)
            BuildCorridor(pathPoints, doorA, doorB);
        }
        else
        {
            Debug.LogWarning($"Failed to connect rooms {roomA.id} and {roomB.id}: No suitable doors found.");
        }
    }

    // --- Wall & Door Generation ---
    public void GenerateWalls(GameObject roomGO, Room room, float roomWidth, float roomDepth, float wallHeight) // Pass depth
    {
        // Calculate wall positions (use depth for Z)
        Vector3 northWallPos = room.position + new Vector3(0, 0, roomDepth / 2);
        Vector3 eastWallPos = room.position + new Vector3(roomWidth / 2, 0, 0);
        Vector3 southWallPos = room.position + new Vector3(0, 0, -roomDepth / 2);
        Vector3 westWallPos = room.position + new Vector3(-roomWidth / 2, 0, 0);

        // Calculate wall lengths (use depth for Z)
        float northSouthWallLength = roomWidth;
        float eastWestWallLength = roomDepth;

        // Build North Wall (along +Z)
        BuildWall(northWallPos, northSouthWallLength, wallHeight, 1, room);

        // Build East Wall (along +X)
        BuildWall(eastWallPos, eastWestWallLength, wallHeight, 2, room);

        // Build South Wall (along -Z)
        BuildWall(southWallPos, northSouthWallLength, wallHeight, 3, room);

        // Build West Wall (along -X)
        BuildWall(westWallPos, eastWestWallLength, wallHeight, 4, room);

        // Create floor
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor";
        floor.transform.position = room.roomBounds.center - Vector3.up * 0.5f; // Center and lower by half height
        floor.transform.localScale = new Vector3(roomWidth, 1, roomDepth); // Use depth for Z
        floor.transform.parent = roomGO.transform;
        if (floorMaterial != null)
        {
            floor.GetComponent<Renderer>().material = floorMaterial;
        }
    }

    public void BuildWall(Vector3 wallPos, float wallLength, float wallHeight, int direction, Room room)
    {
        // Calculate wall normal (direction it faces)
        Vector3 wallNormal = Vector3.zero;
        switch (direction)
        {
            case 1: wallNormal = Vector3.back; break;  // North (+Z), face south (-Z)
            case 2: wallNormal = Vector3.left; break;  // East (+X), face west (-X)
            case 3: wallNormal = Vector3.forward; break; // South (-Z), face north (+Z)
            case 4: wallNormal = Vector3.right; break;  // West (-X), face east (+X)
        }

        // Calculate wall right vector (used for door placement)
        Vector3 wallRight = Vector3.Cross(Vector3.up, wallNormal);

        // Calculate wall center position (adjust for half the wall thickness)
        Vector3 wallCenter = wallPos + wallNormal * corridorWallThickness / 2f;

        // Create wall GameObject
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = $"Room_{room.id}_Wall_Dir{direction}"; // More descriptive name
        wall.transform.position = wallCenter;
        wall.transform.localScale = new Vector3(wallLength, wallHeight, corridorWallThickness);
        wall.transform.rotation = Quaternion.LookRotation(wallNormal);

        // Find the parent room GameObject using the room's ID
        GameObject roomGO = GameObject.Find($"Room_{room.id}");
        if (roomGO != null)
        {
            wall.transform.parent = roomGO.transform;
        }
        else
        {
            Debug.LogError($"Could not find GameObject for Room ID: {room.id} to parent the wall.");
            wall.transform.parent = transform; // Fallback to the RoomGenerator's transform
        }

        if (wallMaterial != null)
        {
            wall.GetComponent<Renderer>().material = wallMaterial;
        }

        // Create a Door object and add it to the room
        Door newDoor = new Door(wallPos, false, direction, room);
        room.doors.Add(newDoor);
        allDoors.Add(newDoor);

        // Calculate door position (centered on the wall)
        Vector3 doorPosition = wallPos; // Start at the wall position
        newDoor.position = doorPosition; // Store the initial position

        // Calculate opening size (leave space for the door frame)
        float openingWidth = 3.0f;
        float openingHeight = 5.0f;

        // Adjust the wall scale to create a hole for the door
        float leftSideLength = (wallLength - openingWidth) / 2f;
        float rightSideLength = leftSideLength;

        // Create left wall segment
        GameObject leftWallSegment = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftWallSegment.name = $"Room_{room.id}_WallSegment_Left_Dir{direction}";
        leftWallSegment.transform.position = wallCenter - wallRight * (rightSideLength + openingWidth / 2f); // Shift left
        leftWallSegment.transform.localScale = new Vector3(leftSideLength, wallHeight, corridorWallThickness);
        leftWallSegment.transform.rotation = Quaternion.LookRotation(wallNormal);
        if (roomGO != null) leftWallSegment.transform.parent = roomGO.transform; else leftWallSegment.transform.parent = transform;
        if (wallMaterial != null)
        {
            leftWallSegment.GetComponent<Renderer>().material = wallMaterial;
        }
        newDoor.WallSegments.Add(leftWallSegment); // Add to Door's segment list

        // Create right wall segment
        GameObject rightWallSegment = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightWallSegment.name = $"Room_{room.id}_WallSegment_Right_Dir{direction}";
        rightWallSegment.transform.position = wallCenter + wallRight * (leftSideLength + openingWidth / 2f); // Shift right
        rightWallSegment.transform.localScale = new Vector3(rightSideLength, wallHeight, corridorWallThickness);
        rightWallSegment.transform.rotation = Quaternion.LookRotation(wallNormal);
        if (roomGO != null) rightWallSegment.transform.parent = roomGO.transform; else rightWallSegment.transform.parent = transform;
        if (wallMaterial != null)
        {
            rightWallSegment.GetComponent<Renderer>().material = wallMaterial;
        }
        newDoor.WallSegments.Add(rightWallSegment); // Add to Door's segment list

        // Create top wall segment
        GameObject topWallSegment = GameObject.CreatePrimitive(PrimitiveType.Cube);
        topWallSegment.name = $"Room_{room.id}_WallSegment_Top_Dir{direction}";
        topWallSegment.transform.position = wallCenter + Vector3.up * (wallHeight + openingHeight) / 2f; // Shift up
        topWallSegment.transform.localScale = new Vector3(openingWidth, wallHeight - openingHeight, corridorWallThickness);
        topWallSegment.transform.rotation = Quaternion.LookRotation(wallNormal);
        if (roomGO != null) topWallSegment.transform.parent = roomGO.transform; //else topWallSegment.transform = transform;
        if (wallMaterial != null)
        {
            topWallSegment.GetComponent<Renderer>().material = wallMaterial;
        }
        newDoor.WallSegments.Add(topWallSegment); // Add to Door's segment list

        // Instantiate door prefab (if assigned)
        if (doorPrefab != null)
        {
            GameObject doorInstance = Instantiate(doorPrefab, doorPosition + Vector3.up * (openingHeight / 2f), Quaternion.LookRotation(wallNormal)); // Position at the wall, at door height
            if (roomGO != null) doorInstance.transform.parent = roomGO.transform; else doorInstance.transform.parent = transform;
            newDoor.DoorPrefabInstance = doorInstance;
        }
    }

    // --- Corridor Building ---
    public void BuildCorridor(List<Vector3> pathPoints, Door doorA, Door doorB)
    {
        if (pathPoints == null || pathPoints.Count < 2)
        {
            Debug.LogError("Invalid path for corridor.");
            return;
        }

        for (int i = 0; i < pathPoints.Count - 1; i++)
        {
            Vector3 startPoint = pathPoints[i];
            Vector3 endPoint = pathPoints[i + 1];

            // Calculate direction and distance for the corridor segment
            Vector3 direction = (endPoint - startPoint).normalized;
            float distance = Vector3.Distance(startPoint, endPoint);

            // Calculate the center position of the corridor segment
            Vector3 center = (startPoint + endPoint) / 2f;

            // Create the corridor segment
            GameObject corridorSegment = GameObject.CreatePrimitive(PrimitiveType.Cube);
            corridorSegment.name = "Corridor_Segment_" + i;
            corridorSegment.transform.position = center + Vector3.up * corridorHeight / 2f; // Raise to half height
            corridorSegment.transform.localScale = new Vector3(distance, corridorHeight, corridorWidth);
            corridorSegment.transform.rotation = Quaternion.LookRotation(direction);
            corridorSegment.transform.parent = transform;

            if (corridorWallMaterial != null)
            {
                corridorSegment.GetComponent<Renderer>().material = corridorWallMaterial;
            }

            // Store the bounds of the corridor segment
            Bounds segmentBounds = new Bounds(center, new Vector3(distance, corridorHeight, corridorWidth));
            corridorSegmentBounds.Add(segmentBounds);
        }

        // Fill in the gaps at the door openings
        FillCorridorGapsAtDoors(doorA, pathPoints[0]);
        FillCorridorGapsAtDoors(doorB, pathPoints.Last());
    }

    // --- Gap Filling at Doors ---
    public void FillCorridorGapsAtDoors(Door door, Vector3 corridorEnd)
    {
        // Calculate the direction from the corridor end to the door position
        Vector3 gapFillDirection = (door.position - corridorEnd).normalized;

        // Calculate the position of the gap filling segment
        Vector3 gapFillPosition = door.position + gapFillDirection * (corridorWidth / 2f); // Adjust for half corridor width

        // Calculate the length of the gap filling segment (from the door position to the corridor end)
        float gapFillLength = Vector3.Distance(door.position, corridorEnd) - corridorWidth / 2f; // Subtract half corridor width

        // Create the gap filling segment
        GameObject gapFillSegment = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gapFillSegment.name = "FilledWall";
        gapFillSegment.transform.position = gapFillPosition + Vector3.up * corridorHeight / 2f; // Raise to half height
        gapFillSegment.transform.localScale = new Vector3(gapFillLength, corridorHeight, corridorWallThickness);
        gapFillSegment.transform.rotation = Quaternion.LookRotation(gapFillDirection);
        gapFillSegment.transform.parent = transform;

        if (corridorWallMaterial != null)
        {
            gapFillSegment.GetComponent<Renderer>().material = corridorWallMaterial;
        }
    }

    // --- Corridor Pathfinding ---
    public PathCalculationResult CalculateCorridorPath(Vector3 start, Vector3 end)
    {
        // Check if a straight path is possible
        if (CanBuildStraightPath(start, end))
        {
            return new PathCalculationResult(new List<Vector3>() { start, end }, Vector3.zero, false);
        }
        else
        {
            // Attempt an L-shaped path
            Vector3 corner = FindLPathCorner(start, end);
            if (corner != Vector3.zero)
            {
                return new PathCalculationResult(new List<Vector3>() { start, corner, end }, corner, true);
            }
            else
            {
                // Fallback: Return a straight path (may overlap)
                Debug.LogWarning("No valid L-path found.  Returning a straight path (may overlap).");
                return new PathCalculationResult(new List<Vector3>() { start, end }, Vector3.zero, false);
            }
        }
    }

    // --- Helper Methods ---
    public bool CanBuildStraightPath(Vector3 start, Vector3 end)
    {
        // Create a bounding box for the potential corridor
        Vector3 min = Vector3.Min(start, end) - new Vector3(corridorWidth / 2f + corridorClearance, 0, corridorWidth / 2f + corridorClearance);
        Vector3 max = Vector3.Max(start, end) + new Vector3(corridorWidth / 2f + corridorClearance, corridorHeight, corridorWidth / 2f + corridorClearance);
        Bounds corridorBounds = new Bounds(min + (max - min) / 2f, max - min);

        // Check for overlaps with existing rooms and corridors
        return !DoesCorridorOverlap(corridorBounds);
    }

    public Vector3 FindLPathCorner(Vector3 start, Vector3 end)
    {
        // Try a corner along the X axis
        Vector3 cornerX = new Vector3(end.x, 0, start.z);
        if (CanBuildStraightPath(start, cornerX) && CanBuildStraightPath(cornerX, end))
        {
            return cornerX;
        }

        // Try a corner along the Z axis
        Vector3 cornerZ = new Vector3(start.x, 0, end.z);
        if (CanBuildStraightPath(start, cornerZ) && CanBuildStraightPath(cornerZ, end))
        {
            return cornerZ;
        }

        return Vector3.zero; // No valid L-path found
    }

    public bool DoesRoomOverlap(Bounds newRoomBounds, List<Room> existingRooms, float minSeparation)
    {
        // Expand the new room's bounds by the minSeparation
        Bounds expandedBounds = newRoomBounds;
        expandedBounds.Expand(new Vector3(minSeparation, 0, minSeparation));

        foreach (Room existingRoom in existingRooms)
        {
            if (expandedBounds.Intersects(existingRoom.roomBounds))
            {
                return true; // Overlap detected
            }
        }
        return false; // No overlap
    }

    public bool DoesCorridorOverlap(Bounds corridorBounds)
    {
        // Check for overlaps with existing rooms
        foreach (Room room in rooms)
        {
            if (corridorBounds.Intersects(room.roomBounds))
            {
                return true; // Overlap detected
            }
        }

        // Check for overlaps with existing corridor segments
        foreach (Bounds segmentBounds in corridorSegmentBounds)
        {
            if (corridorBounds.Intersects(segmentBounds))
            {
                return true; // Overlap detected
            }
        }

        // Check for overlaps with existing corridor corners
        foreach (Bounds cornerBounds in corridorCornerBounds)
        {
            if (corridorBounds.Intersects(cornerBounds))
            {
                return true; // Overlap detected
            }
        }

        return false; // No overlap
    }

    public Door FindSuitableDoor(Room roomA, Room roomB)
    {
        // Find doors in roomA that are not already connected
        List<Door> unconnectedDoors = roomA.doors.Where(d => !d.isConnected).ToList();

        if (unconnectedDoors.Count == 0)
        {
            return null; // No available doors
        }

        // Find the closest unconnected door
        Door closestDoor = unconnectedDoors.OrderBy(d => Vector3.Distance(d.position, roomB.position)).First();
        return closestDoor;
    }

    public IEnumerator GenerateAndBakeSequence()
    {
        GenerateRooms();
        yield return null; // Wait one frame

        GenerateCorridors();
        yield return null; // Wait one frame

        if (navMeshSurface != null)
        {
            navMeshSurface.BuildNavMesh();
            Debug.Log("NavMesh baked.");
        }
        else
        {
            Debug.LogWarning("NavMeshSurface is not assigned.  Navigation will not work.");
        }
    }

    // --- NEW METHOD:  Get All Rooms ---
    public List<Room> GetAllRooms()
    {
        return rooms;
    }
}