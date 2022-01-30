using System.Collections.Generic;
using System.Numerics;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

public class Pawn : Piece
{
    public Pawn()
    {
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
            /*if (!_firstMoveMade && board[currentX, currentY + 2 * direction] == null 
                                &&  board[currentX, currentY + 1 * direction] == null)
                moves.Add(new Vector2Int(currentX, currentY + 2 * direction));*/
            
            if (currentY == 1 && side == 0 
                              && board[currentX, currentY + 2 * direction] == null 
                              &&  board[currentX, currentY + 1 * direction] == null)
                moves.Add(new Vector2Int(currentX, currentY + 2 * direction));
            if (currentY == 6 && side == 1 
                              && board[currentX, currentY + 2 * direction] == null 
                              &&  board[currentX, currentY + 1 * direction] == null)
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

    public override SpecialMove GetSpecialMoves(ref Piece[,] pieces, ref List<Vector2Int[]> moveList,
        ref List<Vector2Int> availableMoves)
    {
        // condition, if white set direction to 1 otherwise set to -1
        int direction = (side == 0) ? 1 : -1;
        
        // add check to see if the pawn is on the 2nd to last rank from its original rank
        // if so, then the next pawn movement is guaranteed to cause a promotion
        
        if ((side == 0 && currentY == 6) || (side == 1 && currentY == 1))
            return SpecialMove.Promotion;

        // en passant
        if (moveList.Count > 0)
        {
            // get last move
            Vector2Int[] lastMove = moveList[moveList.Count - 1];
            // check if piece at position moved to is pawn
            if (pieces[lastMove[1].x, lastMove[1].y].type == PieceType.Pawn)
            {
                // check if piece was moved by 2
                // absolute the value so it works for both sides
                if (Mathf.Abs(lastMove[0].y - lastMove[1].y) == 2)
                {
                    // if move was from other team
                    if (pieces[lastMove[1].x, lastMove[1].y].side != side)
                    {
                        // check if the piece is on the same y level
                        if (lastMove[1].y == currentY)
                        {
                            if (lastMove[1].x == currentX + 1)
                            {
                                availableMoves.Add(new Vector2Int(currentX + 1, currentY + direction));
                                return SpecialMove.EnPassant;
                            }

                            if (lastMove[1].x == currentX - 1)
                            {
                                availableMoves.Add(new Vector2Int(currentX - 1, currentY + direction));
                                return SpecialMove.EnPassant;
                            }

                        }
                    }
                }
            }
        }

        return SpecialMove.None;
    }
}


