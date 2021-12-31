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

    public void Update()
    {
        transform.position = Vector3.Lerp(transform.position, _desiredPosition, Time.deltaTime * 10);
        transform.localScale = Vector3.Lerp(transform.localScale, _desiredScale, Time.deltaTime * 10);
        
    }
    
    public virtual void setPosition(Vector3 position, bool force = false)
    {
        _desiredPosition = position;
        if (force)
        {
            transform.position = position;
        }
    }
    public virtual void setScale(Vector3 scale, bool force = false)
    {
        _desiredScale = scale;
        if (force)
        {
            transform.localScale = _desiredScale;
        }
    }
}
