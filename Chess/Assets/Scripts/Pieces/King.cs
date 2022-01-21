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
}