using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Board : MonoBehaviour
{
    // Program Constants
    private readonly int TILE_COUNT_Y = 8;
    private readonly int TILE_COUNT_X = 8;
    
    // array to hold tiles for the board
    private GameObject[,] _chessboard;

    // Initialisation procedure - Called even if script component is disabled
    private void Awake()
    {
        // Procedure call to generate tiles for the chessboard
        CreateAllTiles(1, TILE_COUNT_X, TILE_COUNT_Y);
        
    }
    
    // Procedure to iterate over tile array and create all tiles
    private void CreateAllTiles(float size, int countX, int countY)
    {
        _chessboard = new GameObject[countX, countY];
        for (int x = 0; x < countX; x++)
        {
            for (int y = 0; y < countY; y++)
            {
                _chessboard[x,y] = CreateSingleTile(size, x, y);
            }
        }
    }

    // Function to create and return a single tile
    private GameObject CreateSingleTile(float size, int x, int y)
    {
        // create new gameObject for tile
        GameObject tile = new GameObject($"X:{x}, Y:{y}");
        // make tile a part of the chessboard object 
        tile.transform.parent = transform;
        
        // stuff that makes things render
        
        // init new mesh
        Mesh mesh = new Mesh();
        // add mesh filter component and assign to newly made mesh
        tile.AddComponent<MeshFilter>().mesh = mesh;
        // add mesh renderer component (guess what this does)
        tile.AddComponent<MeshRenderer>();
        
        // geometry
        
        // dynamically generate 3d vectors for the corners of each tile
        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(x * size, 0, y * size);
        vertices[1] = new Vector3(x * size, 0, y * size);
        vertices[2] = new Vector3((x + 1) * size, 0, y * size);
        vertices[3] = new Vector3((x + 1) * size, 0, (y + 1) * size);

        // triangle array to create the triangles that form the mesh
        // create 2 triangles by going from 0 to 1 then 2 and 1 to 3 then 2
        int[] triangles = {0, 1, 2, 1, 3, 2};
        
        // assign verticies and triangles arrays to the mesh
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        
        
        
        
        
        



            return tile;
    }
}
