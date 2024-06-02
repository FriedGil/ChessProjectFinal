using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using static ChessChallenge.API.BitboardHelper;
using static System.Math;
using System.Threading;

public class MyBot : IChessBot
{
    private Dictionary<string, string> book = new Dictionary<string, string>();
    private bool bookInitialized = false;
    private Dictionary<ulong, double> evalCache = new Dictionary<ulong, double>();
    private Dictionary<ulong, double> maxABCache = new Dictionary<ulong, double>();
    private Dictionary<ulong, double> minABCache = new Dictionary<ulong, double>();
    private Int16 numMoves= 0;
    


    public void InitalizeBook()
    {
        book.Add("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1","e2e4");
        book.Add("rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1","e7e5");
        book.Add("rnbqkbnr/pppp1ppp/8/4p3/4P3/8/PPPP1PPP/RNBQKBNR w KQkq e6 0 2", "g1f3");
        book.Add("rnbqkbnr/pppp1ppp/8/4p3/4P3/5N2/PPPP1PPP/RNBQKB1R b KQkq - 1 2", "b8c6");
        book.Add("r1bqkbnr/pppp1ppp/2n5/4p3/4P3/5N2/PPPP1PPP/RNBQKB1R w KQkq - 2 3", "f1c4");
        book.Add("r1bqkb1r/pppp1ppp/2n2n2/4p3/2B1P3/5N2/PPPP1PPP/RNBQK2R w KQkq - 4 4", "f3g5");
        bookInitialized = true;
    }
    

    public Move Think(Board boardOrig, ChessChallenge.API.Timer timerOrig)
    {
        if (numMoves == 3)
        {
            evalCache.Clear();
            maxABCache.Clear();
            minABCache.Clear();
            numMoves = 0;
        }

        numMoves += 1;
        
        if (!bookInitialized)
        {
            InitalizeBook();
        }
        var board = boardOrig;
        var timer = timerOrig;
        var lastEval = 0;
        

        // Depth of the minimax search
        int depth = 5;

        // if (timer.MillisecondsRemaining < timer.GameStartTimeMilliseconds / 5 | lastEval > 200 )
        // {
        //     depth = 4;
        // }
        //
        // if (timer.MillisecondsRemaining < timer.GameStartTimeMilliseconds / 20)
        // {
        //     depth = 3;
        // }
        Move bestMove = board.GetLegalMoves()[0];

        
        double bestValue = int.MinValue;

        Console.WriteLine("min cache len: " + minABCache.Count);
        Console.WriteLine("max cache len: " + maxABCache.Count);

        if (book.ContainsKey(board.GetFenString()))
        {
            Console.WriteLine("book move :)");
            bestMove = new Move(book[board.GetFenString()], board);
            return bestMove;

        }
        
        else foreach (var move in board.GetLegalMoves())
        {
            board.MakeMove(move);
            double moveValue = Minimax(board, depth - 1, int.MinValue, int.MaxValue, false);
            board.UndoMove(move);

            if (moveValue > bestValue)
            {
                bestValue = moveValue;
                bestMove = move;
            }
        }
        Debug.WriteLine(bestValue);
        Console.WriteLine(bestValue);
        return bestMove;
    }

