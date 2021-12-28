using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Board : MonoBehaviour
{
    [Header("Art stuff yk")] 
    // serializefield makes it so that a private variable will show up in the unity editor
    [SerializeField] private Material material;

    private readonly int TILE_COUNT_Y = 8;
    private readonly int TILE_COUNT_X = 8;
    private GameObject[,] _chessboard;
    private Camera _currentCamera; 
    private Vector2Int _tileAtMouse;
    
    
    
    // Initialisation procedure - Called even if script component is disabled
    private void Awake()
    {
        // Procedure call to generate tiles for the chessboard
        CreateAllTiles(1, TILE_COUNT_X, TILE_COUNT_Y);
        
    }

    private void Update()
    {
        //make camera if it doesn't exist
        if (!_currentCamera)
        {
            _currentCamera = Camera.main;
            return;
        }
        
        // raycasting
        // basically make an invisible ray from a point in a specified direction to detect
        // if there are any colliders in its path        
        RaycastHit info;
        // make ray
        Ray ray = _currentCamera.ScreenPointToRay(Input.mousePosition);
        // cast ray
        if (Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover")))
        {
            // get index of tile hit by ray
            Vector2Int hitPos = TileIndexLookup(info.transform.gameObject);
            
            // if going from not hovering above tile to hovering above tile
            if (_tileAtMouse == -Vector2Int.one)
            {
                _tileAtMouse = hitPos;
                _chessboard[hitPos.x, hitPos.y].layer = LayerMask.NameToLayer("Hover");
            }
            
            // if going from hovering over tile to hovering over different tile
            if (_tileAtMouse != -Vector2Int.one)
            {
                _chessboard[_tileAtMouse.x, _tileAtMouse.y].layer = LayerMask.NameToLayer("Tile");
                _tileAtMouse = hitPos;
                _chessboard[hitPos.x, hitPos.y].layer = LayerMask.NameToLayer("Hover");
            }
        }
        else
        {
            // if the cursor is off the board (so the ray doesnt collide with any of the tiles),
            // reset the last tile that was hovered over and set the value of _tileAtMouse to
            // a negative index (not on the board)
            if (_tileAtMouse != -Vector2Int.one)
            {
                _chessboard[_tileAtMouse.x, _tileAtMouse.y].layer = LayerMask.NameToLayer("Tile");
                _tileAtMouse = -Vector2Int.one;
            }
        }
    }
    
    
    
    // Generate the board

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
        // add mesh renderer component and assign material (guess what this does)
        tile.AddComponent<MeshRenderer>().material = material;
        
        // geometry
        
        // dynamically generate 3d vectors for the corners of each tile
        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(x * size, 0, y * size);
        vertices[1] = new Vector3(x * size, 0, (y + 1) * size);
        vertices[2] = new Vector3((x + 1) * size, 0, y * size);
        vertices[3] = new Vector3((x + 1) * size, 0, (y + 1) * size);

        // triangle array to create the triangles that form the mesh
        // create 2 triangles by going from 0 to 1 then 2 and 1 to 3 then 2
        int[] triangles = {0, 1, 2, 1, 3, 2};
        
        // assign verticies and triangles arrays to the mesh
        mesh.vertices = vertices;
        mesh.triangles = triangles;

        // calculate normals because light is funky
        mesh.RecalculateNormals();

        tile.layer = LayerMask.NameToLayer("Tile");
        tile.AddComponent<BoxCollider>();

        return tile;
    }
    
    
    
    // useful stuff
    private Vector2Int TileIndexLookup(GameObject hitInfo)
    {
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                if (_chessboard[x, y] == hitInfo)
                    return new Vector2Int(x, y);
            }
        }

        return -Vector2Int.one; // Invalid case. If the program gets here then congrats! You broke it! 
    }
}
