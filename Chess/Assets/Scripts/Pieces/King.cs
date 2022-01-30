using System.Collections.Generic;
using UnityEngine;

public class King : Piece
{
    public override List<Vector2Int> GetMoves(ref Piece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> moves = new List<Vector2Int>();


        List<Vector2Int> potentialPositions = new List<Vector2Int>
        {
            // cardinal directions
            // up 
            new Vector2Int(currentX, currentY + 1),
            // down
            new Vector2Int(currentX, currentY - 1),
            // right
            new Vector2Int(currentX + 1, currentY),
            // left
            new Vector2Int(currentX - 1, currentY),
            
            // anti-cardinal directions
            // ne
            new Vector2Int(currentX + 1, currentY + 1),
            // nw
            new Vector2Int(currentX - 1, currentY + 1),
            // se
            new Vector2Int(currentX + 1, currentY - 1),
            // sw
            new Vector2Int(currentX - 1, currentY - 1)
        };
        
        foreach (Vector2Int position in potentialPositions)
        {
            //this took like a solid 5 mins to find for no reason lmao
            // x is >= 1 and <= 100
            if (position.x > -1 && position.x < tileCountX && position.y > -1 && position.y < tileCountY)
            {
                if (board[position.x, position.y] == null)
                    moves.Add(position);

                if (board[position.x, position.y] != null && board[position.x, position.y].side != side)
                    moves.Add(position);
            }
        }
        return moves;
    }

    public override SpecialMove GetSpecialMoves(ref Piece[,] pieces, ref List<Vector2Int[]> moveList, ref List<Vector2Int> availableMoves)
    {
        SpecialMove move = SpecialMove.None;

        // iterate over moves made and look for a move where the starting position is the king's position
        // rinse and repeat for rooks
        Vector2Int[] kingMove = moveList.Find(m => m[0].x == 4 && m[0].y == ((side == 0) ? 0 : 7));
        Vector2Int[] leftRook = moveList.Find(m => m[0].x == 0 && m[0].y == ((side == 0) ? 0 : 7));
        Vector2Int[] rightRook = moveList.Find(m => m[0].x == 7 && m[0].y == ((side == 0) ? 0 : 7));

        // check if the king hasn't moved yet
        if (kingMove == null && currentX == 4)
        {
            // white
            if (side == 0)
            {
                // left rook
                if (leftRook == null && pieces[0, 0].type == PieceType.Rook && pieces[0, 0].side == 0
                    && pieces[3, 0] == null && pieces[2, 0] == null && pieces[3, 0] == null)
                {
                    availableMoves.Add(new Vector2Int(2, 0));
                    move = SpecialMove.Castling;
                }
                
                // right rook
                if (rightRook == null && pieces[7, 0].type == PieceType.Rook && pieces[7, 0].side == 0
                    && pieces[6, 0] == null && pieces[5, 0] == null)
                {
                    availableMoves.Add(new Vector2Int(6, 0));
                    move = SpecialMove.Castling;
                }
            }
            // black
            else
            {
                // left rook
                if (leftRook == null && pieces[0, 7].type == PieceType.Rook && pieces[0, 7].side == 1
                    && pieces[3, 7] == null && pieces[2, 7] == null && pieces[3, 7] == null)
                {
                    availableMoves.Add(new Vector2Int(2, 7));
                    move = SpecialMove.Castling;
                }
                
                // right rook
                if (rightRook == null && pieces[7, 7].type == PieceType.Rook && pieces[7, 7].side == 1
                    && pieces[6, 7] == null && pieces[5, 7] == null)
                {
                    availableMoves.Add(new Vector2Int(6, 7));
                    move = SpecialMove.Castling;
                }
            }
        }
        
        
        return move;
        
    }
}