using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation;
using System.Linq; // Needed for OrderBy

public class RoomGenerator3 : MonoBehaviour
{
    [Header("Generation Settings")]
    public int numRooms = 8; // How many rooms to generate
    public Vector2 roomSizeMinMax = new Vector2(12, 28); // Min/Max width and depth for rooms
    public Vector2 bigRoomSize = new Vector2(200, 200); // Area within which rooms are placed

    [Header("Corridor Settings")]
    public float corridorWallThickness = 0.1f;
    public float corridorHeight = 7f;
    public float corridorWidth = 3f;
    public float corridorClearance = 2.0f; // Min distance corridors keep from rooms/other corridors/corners

    [Header("Prefabs & Components")]
    public NavMeshSurface navMeshSurface; // Assign the NavMesh Surface component here
    public GameObject wallPrefab; // Optional: Not currently used for walls, but keep if needed later
    public GameObject doorPrefab; // Optional: Assign if you want visual doors instantiated
    public GameObject WindowPrefab; // Optional: Not currently used

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
    private class PathCalculationResult
    {
        public List<Vector3> Points { get; set; }
        public Vector3 CornerPoint { get; set; }
        public bool HasCorner { get; set; } // True if it's an L-path, false if straight

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

        // --- NEW PROPERTIES ---
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
        GenerateRooms();
        ConnectRoomsWithCorridors();
        CleanupUnusedDoors(); // <-- ADD THIS CALL HERE
        BakeNavMesh();
        Debug.Log("Dungeon Generation Complete.");
    }

