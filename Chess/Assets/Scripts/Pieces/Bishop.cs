using System.Collections.Generic;
using UnityEngine;

public class Bishop : Piece
{
    public override List<Vector2Int> GetMoves(ref Piece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> moves = new List<Vector2Int>();
        //moves.Add(new Vector2Int(0, 2));

        // diagonal up right

        int checkPos;

        for (int x = currentX + 1; x < tileCountX; x++)
        {
            checkPos = currentY + (x - currentX);
            if (!(checkPos < tileCountY && checkPos > -1))
            {
                break;
            }
            //Debug.Log($"x:{x}, y:{checkPos}");
            if (board[x, checkPos] == null)
            {
                moves.Add(new Vector2Int(x, checkPos));
            }
            else if (board[x, checkPos] != null && board[x, checkPos].side != side)
            {
                moves.Add(new Vector2Int(x, checkPos));
                break;
            }
            else
            {
                break;
            }
        }

        // diagonal down right
        for (int y = currentY - 1; y > -1; y--)
        {
            checkPos = currentX + -(y - currentY);
            if (!(checkPos < tileCountX && checkPos > -1))
            {
                break;
            }

            //Debug.Log($"x:{checkPos}, y:{y}");
            if (board[checkPos, y] == null)
            {
                moves.Add(new Vector2Int(checkPos, y));
            }
            else if (board[checkPos, y] != null && board[checkPos, y].side != side)
            {
                moves.Add(new Vector2Int(checkPos, y));
                break;
            }
            else
            {
                break;
            }
        }

        // diagonal down left
        for (int y = currentY - 1; y > -1; y--)
        {
            checkPos = currentX + (y - currentY);
            
            if (!(checkPos > -1))
            {
                break;
            }

            //Debug.Log($"x:{checkPos}, y:{y}");
            if (board[checkPos, y] == null)
            {
                moves.Add(new Vector2Int(checkPos, y));
            }
            else if (board[checkPos, y] != null && board[checkPos, y].side != side)
            {
                moves.Add(new Vector2Int(checkPos, y));
                break;
            }
            else
            {
                break;
            }
        }

        // diagonal up left
        for (int x = currentX - 1; x > -1; x--)
        {
            //currentY + (x - currentX)
            checkPos = currentY - (x - currentX);
            Debug.Log($"x:{x}, y:{checkPos}");
            if (!(checkPos < tileCountY && checkPos > -1))
            {
                Debug.Log($"found edge at x:{x}, y:{checkPos}");
                break;
            }
            Debug.Log($"x:{x}, y:{checkPos}");
            if (board[x, checkPos] == null)
            {
                moves.Add(new Vector2Int(x, checkPos));
            }
            else if (board[x, checkPos] != null && board[x, checkPos].side != side)
            {
                moves.Add(new Vector2Int(x, checkPos));
                break;
            }
            else
            {
                break;
            }
        }

        return moves;
    }
}