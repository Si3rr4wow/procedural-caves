using System;
﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator3 : MonoBehaviour
{
  public int width;
  public int height;

  public string seed;
  public bool useRandomSeed;

  [Range(0,100)]
  public int randomFillPercent;

  [Range(0,10)]
  public int smoothingAmount;

  [Range(0,10)]
  public int smoothingTolerance;

  [Range(0,50)]
  public int floorDenoisingTolerance;

  [Range(0,50)]
  public int wallDenoisingTolerance;

  [Range(0,10)]
  public int squareSize;

  [Range(0,10)]
  public int borderWidth;

  int[,] map;

  static int xmin;
  static int ymin;
  static int xmax;
  static int ymax;

  void OnDrawGizmos()
  {
    // This makes the viewport live, the start method is responsible for the mesh used by the game
    GenerateMap();
  }

  void Start()
  {
    GenerateMap();
  }

  void GenerateMap()
  {
    xmin = borderWidth;
    xmax = borderWidth + width - 1;
    ymin = borderWidth;
    ymax = borderWidth + height - 1;

    map = new int[(borderWidth * 2) + width, (borderWidth * 2) + height];

    RandomFillMap();
    SmoothMap();
    DenoiseMap(1, wallDenoisingTolerance);
    DenoiseMap(0, floorDenoisingTolerance);

    List<Room> survivingRooms = SurvivingRooms();

    survivingRooms[0].isMainRoom = true;
    survivingRooms[0].isAccessibleFromMainRoom = true;

    ConnectClosestRooms(survivingRooms);

    MeshGenerator meshGenerator = GetComponent<MeshGenerator>();
    meshGenerator.GenerateMesh(map, squareSize);
  }

  void RandomFillMap()
  {
    if(useRandomSeed)
    {
      seed = Time.time.ToString();
    }

    System.Random pseudoRandom = new System.Random(seed.GetHashCode());

    for(int x = 0; x < map.GetLength(0); x++)
    {
      for(int y = 0; y < map.GetLength(1); y++)
      {
        if(isInBorder(x, y))
        {
          map[x,y] = 1;
        }
        else
        {
          map[x,y] = (pseudoRandom.Next(0,100) < randomFillPercent) ? 1 : 0;
        }
      }
    }
  }

  void SmoothMap()
  {
    for (int i = 0; i < smoothingAmount; i ++)
    {
      for(int x = xmin; x <= xmax; x++)
      {
        for(int y = ymin; y <= ymax; y++)
        {
          int surroundingWallCount = GetSurroundingWallCount(x,y);

          map[x,y] = surroundingWallCount > smoothingTolerance ? 1 : 0;
        }
      }
    }
  }

  void DenoiseMap(int tileType, int denoisingThreshold)
  {
    List<List<Coord>> regions = GetRegions(tileType);
    // Debug.Log("Tile Type " + tileType + " conatins " + regions.Count);

    foreach(List<Coord> region in regions)
    {
      if(region.Count <= denoisingThreshold)
      {
        foreach(Coord coord in region)
        {
          map[coord.tileX, coord.tileY] = tileType == 1 ? 0 : 1;
        }
      }
    }
  }

  List<Room> SurvivingRooms()
  {
    List<List<Coord>> regions = GetRegions(0);
    List<Room> rooms = new List<Room>();
    foreach(List<Coord> region in regions)
    {
      rooms.Add(new Room(region, map));
    }
    return rooms;
  }

  void ConnectClosestRooms(List<Room> allRooms, bool forceAccessibilityFromMainRoom = false)
  {
    List<Room> roomListA = new List<Room> ();
    List<Room> roomListB = new List<Room> ();

    if (forceAccessibilityFromMainRoom)
    {
      foreach (Room room in allRooms)
      {
        if (room.isAccessibleFromMainRoom)
        {
          roomListB.Add (room);
        } else {
          roomListA.Add (room);
        }
      }
    } else {
      roomListA = allRooms;
      roomListB = allRooms;
    }

    int bestDistance = 0;
    Coord bestTileA = new Coord ();
    Coord bestTileB = new Coord ();
    Room bestRoomA = new Room ();
    Room bestRoomB = new Room ();
    bool possibleConnectionFound = false;

    foreach (Room roomA in roomListA)
    {
      if (!forceAccessibilityFromMainRoom)
      {
        possibleConnectionFound = false;
        if (roomA.connectedRooms.Count > 0)
        {
          continue;
        }
      }

      foreach (Room roomB in roomListB)
      {
        if (roomA == roomB || roomA.IsConnected(roomB))
        {
          continue;
        }

        for (int tileIndexA = 0; tileIndexA < roomA.edgeTiles.Count; tileIndexA ++)
        {
          for (int tileIndexB = 0; tileIndexB < roomB.edgeTiles.Count; tileIndexB ++)
          {
            Coord tileA = roomA.edgeTiles[tileIndexA];
            Coord tileB = roomB.edgeTiles[tileIndexB];
            int distanceBetweenRooms = (int)(Mathf.Pow (tileA.tileX-tileB.tileX,2) + Mathf.Pow (tileA.tileY-tileB.tileY,2));

            if (distanceBetweenRooms < bestDistance || !possibleConnectionFound)
            {
              bestDistance = distanceBetweenRooms;
              possibleConnectionFound = true;
              bestTileA = tileA;
              bestTileB = tileB;
              bestRoomA = roomA;
              bestRoomB = roomB;
            }
          }
        }
      }
      if (possibleConnectionFound && !forceAccessibilityFromMainRoom)
      {
        CreatePassage(bestRoomA, bestRoomB, bestTileA, bestTileB);
      }
    }

    if (possibleConnectionFound && forceAccessibilityFromMainRoom) {
      CreatePassage(bestRoomA, bestRoomB, bestTileA, bestTileB);
      ConnectClosestRooms(allRooms, true);
    }

    if (!forceAccessibilityFromMainRoom) {
      ConnectClosestRooms(allRooms, true);
    }
  }

  void CreatePassage(Room roomA, Room roomB, Coord tileA, Coord tileB)
  {
    Room.ConnectRooms(roomA, roomB);
    Debug.DrawLine(CoordToWorldPoint(tileA), CoordToWorldPoint(tileB), Color.green, 100);
  }

  Vector3 CoordToWorldPoint(Coord tile)
  {
    return new Vector3(-width/2f+.5f+tile.tileX,2,-height/2f+0.5f+tile.tileY);
  }

  int GetSurroundingWallCount(int gridX, int gridY)
  {
    int wallCount = 0;
    for (int neighbourX = gridX - 1; neighbourX <= gridX + 1; neighbourX ++)
    {
      for (int neighbourY = gridY - 1; neighbourY <= gridY + 1; neighbourY ++)
      {
        if (isInMap(neighbourX, neighbourY))
        {
          if (neighbourX != gridX || neighbourY != gridY)
          {
              wallCount += map[neighbourX,neighbourY];
          }
        }
        else
        {
          wallCount ++;
        }
      }
    }

      return wallCount;
  }

  List<List<Coord>> GetRegions(int tileType)
  {
    List<List<Coord>> regions = new List<List<Coord>>();
    int[,] mapFlags = new int[width, height];

    for(int x = xmin; x <= xmax; x++)
    {
      for(int y = ymin; y <= ymax; y++)
      {
        if(mapFlags[x - xmin,y - ymin] == 0 && map[x,y] == tileType)
        {
          // Debug.Log("Getting Region At " + x +","+y);
          List<Coord> region = GetRegionTiles(x,y);
          // Debug.Log("region tracked from " + x + "," + y + " is size " + region.Count + " and type " + tileType);
          regions.Add(region);

          foreach(Coord coord in region)
          {
            mapFlags[coord.tileX - xmin, coord.tileY - ymin] = 1;
          }
        }
      }
    }

    return regions;
  }

  List<Coord> GetRegionTiles(int startX, int startY) //uses flood fill algo
  {
    List<Coord> tiles = new List<Coord>();
    int[,] mapFlags = new int[(borderWidth * 2) + width, (borderWidth * 2) + height];
    int tileType = map[startX, startY];

    Queue<Coord> queue = new Queue<Coord>();
    queue.Enqueue(new Coord(startX, startY));
    mapFlags[startX, startY] = 1;

    while(queue.Count > 0)
    {
      Coord tile = queue.Dequeue();
      tiles.Add(tile);

      for (int x = tile.tileX - 1; x <= tile.tileX + 1; x ++)
      {
        for (int y = tile.tileY - 1; y <= tile.tileY + 1; y ++)
        {
          // Debug.Log("is " + x + "," + y + " in map? " + isInMap(x,y));
          if (isInMap(x,y) && (y == tile.tileY || x == tile.tileX))
          {
            // Debug.Log("start: " + startX + "," + startY + " progressed to: " + tile.tileX + tile.tileY + " testing neighbor " + x + "," + y);
            if(mapFlags[x,y] == 0 && map[x,y] == tileType)
            {
              // Debug.Log("adding " + x + "," + y + " to queue from " + startX + "," + startY);
              mapFlags[x,y] = 1;
              queue.Enqueue(new Coord(x,y));
            }
          }
        }
      }
    }

    return tiles;
  }

  static bool isInBorder(int x, int y)
  {
    return x < xmin || y < ymin || x > xmax || y > ymax;
  }

  static bool isInMap(int x, int y)
  {
    return !isInBorder(x, y);
  }

  struct Coord
  {
    public int tileX;
    public int tileY;

    public Coord(int x, int y)
    {
      tileX = x;
      tileY = y;
    }
  }

  class Room : IComparable<Room>
  {
    public List<Coord> tiles;
    public List<Coord> edgeTiles;
    public List<Room> connectedRooms;
    public int roomSize;
    public bool isAccessibleFromMainRoom;
    public bool isMainRoom;

    public Room(){ }

    public Room(List<Coord> roomTiles, int[,] map)
    {
      tiles = roomTiles;
      roomSize = tiles.Count;
      connectedRooms = new List<Room>();
      edgeTiles = new List<Coord>();

      foreach(Coord tile in tiles)
      {
        for (int x = tile.tileX - 1; x <= tile.tileX + 1; x ++)
        {
          for (int y = tile.tileY - 1; y <= tile.tileY + 1; y ++)
          {
            if ((y == tile.tileY || x == tile.tileX) && isInMap(x - xmin,y - ymin))
            {
              // Debug.Log(x + "," + y);
              if(map[x - xmin,y - ymin] == 1)
              {
                edgeTiles.Add(tile);
              }
            }
          }
        }
      }
    }

    public void SetAccessibleFromMainRoom()
    {
      if(!isAccessibleFromMainRoom)
      {
        isAccessibleFromMainRoom = true;
        foreach(Room connectedRoom in connectedRooms)
        {
          connectedRoom.SetAccessibleFromMainRoom();
        }
      }
    }

    public static void ConnectRooms(Room roomA, Room roomB)
    {
      if(roomA.isAccessibleFromMainRoom)
      {
        roomB.SetAccessibleFromMainRoom();
      } else if (roomB.isAccessibleFromMainRoom)
      {
        roomA.SetAccessibleFromMainRoom();
      }
      roomA.connectedRooms.Add(roomB);
      roomB.connectedRooms.Add(roomA);
    }

    public bool IsConnected(Room otherRoom)
    {
      return connectedRooms.Contains(otherRoom);
    }

    public int CompareTo(Room otherRoom)
    {
      return otherRoom.roomSize.CompareTo(roomSize);
    }
  }
}