    private double Minimax(Board board, int depth, double alpha, double beta, bool maximizingPlayer)
    {

        
        if (depth == 0 || board.IsDraw() || board.IsInCheckmate())
        {
            return EvaluateBoard(board);
        }

        if (maximizingPlayer)
        {
            if (maxABCache.ContainsKey(board.ZobristKey))
            {
                return maxABCache[board.ZobristKey];
            }
            double maxEval = int.MinValue;
            foreach (var move in board.GetLegalMoves())
            {
                board.MakeMove(move);
                double eval = Minimax(board, depth - 1, alpha, beta, false);
                board.UndoMove(move);
                maxEval = Max(maxEval, eval);
                alpha = Max(alpha, eval);
                if (beta <= alpha)
                {
                    break;
                }
            }
            if (depth >= 4) maxABCache.Add(board.ZobristKey,maxEval);
            return maxEval;
        }
        else
        {
            if (minABCache.ContainsKey(board.ZobristKey))
            {
                return minABCache[board.ZobristKey];
            }
            double minEval = double.MaxValue;
            foreach (var move in board.GetLegalMoves())
            {
                board.MakeMove(move);
                double eval = Minimax(board, depth - 1, alpha, beta, true);
                board.UndoMove(move);
                minEval = Min(minEval, eval);
                beta = Min(beta, eval);
                if (beta <= alpha)
                {
                    break;
                }
            }
            if (depth >= 4) minABCache.Add(board.ZobristKey,minEval);
            return minEval;
        }
    }

    
    private double EvaluateBoard(Board board)
    {
        if (evalCache.ContainsKey(board.ZobristKey))
        {
            return evalCache[board.ZobristKey];
        }
        
        double eval = 0;
        // //piece values
        foreach (var pieces in board.GetAllPieceLists())
        {
            if (board.PlyCount <= 14)
            {
                foreach (var piece in pieces)
                {
                    if (board.IsWhiteToMove)
                    {
                        if (!piece.IsWhite) eval += GetEarlyAdjustedPieceValue(piece);
                        else eval -= GetEarlyAdjustedPieceValue(piece);
                    }
                    else
                    {
                        if (piece.IsWhite) eval += GetEarlyAdjustedPieceValue(piece);
                        else eval -= GetEarlyAdjustedPieceValue(piece);

                    }
                }
            }

            else if (board.PlyCount is < 60 and > 14)
            {

                foreach (var piece in pieces)
                {
                    if (board.IsWhiteToMove)
                    {
                        if (!piece.IsWhite) eval += GetAdjustedPieceValue(piece);
                        else eval -= GetAdjustedPieceValue(piece);
                    }
                    else
                    {
                        if (piece.IsWhite) eval += GetAdjustedPieceValue(piece);
                        else eval -= GetAdjustedPieceValue(piece);

                    }
                }
            }
            else
            {
                foreach (var piece in pieces)
                {
                    if (board.IsWhiteToMove)
                    {
                        if (!piece.IsWhite) eval += GetLateAdjustedPieceValue(piece);
                        else eval -= GetLateAdjustedPieceValue(piece);
                    }
                    else
                    {
                        if (piece.IsWhite) eval += GetLateAdjustedPieceValue(piece);
                        else eval -= GetLateAdjustedPieceValue(piece);

                    }
                }

            }
        }


        //
        // //castling
        // if (board.GetKingSquare(true) == new Square("g1") && board.GetPiece(new Square("f1")).PieceType == PieceType.Rook)  eval += 100;
        // if (board.GetKingSquare(false) == new Square("g8") && board.GetPiece(new Square("f8")).PieceType == PieceType.Rook)  eval -= 100;
        //
        //
        // //attacking...
        // if (board.SquareIsAttackedByOpponent(new Square("f2")))
        // {
        //     eval -= 50;
        // }
        //
        // if (board.SquareIsAttackedByOpponent(new Square("f7")))
        // {
        //     eval += 50;
        // }
        //
        // //development
        // foreach (var k in board.GetPieceList(PieceType.Knight,true))
        // {
        //     if (!k.Square.Equals(new Square("b1")) && !k.Square.Equals(new Square("g1"))) eval += 50;
        //
        // }
        // foreach (var k in board.GetPieceList(PieceType.Knight,false))
        // {
        //     if (!k.Square.Equals(new Square("b8")) && !k.Square.Equals(new Square("g8"))) eval -= 50;
        // }
        //
        // foreach (var b in board.GetPieceList(PieceType.Bishop,true))
        // {
        //     if (!b.Square.Equals(new Square("c1")) && !b.Square.Equals(new Square("f1"))) eval += 50;
        //
        // }
        // foreach (var b in board.GetPieceList(PieceType.Bishop,false))
        // {
        //     if (!b.Square.Equals(new Square("c8")) && !b.Square.Equals(new Square("f8"))) eval -= 50;
        // }
        
        
        //
        if (board.IsDraw())
        {
            evalCache.Add(board.ZobristKey,0);
            return 0;
        }
        
        evalCache.Add(board.ZobristKey,eval);
        return eval;
    }
    
    private double GetAdjustedPieceValue(Piece piece)
    {
        double val = GetPieceValue(piece);
        double fileMultiplier = 1;
        double rowMultiplier = 1;
        double activityMultiplier = 1;
        if (piece.IsKnight)
        {
            fileMultiplier = 90.0 * 7 / 16 / Abs(piece.Square.File * 100 - 350); //from *2 to *100/350 
            rowMultiplier = 110.0 * 7 / 16 / Abs(piece.Square.Rank * 100 - 350); //from *2 to *100/350
        }
        if (piece.IsBishop)
        {
            fileMultiplier = 90.0 * 7 / 16 / Abs(piece.Square.File * 100 - 350); //from *2 to *100/350 
            rowMultiplier = 110.0 * 7 / 16 / Abs(piece.Square.Rank * 100 - 350); //from *2 to *100/350
        }
        if (piece.IsPawn)
        {
            if (piece.IsWhite)
            {
                rowMultiplier = (1.5*piece.Square.File / 7.0 + 1.5) / 1.5;
            }
            else
            {
                rowMultiplier = ((7-1.5*piece.Square.File) / 7.0 + 1.5) / 1.5;
            }
            
            if (piece.Square.File == 3 || piece.Square.File == 4)
            {
                fileMultiplier = 1.25;
            }

            if (piece.Square.File == 0 || piece.Square.File == 7)
            {
                fileMultiplier = 0.75;
            }
            
        }
        
        if (piece.IsBishop && (piece.Square.Equals(new Square(1, 1)) || piece.Square.Equals(new Square(1, 6)) ||
                               piece.Square.Equals(new Square(6, 1)) || piece.Square.Equals(new Square(6, 6))))
        {
            activityMultiplier = 1.5;
        }
        
        val = (7 * val + val * fileMultiplier) / 8;
        val = (7 * val + val * rowMultiplier) / 8;
        val = (7 * val + val * activityMultiplier) / 8;
        return val;
    }
    
