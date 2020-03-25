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

  [Range(0,10)]
  public int squareSize;

  int[,] map;

  void Start()
  {
    GenerateMap();
  }

  void GenerateMap()
  {
    map = new int[width, height];

    RandomFillMap();
    SmoothMap();
    AddMapBorder();

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

    for(int x = 0; x < width; x++)
    {
      for(int y = 0; y < width; y++)
      {
        map[x,y] = (pseudoRandom.Next(0,100) < randomFillPercent) ? 1 : 0;
      }
    }
  }

  void AddMapBorder()
  {
    for(int x = 0; x < width; x++)
    {
      map[x,0] = 1;
      map[x,height - 1] = 1;
    }
    for(int y = 0; y < height; y++)
    {
      map[0,y] = 1;
      map[width - 1,y] = 1;
    }
  }

  void SmoothMap()
  {
    for (int i = 0; i < smoothingAmount; i ++)
    {
      for(int x = 0; x < width; x++)
      {
        for(int y = 0; y < width; y++)
        {
          int surroundingWallCount = GetSurroundingWallCount(x,y);

          map[x,y] = surroundingWallCount > smoothingTolerance ? 1 : 0;
        }
      }
    }
  }

  int GetSurroundingWallCount(int gridX, int gridY) {
      int wallCount = 0;
      for (int neighbourX = gridX - 1; neighbourX <= gridX + 1; neighbourX ++)
      {
          for (int neighbourY = gridY - 1; neighbourY <= gridY + 1; neighbourY ++)
          {
              if (neighbourX >= 0 && neighbourX < width && neighbourY >= 0 && neighbourY < height)
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

  void OnDrawGizmos()
  {
    // This makes the viewport live, the start method is responsible for the mesh used by the game
    // GenerateMap();
  }
}