    // --- Room Generation ---
    public void GenerateRooms()
    {
        Debug.Log($"Attempting to generate {numRooms} rooms...");
        for (int i = 0; i < numRooms; i++)
        {
            bool roomPlaced = false;
            int maxAttempts = 200;
            int attempts = 0;
            while (!roomPlaced && attempts < maxAttempts)
            {
                attempts++;
                float roomWidth = Random.Range(roomSizeMinMax.x, roomSizeMinMax.y);
                float roomHeight = Random.Range(roomSizeMinMax.x, roomSizeMinMax.y);
                float posX = Random.Range(-bigRoomSize.x / 2 + roomWidth / 2, bigRoomSize.x / 2 - roomWidth / 2);
                float posZ = Random.Range(-bigRoomSize.y / 2 + roomWidth / 2, bigRoomSize.y / 2 - roomWidth / 2);

                // Ground Y-level is 0
                Bounds newRoomBounds = new Bounds(new Vector3(posX, 0, posZ), new Vector3(roomWidth, 1, roomHeight)); // Use Y=1 for bounds height initially

                if (!DoesRoomOverlap(newRoomBounds, rooms, corridorClearance * 2f))
                {
                    roomPlaced = true;
                    GameObject roomGO = new GameObject("Room_" + i);
                    roomGO.transform.position = new Vector3(posX, 0, posZ);

                    Room newRoom = new Room(newRoomBounds, roomGO.transform.position, i);
                    rooms.Add(newRoom);

                    // Generate walls and associate doors with this new room
                    GenerateWalls(roomGO, newRoom, roomWidth, roomHeight, corridorHeight); // Use corridor height for walls
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
        /*
        if (playerPrefab != null && rooms.Count > 0)
        {
             playerInstance = Instantiate(playerPrefab, rooms[0].roomBounds.center + Vector3.up * 0.5f, Quaternion.identity);
             playerInstance.name = "Player";
             // NiPlayerMovement playerScript = playerInstance.GetComponent<NiPlayerMovement>(); // Get component if needed
             Debug.Log("Spawned Player in Room 0");
        }

        if (dogPrefab != null && foodBowlPrefab != null && playerInstance != null && rooms.Count > numRooms / 2)
        {
             int dogRoomIndex = numRooms / 2;
             if(dogRoomIndex < rooms.Count) { // Check index exists
                 Room dogRoom = rooms[dogRoomIndex];
                 Vector3 dogSpawnPos = dogRoom.roomBounds.center + Vector3.up * 0.5f;
                 GameObject dogInstance = Instantiate(dogPrefab, dogSpawnPos, Quaternion.identity);
                 dogInstance.name = "Dog";
                 Debug.Log($"Spawned Dog in Room {dogRoomIndex}");

                 // Spawn food bowl in the same room
                 float bowlX = Random.Range(dogRoom.roomBounds.min.x + 1, dogRoom.roomBounds.max.x - 1);
                 float bowlZ = Random.Range(dogRoom.roomBounds.min.z + 1, dogRoom.roomBounds.max.z - 1);
                 GameObject foodBowlInstance = Instantiate(foodBowlPrefab, new Vector3(bowlX, 0.5f, bowlZ), Quaternion.identity);
                 foodBowlInstance.name = "FoodBowl";
                 Debug.Log($"Spawned Food Bowl in Room {dogRoomIndex}");

                 // Configure Dog script if needed
                 DogStateManager dogScript = dogInstance.GetComponent<DogStateManager>();
                 if (dogScript != null)
                 {
                     // Example setup - adjust as per your DogStateManager script
                     // dogScript.corners = GetRoomCorners(dogRoom); // You might need a GetRoomCorners helper
                     dogScript.foodBowl = foodBowlInstance;
                     dogScript.player = playerInstance;
                 }
             }
        }

        if (angelPrefab != null && playerInstance != null && rooms.Count > 1) // Spawn in second-to-last room if possible
        {
             int angelRoomIndex = rooms.Count - 2; // Try second to last
             if (angelRoomIndex <= 0 && rooms.Count > 1) angelRoomIndex = 1; // Fallback to room 1 if only 2 rooms

             if(angelRoomIndex >= 0 && angelRoomIndex < rooms.Count) {
                 Room angelRoom = rooms[angelRoomIndex];
                 Vector3 angelSpawnPos = angelRoom.roomBounds.center + Vector3.up * 0.5f;
                 GameObject angelInstance = Instantiate(angelPrefab, angelSpawnPos, Quaternion.identity);
                 angelInstance.name = "Angel";
                 Debug.Log($"Spawned Angel in Room {angelRoomIndex}");

                 // Configure Angel script
                 MikesWeepingAngel angelScript = angelInstance.GetComponent<MikesWeepingAngel>();
                 if (angelScript != null) {
                     Camera playerCam = playerInstance.GetComponentInChildren<Camera>();
                     if(playerCam != null) {
                         angelScript.playerCam = playerCam;
                     } else {
                         Debug.LogError("Player Camera not found for Angel script!");
                     }
                 }
             }
        }
        */
    }

    private bool DoesRoomOverlap(Bounds newBounds, List<Room> existingRooms, float minSpacing)
    {
        foreach (Room room in existingRooms)
        {
            Bounds expandedBounds = room.roomBounds;
            expandedBounds.Expand(minSpacing); // Check against expanded bounds for spacing

            if (expandedBounds.Intersects(newBounds))
            {
                return true;
            }
        }
        return false;
    }

    // Generates walls for a room, creating door openings and Door objects.
    private void GenerateWalls(GameObject parent, Room currentRoom, float width, float height, float wallFullHeight)
    {
        Vector3 position = currentRoom.position;

        // Define wall center positions based on room center and dimensions
        Vector3[] wallCenters = {
            new Vector3(position.x, wallFullHeight / 2, position.z + height / 2), // North (Z+)
            new Vector3(position.x + width / 2, wallFullHeight / 2, position.z),   // East  (X+)
            new Vector3(position.x, wallFullHeight / 2, position.z - height / 2), // South (Z-)
            new Vector3(position.x - width / 2, wallFullHeight / 2, position.z)    // West  (X-)
        };
        // Define wall scales based on room dimensions and thickness
        Vector3[] wallScales = {
            new Vector3(width, wallFullHeight, corridorWallThickness), // N/S scale
            new Vector3(corridorWallThickness, wallFullHeight, height), // E/W scale
            new Vector3(width, wallFullHeight, corridorWallThickness), // N/S scale
            new Vector3(corridorWallThickness, wallFullHeight, height)  // E/W scale
        };
        int[] wallDirections = { 1, 2, 3, 4 }; // N, E, S, W

        for (int i = 0; i < 4; i++)
        {
            // Pass necessary info to create the wall with a door opening
            GenerateWallWithDoor(parent, currentRoom, wallCenters[i], wallScales[i], wallDirections[i], wallFullHeight);
        }
    }

    // Generates a wall section with a door opening and adds the Door object.
    private void GenerateWallWithDoor(GameObject parent, Room currentRoom, Vector3 wallCenter, Vector3 wallScale, int direction, float wallFullHeight)
    {
        float doorWidth = corridorWidth;
        float doorHeight = corridorHeight - 1f; // Door opening height (slightly less than corridor)
        float wallThickness = corridorWallThickness;

        // --- Calculate wall parts ---
        GameObject wallSegment1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        GameObject wallSegment2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        GameObject topSegment = GameObject.CreatePrimitive(PrimitiveType.Cube); // Above the door

        // Name them for easier debugging
        wallSegment1.name = $"Room_{currentRoom.id}_WallPart_Dir{direction}_Side1";
        wallSegment2.name = $"Room_{currentRoom.id}_WallPart_Dir{direction}_Side2";
        topSegment.name = $"Room_{currentRoom.id}_WallPart_Dir{direction}_Top";


        // Assign common properties
        wallSegment1.layer = wallSegment2.layer = topSegment.layer = LayerMask.NameToLayer("NotWalkable");
        wallSegment1.transform.parent = wallSegment2.transform.parent = topSegment.transform.parent = parent.transform;
        wallSegment1.GetComponent<Renderer>().material.color = Color.grey; // Example color
        wallSegment2.GetComponent<Renderer>().material.color = Color.grey;
        topSegment.GetComponent<Renderer>().material.color = Color.grey;


        // Position door opening near the floor level (calculation unchanged)
        Vector3 doorOpeningCenter = wallCenter + new Vector3(0, -wallFullHeight / 2 + doorHeight / 2, 0);

        if (direction == 1 || direction == 3) // North or South (Wall is horizontal)
        {
            float sideWallWidth = Mathf.Max(0, (wallScale.x - doorWidth) / 2); // Width of wall parts beside door

            wallSegment1.transform.localScale = new Vector3(sideWallWidth, wallFullHeight, wallThickness);
            wallSegment1.transform.position = wallCenter + new Vector3(-(doorWidth / 2 + sideWallWidth / 2), 0, 0);

            wallSegment2.transform.localScale = new Vector3(sideWallWidth, wallFullHeight, wallThickness);
            wallSegment2.transform.position = wallCenter + new Vector3(doorWidth / 2 + sideWallWidth / 2, 0, 0);

            topSegment.transform.localScale = new Vector3(doorWidth, wallFullHeight - doorHeight, wallThickness);
            topSegment.transform.position = wallCenter + new Vector3(0, doorHeight / 2 + (wallFullHeight - doorHeight) / 2, 0);
        }
        else // East or West (Wall is vertical)
        {
            float sideWallDepth = Mathf.Max(0, (wallScale.z - doorWidth) / 2); // Depth of wall parts beside door

            wallSegment1.transform.localScale = new Vector3(wallThickness, wallFullHeight, sideWallDepth);
            wallSegment1.transform.position = wallCenter + new Vector3(0, 0, -(doorWidth / 2 + sideWallDepth / 2));

            wallSegment2.transform.localScale = new Vector3(wallThickness, wallFullHeight, sideWallDepth);
            wallSegment2.transform.position = wallCenter + new Vector3(0, 0, doorWidth / 2 + sideWallDepth / 2);

            topSegment.transform.localScale = new Vector3(wallThickness, wallFullHeight - doorHeight, doorWidth);
            topSegment.transform.position = wallCenter + new Vector3(0, doorHeight / 2 + (wallFullHeight - doorHeight) / 2, 0);
        }

        // --- Create and store the Door object ---
        Vector3 logicalDoorPos = new Vector3(wallCenter.x, 0, wallCenter.z);
        Door newDoor = new Door(logicalDoorPos, false, direction, currentRoom);

        // --- STORE GEOMETRY REFERENCES ---
        newDoor.WallSegments.Add(wallSegment1);
        newDoor.WallSegments.Add(wallSegment2);
        newDoor.WallSegments.Add(topSegment);

        currentRoom.doors.Add(newDoor);
        allDoors.Add(newDoor);

        // Optional: Instantiate a visual door prefab AND STORE REFERENCE
        if (doorPrefab != null) {
            Quaternion doorRotation = (direction == 2 || direction == 4) ? Quaternion.Euler(0, 90, 0) : Quaternion.identity;
            // --- STORE PREFAB INSTANCE ---
            newDoor.DoorPrefabInstance = Instantiate(doorPrefab, logicalDoorPos, doorRotation, parent.transform); // Assign the instance
            newDoor.DoorPrefabInstance.name = $"Room_{currentRoom.id}_DoorPrefab_Dir{direction}"; // Name for debugging
        }
    }

    // --- NEW FUNCTION for Cleanup ---
    private void CleanupUnusedDoors()
    {
        Debug.Log("Starting unused door cleanup...");
        int removedCount = 0;
        // We need the original wall height, get it from a setting
        float wallFullHeight = corridorHeight;

        // Iterate backwards or use a copy if removing from the list being iterated
        List<Door> doorsToRemove = new List<Door>(); // Store doors to remove from main lists later

        foreach (Door door in allDoors)
        {
            if (!door.isConnected)
            {
                removedCount++;
                // Debug.Log($"Cleaning up unused door in Room {door.associatedRoom.id} facing direction {door.direction}");

                // 1. Destroy the instantiated prefab (if exists)
                if (door.DoorPrefabInstance != null)
                {
                    Destroy(door.DoorPrefabInstance);
                }

                // 2. Destroy the wall segments that formed the opening
                foreach (GameObject segment in door.WallSegments)
                {
                    if (segment != null) // Check if not already destroyed
                    {
                        Destroy(segment);
                    }
                }
                door.WallSegments.Clear(); // Clear the list

                // 3. Create a solid wall segment to fill the gap
                // We need to reconstruct the original wall's center and scale
                Room room = door.associatedRoom;
                float roomWidth = room.roomBounds.size.x;
                float roomDepth = room.roomBounds.size.z; // Use depth for Z axis
                Vector3 wallCenter;
                Vector3 wallScale;

                // Recalculate wall parameters based on door direction and room bounds
                switch (door.direction)
                {
                    case 1: // North (+Z)
                        wallCenter = new Vector3(room.position.x, wallFullHeight / 2, room.roomBounds.max.z);
                        wallScale = new Vector3(roomWidth, wallFullHeight, corridorWallThickness);
                        break;
                    case 2: // East (+X)
                        wallCenter = new Vector3(room.roomBounds.max.x, wallFullHeight / 2, room.position.z);
                        wallScale = new Vector3(corridorWallThickness, wallFullHeight, roomDepth);
                        break;
                    case 3: // South (-Z)
                        wallCenter = new Vector3(room.position.x, wallFullHeight / 2, room.roomBounds.min.z);
                        wallScale = new Vector3(roomWidth, wallFullHeight, corridorWallThickness);
                        break;
                    case 4: // West (-X)
                        wallCenter = new Vector3(room.roomBounds.min.x, wallFullHeight / 2, room.position.z);
                        wallScale = new Vector3(corridorWallThickness, wallFullHeight, roomDepth);
                        break;
                    default:
                        Debug.LogError($"Invalid direction {door.direction} for door cleanup.");
                        continue; // Skip this door
                }

                // Create the solid filler wall
                GameObject solidWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                solidWall.name = $"Room_{room.id}_FilledWall_Dir{door.direction}";
                solidWall.transform.position = wallCenter;
                solidWall.transform.localScale = wallScale;
                solidWall.transform.parent = room.roomBounds.center == Vector3.zero ? transform : GameObject.Find($"Room_{room.id}")?.transform ?? transform; // Try to parent to room GO
                 solidWall.layer = LayerMask.NameToLayer("NotWalkable");
                solidWall.GetComponent<Renderer>().material.color = Color.grey; // Match other walls

                // Mark this door for removal from lists
                doorsToRemove.Add(door);
            }
        }

        // Actually remove the door objects from the main lists
        foreach(Door doorToRemove in doorsToRemove)
        {
            allDoors.Remove(doorToRemove);
            if(doorToRemove.associatedRoom != null)
            {
                doorToRemove.associatedRoom.doors.Remove(doorToRemove);
            }
        }


        Debug.Log($"Finished cleanup. Removed {removedCount} unused doors.");
    }

    // --- Corridor Generation ---

    private void ConnectRoomsWithCorridors()
    {
        if (rooms.Count < 2) {
            Debug.Log("Not enough rooms to connect.");
            return;
        }
        Debug.Log("Starting corridor connection process...");

        List<Room> connected = new List<Room>();
        List<Room> unconnected = new List<Room>(rooms);

        connected.Add(unconnected[0]);
        unconnected.RemoveAt(0);
        int connectionsMade = 0;

        // Loop until all rooms are connected or no more connections can be made
        while (unconnected.Count > 0)
        {
            Room bestSourceRoom = null;
            Room bestTargetRoom = null;
            Door bestSourceDoor = null;
            Door bestTargetDoor = null;
            float shortestDist = float.MaxValue;
            PathCalculationResult bestPathResult = null;

            // Find the overall best connection between any connected and unconnected room
            foreach (Room sourceRoom in connected)
            {
                foreach (Room targetRoom in unconnected)
                {
                    // Basic distance check (optional optimization: skip pairs that are too far apart)
                    // float dist = Vector3.Distance(sourceRoom.position, targetRoom.position);

                    // Check all available door pairs between these two rooms
                    foreach (Door sourceDoor in sourceRoom.doors)
                    {
                        if (sourceDoor.isConnected) continue; // Skip already connected doors

                        foreach (Door targetDoor in targetRoom.doors)
                        {
                            if (targetDoor.isConnected) continue; // Skip already connected doors

                            // Check if doors generally face each other
                            Vector3 roomToRoomDir = (targetRoom.position - sourceRoom.position).normalized;
                            if (Vector3.Dot(GetDirectionVector(sourceDoor.direction), roomToRoomDir) < 0.1f) continue; // Source door points away
                            if (Vector3.Dot(GetDirectionVector(targetDoor.direction), -roomToRoomDir) < 0.1f) continue; // Target door points away

                            // Calculate potential path
                            PathCalculationResult currentPathResult = CalculateLPath(sourceDoor, targetDoor);

                            if (currentPathResult != null && currentPathResult.Points.Count >= 2)
                            {
                                // Use actual path length for finding the "best" path
                                float currentPathLength = CalculatePathLength(currentPathResult.Points);

                                // Is this path shorter than the best found so far?
                                if (currentPathLength < shortestDist)
                                {
                                    // Check for overlaps *only if* it's a candidate for the shortest path
                                    if (!DoesPathOverlap(currentPathResult, sourceRoom, targetRoom, corridorClearance))
                                    {
                                        shortestDist = currentPathLength;
                                        bestSourceRoom = sourceRoom;
                                        bestTargetRoom = targetRoom;
                                        bestSourceDoor = sourceDoor;
                                        bestTargetDoor = targetDoor;
                                        bestPathResult = currentPathResult;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // If we found a valid, non-overlapping connection in this iteration
            if (bestPathResult != null && bestSourceDoor != null && bestTargetDoor != null && bestTargetRoom != null && bestSourceRoom != null)
            {
                // Build the corridor geometry
                BuildCorridorFromPath(bestPathResult, corridorWidth, corridorHeight, corridorWallThickness);

                // Mark doors as connected
                bestSourceDoor.isConnected = true;
                bestTargetDoor.isConnected = true;

                // Move target room to the connected list
                connected.Add(bestTargetRoom);
                unconnected.Remove(bestTargetRoom);
                connectionsMade++;

                Debug.Log($"Connected Room {bestSourceRoom.id} (Door {bestSourceDoor.direction}) to Room {bestTargetRoom.id} (Door {bestTargetDoor.direction}). Path Length: {shortestDist:F1}. Remaining unconnected: {unconnected.Count}");
            }
            else
            {
                // No valid connection found in this pass - maybe remaining rooms are isolated or blocked
                Debug.LogError($"Could not find a non-overlapping path to connect any of the remaining {unconnected.Count} rooms. Stopping corridor generation.");
                break; // Exit the loop
            }
        }
        Debug.Log($"Finished corridor connection process. Made {connectionsMade} connections.");
    }

    // Calculates a simple L-shaped path between two doors. Returns PathCalculationResult.
    private PathCalculationResult CalculateLPath(Door startDoor, Door endDoor)
    {
        List<Vector3> path = new List<Vector3>();
        float stepOut = corridorWidth / 2.0f; // How far the path centerline steps out from the wall

        Vector3 startDir = GetDirectionVector(startDoor.direction);
        Vector3 endDir = GetDirectionVector(endDoor.direction); // Use end door's actual direction vector

        Vector3 p0 = startDoor.position; // Start point on wall
        Vector3 p1 = p0 + startDir * stepOut; // Point just outside start wall

        Vector3 p4 = endDoor.position; // End point on wall
        Vector3 p3 = p4 + endDir * stepOut; // Point just outside end wall

        // Check if doors are roughly aligned vertically or horizontally (straight path)
        bool alignedX = Mathf.Abs(p1.x - p3.x) < 0.1f;
        bool alignedZ = Mathf.Abs(p1.z - p3.z) < 0.1f;

        Vector3 p2 = Vector3.zero; // Corner point
        bool hasCorner = false;

        if (alignedX && alignedZ) // Points p1 and p3 are virtually identical
        {
             path.AddRange(new Vector3[] { p0, p1, p4 }); // Straight shot from door to door essentially
             hasCorner = false;
        }
        else if (alignedX || alignedZ) // Straight path segment needed between p1 and p3
        {
            path.AddRange(new Vector3[] { p0, p1, p3, p4 });
            hasCorner = false;
        }
        else // L-path needed
        {
            hasCorner = true;
            // Determine the corner point (p2)
            Vector3 cornerA = new Vector3(p1.x, 0, p3.z); // Horizontal first from p1
            Vector3 cornerB = new Vector3(p3.x, 0, p1.z); // Vertical first from p1

            // Choose corner based on start door direction preference
            if (startDoor.direction == 1 || startDoor.direction == 3) p2 = cornerB; // N/S start -> prefer vertical segment first from p1
            else p2 = cornerA; // E/W start -> prefer horizontal segment first from p1

             // Build the path list carefully adding distinct points
            path.Add(p0);
            path.Add(p1); // p1 should always be distinct from p0
            if (Vector3.Distance(p1, p2) > 0.1f) path.Add(p2); // Add corner if distinct
            else hasCorner = false; // Corner collapsed, effectively straight

            if (Vector3.Distance(p2, p3) > 0.1f || !hasCorner) // Add p3 if distinct from corner, or if path is straight
            {
                if(Vector3.Distance(path[path.Count-1], p3) > 0.1f) path.Add(p3);
            }

            if(Vector3.Distance(path[path.Count-1], p4) > 0.1f) path.Add(p4); // Add end point if distinct
        }

        // Final cleanup of very close points
        for (int i = path.Count - 1; i > 0; i--)
        {
            if (Vector3.Distance(path[i], path[i - 1]) < 0.05f)
            {
                path.RemoveAt(i);
            }
        }

         // Return the result object
         return new PathCalculationResult(path, p2, hasCorner && path.Count > 3); // Valid corner needs >= 4 points
    }

    // Enhanced overlap check using PathCalculationResult and checking corner volumes
    private bool DoesPathOverlap(PathCalculationResult pathResult, Room startRoom, Room endRoom, float clearance)
    {
        List<Vector3> pathPoints = pathResult.Points;
        if (pathPoints.Count < 2) return false; // Invalid path

        // Calculate potential corner bounds for the *new* path, expanded by clearance
        Bounds? potentialNewCornerBounds = null;
        if (pathResult.HasCorner)
        {
            Bounds cornerBounds = CreateBoundsForCorner(pathResult.CornerPoint, corridorWidth, corridorHeight);
            cornerBounds.Expand(clearance); // Apply clearance to corner check
            potentialNewCornerBounds = cornerBounds;
        }

        // --- Check 1: New Segments vs Existing Geometry ---
        for (int i = 0; i < pathPoints.Count - 1; i++)
        {
            // Calculate segment bounds expanded by clearance
            Bounds segmentBounds = CreateBoundsForSegment(pathPoints[i], pathPoints[i + 1], corridorWidth, corridorHeight);
            segmentBounds.Expand(clearance);

            // Check segment vs other rooms
            foreach (Room room in rooms)
            {
                if (room == startRoom || room == endRoom) continue;
                if (segmentBounds.Intersects(room.roomBounds)) { /* Debug.Log("Segment overlaps room"); */ return true; }
            }

            // Check segment vs existing segments
            foreach (Bounds existingSegBounds in corridorSegmentBounds)
            {
                if (segmentBounds.Intersects(existingSegBounds)) { /* Debug.Log("Segment overlaps segment"); */ return true; }
            }

            // Check segment vs existing CORNERS
            foreach (Bounds existingCornerBounds in corridorCornerBounds)
            {
                if (segmentBounds.Intersects(existingCornerBounds)) { /* Debug.Log("Segment overlaps corner"); */ return true; }
            }
        }

        // --- Check 2: New Corner vs Existing Geometry (only if new path has a corner) ---
        if (potentialNewCornerBounds.HasValue)
        {
             Bounds newCornerBounds = potentialNewCornerBounds.Value; // Already expanded

            // Check new corner vs other rooms
            foreach (Room room in rooms)
            {
                if (room == startRoom || room == endRoom) continue;
                if (newCornerBounds.Intersects(room.roomBounds)) { /* Debug.Log("Corner overlaps room"); */ return true; }
            }

            // Check new corner vs existing segments
            foreach (Bounds existingSegBounds in corridorSegmentBounds)
            {
                if (newCornerBounds.Intersects(existingSegBounds)) { /* Debug.Log("Corner overlaps segment"); */ return true; }
            }

            // Check new corner vs existing CORNERS
            foreach (Bounds existingCornerBounds in corridorCornerBounds)
            {
                if (newCornerBounds.Intersects(existingCornerBounds)) { /* Debug.Log("Corner overlaps corner"); */ return true; }
            }
        }

        return false; // No overlaps found
    }

    // Builds the corridor geometry, adjusting segments near corners for a clean join.
    private void BuildCorridorFromPath(PathCalculationResult pathResult, float width, float height, float thickness)
    {
        GameObject corridorParent = new GameObject($"Corridor_{corridorSegmentBounds.Count + corridorCornerBounds.Count}"); // Unique name
        List<Vector3> path = pathResult.Points;
        Vector3 cornerPoint = pathResult.CornerPoint;
        bool hasCorner = pathResult.HasCorner;
        float halfWidth = width / 2.0f;

        // --- Identify ALL Points for Geometry Building (INCLUDING WALL POINTS p0 and p4) ---
        List<Vector3> geometryPoints = new List<Vector3>();
        // Use checks to prevent index out of bounds if path is unexpectedly short
        if (path.Count >= 1) geometryPoints.Add(path[0]); // p0

        if (path.Count >= 2) geometryPoints.Add(path[1]); // p1

        if (hasCorner && path.Count >= 4) // Need at least p0,p1,p2,p3 for a corner path
        {
            // Find the actual corner point p2 in the path list reliably
            int cornerIdx_check = -1;
            for(int k=1; k < path.Count -1; k++) { // Search between p0 and p4
                if(Vector3.Distance(path[k], cornerPoint) < 0.01f) {
                    cornerIdx_check = k;
                    break;
                }
            }
            // Use the point from the path list if found, otherwise fallback to cornerPoint property
             if (cornerIdx_check != -1) {
                 geometryPoints.Add(path[cornerIdx_check]); // Add the actual point p2 from the list
             } else {
                 Debug.LogWarning($"BuildCorridorFromPath: Corner point {cornerPoint} not found reliably in path list: {string.Join(",", path)}! Using fallback property.");
                 geometryPoints.Add(cornerPoint); // Fallback to the property value
             }
        }

        // Add p3 (check counts relative to a full path p0-p4 or p0-p3)
        if (!hasCorner && path.Count >= 3) // Straight path p0,p1,p3,p4 needs at least 3 points in original path for p3
        {
             geometryPoints.Add(path[path.Count - 2]); // Add p3
        }
        else if (hasCorner && path.Count >= 4) // Corner path p0,p1,p2,p3,p4 needs at least 4 points for p3
        {
             geometryPoints.Add(path[path.Count - 2]); // Add p3
        }


        // Add p4 (check count >= 1 originally is enough for path[path.Count - 1])
         if (path.Count >= 1) geometryPoints.Add(path[path.Count - 1]); // p4

        // Remove duplicates from collapsed points (requires System.Linq)
        geometryPoints = geometryPoints.Distinct().ToList();

        // --- Build Geometry Segments ---
        for (int i = 0; i < geometryPoints.Count - 1; i++)
        {
            Vector3 startPoint = geometryPoints[i];
            Vector3 endPoint = geometryPoints[i + 1];
            Vector3 segmentVector = endPoint - startPoint;

            // Skip zero-length segments robustly
            if (segmentVector.sqrMagnitude < 0.001f) {
                 continue;
            }

            Vector3 segmentDirection = segmentVector.normalized;

            Vector3 effectiveStart = startPoint;
            Vector3 effectiveEnd = endPoint;

            // --- Adjust for INNER corner ---
            if (hasCorner) {
                // Segment is ENDING at the corner point (p2)
                if (Vector3.Distance(endPoint, cornerPoint) < 0.01f)
                {
                    // Check if this segment started near p1 (the first centerline point)
                    // path[1] should exist if hasCorner is true and path is valid
                    bool segmentStartedAtP1 = false;
                    if (path.Count >= 2) { // Need at least p0, p1
                       segmentStartedAtP1 = (Vector3.Distance(startPoint, path[1]) < 0.01f);
                    }

                    // ONLY shorten (adjust effectiveEnd) if this segment DID NOT start at p1.
                    // If it *did* start at p1 (it's the p1->p2 segment), let it run fully to the cornerPoint.
                    if (!segmentStartedAtP1)
                    {
                        effectiveEnd = endPoint - segmentDirection * halfWidth;
                         // Debug.Log($"Inner Adjust: Shortening segment {i} ending at corner.");
                    }
                    // else: // This is p1->p2 segment, effectiveEnd remains endPoint (cornerPoint)
                    // { Debug.Log($"Inner Adjust: Keeping segment {i} (p1->p2) end at corner."); }

                }
                // Segment is STARTING at the corner point (p2)
                else if (Vector3.Distance(startPoint, cornerPoint) < 0.01f)
                {
                    // Always apply the standard adjustment for segments starting at the corner (pushes p2->p3 start away).
                    effectiveStart = startPoint + segmentDirection * halfWidth;
                     // Debug.Log($"Inner Adjust: Shifting segment {i} (p2->p3) start from corner.");
                }
            }
            // --- End of Inner Corner Adjustment ---


            // Re-check effective segment length after potential adjustments
             if (Vector3.Distance(effectiveStart, effectiveEnd) < 0.01f) {
                 // Debug.Log($"Skipping zero-length effective segment {i}");
                 continue;
             }

            // Create the segment geometry
            Bounds createdGeoBounds = CreateCorridorSegment(corridorParent.transform, effectiveStart, effectiveEnd, width, height, thickness);

            // Store Logical Bounds for Overlap Checking (using original CENTERLINE points p1, p2, p3)
            // Only store bounds for the main pathway (p1->p2, p2->p3 or p1->p3)
            bool storeBounds = false;
            Vector3 boundsStart = Vector3.zero;
            Vector3 boundsEnd = Vector3.zero;

             // Check based on the original segment points (startPoint, endPoint) before adjustments
             if (path.Count >= 3) // Need at least p0,p1,p3 for p1->p3 check
             {
                Vector3 p1 = path[1];
                Vector3 p3 = path[path.Count - 2]; // Original p3 location

                // Segment starts at p1? -> Store bounds for p1->p2 (or p1->p3 if straight)
                if (Vector3.Distance(startPoint, p1) < 0.01f)
                {
                     storeBounds = true;
                     boundsStart = p1;
                     boundsEnd = hasCorner ? cornerPoint : p3; // End at p2 (corner) or p3 (straight)
                }
                // Segment starts at p2 (corner)? -> Store bounds for p2->p3
                else if (hasCorner && Vector3.Distance(startPoint, cornerPoint) < 0.01f)
                {
                     storeBounds = true;
                     boundsStart = cornerPoint;
                     boundsEnd = p3; // Always ends at p3
                }

                if (storeBounds && createdGeoBounds.size != Vector3.zero)
                {
                    // Ensure the logical bounds segment has length
                     if(Vector3.Distance(boundsStart, boundsEnd) > 0.1f) {
                        Bounds logicalBounds = CreateBoundsForSegment(boundsStart, boundsEnd, width, height);
                        corridorSegmentBounds.Add(logicalBounds);
                        // DrawBounds(logicalBounds, Color.cyan, 15f);
                     }
                }
             } // End bounds check conditional

        } // --- End of segment building loop ---


        // --- Create Corner Geometry (Floor, Ceiling, AND OUTER WALLS) ---
        if (hasCorner)
        {
            // --- Corner Floor ---
            GameObject cornerFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cornerFloor.name = "CornerFloor";
            cornerFloor.transform.position = new Vector3(cornerPoint.x, -thickness / 2, cornerPoint.z);
            cornerFloor.transform.localScale = new Vector3(width, thickness, width);
            cornerFloor.transform.parent = corridorParent.transform;
            cornerFloor.GetComponent<Renderer>().material.color = Color.white;

            // --- Corner Ceiling ---
            GameObject cornerCeiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cornerCeiling.name = "CornerCeiling";
            cornerCeiling.transform.position = new Vector3(cornerPoint.x, height + thickness / 2, cornerPoint.z);
            cornerCeiling.transform.localScale = new Vector3(width, thickness, width);
            cornerCeiling.transform.parent = corridorParent.transform;
            cornerCeiling.layer = LayerMask.NameToLayer("NotWalkable");
            cornerCeiling.GetComponent<Renderer>().material.color = Color.Lerp(Color.grey, Color.black, 0.5f);


            // --- Create Outer Corner Walls ---
            Vector3 p_before_corner = Vector3.zero;
            Vector3 p_after_corner = Vector3.zero;
            int cornerIdx = -1;
             // Find corner index in the final geometryPoints list
             for(int k=0; k<geometryPoints.Count; k++){
                 if(Vector3.Distance(geometryPoints[k], cornerPoint) < 0.01f) {
                     cornerIdx = k;
                     break;
                 }
             }

            bool canCreateOuterWalls = false;
            if (cornerIdx > 0 && cornerIdx < geometryPoints.Count - 1) { // Check valid index
                 p_before_corner = geometryPoints[cornerIdx - 1]; // Point from geometry list
                 p_after_corner = geometryPoints[cornerIdx + 1]; // Point from geometry list
                 // Check if points are distinct enough from corner
                 if(Vector3.Distance(p_before_corner, cornerPoint) > 0.01f && Vector3.Distance(p_after_corner, cornerPoint) > 0.01f) {
                    canCreateOuterWalls = true;
                 }
            }

            if(!canCreateOuterWalls){
                  Debug.LogError($"BuildCorridorFromPath: Could not find valid points around corner ({cornerPoint}) in list: {string.Join(",", geometryPoints)} to generate outer walls.");
            }


            if (canCreateOuterWalls)
            {
                Vector3 inDir = (cornerPoint - p_before_corner).normalized; // Direction into corner
                Vector3 outDir = (p_after_corner - cornerPoint).normalized; // Direction leaving corner

                // Check for zero vectors just in case
                if (inDir.sqrMagnitude < 0.001f || outDir.sqrMagnitude < 0.001f) {
                     Debug.LogError("BuildCorridorFromPath: Zero direction vector encountered for outer walls.");
                } else {

                    Vector3 cornerCenterY = new Vector3(cornerPoint.x, height / 2, cornerPoint.z);
                    Vector3 perpToIn = Vector3.Cross(Vector3.up, inDir).normalized;
                    Vector3 perpToOut = Vector3.Cross(Vector3.up, outDir).normalized;
                    float turnDotY = Vector3.Cross(inDir, outDir).y; // Check turn direction

                    // If turnDotY is very close to zero, vectors are parallel/anti-parallel (not a corner?)
                    if (Mathf.Abs(turnDotY) < 0.1f) {
                         Debug.LogWarning($"BuildCorridorFromPath: Turn direction is near zero ({turnDotY}) at corner {cornerPoint}. Skipping outer walls.");
                    } else {
                        // Determine offset direction for outer walls
                        Vector3 wall1_OffsetDir = (turnDotY > 0) ? -perpToIn : perpToIn;   // Offset perpendicular to IN
                        Vector3 wall2_OffsetDir = (turnDotY > 0) ? -perpToOut : perpToOut; // Offset perpendicular to OUT

                        // Create Wall 1 (Aligned with OUT segment)
                        GameObject outerWall1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        outerWall1.name = "OuterCornerWall_1";
                        outerWall1.transform.parent = corridorParent.transform;
                        outerWall1.layer = LayerMask.NameToLayer("NotWalkable");
                        outerWall1.GetComponent<Renderer>().material.color = Color.grey;
                        outerWall1.transform.position = cornerCenterY + wall1_OffsetDir * halfWidth; // Use correct offset dir
                        bool outIsHorizontal = Mathf.Abs(outDir.x) > Mathf.Abs(outDir.z);
                        outerWall1.transform.localScale = outIsHorizontal
                            ? new Vector3(width, height, thickness)
                            : new Vector3(thickness, height, width);
                        outerWall1.transform.Rotate(0f, 90f, 0f, Space.Self); // Rotate


                        // Create Wall 2 (Aligned with IN segment)
                        GameObject outerWall2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        outerWall2.name = "OuterCornerWall_2";
                        outerWall2.transform.parent = corridorParent.transform;
                        outerWall2.layer = LayerMask.NameToLayer("NotWalkable");
                        outerWall2.GetComponent<Renderer>().material.color = Color.grey;
                        outerWall2.transform.position = cornerCenterY + wall2_OffsetDir * halfWidth; // Use correct offset dir
                        bool inIsHorizontal = Mathf.Abs(inDir.x) > Mathf.Abs(inDir.z);
                        outerWall2.transform.localScale = inIsHorizontal
                            ? new Vector3(width, height, thickness)
                            : new Vector3(thickness, height, width);
                        outerWall2.transform.Rotate(0f, 90f, 0f, Space.Self); // Rotate
                    } // End check for parallel vectors
                } // End check for zero vectors
            } // End if(canCreateOuterWalls)


            // Store the logical corner bounds for overlap checking (as before)
            Bounds logicalCornerBounds = CreateBoundsForCorner(cornerPoint, width, height);
            corridorCornerBounds.Add(logicalCornerBounds);
            // DrawBounds(logicalCornerBounds, Color.magenta, 15f);

        } // --- End if (hasCorner) ---

    } // --- End of BuildCorridorFromPath ---

    // Creates geometry for a single corridor segment and returns its logical bounds
    private Bounds CreateCorridorSegment(Transform parent, Vector3 startPos, Vector3 endPos, float width, float height, float thickness)
    {
        // Ignore zero-length segments
        if (Vector3.Distance(startPos, endPos) < 0.01f) return new Bounds(Vector3.zero, Vector3.zero);

        // Calculate segment properties
        Vector3 direction = (endPos - startPos).normalized;
        float segmentLength = Vector3.Distance(startPos, endPos);
        Vector3 segmentCenter = (startPos + endPos) / 2;
        bool isHorizontal = Mathf.Abs(direction.x) > Mathf.Abs(direction.z);

        // --- Create Geometry ---
        Vector3 floorPos = new Vector3(segmentCenter.x, -thickness / 2, segmentCenter.z);
        Vector3 floorScale = isHorizontal ? new Vector3(segmentLength, thickness, width) : new Vector3(width, thickness, segmentLength);
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "FloorSegment"; floor.transform.position = floorPos; floor.transform.localScale = floorScale; floor.transform.parent = parent;
        floor.GetComponent<Renderer>().material.color = Color.white; // Example color

        Vector3 ceilPos = new Vector3(segmentCenter.x, height + thickness / 2, segmentCenter.z);
        GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ceiling.name = "CeilingSegment"; ceiling.transform.position = ceilPos; ceiling.transform.localScale = floorScale; ceiling.transform.parent = parent;
        ceiling.layer = LayerMask.NameToLayer("NotWalkable");
        ceiling.GetComponent<Renderer>().material.color = Color.Lerp(Color.grey, Color.black, 0.5f); // Dark grey

        Vector3 wallCenterY = new Vector3(segmentCenter.x, height / 2, segmentCenter.z);
        GameObject wall1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall1.name = "WallSegment1"; wall1.layer = LayerMask.NameToLayer("NotWalkable"); wall1.transform.parent = parent;
        wall1.GetComponent<Renderer>().material.color = Color.grey;
        GameObject wall2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall2.name = "WallSegment2"; wall2.layer = LayerMask.NameToLayer("NotWalkable"); wall2.transform.parent = parent;
        wall2.GetComponent<Renderer>().material.color = Color.grey;

        if (isHorizontal) {
            wall1.transform.position = wallCenterY + new Vector3(0, 0, width / 2); wall1.transform.localScale = new Vector3(segmentLength, height, thickness);
            wall2.transform.position = wallCenterY + new Vector3(0, 0, -width / 2); wall2.transform.localScale = new Vector3(segmentLength, height, thickness);
        } else {
            wall1.transform.position = wallCenterY + new Vector3(width / 2, 0, 0); wall1.transform.localScale = new Vector3(thickness, height, segmentLength);
            wall2.transform.position = wallCenterY + new Vector3(-width / 2, 0, 0); wall2.transform.localScale = new Vector3(thickness, height, segmentLength);
        }
        // --- End Geometry Creation ---

        // Calculate and return the logical bounds for overlap checking (centered vertically)
        Vector3 boundsCenter = new Vector3(segmentCenter.x, height / 2, segmentCenter.z);
        Vector3 boundsSize = isHorizontal ? new Vector3(segmentLength, height, width) : new Vector3(width, height, segmentLength);
        return new Bounds(boundsCenter, boundsSize);
    }

    // Helper to create Bounds for a corridor segment for overlap checking
    private Bounds CreateBoundsForSegment(Vector3 startPos, Vector3 endPos, float width, float height)
    {
        // Ignore zero-length segments
        if (Vector3.Distance(startPos, endPos) < 0.01f) return new Bounds(Vector3.zero, Vector3.zero);

        // Calculate segment properties
        Vector3 direction = (endPos - startPos).normalized;
        float segmentLength = Vector3.Distance(startPos, endPos);
        Vector3 segmentCenter = (startPos + endPos) / 2;
        bool isHorizontal = Mathf.Abs(direction.x) > Mathf.Abs(direction.z);

        // Calculate the logical bounds for overlap checking (centered vertically)
        Vector3 boundsCenter = new Vector3(segmentCenter.x, height / 2, segmentCenter.z);
        Vector3 boundsSize = isHorizontal
            ? new Vector3(segmentLength, height, width)
            : new Vector3(width, height, segmentLength);

        return new Bounds(boundsCenter, boundsSize);
    }

    // Helper to create Bounds for a corner volume used in overlap checks
    private Bounds CreateBoundsForCorner(Vector3 cornerPoint, float width, float height)
    {
        Vector3 center = new Vector3(cornerPoint.x, height / 2, cornerPoint.z); // Center vertically
        Vector3 size = new Vector3(width, height, width); // Volume is roughly width x height x width
        return new Bounds(center, size);
    }

    // Helper to calculate the total length of a path defined by points
    private float CalculatePathLength(List<Vector3> path)
    {
        float length = 0f;
        for (int i = 0; i < path.Count - 1; i++)
        {
            length += Vector3.Distance(path[i], path[i + 1]);
        }
        return length;
    }

    // Returns a unit vector representing a given direction (1=N, 2=E, 3=S, 4=W).
    private Vector3 GetDirectionVector(int direction)
    {
        switch (direction)
        {
            case 1: return Vector3.forward; // North (+Z)
            case 2: return Vector3.right;   // East (+X)
            case 3: return Vector3.back;    // South (-Z)
            case 4: return Vector3.left;    // West (-X)
            default: Debug.LogWarning($"Invalid direction: {direction}"); return Vector3.zero;
        }
    }

     // --- Optional Debug Helper ---
    // void DrawBounds(Bounds b, Color c, float duration = 0) {
    //     // Draw lines to visualize the bounds
    //     Vector3 p1 = b.min;
    //     Vector3 p2 = new Vector3(b.max.x, b.min.y, b.min.z);
    //     Vector3 p3 = new Vector3(b.max.x, b.min.y, b.max.z);
    //     Vector3 p4 = new Vector3(b.min.x, b.min.y, b.max.z);
    //     Vector3 p5 = new Vector3(b.min.x, b.max.y, b.min.z);
    //     Vector3 p6 = new Vector3(b.max.x, b.max.y, b.min.z);
    //     Vector3 p7 = b.max;
    //     Vector3 p8 = new Vector3(b.min.x, b.max.y, b.max.z);

    //     Debug.DrawLine(p1, p2, c, duration); Debug.DrawLine(p2, p3, c, duration); Debug.DrawLine(p3, p4, c, duration); Debug.DrawLine(p4, p1, c, duration);
    //     Debug.DrawLine(p5, p6, c, duration); Debug.DrawLine(p6, p7, c, duration); Debug.DrawLine(p7, p8, c, duration); Debug.DrawLine(p8, p5, c, duration);
    //     Debug.DrawLine(p1, p5, c, duration); Debug.DrawLine(p2, p6, c, duration); Debug.DrawLine(p3, p7, c, duration); Debug.DrawLine(p4, p8, c, duration);
    // }


    // --- NavMesh Baking ---
    void BakeNavMesh()
    {
        if (navMeshSurface != null)
        {
            Debug.Log("Baking NavMesh...");
            navMeshSurface.BuildNavMesh();
            Debug.Log("NavMesh Bake Complete.");
        }
        else
        {
            Debug.LogError("NavMeshSurface is not assigned in the Inspector!");
        }
    }

} // End of RoomGenerator3 class