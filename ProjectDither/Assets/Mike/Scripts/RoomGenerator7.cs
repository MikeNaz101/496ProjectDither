using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation; // Required for NavMeshSurface
using System.Linq;
using Random = UnityEngine.Random; // Needed for OrderBy, Any, FindIndex, etc.
//using System;

public class RoomGenerator7 : MonoBehaviour
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
    private class PathCalculationResult
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
        foreach (Transform child in transform) {
             // Avoid destroying essential scene objects, maybe check names/tags
            if(child.name.StartsWith("Room_") || child.name.StartsWith("Corridor_") || child.name.StartsWith("FilledWall_")) {
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

         if (dogPrefab != null && foodBowlPrefab != null && playerInstance != null && rooms.Count > numRooms / 2)
         {
              int dogRoomIndex = numRooms / 2;
              if(dogRoomIndex < rooms.Count) {
                  Room dogRoom = rooms[dogRoomIndex];
                  Vector3 dogSpawnPos = dogRoom.roomBounds.center + Vector3.up * 0.5f;
                  GameObject dogInstance = Instantiate(dogPrefab, dogSpawnPos, Quaternion.identity);
                  dogInstance.name = "Dog";
                  Debug.Log($"Spawned Dog in Room {dogRoomIndex}");

                  float bowlX = Random.Range(dogRoom.roomBounds.min.x + 1, dogRoom.roomBounds.max.x - 1);
                  float bowlZ = Random.Range(dogRoom.roomBounds.min.z + 1, dogRoom.roomBounds.max.z - 1);
                  GameObject foodBowlInstance = Instantiate(foodBowlPrefab, new Vector3(bowlX, 0.5f, bowlZ), Quaternion.identity);
                  foodBowlInstance.name = "FoodBowl";
                  Debug.Log($"Spawned Food Bowl in Room {dogRoomIndex}");

                  DogStateManager dogScript = dogInstance.GetComponent<DogStateManager>();
                  if (dogScript != null)
                  {
                      dogScript.foodBowl = foodBowlInstance;
                      dogScript.player = playerInstance;
                  }
              }
         }

         if (angelPrefab != null && playerInstance != null && rooms.Count > 1)
         {
              int angelRoomIndex = rooms.Count - 2;
              if (angelRoomIndex <= 0 && rooms.Count > 1) angelRoomIndex = 1;

              if(angelRoomIndex >= 0 && angelRoomIndex < rooms.Count) {
                  Room angelRoom = rooms[angelRoomIndex];
                  Vector3 angelSpawnPos = angelRoom.roomBounds.center + Vector3.up * 0.5f;
                  GameObject angelInstance = Instantiate(angelPrefab, angelSpawnPos, Quaternion.identity);
                  angelInstance.name = "Angel";
                  Debug.Log($"Spawned Angel in Room {angelRoomIndex}");

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

    }

    private bool DoesRoomOverlap(Bounds newBounds, List<Room> existingRooms, float minSpacing)
    {
        // Expand the new bounds slightly to check against the minimum spacing requirement
        Bounds checkBounds = newBounds;
        checkBounds.Expand(minSpacing); // Expand by the required minimum distance

        foreach (Room room in existingRooms)
        {
            // Check if the expanded bounds of the new room intersect the original bounds of existing rooms
            if (checkBounds.Intersects(room.roomBounds))
            {
                return true;
            }
        }
        return false;
    }


    // Generates walls for a room, creating door openings and Door objects.
    // Use roomDepth for Z scaling of E/W walls
    private void GenerateWalls(GameObject parent, Room currentRoom, float width, float roomDepth, float wallFullHeight)
    {
        Vector3 position = currentRoom.position;

        // Define wall center positions based on room center and dimensions
        Vector3[] wallCenters = {
            new Vector3(position.x, wallFullHeight / 2, position.z + roomDepth / 2), // North (Z+)
            new Vector3(position.x + width / 2, wallFullHeight / 2, position.z),     // East  (X+)
            new Vector3(position.x, wallFullHeight / 2, position.z - roomDepth / 2), // South (Z-)
            new Vector3(position.x - width / 2, wallFullHeight / 2, position.z)      // West  (X-)
        };
        // Define wall scales based on room dimensions and thickness
        Vector3[] wallScales = {
            new Vector3(width, wallFullHeight, corridorWallThickness),     // N/S scale
            new Vector3(corridorWallThickness, wallFullHeight, roomDepth), // E/W scale (use roomDepth)
            new Vector3(width, wallFullHeight, corridorWallThickness),     // N/S scale
            new Vector3(corridorWallThickness, wallFullHeight, roomDepth)  // E/W scale (use roomDepth)
        };
        int[] wallDirections = { 1, 2, 3, 4 }; // N, E, S, W

        for (int i = 0; i < 4; i++)
        {
            // Pass necessary info to create the wall with a door opening
            GenerateWallWithDoor(parent, currentRoom, wallCenters[i], wallScales[i], wallDirections[i], wallFullHeight);
        }

        // --- Create the Floor for the Room! Yum! 🍰 ---
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = $"Room_{currentRoom.id}_Floor";
        // Place it at Y=0 with thickness, adjust center slightly
        floor.transform.position = new Vector3(position.x, -corridorWallThickness / 2f, position.z);
        floor.transform.localScale = new Vector3(width, corridorWallThickness, roomDepth);
        floor.transform.parent = parent.transform;

        // Apply the lovely floor material!
        Renderer floorRenderer = floor.GetComponent<Renderer>();
        if (floorMaterial != null)
        {
            floorRenderer.material = floorMaterial;
        }
        else
        {
            floorRenderer.material.color = Color.white; // Fallback color
            Debug.LogWarning("No floor material assigned! Room floor is white.");
        }
    }

    // Generates a wall section with a door opening and adds the Door object.
    private void GenerateWallWithDoor(GameObject parent, Room currentRoom, Vector3 wallCenter, Vector3 wallScale, int direction, float wallFullHeight)
    {
        float doorWidth = corridorWidth;
        float doorHeight = corridorHeight - 1f; // Door opening height
        float wallThickness = corridorWallThickness;

        // --- Calculate wall parts ---
        GameObject wallSegment1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        GameObject wallSegment2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        GameObject topSegment = GameObject.CreatePrimitive(PrimitiveType.Cube); // Above the door

        wallSegment1.name = $"Room_{currentRoom.id}_WallPart_Dir{direction}_Side1";
        wallSegment2.name = $"Room_{currentRoom.id}_WallPart_Dir{direction}_Side2";
        topSegment.name = $"Room_{currentRoom.id}_WallPart_Dir{direction}_Top";

        wallSegment1.layer = wallSegment2.layer = topSegment.layer = LayerMask.NameToLayer("NotWalkable"); // Ensure layer exists
        wallSegment1.transform.parent = wallSegment2.transform.parent = topSegment.transform.parent = parent.transform;

        // --- Apply the ROOM wall material! 🍓 ---
        Renderer renderer1 = wallSegment1.GetComponent<Renderer>();
        if (wallMaterial != null) renderer1.material = wallMaterial; else renderer1.material.color = Color.grey; // Fallback color

        Renderer renderer2 = wallSegment2.GetComponent<Renderer>();
        if (wallMaterial != null) renderer2.material = wallMaterial; else renderer2.material.color = Color.grey;

        Renderer rendererTop = topSegment.GetComponent<Renderer>();
        if (wallMaterial != null) rendererTop.material = wallMaterial; else rendererTop.material.color = Color.grey;
        // --- End Material Application ---


        // Position door opening near the floor level
        Vector3 doorOpeningCenter = wallCenter + new Vector3(0, -wallFullHeight / 2 + doorHeight / 2, 0);

        // Recalculate positions and scales based on door opening center and dimensions
        if (direction == 1 || direction == 3) // North or South (Wall runs along X)
        {
            float sideWallWidth = Mathf.Max(0, (wallScale.x - doorWidth) / 2);

            // Wall Segment 1 (Left)
            wallSegment1.transform.localScale = new Vector3(sideWallWidth, wallFullHeight, wallThickness);
            wallSegment1.transform.position = new Vector3(wallCenter.x - (doorWidth / 2 + sideWallWidth / 2), wallCenter.y, wallCenter.z);

            // Wall Segment 2 (Right)
            wallSegment2.transform.localScale = new Vector3(sideWallWidth, wallFullHeight, wallThickness);
            wallSegment2.transform.position = new Vector3(wallCenter.x + (doorWidth / 2 + sideWallWidth / 2), wallCenter.y, wallCenter.z);

            // Top Segment (Above Door)
            topSegment.transform.localScale = new Vector3(doorWidth, wallFullHeight - doorHeight, wallThickness);
            topSegment.transform.position = new Vector3(wallCenter.x, doorOpeningCenter.y + doorHeight/2 + (wallFullHeight - doorHeight)/2, wallCenter.z); // Centered above opening
        }
        else // East or West (Wall runs along Z)
        {
            float sideWallDepth = Mathf.Max(0, (wallScale.z - doorWidth) / 2);

             // Wall Segment 1 (Near)
            wallSegment1.transform.localScale = new Vector3(wallThickness, wallFullHeight, sideWallDepth);
            wallSegment1.transform.position = new Vector3(wallCenter.x, wallCenter.y, wallCenter.z - (doorWidth / 2 + sideWallDepth / 2));

            // Wall Segment 2 (Far)
            wallSegment2.transform.localScale = new Vector3(wallThickness, wallFullHeight, sideWallDepth);
            wallSegment2.transform.position = new Vector3(wallCenter.x, wallCenter.y, wallCenter.z + (doorWidth / 2 + sideWallDepth / 2));

            // Top Segment (Above Door)
            topSegment.transform.localScale = new Vector3(wallThickness, wallFullHeight - doorHeight, doorWidth);
            topSegment.transform.position = new Vector3(wallCenter.x, doorOpeningCenter.y + doorHeight/2 + (wallFullHeight - doorHeight)/2 , wallCenter.z); // Centered above opening
        }

        // Create and store the Door object
        Vector3 logicalDoorPos = new Vector3(wallCenter.x, 0, wallCenter.z); // Y=0 for logical position
        Door newDoor = new Door(logicalDoorPos, false, direction, currentRoom);

        // Store geometry references
        newDoor.WallSegments.Add(wallSegment1);
        newDoor.WallSegments.Add(wallSegment2);
        newDoor.WallSegments.Add(topSegment);

        currentRoom.doors.Add(newDoor);
        allDoors.Add(newDoor);

        // Optional: Instantiate a visual door prefab
        if (doorPrefab != null) {
            Quaternion doorRotation = (direction == 2 || direction == 4) ? Quaternion.Euler(0, 90, 0) : Quaternion.identity;
            // Adjust position slightly if needed based on pivot, example assumes centered pivot
            Vector3 doorVisualPos = new Vector3(logicalDoorPos.x, 0, logicalDoorPos.z); // Adjust Y if prefab pivot is not at base
            newDoor.DoorPrefabInstance = Instantiate(doorPrefab, doorVisualPos, doorRotation, parent.transform);
            newDoor.DoorPrefabInstance.name = $"Room_{currentRoom.id}_DoorPrefab_Dir{direction}";
        }
    }

    // --- Door Cleanup ---
    private void CleanupUnusedDoors()
    {
        Debug.Log("Starting unused door cleanup...");
        int removedCount = 0;
        float wallFullHeight = corridorHeight; // Assuming wall height matches corridor height

        List<Door> doorsToRemove = new List<Door>();

        foreach (Door door in allDoors)
        {
            if (!door.isConnected)
            {
                removedCount++;

                // 1. Destroy the instantiated prefab
                if (door.DoorPrefabInstance != null)
                {
                    Destroy(door.DoorPrefabInstance);
                }

                // 2. Destroy the wall segments that formed the opening
                foreach (GameObject segment in door.WallSegments)
                {
                    if (segment != null) Destroy(segment);
                }
                door.WallSegments.Clear();

                // 3. Create a solid wall segment to fill the gap
                Room room = door.associatedRoom;
                float roomWidth = room.roomBounds.size.x;
                float roomDepth = room.roomBounds.size.z;
                Vector3 wallCenter;
                Vector3 wallScale;

                // Recalculate original wall parameters
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
                        continue;
                }

                // Find the parent room GameObject
                 GameObject roomParentGO = GameObject.Find($"Room_{room.id}");
                 Transform parentTransform = (roomParentGO != null) ? roomParentGO.transform : this.transform; // Fallback to generator object

                GameObject solidWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                solidWall.name = $"Room_{room.id}_FilledWall_Dir{door.direction}";
                solidWall.transform.position = wallCenter;
                solidWall.transform.localScale = wallScale;
                solidWall.transform.parent = parentTransform;
                solidWall.layer = LayerMask.NameToLayer("NotWalkable"); // Match other walls

                // --- Apply the ROOM wall material to the filled wall! 🍓 ---
                Renderer solidWallRenderer = solidWall.GetComponent<Renderer>();
                if (wallMaterial != null) solidWallRenderer.material = wallMaterial; else solidWallRenderer.material.color = Color.grey; // Fallback

                doorsToRemove.Add(door);
            }
        }

        // Remove the door objects from lists
        foreach(Door doorToRemove in doorsToRemove)
        {
            allDoors.Remove(doorToRemove);
            if(doorToRemove.associatedRoom != null)
            {
                doorToRemove.associatedRoom.doors.Remove(doorToRemove);
            }
        }
        Debug.Log($"Finished cleanup. Removed {removedCount} unused doors and filled openings.");
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

        while (unconnected.Count > 0)
        {
            Room bestSourceRoom = null;
            Room bestTargetRoom = null;
            Door bestSourceDoor = null;
            Door bestTargetDoor = null;
            float shortestDist = float.MaxValue;
            PathCalculationResult bestPathResult = null; // Using OLD PathCalculationResult for now

            foreach (Room sourceRoom in connected)
            {
                foreach (Room targetRoom in unconnected)
                {
                    foreach (Door sourceDoor in sourceRoom.doors)
                    {
                        if (sourceDoor.isConnected) continue;
                        foreach (Door targetDoor in targetRoom.doors)
                        {
                            if (targetDoor.isConnected) continue;

                            PathCalculationResult currentPathResult = CalculateLPath(sourceDoor, targetDoor);

                            if (currentPathResult != null && currentPathResult.Points.Count >= 2)
                            {
                                float currentPathLength = CalculatePathLength(currentPathResult.Points);

                                if (currentPathLength < shortestDist)
                                {
                                     if (!DoesPathOverlap(currentPathResult.Points, sourceRoom, targetRoom, corridorClearance)) // Pass points list directly
                                     {
                                         shortestDist = currentPathLength;
                                         bestSourceRoom = sourceRoom;
                                         bestTargetRoom = targetRoom;
                                         bestSourceDoor = sourceDoor;
                                         bestTargetDoor = targetDoor;
                                         bestPathResult = currentPathResult; // Store the L-Path result
                                     }
                                }
                            }
                        }
                    }
                }
            }

            if (bestPathResult != null && bestSourceDoor != null && bestTargetDoor != null)
            {
                BuildCorridorFromPath(bestPathResult.Points, corridorWidth, corridorHeight, corridorWallThickness);

                bestSourceDoor.isConnected = true;
                bestTargetDoor.isConnected = true;
                connected.Add(bestTargetRoom);
                unconnected.Remove(bestTargetRoom);
                connectionsMade++;
                Debug.Log($"Connected Room {bestSourceRoom.id} (Door {bestSourceDoor.direction}) to Room {bestTargetRoom.id} (Door {bestTargetDoor.direction}). Remaining: {unconnected.Count}");
            }
            else
            {
                Debug.LogError($"Could not find a non-overlapping path to connect any of the remaining {unconnected.Count} rooms. Stopping.");
                break;
            }
        }
        Debug.Log($"Finished corridor connection process. Made {connectionsMade} connections.");

        // --- Phase 2: Add Extra Connections to Ensure Minimum Connectivity ---
        Debug.Log("Starting Phase 2: Ensuring minimum room connections...");
        int safetyBreakMaxAttempts = rooms.Count * 3; // Safety counter to prevent infinite loops
        int currentExtraConnectionAttempts = 0;
        List<Room> roomsCheckedInThisCycle = new List<Room>(); // To prevent trying the same failed room immediately

        while (currentExtraConnectionAttempts < safetyBreakMaxAttempts)
        {
            currentExtraConnectionAttempts++; // Increment attempt counter

            // Find all rooms that currently have fewer than 2 connections
            List<Room> roomsNeedingConnections = rooms.Where(r => r.doors.Count(d => d.isConnected) < 2).ToList();

            if (roomsNeedingConnections.Count == 0)
            {
                Debug.Log("All rooms now have at least 2 connections.");
                break; // Exit loop - success!
            }

            // Prioritize rooms not checked recently if possible, otherwise just take the first
            Room roomToConnect = roomsNeedingConnections.FirstOrDefault(r => !roomsCheckedInThisCycle.Contains(r)) ?? roomsNeedingConnections[0];
            roomsCheckedInThisCycle.Add(roomToConnect); // Mark as checked for this cycle

            Debug.Log($"Attempting to add second connection for Room {roomToConnect.id} (currently has {roomToConnect.doors.Count(d => d.isConnected)} connection(s))...");
            bool connectionAddedForThisRoom = false;

            // --- Find the best possible connection for this room to ANY OTHER room ---
            Room bestTargetRoom = null;
            Door bestSourceDoorForRoomA = null;
            Door bestTargetDoorForRoomB = null;
            List<Vector3> bestPathPoints = null;
            float shortestPathFound = float.MaxValue;

            // Iterate through all potential target rooms
            foreach (Room potentialTargetRoom in rooms)
            {
                if (roomToConnect == potentialTargetRoom) continue; // Cannot connect to self

                // Find the best door pair and path between roomToConnect and potentialTargetRoom
                foreach (Door sourceDoor in roomToConnect.doors.Where(d => !d.isConnected))
                {
                    foreach (Door targetDoor in potentialTargetRoom.doors.Where(d => !d.isConnected))
                    {
                        // Calculate path (Use L-Path for now, replace if you have A*)
                        PathCalculationResult pathResult = CalculateLPath(sourceDoor, targetDoor);

                        if (pathResult != null && pathResult.Points.Count >= 2)
                        {
                            // Check Overlap
                            if (!DoesPathOverlap(pathResult.Points, roomToConnect, potentialTargetRoom, corridorClearance))
                            {
                                float pathLength = CalculatePathLength(pathResult.Points);
                                if (pathLength < shortestPathFound)
                                {
                                    shortestPathFound = pathLength;
                                    bestTargetRoom = potentialTargetRoom;
                                    bestSourceDoorForRoomA = sourceDoor;
                                    bestTargetDoorForRoomB = targetDoor;
                                    bestPathPoints = pathResult.Points;
                                }
                            }
                        }
                    }
                }
            } // End foreach potentialTargetRoom

            // --- If we found a valid connection for roomToConnect ---
            if (bestPathPoints != null)
            {
                Debug.Log($"Adding extra connection: Room {roomToConnect.id} -> Room {bestTargetRoom.id}. Path Length: {shortestPathFound:F1}");
                BuildCorridorFromPath(bestPathPoints, corridorWidth, corridorHeight, corridorWallThickness);
                bestSourceDoorForRoomA.isConnected = true;
                bestTargetDoorForRoomB.isConnected = true;
                connectionAddedForThisRoom = true;
                roomsCheckedInThisCycle.Clear(); // Reset checked list as we made progress
                // The loop will continue to check if more rooms need connections
            }
            else
            {
                // We checked all other rooms and couldn't find a valid connection for roomToConnect
                Debug.LogWarning($"Could not find an additional non-overlapping path for Room {roomToConnect.id}. It may remain under-connected.");
                // Keep it in roomsCheckedInThisCycle so we don't immediately retry it if others also fail
                if (roomsCheckedInThisCycle.Count >= roomsNeedingConnections.Count)
                {
                    // If we've checked all currently needy rooms this cycle and added no connections, break
                    Debug.LogWarning("Stuck trying to add connections. Some rooms might remain under-connected.");
                    break;
                }
            }

            // Safety break check
            if (currentExtraConnectionAttempts >= safetyBreakMaxAttempts)
            {
                Debug.LogError("Exceeded max attempts for adding extra connections. Process stopped.");
                break;
            }

        } // End while loop for extra connections
        // --- End of Phase 2 ---
        // --- Final Step: Attempt to Connect Last Room to First Room ---
        Debug.Log("Attempting final loop connection: Last room to First room...");
        if (rooms.Count >= 2)
        {
            Room firstRoom = rooms[0];
            Room lastRoom = rooms[rooms.Count - 1];
            bool loopConnected = false; // Tracks if this specific connection was made

            Door bestLoopSourceDoor = null;
            Door bestLoopTargetDoor = null;
            float shortestLoopDist = float.MaxValue;
            List<Vector3> bestLoopPathPoints = null;

            // Check all available UNUSED door pairs between the last and first room
            foreach (Door sourceDoor in lastRoom.doors.Where(d => !d.isConnected)) // Only check unused doors
            {
                foreach (Door targetDoor in firstRoom.doors.Where(d => !d.isConnected)) // Only check unused doors
                {
                    // Calculate path (Use L-Path for now, replace if you have A*)
                    PathCalculationResult loopPathResult = CalculateLPath(sourceDoor, targetDoor);

                    if (loopPathResult != null && loopPathResult.Points.Count >= 2)
                    {
                        // Check Overlap
                        if (!DoesPathOverlap(loopPathResult.Points, lastRoom, firstRoom, corridorClearance))
                        {
                            float currentLoopPathLength = CalculatePathLength(loopPathResult.Points);
                            if (currentLoopPathLength < shortestLoopDist)
                            {
                                shortestLoopDist = currentLoopPathLength;
                                bestLoopSourceDoor = sourceDoor;
                                bestLoopTargetDoor = targetDoor;
                                bestLoopPathPoints = loopPathResult.Points;
                            }
                        }
                    }
                }
            }

            // If a valid path was found between first and last
            if (bestLoopPathPoints != null && bestLoopSourceDoor != null && bestLoopTargetDoor != null)
            {
                Debug.Log($"Found valid loop connection! Path Length: {shortestLoopDist:F1}. Building corridor...");
                BuildCorridorFromPath(bestLoopPathPoints, corridorWidth, corridorHeight, corridorWallThickness);
                bestLoopSourceDoor.isConnected = true;
                bestLoopTargetDoor.isConnected = true;
                loopConnected = true; // Mark success
            }
            else
            {
                Debug.Log("Could not find a suitable non-overlapping path to connect the last room back to the first.");
            }
        }
        else {
            Debug.Log("Not enough rooms (need >= 2) to attempt loop connection.");
        }
        // --- End of last-to-first connection attempt ---

        Debug.Log($"Finished corridor connection process."); // Final log moved after all attempts

    }// End of ConnectRoomsWithCorridors function

    // Calculates a simple L-shaped path between two doors, ensuring symmetric straight segments at start/end.
    private PathCalculationResult CalculateLPath(Door startDoor, Door endDoor)
    {
        List<Vector3> path = new List<Vector3>();
        Vector3 startDir = GetDirectionVector(startDoor.direction);
        Vector3 endDir = GetDirectionVector(endDoor.direction);

        Vector3 p0 = startDoor.position; // Start point on wall
        Vector3 p1 = p0 + startDir * corridorInitialStraightLength; // Point defined distance straight out from start wall

        Vector3 p4 = endDoor.position; // End point on wall
        Vector3 p3 = p4 + endDir * corridorInitialStraightLength; // Point defined distance straight "out" from end wall

        bool alignedX = Mathf.Abs(p1.x - p3.x) < 0.1f;
        bool alignedZ = Mathf.Abs(p1.z - p3.z) < 0.1f;

        Vector3 p2 = Vector3.zero; // Corner point
        bool hasCorner = false;

        path.Clear();
        path.Add(p0);

        if (Vector3.Distance(p1, p3) < 0.1f) // Nearly aligned after initial push
        {
            if(Vector3.Distance(p0, p1) > 0.05f) path.Add(p1);
            if(Vector3.Distance(path[path.Count - 1], p4) > 0.05f) path.Add(p4);
            hasCorner = false;
        }
        else if (alignedX || alignedZ) // Straight path between p1 and p3
        {
            if(Vector3.Distance(p0, p1) > 0.05f) path.Add(p1);
            if(Vector3.Distance(path[path.Count - 1], p3) > 0.05f) path.Add(p3);
            if(Vector3.Distance(path[path.Count - 1], p4) > 0.05f) path.Add(p4);
            hasCorner = false;
        }
        else // L-path needed between p1 and p3
        {
            hasCorner = true;
            Vector3 cornerA = new Vector3(p1.x, 0, p3.z);
            Vector3 cornerB = new Vector3(p3.x, 0, p1.z);
            if (startDoor.direction == 1 || startDoor.direction == 3) p2 = cornerB;
            else p2 = cornerA;

            if(Vector3.Distance(p0, p1) > 0.05f) path.Add(p1);
            if (Vector3.Distance(path[path.Count - 1], p2) > 0.1f) path.Add(p2);
            else hasCorner = false;
            if (Vector3.Distance(path[path.Count - 1], p3) > 0.1f) path.Add(p3);
            if(Vector3.Distance(path[path.Count - 1], p4) > 0.05f) path.Add(p4);
            hasCorner = hasCorner && path.Any(pt => Vector3.Distance(pt, p2) < 0.01f);
        }

        // Cleanup close points
        for (int i = path.Count - 1; i > 0; i--) {
            if (Vector3.Distance(path[i], path[i - 1]) < 0.05f) path.RemoveAt(i);
        }

        bool finalHasCorner = hasCorner && path.Count >= 4; // L-path needs at least p0,p1,p2,p3/p4
        return new PathCalculationResult(path, p2, finalHasCorner);
    }


     // Checks if the generated path points overlap existing geometry.
    private bool DoesPathOverlap(List<Vector3> pathPoints, Room startRoom, Room endRoom, float clearance)
    {
        if (pathPoints.Count < 2) return false;

        // Check each segment and implied corner
        for (int i = 0; i < pathPoints.Count - 1; i++)
        {
            Vector3 start = pathPoints[i];
            Vector3 end = pathPoints[i + 1];

            // Create bounds for the segment, expanded by clearance
            Bounds segmentBounds = CreateBoundsForSegment(start, end, corridorWidth, corridorHeight);
            segmentBounds.Expand(clearance); // Apply clearance

            // Check segment vs other rooms
            foreach (Room room in rooms)
            {
                if (room == startRoom || room == endRoom) continue;
                if (segmentBounds.Intersects(room.roomBounds)) { /* Debug.Log("Segment overlaps room"); */ return true; }
            }

            // Check segment vs existing corridor segments
            foreach (Bounds existingSegBounds in corridorSegmentBounds)
            {
                if (segmentBounds.Intersects(existingSegBounds)) { /* Debug.Log("Segment overlaps segment"); */ return true; }
            }

            // Check segment vs existing corridor corners
            foreach (Bounds existingCornerBounds in corridorCornerBounds)
            {
                if (segmentBounds.Intersects(existingCornerBounds)) { /* Debug.Log("Segment overlaps corner"); */ return true; }
            }

            // Check if the end point of this segment is a potential corner
            if (i < pathPoints.Count - 2)
            {
                 Vector3 next = pathPoints[i + 2];
                 Vector3 dirIn = (end - start).normalized;
                 Vector3 dirOut = (next - end).normalized;
                 if (Mathf.Abs(Vector3.Dot(dirIn, dirOut)) < 0.01f) // It's a corner
                 {
                     Bounds cornerBounds = CreateBoundsForCorner(end, corridorWidth, corridorHeight);
                     cornerBounds.Expand(clearance); // Apply clearance

                    // Check corner vs other rooms
                    foreach (Room room in rooms) {
                        if (room == startRoom || room == endRoom) continue;
                        if (cornerBounds.Intersects(room.roomBounds)) { /* Debug.Log("Corner overlaps room"); */ return true; }
                    }
                    // Check corner vs existing segments
                    foreach (Bounds existingSegBounds in corridorSegmentBounds) {
                         if (cornerBounds.Intersects(existingSegBounds)) { /* Debug.Log("Corner overlaps segment"); */ return true; }
                    }
                    // Check corner vs existing corners
                    foreach (Bounds existingCornerBounds in corridorCornerBounds) {
                         if (cornerBounds.Intersects(existingCornerBounds)) { /* Debug.Log("Corner overlaps corner"); */ return true; }
                    }
                 }
            }
        }
        return false; // No overlaps found
    }

    // ------------------------------------------------------------------
    // VVVVVV  BuildCorridorFromPath and its Helpers VVVVVV
    // ------------------------------------------------------------------

    /// <summary>
    /// Builds the corridor geometry from a list of path points, handling multiple corners.
    /// </summary>
    private void BuildCorridorFromPath(List<Vector3> pathPoints, float width, float height, float thickness)
    {
        if (pathPoints == null || pathPoints.Count < 2)
        {
            Debug.LogError("BuildCorridorFromPath requires at least 2 path points.");
            return;
        }

        GameObject corridorParent = new GameObject($"Corridor_{corridorSegmentBounds.Count + corridorCornerBounds.Count}"); // Example naming
        corridorParent.transform.parent = this.transform; // Parent to generator
        float halfWidth = width / 2.0f;
        List<Vector3> detectedCorners = new List<Vector3>();

        // --- Pass 1: Detect all corner points ---
        for (int i = 1; i < pathPoints.Count - 1; i++)
        {
            Vector3 prevPoint = pathPoints[i - 1];
            Vector3 currentPoint = pathPoints[i];
            Vector3 nextPoint = pathPoints[i + 1];
            Vector3 dirIn = (currentPoint - prevPoint).normalized;
            Vector3 dirOut = (nextPoint - currentPoint).normalized;

            if (Mathf.Abs(Vector3.Dot(dirIn, dirOut)) < 0.01f) // 90 degree turn?
            {
                if (!detectedCorners.Any(c => Vector3.Distance(c, currentPoint) < 0.01f))
                {
                    detectedCorners.Add(currentPoint);
                }
            }
        }

        // --- Pass 2: Build Segments with Adjustments ---
        for (int i = 0; i < pathPoints.Count - 1; i++)
        {
            Vector3 startPoint = pathPoints[i];
            Vector3 endPoint = pathPoints[i + 1];
            Vector3 segmentVector = endPoint - startPoint;

            if (segmentVector.sqrMagnitude < 0.001f) continue;
            Vector3 segmentDirection = segmentVector.normalized;

            Vector3 effectiveStart = startPoint;
            Vector3 effectiveEnd = endPoint;

            // Adjust start if 'startPoint' is a detected corner
            if (detectedCorners.Any(c => Vector3.Distance(c, startPoint) < 0.01f))
            {
                effectiveStart = startPoint + segmentDirection * halfWidth;
            }

            // Adjust end if 'endPoint' is a detected corner
            if (detectedCorners.Any(c => Vector3.Distance(c, endPoint) < 0.01f))
            {
                effectiveEnd = endPoint - segmentDirection * halfWidth;
            }

            if (Vector3.Distance(effectiveStart, effectiveEnd) < 0.01f) continue;

            CreateCorridorSegment(corridorParent.transform, effectiveStart, effectiveEnd, width, height, thickness);

            Bounds logicalSegmentBounds = CreateBoundsForSegment(startPoint, endPoint, width, height);
            corridorSegmentBounds.Add(logicalSegmentBounds);
        }

        // --- Pass 3: Build Corner Geometry (Floor, Ceiling, Outer Walls) ---
        foreach (Vector3 cornerPoint in detectedCorners)
        {
            int cornerIdx = pathPoints.FindIndex(p => Vector3.Distance(p, cornerPoint) < 0.01f);

            if (cornerIdx > 0 && cornerIdx < pathPoints.Count - 1)
            {
                Vector3 p_before_corner = pathPoints[cornerIdx - 1];
                Vector3 p_after_corner = pathPoints[cornerIdx + 1];

                CreateCornerFloorCeiling(corridorParent.transform, cornerPoint, width, height, thickness);
                CreateOuterCornerWalls(corridorParent.transform, p_before_corner, cornerPoint, p_after_corner, width, height, thickness);

                Bounds logicalCornerBounds = CreateBoundsForCorner(cornerPoint, width, height);
                corridorCornerBounds.Add(logicalCornerBounds);
            }
            else {
                Debug.LogWarning($"Could not build geometry for corner {cornerPoint}: Invalid index {cornerIdx} or missing adjacent points.");
            }
        }
    }

    // Creates geometry for a single straight corridor segment.
    private Bounds CreateCorridorSegment(Transform parent, Vector3 effectiveStart, Vector3 effectiveEnd, float width, float height, float thickness)
    {
        Vector3 direction = (effectiveEnd - effectiveStart).normalized;
        float segmentLength = Vector3.Distance(effectiveStart, effectiveEnd);
        Vector3 segmentCenter = (effectiveStart + effectiveEnd) / 2;
        bool isHorizontal = Mathf.Abs(direction.x) > Mathf.Abs(direction.z);

        // --- Floor - Still fruity! 🍇 ---
        Vector3 floorPos = new Vector3(segmentCenter.x, -thickness / 2, segmentCenter.z);
        Vector3 floorScale = isHorizontal ? new Vector3(segmentLength, thickness, width) : new Vector3(width, thickness, segmentLength);
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "FloorSegment"; floor.transform.position = floorPos; floor.transform.localScale = floorScale; floor.transform.parent = parent;
        // Apply the floor material!
        Renderer floorRenderer = floor.GetComponent<Renderer>();
        if (floorMaterial != null) floorRenderer.material = floorMaterial; else floorRenderer.material.color = Color.white; // Fallback

        // --- Ceiling ---
        Vector3 ceilPos = new Vector3(segmentCenter.x, height + thickness / 2, segmentCenter.z);
        GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ceiling.name = "CeilingSegment"; ceiling.transform.position = ceilPos; ceiling.transform.localScale = floorScale; ceiling.transform.parent = parent;
        ceiling.layer = LayerMask.NameToLayer("NotWalkable");
        ceiling.GetComponent<Renderer>().material.color = Color.Lerp(Color.grey, Color.black, 0.5f); // You could use corridorWallMaterial here too if desired!

        // --- Side Walls - Use the special CORRIDOR material! 💚 ---
        Vector3 wallCenterY = new Vector3(segmentCenter.x, height / 2, segmentCenter.z);
        GameObject wall1 = GameObject.CreatePrimitive(PrimitiveType.Cube); wall1.name = "WallSegment1"; wall1.layer = LayerMask.NameToLayer("NotWalkable"); wall1.transform.parent = parent;
        GameObject wall2 = GameObject.CreatePrimitive(PrimitiveType.Cube); wall2.name = "WallSegment2"; wall2.layer = LayerMask.NameToLayer("NotWalkable"); wall2.transform.parent = parent;
        // Apply CORRIDOR wall material!
        Renderer wall1Renderer = wall1.GetComponent<Renderer>();
        if (corridorWallMaterial != null) wall1Renderer.material = corridorWallMaterial; else wall1Renderer.material.color = Color.grey; // Fallback
        Renderer wall2Renderer = wall2.GetComponent<Renderer>();
        if (corridorWallMaterial != null) wall2Renderer.material = corridorWallMaterial; else wall2Renderer.material.color = Color.grey; // Fallback

        if (isHorizontal) {
            wall1.transform.position = wallCenterY + new Vector3(0, 0, width / 2); wall1.transform.localScale = new Vector3(segmentLength, height, thickness);
            wall2.transform.position = wallCenterY + new Vector3(0, 0, -width / 2); wall2.transform.localScale = new Vector3(segmentLength, height, thickness);
        } else {
            wall1.transform.position = wallCenterY + new Vector3(width / 2, 0, 0); wall1.transform.localScale = new Vector3(thickness, height, segmentLength);
            wall2.transform.position = wallCenterY + new Vector3(-width / 2, 0, 0); wall2.transform.localScale = new Vector3(thickness, height, segmentLength);
        }
         // Return approximate bounds
        Vector3 boundsCenter = new Vector3(segmentCenter.x, height / 2, segmentCenter.z);
        Vector3 boundsSize = isHorizontal ? new Vector3(segmentLength, height, width) : new Vector3(width, height, segmentLength);
        return new Bounds(boundsCenter, boundsSize);
    }

     // Calculates the logical bounds for a segment using original start/end points.
    private Bounds CreateBoundsForSegment(Vector3 startPos, Vector3 endPos, float width, float height)
    {
        if (Vector3.Distance(startPos, endPos) < 0.01f) return new Bounds(Vector3.zero, Vector3.zero);
        Vector3 direction = (endPos - startPos).normalized;
        float segmentLength = Vector3.Distance(startPos, endPos);
        Vector3 segmentCenter = (startPos + endPos) / 2;
        bool isHorizontal = Mathf.Abs(direction.x) > Mathf.Abs(direction.z);
        Vector3 boundsCenter = new Vector3(segmentCenter.x, height / 2, segmentCenter.z);
        Vector3 boundsSize = isHorizontal ? new Vector3(segmentLength, height, width) : new Vector3(width, height, segmentLength);
        return new Bounds(boundsCenter, boundsSize);
    }

     // Creates the square floor and ceiling patches at a corner point.
    private void CreateCornerFloorCeiling(Transform parent, Vector3 cornerPoint, float width, float height, float thickness)
    {
        // --- Corner Floor - Always yummy! 🍑 ---
        GameObject cornerFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cornerFloor.name = "CornerFloor";
        cornerFloor.transform.position = new Vector3(cornerPoint.x, -thickness / 2, cornerPoint.z);
        cornerFloor.transform.localScale = new Vector3(width, thickness, width);
        cornerFloor.transform.parent = parent;
        // Apply the floor material!
        Renderer cornerFloorRenderer = cornerFloor.GetComponent<Renderer>();
        if (floorMaterial != null) cornerFloorRenderer.material = floorMaterial; else cornerFloorRenderer.material.color = Color.white; // Fallback

        // --- Corner Ceiling ---
        GameObject cornerCeiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cornerCeiling.name = "CornerCeiling";
        cornerCeiling.transform.position = new Vector3(cornerPoint.x, height + thickness / 2, cornerPoint.z);
        cornerCeiling.transform.localScale = new Vector3(width, thickness, width);
        cornerCeiling.transform.parent = parent;
        cornerCeiling.layer = LayerMask.NameToLayer("NotWalkable");
        cornerCeiling.GetComponent<Renderer>().material.color = Color.Lerp(Color.grey, Color.black, 0.5f); // Apply corridorWallMaterial here?
    }

    // Creates the two outer walls at a corner point.
    private void CreateOuterCornerWalls(Transform parent, Vector3 p_before_corner, Vector3 cornerPoint, Vector3 p_after_corner, float width, float height, float thickness)
    {
        Vector3 inDir = (cornerPoint - p_before_corner).normalized;
        Vector3 outDir = (p_after_corner - cornerPoint).normalized;
        float halfWidth = width / 2.0f;

         if (Mathf.Abs(Vector3.Dot(inDir, outDir)) > 0.01f) {
               Debug.LogWarning($"Attempting CreateOuterCornerWalls with non-perpendicular directions at {cornerPoint}. Skipping wall generation for this corner.");
               return;
        }

        Vector3 cornerCenterY = new Vector3(cornerPoint.x, height / 2, cornerPoint.z);
        Vector3 perpToIn = Vector3.Cross(Vector3.up, inDir).normalized;
        Vector3 perpToOut = Vector3.Cross(Vector3.up, outDir).normalized;
        float turnDotY = Vector3.Cross(inDir, outDir).y;
        Vector3 wall1_OffsetDir = (turnDotY > 0) ? -perpToIn : perpToIn;
        Vector3 wall2_OffsetDir = (turnDotY > 0) ? -perpToOut : perpToOut;

        // --- Wall 1 (Parallel to Outgoing) - Use CORRIDOR Material! 💚 ---
        GameObject outerWall1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        outerWall1.name = "OuterCornerWall_1"; outerWall1.transform.parent = parent; outerWall1.layer = LayerMask.NameToLayer("NotWalkable");
        // Apply CORRIDOR wall material!
        Renderer outerWall1Renderer = outerWall1.GetComponent<Renderer>();
        if (corridorWallMaterial != null) outerWall1Renderer.material = corridorWallMaterial; else outerWall1Renderer.material.color = Color.grey; // Fallback
        // Use the adjusted position
        outerWall1.transform.position = cornerCenterY + wall1_OffsetDir * (halfWidth + thickness / 2.0f);
        bool outIsHorizontal = Mathf.Abs(outDir.x) > Mathf.Abs(outDir.z);
        outerWall1.transform.localScale = outIsHorizontal ? new Vector3(width, height, thickness) : new Vector3(thickness, height, width);
        outerWall1.transform.Rotate(0f, 90f, 0f, Space.Self);

        // --- Wall 2 (Parallel to Incoming) - Use CORRIDOR Material! 💚 ---
        GameObject outerWall2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        outerWall2.name = "OuterCornerWall_2"; outerWall2.transform.parent = parent; outerWall2.layer = LayerMask.NameToLayer("NotWalkable");
        // Apply CORRIDOR wall material!
        Renderer outerWall2Renderer = outerWall2.GetComponent<Renderer>();
        if (corridorWallMaterial != null) outerWall2Renderer.material = corridorWallMaterial; else outerWall2Renderer.material.color = Color.grey; // Fallback
        // Use the adjusted position
        outerWall2.transform.position = cornerCenterY + wall2_OffsetDir * (halfWidth + thickness / 2.0f);
        bool inIsHorizontal = Mathf.Abs(inDir.x) > Mathf.Abs(inDir.z);
        outerWall2.transform.localScale = inIsHorizontal ? new Vector3(width, height, thickness) : new Vector3(thickness, height, width);
        outerWall2.transform.Rotate(0f, 90f, 0f, Space.Self);
    }

    // Helper to create Bounds for a corner volume used in overlap checks.
     private Bounds CreateBoundsForCorner(Vector3 cornerPoint, float width, float height)
     {
         Vector3 center = new Vector3(cornerPoint.x, height / 2, cornerPoint.z);
         Vector3 size = new Vector3(width, height, width);
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

     // --- Optional Debug Helper ---
     void DrawBounds(Bounds b, Color c, float duration = 0) {
          Vector3 p1 = b.min; Vector3 p2 = new Vector3(b.max.x, b.min.y, b.min.z);
          Vector3 p3 = new Vector3(b.max.x, b.min.y, b.max.z); Vector3 p4 = new Vector3(b.min.x, b.min.y, b.max.z);
          Vector3 p5 = new Vector3(b.min.x, b.max.y, b.min.z); Vector3 p6 = new Vector3(b.max.x, b.max.y, b.min.z);
          Vector3 p7 = b.max; Vector3 p8 = new Vector3(b.min.x, b.max.y, b.max.z);
          Debug.DrawLine(p1, p2, c, duration); Debug.DrawLine(p2, p3, c, duration); Debug.DrawLine(p3, p4, c, duration); Debug.DrawLine(p4, p1, c, duration);
          Debug.DrawLine(p5, p6, c, duration); Debug.DrawLine(p6, p7, c, duration); Debug.DrawLine(p7, p8, c, duration); Debug.DrawLine(p8, p5, c, duration);
          Debug.DrawLine(p1, p5, c, duration); Debug.DrawLine(p2, p6, c, duration); Debug.DrawLine(p3, p7, c, duration); Debug.DrawLine(p4, p8, c, duration);
     }

     System.Collections.IEnumerator GenerateAndBakeSequence()
     {
         Debug.Log("Starting Dungeon Generation Coroutine...");
         GenerateRooms();
         ConnectRoomsWithCorridors();
         CleanupUnusedDoors();
         // IMPORTANT: Wait until the end of the frame AFTER potentially destroying/creating objects
         yield return new WaitForEndOfFrame();
         // Maybe even wait one more frame for good measure? (Optional)
         // yield return null;

         BakeNavMesh(); // Now bake the NavMesh
         Debug.Log("Dungeon Generation Coroutine Complete.");
         Debug.Log("RoomGenerator: About to invoke OnRoomsGenerated event. Rooms count: " + rooms.Count);
         OnRoomsGenerated?.Invoke(rooms);
         Debug.Log("RoomGenerator: OnRoomsGenerated event invoked.");

         Debug.Log("RoomGenerator: GenerateAndBakeSequence() complete.");
     }

} // End of RoomGenerator6 class