using System.Collections.Generic;
using UnityEngine;

public enum PieceType
{
    None = 0,
    Pawn = 1,
    Rook = 2,
    Knight = 3,
    Bishop = 4,
    Queen = 5,
    King = 6
}


// parent class for pieces
public class Piece : MonoBehaviour
{
    // for storing present data about the piece
    public int side;
    public int currentX;
    public int currentY;
    public PieceType type;

    // for movement
    private Vector3 _desiredPosition;
    // for when killed
    private Vector3 _desiredScale = Vector3.one;

    
    // handles movement of the piece
    public void Update()
    {
        // Vector3.Lerp linearly interpolates between 2 points 
        transform.position = Vector3.Lerp(transform.position, _desiredPosition, Time.deltaTime * 10);
        transform.localScale = Vector3.Lerp(transform.localScale, _desiredScale, Time.deltaTime * 10);
        
    }

    public virtual List<Vector2Int> GetMoves(ref Piece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> moves = new List<Vector2Int>();

        // preloading locations into the list for debugging. 
        // no need to remove honestly since this function gets overwritten for every use by the end lol/ 
        moves.Add(new Vector2Int(3, 3));
        moves.Add(new Vector2Int(3, 4));
        moves.Add(new Vector2Int(4, 3));
        moves.Add(new Vector2Int(4, 4));

        return moves;
    }
    
    
    // sets position of piece. if force is true, the lerp in Update() is not used and the movement is instantaneous
    public virtual void SetPosition(Vector3 position, bool force = false)
    {
        _desiredPosition = position;
        if (force)
        {
            transform.position = position;
        }
    }
    
    // sets scale of piece. if force is true, the lerp in Update() is not used and the rescaling is instantaneous
    public virtual void SetScale(Vector3 scale, bool force = false)
    {
        _desiredScale = scale;
        if (force)
        {
            transform.localScale = _desiredScale;
        }
    }
}
