using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.AI;

public enum SpecialMove
{
    None = 0,
    EnPassant,
    Castling,
    Promotion
}

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
    //special moves
    private SpecialMove _specialMove;
    private List<Vector2Int[]> _moveList = new List<Vector2Int[]>();
    
    
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
                        //Debug.Log($"found {_availableMoves.Count} moves for {_currentlyHeld.type} at " +
                        //          $"x:{_currentlyHeld.currentX}, y:{_currentlyHeld.currentY}");
                        
                        // get a list of special moves
                        _specialMove = _currentlyHeld.GetSpecialMoves(ref _pieces, ref _moveList, ref _availableMoves);

                        // the reason debug logs for positions of other pieces are logged are because this is called
                        // so the checks for all pieces are ran
                        PreventCheck();
                        
                        // highlight tiles that can be moved to
                        HighlightTiles();
                    }
                }
            }

            // If mouse 1 released and piece held
            if (Input.GetMouseButtonUp(0) && _currentlyHeld != null)
            {
                //Debug.Log($"saving position at x:{_currentlyHeld.currentX}, y:{_currentlyHeld.currentY}");
                // save previous piece position
                Vector2Int prevPos = new Vector2Int(_currentlyHeld.currentX, _currentlyHeld.currentY);
                
                // check if the move was valid
                bool moveIsValid = MovePiece(hitPos.x, hitPos.y, _currentlyHeld);

                // if the move wasnt valid, return the piece to its previous position and blank the piece reference
                if (!moveIsValid)
                {
                    //Debug.Log($"move failed. returning to pos at x:{prevPos.x}, y:{prevPos.y}");
                    _currentlyHeld.SetPosition(new Vector3(prevPos.x, offsetY, prevPos.y));
                }

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

    // this is for moving the actual piece visually after moving it in the array
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
        _availableMoves.Clear();
        _moveList.Clear();
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
    
    
    // special moves
    private void ProcessSpecialMove()
    {
        // en passant
        if (_specialMove == SpecialMove.EnPassant)
        {
            // get pawn that has just been moved
            Vector2Int[] newMove = _moveList[_moveList.Count - 1];
            Piece myPawn = _pieces[newMove[1].x, newMove[1].y];
            // get pawn to potentially en passant
            Vector2Int[] targetPawn = _moveList[_moveList.Count - 2];
            Piece enemyPawn = _pieces[targetPawn[1].x, targetPawn[1].y];
            
            // could cast turn checking bool to int for this
            
            // check if they're on the same file
            if (myPawn.currentX == enemyPawn.currentX)
            {
                // check the ranks they're on
                if (myPawn.currentY == enemyPawn.currentY - 1 || myPawn.currentY == enemyPawn.currentY + 1)
                {
                    if (enemyPawn.side == 0)
                    {
                        // send the piece to the graveyard
                        _deadWhite.Add(enemyPawn);
                        // scale the piece down
                        enemyPawn.SetScale(Vector3.one * deadPieceScalar);
                        // set the position of the piece to the side of the board
                        // deathOffsetY because the edge of the board is higher than the center so pieces need to be higher
                        // 8 * tileSize puts it to the edge
                        // -1 * tileSize makes it so its just behind the first rank on the board
                        // (Vector3.forward * deathSpacing * deadWhite.Count makes it so that each dead piece is a bit
                        // ahead of the last one on the z axis by adding a vector of (0, 0, deathSpacing) for each piece
                        // that has died so far 
                        enemyPawn.SetPosition(new Vector3(8 * tileSize, deathOffsetY, -1 * tileSize)
                                              + Vector3.forward * deathSpacing * _deadWhite.Count);
                    }
                    else
                    {
                        // send the piece to the graveyard
                        _deadBlack.Add(enemyPawn);
                        enemyPawn.SetScale(Vector3.one * deadPieceScalar);
                        enemyPawn.SetPosition(new Vector3((-1 * tileSize), deathOffsetY, 8 * tileSize) 
                                              + Vector3.back * deathSpacing * _deadBlack.Count);
                    
                    }
                    // blank the piece ref
                    _pieces[enemyPawn.currentX, enemyPawn.currentY] = null;
                }
            }
        }
        // castling
        if (_specialMove == SpecialMove.Castling)
        {
            Vector2Int[] lastMove = _moveList[_moveList.Count - 1];

            // left rook
            if (lastMove[1].x == 2)
            {
                if (lastMove[1].y == 0) // white
                {
                    Piece rook = _pieces[0, 0];
                    _pieces[3, 0] = rook;
                    SetPiecePosition(3,0);
                    _pieces[0, 0] = null;
                }
                else if (lastMove[1].y == 7) //black
                {
                    Piece rook = _pieces[0, 7];
                    _pieces[3, 7] = rook;
                    SetPiecePosition(3,7);
                    _pieces[0, 7] = null;
                }
            }
            // right rook
            else if (lastMove[1].x == 6)
            {
                if (lastMove[1].y == 0) // white
                {
                    Piece rook = _pieces[7, 0];
                    _pieces[5, 0] = rook;
                    SetPiecePosition(5,0);
                    _pieces[7, 0] = null;
                }
                else if (lastMove[1].y == 7) //black
                {
                    Piece rook = _pieces[7, 7];
                    _pieces[5, 7] = rook;
                    SetPiecePosition(5,7);
                    _pieces[7, 7] = null;
                }
            }
        }
        // pawn promotion
        if (_specialMove == SpecialMove.Promotion)
        {
            Vector2Int[] lastMove = _moveList[_moveList.Count - 1];
            Piece targetPawn = _pieces[lastMove[1].x, lastMove[1].y];

            // check if its actually a pawn
            if (targetPawn.type == PieceType.Pawn)
            {
                // white
                if (targetPawn.side == 0 && lastMove[1].y == 7)
                {
                    // temporarily automatically promoting to a queen because honestly why would you choose otherwise
                    Piece newQueen = SpawnPiece(PieceType.Queen, 0);
                    // destroy pawn gameObject
                    Destroy(_pieces[lastMove[1].x, lastMove[1].y].gameObject);
                    // overwrite pawn ref with queen ref
                    _pieces[lastMove[1].x, lastMove[1].y] = newQueen;
                    // set position of queen
                    SetPiecePosition(lastMove[1].x, lastMove[1].y, true);

                }
                
                // black
                if (targetPawn.side == 1 && lastMove[1].y == 0)
                {
                    // temporarily automatically promoting to a queen because honestly why would you choose otherwise
                    Piece newQueen = SpawnPiece(PieceType.Queen, 1);
                    // destroy pawn gameObject
                    Destroy(_pieces[lastMove[1].x, lastMove[1].y].gameObject);
                    // overwrite pawn ref with queen ref
                    _pieces[lastMove[1].x, lastMove[1].y] = newQueen;
                    // set position of queen
                    SetPiecePosition(lastMove[1].x, lastMove[1].y, true);

                }
            }
        }
    }

    
    // this function handles check
    // it allows for pieces to be "pinned", as well as forcing the king to be protected
    private void PreventCheck()
    {
        // create temp variable for storing king piece ref
        Piece targetKing = null;
        // iterate over the array of pieces and get the king for the side whose turn it currently is
        for (int x = 0; x < TileCountX; x++)
            for (int y = 0; y < TileCountY; y++)
                if (_pieces[x,y] != null)
                    if (_pieces[x, y].type == PieceType.King && _pieces[x, y].side == _currentlyHeld.side)
                        targetKing = _pieces[x, y];
        // ref availablemoves because need to delete moves that could endanger the king
        SinglePieceMoveSimulation(_currentlyHeld, ref _availableMoves, targetKing);
        //Debug.Log($"completed check prevention - found {_availableMoves.Count} moves for {_currentlyHeld.type} at " +
        //          $"x:{_currentlyHeld.currentX}, y:{_currentlyHeld.currentY}");
    }

    private void SinglePieceMoveSimulation(Piece piece, ref List<Vector2Int> moves, Piece targetking)
    {
        // save current values for resetting after the func call
        //Debug.Log("beginning move sim");
        //Debug.Log(piece.type);
        //Debug.Log($"x:{piece.currentX}, y:{piece.currentY}");
        int actualX = piece.currentX;
        int actualY = piece.currentY;
        List<Vector2Int> movesToRemove = new List<Vector2Int>();
        

        // simulate moves and check if we are in check
        //iterate over all of the possible moves for the given piece (these are passed as parameters to this procedure)
        for (int i = 0; i < moves.Count; i++)
        {
            // store x and y values for the move being simulated
            int simX = moves[i].x;
            int simY = moves[i].y;

            // store the position of the king to simulate it
            Vector2Int simKingPos = new Vector2Int(targetking.currentX, targetking.currentY);
            // has a king move been simulated?
            // check if the piece being simulated is a king (meaning the king will be moving)
            if (piece.type == PieceType.King)
            {
                // if yes, update the position of the king to the destination (i.e. a potential available move)
                simKingPos = new Vector2Int(simX, simY);
            }
            
            
            // Copy the board layout rather than directly reference it and get a list of the attacking pieces
            // i.e. piece of the opposite side to the piece being simulated
            
            // Copy the 2d array representing the board state instead of referencing it
            Piece[,] sim = new Piece[TileCountX, TileCountY];
            // this list holds all the opponent's pieces
            List<Piece> simAttackingPieces = new List<Piece>();
            for (int x = 0; x < TileCountX; x++)
            {
                for (int y = 0; y < TileCountY; y++)
                {
                    if (_pieces[x, y] != null)
                    {
                        sim[x, y] = _pieces[x, y];
                        if (sim[x, y].side != piece.side)
                            simAttackingPieces.Add(sim[x, y]);
                    }
                }
            }
            
            // simulate the move
            
            // move the piece (but not graphically)
            sim[actualX, actualY] = null;
            piece.currentX = simX;
            piece.currentY = simY;
            sim[simX, simY] = piece;
            
            
            // did a piece get taken  during the sim?
            // i.e. is there a piece in simAttackingPieces that has the same coords as where we just moved to?
            var deadPiece = simAttackingPieces.Find((p => p.currentX == simX && p.currentY == simY));
            // if yes, then remove it from pieces that can attack us
            if (deadPiece != null)
                simAttackingPieces.Remove(deadPiece);
            
            // using the new board state (with the piece passed as a param being moved), iterate over all of the pieces
            // that can attack us and get their available moves
            // each iteration, iterate over the array of potential moves and add them to the full list of moves to check
            // so simMoves will have every possible move for every piece that the enemy can make
            List<Vector2Int> simMoves = new List<Vector2Int>();
            for (int a = 0; a < simAttackingPieces.Count; a++)
            {
                var pieceMoves = simAttackingPieces[a].GetMoves(ref sim, TileCountX, TileCountY);
                for (int b = 0; b < pieceMoves.Count; b++)
                    simMoves.Add(pieceMoves[b]);
            }
            
            
            // check all of the potential moves that could've been made by the enemy and check if any of their positions
            // match the position of the king on this simulated board
            // if the king's position is the same as one of the moves' positions, that means a piece is attacking it, in
            // which case, this move that is being simulated must be removed from the moves that can be made
            //Debug.Log($"there are {simMoves.Count} moves that the enemy can make");
            foreach (Vector2Int m in simMoves)
            {
                //Debug.Log($"move: x{m.x}, y{m.y}");
            }
            if (ContainsValidMove(ref simMoves, simKingPos))
            {
                //Debug.Log("found a move to remove");
                movesToRemove.Add(moves[i]);
            }

            // restore piece data - when the move was simulated, the x and y positions of the piece were changed so 
            // they must be restored to their original values saved at the start of this procedure
            piece.currentX = actualX;
            piece.currentY = actualY;
        }
        
        // remove from the available moves list
        for (int i = 0; i < movesToRemove.Count; i++)
        {
            moves.Remove(movesToRemove[i]);
        }

        //Debug.Log("FINISHED SIM");
        foreach (var move in movesToRemove)
        {
            //Debug.Log($"x:{move.x}, y:{move.y}");
        }
    }


    private bool CheckForCheckmate()
    {
        for (int i = 0; i < TileCountX; i++)
        {
            for (int j = 0; j < TileCountY; j++)
            {
                if (_pieces[i,j] != null)
                {
                    //Debug.Log(_pieces[i,j].type);
                    //Debug.Log($"x:{_pieces[i,j].currentX}, y:{_pieces[i,j].currentY}");
                }
            }
        }

        Vector2Int[] lastMove = _moveList[_moveList.Count - 1];
        int targetTeam = (_pieces[lastMove[1].x, lastMove[1].y].side == 0) ? 1 : 0;


        Piece targetKing = null;
        List<Piece> attackingPieces = new List<Piece>();
        List<Piece> defendingPieces = new List<Piece>();
        for (int x = 0; x < TileCountX; x++)
        {
            for (int y = 0; y < TileCountY; y++)
            {
                if (_pieces[x, y] != null)
                {
                    if (_pieces[x, y].side == targetTeam)
                    {
                        defendingPieces.Add(_pieces[x, y]);
                        if (_pieces[x, y].type == PieceType.King)
                        {
                            targetKing = _pieces[x, y];
                        }
                    }
                    else
                    {
                        attackingPieces.Add(_pieces[x,y]);
                    }
                }
            }
        }

        /*
        foreach (Piece p in defendingPieces)
        {
            //Debug.Log(p.type);
        }
        //Debug.Log(defendingPieces[0].side);
        */

        // check if the king is being attacked
        //Debug.Log("checking if king in danger");
        List<Vector2Int> attackingMoves = new List<Vector2Int>();

        for (int i = 0; i < attackingPieces.Count; i++)
        {
            List<Vector2Int> pieceMoves = attackingPieces[i].GetMoves(ref _pieces, TileCountX, TileCountY);
            for (int j = 0; j < pieceMoves.Count; j++)
                attackingMoves.Add(pieceMoves[j]);
        }
        
        //Debug.Log("added moves to attackingmoves array");
        if (ContainsValidMove(ref attackingMoves, new Vector2Int(targetKing.currentX, targetKing.currentY)))
        {
            for (int i = 0; i < defendingPieces.Count; i++)
            {
                List<Vector2Int> defendingMoves = defendingPieces[i].GetMoves(ref _pieces, TileCountX, TileCountY);
                SinglePieceMoveSimulation(defendingPieces[i], ref defendingMoves, targetKing);
                
                // if there is ever a possible move, we're chillin
                if (defendingMoves.Count != 0)
                {
                    //Debug.Log("found potential moves");
                    //Debug.Log(defendingPieces[i].type);
                    foreach (Vector2Int m in defendingMoves)
                    {
                        //Debug.Log($"x:{m.x}, y:{m.y}");
                    }
                    return false;
                }
            }

            return true; // checkmate, ggs
        }
        return false;
        
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
        if (!ContainsValidMove(ref _availableMoves, new Vector2Int(x, y)))
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
        _moveList.Add(new Vector2Int[] {prevPos, new Vector2Int(x, y)});
        
        ProcessSpecialMove();

        if (CheckForCheckmate())
        {
            Checkmate(currentlyHeld.side);
        }
        
        return true;
    }
    
    // procedure to check a list of moves against a specific move
    // used to check for tiles to rehighlight after being hovered over and for checking if the king is in danger
    private bool ContainsValidMove(ref List<Vector2Int> availableMoves, Vector2Int position)
    {
        foreach (Vector2Int move in availableMoves)
            if (move.x == position.x && move.y == position.y)
                return true;
        return false;
    } 
    
    
    
    
}