    private double GetLateAdjustedPieceValue(Piece piece)
    {
        double val = GetPieceValue(piece);
        double fileMultiplier = 1;
        double rowMultiplier = 1;
        double activityMultiplier = 1;
        if (piece.IsKnight)
        {
            fileMultiplier = 90.0 * 7 / 16 / Abs(piece.Square.File * 100 - 350); //from *2 to *100/350 
            rowMultiplier = 110.0 * 7 / 16 / Abs(piece.Square.Rank * 100 - 350); //from *2 to *100/350
        }
        if (piece.IsBishop)
        {
            fileMultiplier = 90.0 * 7 / 16 / Abs(piece.Square.File * 100 - 350); //from *2 to *100/350 
            rowMultiplier = 110.0 * 7 / 16 / Abs(piece.Square.Rank * 100 - 350); //from *2 to *100/350
        }
        if (piece.IsPawn)
        {
            if (piece.IsWhite)
            {
                rowMultiplier = (1.5*piece.Square.File / 7.0 + 1.5) / 1.5;
            }
            else
            {
                rowMultiplier = ((7-1.5*piece.Square.File) / 7.0 + 1.5) / 1.5;
            }
            
            if (piece.Square.File == 3 || piece.Square.File == 4)
            {
                fileMultiplier = 1.25;
            }

            if (piece.Square.File == 0 || piece.Square.File == 7)
            {
                fileMultiplier = 0.75;
            }
            
        }
        
        if (piece.IsBishop && (piece.Square.Equals(new Square(1, 1)) || piece.Square.Equals(new Square(1, 6)) ||
                               piece.Square.Equals(new Square(6, 1)) || piece.Square.Equals(new Square(6, 6))))
        {
            activityMultiplier = 1.5;
        }
        
        val = (10 * val + val * fileMultiplier) / 11;
        val = (10 * val + val * rowMultiplier) / 11;
        val = (10 * val + val * activityMultiplier) / 11;
        return val;
    }

    private double GetEarlyAdjustedPieceValue(Piece piece)
    {
        double val = GetPieceValue(piece);
        double fileMultiplier = 1;
        double rowMultiplier = 1;
        double activityMultiplier = 1;
        if (piece.IsKnight)
        {
            fileMultiplier = 90.0 * 7 / 16 / Abs(piece.Square.File * 100 - 350); //from *2 to *100/350 
            rowMultiplier = 110.0 * 7 / 16 / Abs(piece.Square.Rank * 100 - 350); //from *2 to *100/350
        }

        if (piece.IsBishop)
        {
            fileMultiplier = 90.0 * 7 / 16 / Abs(piece.Square.File * 100 - 350); //from *2 to *100/350 
            rowMultiplier = 110.0 * 7 / 16 / Abs(piece.Square.Rank * 100 - 350); //from *2 to *100/350
        }

        if (piece.IsPawn)
        {
            if (piece.IsWhite)
            {
                rowMultiplier = (1.5 * piece.Square.File / 7.0 + 1.5) / 1.5;
            }
            else
            {
                rowMultiplier = ((7 - 1.5 * piece.Square.File) / 7.0 + 1.5) / 1.5;
            }

            if (piece.Square.File == 3 || piece.Square.File == 4)
            {
                fileMultiplier = 1.25;
            }

            if (piece.Square.File == 0 || piece.Square.File == 7)
            {
                fileMultiplier = 0.75;
            }

        }

        if (piece.IsBishop && (piece.Square.Equals(new Square(1, 1)) || piece.Square.Equals(new Square(1, 6)) ||
                               piece.Square.Equals(new Square(6, 1)) || piece.Square.Equals(new Square(6, 6))))
        {
            activityMultiplier = 1.5;
        }

        if (piece.IsQueen)
        {
            if (piece.Square.Equals(new Square("d1")) || piece.Square.Equals(new Square("d8")))
            {
                activityMultiplier *= 2;
            }
        }

        val = (5 * val + val * fileMultiplier) / 6;
        val = (5 * val + val * rowMultiplier) / 6;
        val = (5 * val + val * activityMultiplier) / 6;
        return val;
    }

    private double GetPieceValue(Piece piece)
    {
        switch (piece.PieceType)
        {
            case PieceType.Pawn:
                return 100;
            case PieceType.Knight:
                return 320;
            case PieceType.Bishop:
                return 330;
            case PieceType.Rook:
                return 550;
            case PieceType.Queen:
                return 900;
            case PieceType.King:
                return 20000;
            default:
                return 0;
        }
    }
    
    
    
    
}