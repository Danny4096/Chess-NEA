using System.Collections.Generic;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.AI;

public class Board : MonoBehaviour
{
    [Header("Art stuff yk")] 
    // [SerializeField] makes it so that a private variable will show up in the unity editor
    [SerializeField] private Material material;
    // values for positioning created tiles correctly with the board model
    [SerializeField] private float tileSize = 1.0f;
    [SerializeField] private float offsetY = 0.01f;
    [SerializeField] private float deathOffsetY = 0.12f;
    [SerializeField] private float dragOffsetY = 1.0f;
    // this is the vector that represents the pivot of the board which is not necessarily the actual center of
    // the board such as in this case where the pivot is at -3.5, 0, -3.5 if the board is centered at 0,0,0
    [SerializeField] private Vector3 boardCenter = new Vector3(-3.5f, 0, -3.5f);
    [SerializeField] private GameObject victoryScreen;
    

    [Header("prefabs and materials")] 
    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private Material[] pieceMaterials;


    // Logic
    // this one is for the positions of pieces on the board
    private Piece[,] _pieces;
    private Piece _currentlyHeld;
    private List<Piece> _deadWhite = new List<Piece>();
    private List<Piece> _deadBlack = new List<Piece>();
    private List<Vector2Int> _availableMoves = new List<Vector2Int>();
    private const int TileCountY = 8;
    private const int TileCountX = 8;
    // this one is for the tiles on the board
    private GameObject[,] _tiles;
    private Camera _currentCamera;
    private Vector2Int _tileAtMouse;
    private Vector3 _bounds;
    private bool isWhiteTurn;
    [SerializeField] private float deadPieceScalar = 0.5f;
    [SerializeField] private float deathSpacing = 0.5f;

    
    // Initialisation procedure - Called even if script component is disabled
    private void Awake()
    {
        // set white move at init
        isWhiteTurn = true;
        // Procedure call to generate tiles for the chessboard
        CreateAllTiles(tileSize, TileCountX, TileCountY);
        // Function call to load piece models
        prefabs = LoadPrefabs();
        pieceMaterials = LoadPieceMaterials();
        // Procedure call to spawn pieces onto the board
        SpawnPieces();
        // Procedure call to set the initial positions for every piece on the board
        SetAllPiecePositions();
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
        if (Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover", "Highlight")))
        {
            // get index of tile hit by ray
            // info.transform.gameObject takes the hitinfo and makes it represent the gameobject that the ray hit
            Vector2Int hitPos = TileIndexLookup(info.transform.gameObject);

            // if going from not hovering above tile to hovering above tile
            if (_tileAtMouse == -Vector2Int.one)
            {
                _tileAtMouse = hitPos;
                _tiles[hitPos.x, hitPos.y].layer = LayerMask.NameToLayer("Hover");
            }

            // if going from hovering over tile to hovering over different tile
            if (_tileAtMouse != hitPos)
            {
                // after switching the tile being hovered over, pass a reference to the list of available moves and 
                // a vector2 representing the tile that was being hovered over 
                _tiles[_tileAtMouse.x, _tileAtMouse.y].layer = 
                    ContainsValidMove(ref _availableMoves, _tileAtMouse) ?
                        LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile") ;
                _tileAtMouse = hitPos;
                _tiles[hitPos.x, hitPos.y].layer = LayerMask.NameToLayer("Hover");
            }

            // If mouse 1 clicked
            if (Input.GetMouseButtonDown(0))
            {
                // check if clicking a tile that contains a chess piece
                if (_pieces[hitPos.x, hitPos.y] != null)
                {
                    // check if it's the user's turn
                    if ((_pieces[hitPos.x, hitPos.y].side == 0 && isWhiteTurn) 
                        || (_pieces[hitPos.x, hitPos.y].side == 1 && !isWhiteTurn))
                    {
                        // get ref to piece
                        _currentlyHeld = _pieces[hitPos.x, hitPos.y];
                        // get list of places the piece can be moved to
                        _availableMoves = _currentlyHeld.GetMoves(ref _pieces, TileCountX, TileCountY);
                        // highlight tiles that can be moved to
                        HighlightTiles();
                    }
                }
            }

            // If mouse 1 released and piece held
            if (Input.GetMouseButtonUp(0) && _currentlyHeld != null)
            {
                // save previous piece position
                Vector2Int prevPos = new Vector2Int(_currentlyHeld.currentX, _currentlyHeld.currentY);
                
                // check if the move was valid
                bool moveIsValid = MovePiece(hitPos.x, hitPos.y, _currentlyHeld);

                // if the move wasnt valid, return the piece to its previous position and blank the piece reference
                if (!moveIsValid)
                    _currentlyHeld.SetPosition(new Vector3(prevPos.x, offsetY, prevPos.y));
                // clear the piece reference
                _currentlyHeld = null;
                // remove tile highlight
                UnhighlightTiles();
            }
        }
        else
        {
            // if the cursor is off the board (so the ray doesnt collide with any of the tiles),
            // reset the last tile that was hovered over and set the value of _tileAtMouse to
            // a negative index (not on the board)
            if (_tileAtMouse != -Vector2Int.one)
            {
                // reset the tile layer to "Tile"
                _tiles[_tileAtMouse.x, _tileAtMouse.y].layer = 
                    ContainsValidMove(ref _availableMoves, _tileAtMouse) ? 
                        LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile");
                // set the current hovering tile to an invalid position
                _tileAtMouse = -Vector2Int.one;
            }

            if (_currentlyHeld && Input.GetMouseButtonUp((0)))
            {
                _currentlyHeld.SetPosition(new Vector3(_currentlyHeld.currentX, offsetY, _currentlyHeld.currentY));
                _currentlyHeld = null;
                UnhighlightTiles();
            }
        }
        
        // When holding a piece
        if (_currentlyHeld)
        {
            // create new plane just above the board
            Plane horizontal = new Plane(Vector3.up, Vector3.up * offsetY);
            float dist = 0.0f;
            // cast ray onto plane
            if (horizontal.Raycast(ray, out dist))
            {
                _currentlyHeld.SetPosition(ray.GetPoint(dist) + Vector3.up * dragOffsetY);
            }
        }
    }

    // Generate the board

    // Procedure to iterate over tile array and create all tiles
    private void CreateAllTiles(float size, int countX, int countY)
    {
        // if the board is above y=0
        offsetY += transform.position.y;
        // define bounds of the board
        // x: (number of tiles/2) * size == (8/2)*1
        // y: 0
        // z: ((number of tiles/2) * size) = ((8/2)*1)
        // adding boardCentre to account for a shift in the centre of the board
        // in this case the centre is -3.5,0,-3.5 because for the model im using,
        // the pivot is in the centre of the first square
        _bounds = new Vector3(((countX / 2.0f) * size), 0, ((countY / 2.0f) * size)) + boardCenter;
        
        // defining 2d array representing tiles on the board
        _tiles = new GameObject[countX, countY];
        // populate the array with tiles
        for (int x = 0; x < countX; x++)
        {
            for (int y = 0; y < countY; y++)
            {
                _tiles[x,y] = CreateSingleTile(size, x, y);
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
        // use the y offset and subtract bounds so that the generated tiles line up correctly with the board model
        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(x * size, offsetY, y * size) - _bounds;
        vertices[1] = new Vector3(x * size, offsetY, (y + 1) * size) - _bounds;
        vertices[2] = new Vector3((x + 1) * size, offsetY, y * size) - _bounds;
        vertices[3] = new Vector3((x + 1) * size, offsetY, (y + 1) * size) - _bounds;

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
    
    
    
    // spawning pieces
    private void SpawnPieces()
    {
        // initialise the Piece array that will hold the pieces and essentially represent the board
        _pieces = new Piece[TileCountX, TileCountY];
        int white = 0;
        int black = 1;
        // white pieces
        _pieces[0, 0] = SpawnPiece(PieceType.Rook, white);
        _pieces[1, 0] = SpawnPiece(PieceType.Knight, white);
        _pieces[2, 0] = SpawnPiece(PieceType.Bishop, white);
        _pieces[3, 0] = SpawnPiece(PieceType.Queen, white);
        _pieces[4, 0] = SpawnPiece(PieceType.King, white);
        _pieces[5, 0] = SpawnPiece(PieceType.Bishop, white);
        _pieces[6, 0] = SpawnPiece(PieceType.Knight, white);
        _pieces[7, 0] = SpawnPiece(PieceType.Rook, white);
        
        // black pieces
        _pieces[0, 7] = SpawnPiece(PieceType.Rook, black);
        _pieces[1, 7] = SpawnPiece(PieceType.Knight, black);
        _pieces[2, 7] = SpawnPiece(PieceType.Bishop, black);
        _pieces[3, 7] = SpawnPiece(PieceType.Queen, black);
        _pieces[4, 7] = SpawnPiece(PieceType.King, black);
        _pieces[5, 7] = SpawnPiece(PieceType.Bishop, black);
        _pieces[6, 7] = SpawnPiece(PieceType.Knight, black);
        _pieces[7, 7] = SpawnPiece(PieceType.Rook, black);
        
        // pawns
        for (int i = 0; i < TileCountX; i++)
        {
            _pieces[i, 1] = SpawnPiece(PieceType.Pawn, white);
            _pieces[i, 6] = SpawnPiece(PieceType.Pawn, black);
        }
    }
    
    private Piece SpawnPiece(PieceType type, int side)
    {
        // create instance of the piece and get the Piece component from it
        GameObject pieceObject = Instantiate(prefabs[(int) type - 1], transform, true);
        Piece piece = pieceObject.GetComponent<Piece>();

        
        // this code has been replaced by code in the start function of the piece class
        // the models are the same for both sides so I wrote this to rotate the pieces 180 degrees about
        // the y-axis so the black pieces face inwards and not outwards
        /*
        if (side == 1)
        {
            pieceObject.transform.eulerAngles = new Vector3(0, 180, 0);
        }
        */
        // set the type and side for the newly instantiated piece
        piece.type = type;
        piece.side = side;
        
        //piece.transform.localScale = Vector3.one;
        //new Vector3(0.01f, 0.01f, 0.01f);
        // set the material for the mesh renderer for the piece to one of the 2 materials in the sideMaterials array
        piece.GetComponent<MeshRenderer>().material = pieceMaterials[((int)type * 2) + side];
        return piece;
        
        
        //((int)type * (side + 1)) - 1

    }


    // Positioning pieces
    
    private void SetAllPiecePositions()
    {
        // iterate over all the pieces and set their positions
        for (int x = 0; x < TileCountX; x++)
        {
            for (int y = 0; y < TileCountY; y++)
            {
                if (_pieces[x, y] != null)
                {
                    // set force true since no animation needed because this is for init position
                    SetPiecePosition(x, y, true);
                }
            }
        }
    }

    // bool force set to false for smooth piece movement
    private void SetPiecePosition(int x, int y, bool force = false)
    {
        _pieces[x, y].currentX = x;
        _pieces[x, y].currentY = y;
        _pieces[x, y].SetPosition(new Vector3(x * tileSize, offsetY, y * tileSize), force);
    }

    
    // Highlighting tiles

    private void HighlightTiles()
    {
        foreach (Vector2Int tile in _availableMoves)
        {
            _tiles[tile.x, tile.y].layer = LayerMask.NameToLayer("Highlight");
        }
    }

    private void UnhighlightTiles()
    {
        foreach (Vector2Int tile in _availableMoves)
        {
            _tiles[tile.x, tile.y].layer = LayerMask.NameToLayer("Tile");
        }
        
        _availableMoves.Clear();
    }
    
    // checkmate

    private void Checkmate(int team)
    {
        DisplayVictory(team);
    }

    private void DisplayVictory(int winningTeam)
    {
        // set the victory screen to be active
        victoryScreen.SetActive(true);
        // set the win message object to active (0 is white, 1 is black. be default both are inactive)
        victoryScreen.transform.GetChild(winningTeam).gameObject.SetActive(true);
    }

    public void ResetButton()
    {
        // clear UI
        victoryScreen.transform.GetChild(0).gameObject.SetActive(false);
        victoryScreen.transform.GetChild(1).gameObject.SetActive(false);
        victoryScreen.SetActive(false);
        
        // clean up
        
        // fields reset
        _currentlyHeld = null;
        _availableMoves = new List<Vector2Int>();
        isWhiteTurn = true;

        // iterate over array of pieces and destroy piece gameObjects
        for (int x = 0; x < TileCountX; x++)
        {
            for (int y = 0; y < TileCountY; y++)
            {
                if (_pieces[x,y] != null)
                    Destroy(_pieces[x,y].gameObject);

                // clear obj reference from array
                _pieces[x, y] = null;
            }
        }

        
        // iterate over dead pieces arrays and destroy gameObjects
        
        for (int i = 0; i < _deadWhite.Count; i++)
            Destroy(_deadWhite[i].gameObject);
        
        for (int i = 0; i < _deadBlack.Count; i++)
            Destroy(_deadBlack[i].gameObject);
        
        // clear object references in dead piece arrays
        _deadWhite.Clear();
        _deadBlack.Clear();
        
        
        // respawn pieces
        SpawnPieces();
        SetAllPiecePositions();
    }

    public void ExitButton()
    {
        Application.Quit();
    }
    
    
    
    // useful stuff
    
    // used to find tile when raycasting
    private Vector2Int TileIndexLookup(GameObject hitInfo)
    {
        // when the hitinfo type varaible is cast to a gameobject, it is essentially a copy of the gameobject that the
        // ray collided with. this means that we can iterate over the array of every tile that makes up the board
        // and find the one that the ray collided with and then make a vector2int with the x and y positions of the tile
        for (int x = 0; x < TileCountX; x++)
        {
            for (int y = 0; y < TileCountY; y++)
            {
                if (_tiles[x, y] == hitInfo)
                    return new Vector2Int(x, y);
            }
        }

        return -Vector2Int.one; // Invalid case. If the program gets here then congrats! You broke it! 
    }
    
    // guess what this does
    private GameObject[] LoadPrefabs()
    {
        GameObject[] resources; 
        resources = Resources.LoadAll<GameObject>("Prefabs");
        return resources;
    }

    // surprisingly, this loads the materials for the pieces
    private Material[] LoadPieceMaterials()
    {
        Material[] resources; 
        resources = Resources.LoadAll<Material>("PieceMaterials");
        return resources;
    }
    
    // shockingly, this handles piece movement
    private bool MovePiece(int x, int y, Piece currentlyHeld)
    {
        // check if the move is a valid move
        if (!ContainsValidMove(ref _availableMoves, new Vector2(x, y)))
        {
            return false;
        }
        // check if tile is not free
        if (_pieces[x, y] != null)
        {
            Piece existingPiece = _pieces[x, y];
            
            // if the piece is the same colour as the currentlyheld piece, move the current piece back
            if (currentlyHeld.side == existingPiece.side)
                return false;
            
            // if its a white piece
            if (existingPiece.side == 0)
            {
                if (existingPiece.type == PieceType.King)
                {
                    Checkmate(1);
                }
                
                _deadWhite.Add(existingPiece);
                // scale the piece down
                existingPiece.SetScale(Vector3.one * deadPieceScalar);
                // set the position of the piece to the side of the board
                // deathOffsetY because the edge of the board is higher than the center so pieces need to be higher
                // 8 * tileSize puts it to the edge
                // -1 * tileSize makes it so its just behind the first rank on the board
                // (Vector3.forward * deathSpacing * deadWhite.Count makes it so that each dead piece is a bit
                // ahead of the last one on the z axis by adding a vector of (0, 0, deathSpacing) for each piece
                // that has died so far 
                existingPiece.SetPosition(new Vector3(8 * tileSize, deathOffsetY, -1 * tileSize)
                                          + Vector3.forward * deathSpacing * _deadWhite.Count);
            }
            else
            {
                if (existingPiece.type == PieceType.King)
                {
                    Checkmate(0);
                }
                
                _deadBlack.Add(existingPiece);
                existingPiece.SetScale(Vector3.one * deadPieceScalar);
                existingPiece.SetPosition(new Vector3((-1 * tileSize), deathOffsetY, 8 * tileSize) 
                                          + Vector3.back * deathSpacing * _deadBlack.Count);
                    
            }
        }
        
        Vector2Int prevPos = new Vector2Int(currentlyHeld.currentX, currentlyHeld.currentY);
        _pieces[x, y] = currentlyHeld;
        _pieces[prevPos.x, prevPos.y] = null;
        SetPiecePosition(x, y);

        isWhiteTurn = !isWhiteTurn;
        
        // set pawn flag to sow first move made
        if (_pieces[x, y].type == PieceType.Pawn)
        {
            // getter and setter in the pawn class
            //bool a = _pieces[x, y].GetComponent<Pawn>().firstMoveMade;
            _pieces[x, y].GetComponent<Pawn>().firstMoveMade = true;
        }
        
        return true;
    }


    // function used to check for tiles to rehighlight after being hovered over
    private bool ContainsValidMove(ref List<Vector2Int> availableMoves, Vector2 position)
    {
        foreach (Vector2Int move in availableMoves)
            if (move.x == position.x && move.y == position.y)
                return true;
        return false;
    } 
    
    
    
    
}