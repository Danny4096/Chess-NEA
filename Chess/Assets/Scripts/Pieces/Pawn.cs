using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

public class Pawn : Piece
{
    private bool _firstMoveMade;
    private bool _enpassantable;

    public bool enpassantable
    {
        get { return _enpassantable; }
        set { _enpassantable = value; }
    }
    
    public bool firstMoveMade
    {
        get { return _firstMoveMade; }
        set { _firstMoveMade = value; }
    }
    public Pawn()
    {
        // set the firstMoveMade bool to false because, surprisingly, the first move hasn't been made yet 
        _firstMoveMade = false;
        _enpassantable = false;
    }

    public override List<Vector2Int> GetMoves(ref Piece[,] board, int tileCountX, int tileCountY)
    {
        // condition, if white set direction to 1 otherwise set to -1
        int direction = (side == 0) ? 1 : -1;
        //int lastRank = (side == 0) ? tileCountY - 1 : 0;
        
        List<Vector2Int> moves = new List<Vector2Int>();

        
        if (currentY + direction > -1 && currentY + direction < tileCountX)
        {
            // check for 1 square ahead
            if (board[currentX, currentY + direction] == null)
                moves.Add(new Vector2Int(currentX, currentY + direction));

            // check for 2 squares ahead on the first move
            if (!_firstMoveMade && board[currentX, currentY + 2 * direction] == null)
                moves.Add(new Vector2Int(currentX, currentY + 2 * direction));

            // check for diagonal kill

            // if the pawn isnt on the edge files
            if (currentX > 0 && currentX < tileCountX - 1)
            {
                if (board[currentX + 1, currentY + direction] != null &&
                    board[currentX + 1, currentY + direction].side != side)
                    moves.Add(new Vector2Int(currentX + 1, currentY + direction));

                if (board[currentX - 1, currentY + direction] != null &&
                    board[currentX - 1, currentY + direction].side != side)
                    moves.Add(new Vector2Int(currentX - 1, currentY + direction));
            }

            // if the pawn is on the edge files
            if (currentX == tileCountX - 1)
            {
                if (board[currentX - 1, currentY + direction] != null &&
                    board[currentX - 1, currentY + direction].side != side)
                    moves.Add(new Vector2Int(currentX - 1, currentY + direction));
            }

            if (currentX == 0)
            {
                if (board[currentX + 1, currentY + direction] != null &&
                    board[currentX + 1, currentY + direction].side != side)
                    moves.Add(new Vector2Int(currentX + 1, currentY + direction));
            }
        }

        return moves;
    }
}
