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

  [Range(0,10)]
  public int borderWidth;

  int[,] map;

  int xmin;
  int ymin;
  int xmax;
  int ymax;

  void Start()
  {
    GenerateMap();
  }

  void GenerateMap()
  {
    xmin = borderWidth + 1;
    xmax = borderWidth + width - 1;
    ymin = borderWidth + 1;
    ymax = borderWidth + height - 1;

    map = new int[(borderWidth * 2) + width, (borderWidth * 2) + height];

    RandomFillMap();
    SmoothMap();

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

    bool isInBorder(int x, int y)
    {
      return x < xmin || y < ymin || x > xmax || y > ymax;
    }

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
    GenerateMap();
  }
}
