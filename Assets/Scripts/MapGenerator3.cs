using System.Collections;
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

  int xmin;
  int ymin;
  int xmax;
  int ymax;

  void OnDrawGizmos()
  {
    // This makes the viewport live, the start method is responsible for the mesh used by the game
    // GenerateMap();
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
    Debug.Log("Tile Type " + tileType + " conatins " + regions.Count);

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
    int[,] mapFlags = new int[(borderWidth * 2) + width, (borderWidth * 2) + height];

    for(int x = 0; x < mapFlags.GetLength(0); x++)
    {
      for(int y = 0; y < mapFlags.GetLength(1); y++)
      {
        if(mapFlags[x,y] == 0 && map[x,y] == tileType)
        {
          List<Coord> region = GetRegionTiles(x,y);
          // Debug.Log("region tracked from " + x + "," + y + " is size " + region.Count + " and type " + tileType);
          regions.Add(region);

          foreach(Coord coord in region)
          {
            mapFlags[coord.tileX, coord.tileY] = 1;
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

  bool isInBorder(int x, int y)
  {
    return x < xmin || y < ymin || x > xmax || y > ymax;
  }

  bool isInMap(int x, int y)
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
}
