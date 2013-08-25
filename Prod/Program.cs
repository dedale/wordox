using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Ded.Wordox
{
    [Serializable]
    public class GiveUpException : Exception
    {
        #region Private stuff
        protected GiveUpException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
        #endregion
        public GiveUpException()
        {
        }
        public GiveUpException(string message)
            : base(message)
        {
        }
        public GiveUpException(string format, params string[] args)
            : base(string.Format(CultureInfo.InvariantCulture, format, args))
        {
        }
        public GiveUpException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
    class Progress
    {
        #region class InternalProgress
        private sealed class InternalProgress<T> : IDisposable
        {
            #region Fields
            private readonly ManualResetEvent signal;
            private readonly T result;
            #endregion
            #region Private stuff
            private void Wait()
            {
                while (!signal.WaitOne(TimeSpan.Zero))
                {
                    Console.Write(".");
                    signal.WaitOne(TimeSpan.FromMilliseconds(500));
                }
            }
            private void Dispose(bool disposing)
            {
                if (disposing)
                    signal.Dispose();
            }
            #endregion
            public InternalProgress(Func<T> func)
            {
                signal = new ManualResetEvent(false);
                var thread = new Thread(Wait);
                thread.IsBackground = true;
                thread.Start();
                result = func();
                signal.Set();
                Console.WriteLine();
            }
            public T Result { get { return result; } }
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }
        #endregion
        public static T Wait<T>(Func<T> func)
        {
            return new InternalProgress<T>(func).Result;
        }
    }
    class Shell
    {
        #region Fields
        private readonly RandomValues random;
        private readonly WordGraph graph;
        private Board board;
        #endregion
        #region Private stuff
        private static string ReadLine()
        {
            string line = Console.ReadLine();
            if (!string.IsNullOrEmpty(line) && line.Trim() == "quit")
            {
                Console.WriteLine();
                throw new GiveUpException();
            }
            return line;
        }
        internal static string GetNewRack(WordGraph graph, IList<char> current, string line)
        {
            if (line == null)
                return null;
            string newRack = line.Trim().ToUpperInvariant();
            if (newRack.Contains("*", StringComparison.OrdinalIgnoreCase))
                return graph.GetRandom(new string(current.ToArray()));
            if (newRack.Length + current.Count == Rack.Size)
                return new string(current.ToArray()) + newRack;
            return null;
        }
        private Rack ReadRack(IList<char> list)
        {
            string newRack = null;
            while (!Rack.Check(newRack))
            {
                using (new DisposableColor(ConsoleColor.Green))
                    Console.Write("rack? ");
                if (list.Count > 0)
                {
                    Console.Write("[");
                    using (new DisposableColor(ConsoleColor.Yellow))
                        Console.Write(new string(list.ToArray()));
                    Console.Write("] ");
                }
                newRack = GetNewRack(graph, list, ReadLine());
            }
            using (new DisposableColor(ConsoleColor.White))
                Console.Write("rack:");
            using (new DisposableColor(ConsoleColor.Yellow))
                Console.WriteLine(" " + newRack);
            Console.WriteLine();
            return new Rack(newRack);
        }
        private PlayPath FindMove(Rack rack)
        {
            PlayGraph play = new PlayGraph(graph, board, rack);
            var moveFinder = new MoveFinder(graph, board, play);
            return moveFinder.GetBestMove().Item2;
        }
        internal static PlayPath GetPath(Board board, Rack rack, WordPart part)
        {
            ISet<Cell> excluded = board.GetExcluded(part);
            ConstantList<LetterPlay> played = part.GetPlayed(excluded);
            var extras = new List<WordPart>();
            var otherDirection = part.Direction == Direction.Right ? Direction.Down : Direction.Right;
            var pending = new List<char>(rack.Letters);
            foreach (LetterPlay lp in played)
            {
                var before = board.GetBeforePart(lp.Cell, otherDirection);
                var after = board.GetAfterPart(lp.Cell, otherDirection);
                var pair = new WordPartPair(before, after);
                WordPart extra = pair.Play(lp.Cell, lp.Letter);
                if (extra != null)
                    extras.Add(extra);
                pending.Remove(lp.Letter);
            }
            return new PlayPath(part, new WordPartCollection(extras.ToConstant()), played, pending.ToConstant());
        }
        private static void Log(Exception e)
        {
            using (new DisposableColor(ConsoleColor.Red))
                Console.WriteLine("{0}: {1}", e.GetType().Name, e.Message);
            Console.WriteLine();
        }
        private static bool Check(Board board, Rack rack, PlayPath path, bool verbose = true)
        {
            try
            {
                board.Play(path);
                var letters = new List<char>(rack.Letters);
                foreach (LetterPlay lp in path.Played)
                {
                    if (!letters.Contains(lp.Letter))
                        throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "{0} not in rack ({1})", lp.Letter, rack.Value));
                    letters.Remove(lp.Letter);
                }
                return true;
            }
            catch (OutOfBoardException e)
            {
                if (verbose)
                    Log(e);
                return false;
            }
            catch (ArgumentException e)
            {
                if (verbose)
                    Log(e);
                return false;
            }
        }
        private static Direction? Parse(string value)
        {
            if (string.IsNullOrEmpty(value))
                return null;
            return (Direction)Enum.Parse(typeof(Direction), value);
        }
        private static PlayPath GetPath(Board board, Rack rack, string word, Cell cell, Direction? direction)
        {
            var directions = new List<Direction>();
            if (!direction.HasValue)
                directions.AddRange(new[] { Direction.Down, Direction.Right });
            else
                directions.Add(direction.Value);
            var paths = new List<PlayPath>();
            foreach (Direction d in directions)
            {
                try
                {
                    var part = new WordPart(word, cell, d);
                    PlayPath path = GetPath(board, rack, part);
                    if (Check(board, rack, path, false))
                        paths.Add(path);
                }
                catch (OutOfBoardException)
                {
                }
            }
            if (paths.Count == 1)
                return paths[0];
            if (paths.Count > 1)
                Log(new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Cannot guess direction to play {0} at {1}", word, cell)));
            else
                Log(new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Cannot play {0} at {1}", word, cell)));
            return null;
        }
        private static Cell GetCell(int row, int column)
        {
            try
            {
                return new Cell(row, column);
            }
            catch (OutOfBoardException e)
            {
                Log(e);
                return null;
            }
        }
        private static PlayPath GetPath(Board board, Rack rack, string word, int row, int column, Direction? direction)
        {
            Cell cell = GetCell(row, column);
            if (cell == null)
                return null;
            PlayPath path = GetPath(board, rack, word, cell, direction);
            if (path == null)
                return null;
            return path;
        }
        private PlayPath ReadMove(Board board, Rack rack)
        {
            var regex = new Regex(@"^\s*(?<word>[a-z]+)\s*(?<row>\d),(?<column>\d)\s*(?<direction>(down|right|))\s*$", RegexOptions.IgnoreCase);
            using (new DisposableColor(ConsoleColor.Green))
                Console.Write("move? ");
            Console.Write("[word r,c down|right] [play|guess|skip] ");
            while (true)
            {
                try
                {
                    string line = null;
                    using (new DisposableColor(PlayerScore.GetColor(board.Current)))
                        line = ReadLine().Trim();
                    if (line == "skip")
                        return null;
                    if (line == "play" || line == "guess")
                    {
                        if (board.IsEmpty)
                        {
                            var game = new Game(random, graph);
                            WordPart part = game.GetFirstWord(rack);
                            Console.WriteLine(part);
                            Console.WriteLine();

                            var played = part.GetPlayed();
                            var pending = new List<char>(rack.Letters);
                            foreach (LetterPlay lp in played)
                                pending.Remove(lp.Letter);
                            var path = new PlayPath(part, new WordPartCollection(), part.GetPlayed(), pending.ToConstant());
                            if (line == "play")
                                return path;
                        }
                        else
                        {
                            PlayPath path = FindMove(rack);
                            if (line == "play")
                                return path;
                        }
                    }
                    Match m = regex.Match(line);
                    if (m.Success)
                    {
                        string word = m.Groups["word"].Value;
                        int row = int.Parse(m.Groups["row"].Value, CultureInfo.InvariantCulture);
                        int column = int.Parse(m.Groups["column"].Value, CultureInfo.InvariantCulture);
                        Direction? direction = Parse(m.Groups["direction"].Value);
                        PlayPath path = GetPath(board, rack, word, row, column, direction);
                        if (path != null)
                        {
                            Console.WriteLine();
                            return path;
                        }
                    }
                    Console.Write("? ");
                }
                catch (FormatException e)
                {
                    var color = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("{0} : {1}", e.GetType().Name, e.Message);
                    Console.ForegroundColor = color;
                    Console.Write("? ");
                }
            }
        }
        #endregion
        public Shell()
        {
            random = new RandomValues();
            Console.Write("Loading");
            graph = Progress.Wait<WordGraph>(() => WordGraph.French);
            board = new Board();
        }
        public void Run()
        {
            Rack rack = null;
            while (true)
            {
                try
                {
                    bool skipped = false;
                    var pending = new List<char>();
                    while (!board.Score.Other.Wins)
                    {
                        board.Write();
                        Console.WriteLine();

                        if (!skipped)
                            rack = ReadRack(pending);
                        skipped = false;
                        PlayPath move = ReadMove(board, rack);
                        if (move == null)
                        {
                            board = board.Skip();
                            skipped = true;
                            continue;
                        }
                        using (new DisposableColor(ConsoleColor.Yellow))
                            move.Write();
                        board = board.Play(move);
                        pending.Clear();
                        pending.AddRange(move.Pending);

                        bool vortex = false;
                        foreach (LetterPlay lp in move.Played)
                            vortex |= lp.Cell.IsVortex;
                        if (vortex)
                        {
                            board.Write();
                            Console.WriteLine();
                            board = board.Clear();
                            pending.Clear();
                        }
                    }
                    board.Write();
                    rack = null;
                    using (new DisposableColor(PlayerScore.GetColor(board.Other)))
                        Console.WriteLine("{0} wins", board.Other);
                    Console.WriteLine();
                }
                catch (GiveUpException)
                {
                    if (board.IsEmpty && rack == null)
                        return;
                    rack = null;
                }
                board = new Board();
            }
        }
    }
    class Program
    {
        [STAThread]
        public static int Main(/*params string[] args*/)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            new Shell().Run();
            if (System.Diagnostics.Debugger.IsAttached)
            {
                Console.Write("Press a key");
                Console.ReadKey();
            }
            return 0;
        }
    }
}
