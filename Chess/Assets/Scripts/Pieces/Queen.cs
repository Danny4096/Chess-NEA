using System.Collections.Generic;
using UnityEngine;

public class Queen : Piece
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

        // iterate over all y co-ordinates on the board at current x position and check if piece can be moved there
        for (int y = currentY + 1; y < tileCountY; y++)
        {
            // skip over the tile the piece has just been moved from
            if (y == currentY)
                continue;
            // if the position is empty
            if (board[currentX, y] == null)
            {
                //Debug.Log($"x:{currentX}, y:{y}");
                moves.Add(new Vector2Int(currentX, y));
            }

            // if the position has a piece and the piece is an opponent's piece
            if (board[currentX, y] != null)
            {
                if (board[currentX, y].side != side)
                    moves.Add(new Vector2Int(currentX, y));
                break;
            }
        }

        for (int y = currentY - 1; y > -1; y--)
        {
            // skip over the tile the piece has just been moved from
            if (y == currentY)
                continue;
            // if the position is empty
            if (board[currentX, y] == null)
            {
                //Debug.Log($"x:{currentX}, y:{y}");
                moves.Add(new Vector2Int(currentX, y));
            }

            // if the position has a piece and the piece is an opponent's piece
            if (board[currentX, y] != null)
            {
                if (board[currentX, y].side != side)
                    moves.Add(new Vector2Int(currentX, y));
                break;
            }
        }
        
        // iterate over all x co-ordinates on the board at current y position and check if piece can be moved there
        for (int x = currentX + 1; x < tileCountX; x++)
        {
            // skip over the tile the piece has just been moved from
            if (x == currentX)
                continue;
            // if the position is empty
            if (board[x, currentY] == null)
                moves.Add(new Vector2Int(x, currentY));

            // if the position has a piece and the piece is an opponent's piece
            if (board[x, currentY] != null)
            {
                if (board[x, currentY].side != side)
                    moves.Add(new Vector2Int(x, currentY));
                break;
            }
        }

        for (int x = currentX - 1; x > -1; x--)
        {
            // skip over the tile the piece has just been moved from
            if (x == currentX)
                continue;
            // if the position is empty
            if (board[x, currentY] == null)
                moves.Add(new Vector2Int(x, currentY));

            // if the position has a piece and the piece is an opponent's piece
            if (board[x, currentY] != null)
            {
                if (board[x, currentY].side != side)
                    moves.Add(new Vector2Int(x, currentY));
                break;
            }
        }
        
        return moves;
    }
}