using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ded.Wordox
{
    class MoveFinder
    {
        #region Fields
        private readonly WordGraph graph;
        private readonly Board board;
        private readonly PlayGraph play;
        #endregion
        #region Private stuff
        internal List<Tuple<PlayInfo, PlayPath>> GetAllMoves()
        {
            var infos = new List<Tuple<PlayInfo, PlayPath>>();
            foreach (PlayPath path in play.Valids)
            {
                bool vortex = false;
                foreach (LetterPlay lp in path.Played)
                    vortex |= lp.Cell.IsVortex;
                Tuple<Fix, Fix> oneTwoFixes = play.GetFixes(path);
                var info = new PlayInfo(vortex, board.Play(path).Score, oneTwoFixes.Item1, oneTwoFixes.Item2);
                infos.Add(new Tuple<PlayInfo, PlayPath>(info, path));
            }
            return infos;
        }
        private static void WriteMoves(IComparer<PlayInfo> comparer, IEnumerable<Tuple<double, PlayInfo, PlayPath>> moves)
        {
            Console.WriteLine(comparer);
            foreach (Tuple<double, PlayInfo, PlayPath> tuple in moves)
            {
                double weight = tuple.Item1;
                PlayInfo info = tuple.Item2;
                PlayPath path = tuple.Item3;
                Console.WriteLine("{0} word: {1} - {2}{3}{4}[{5}] ({6}){7}",
                                  weight.ToString("0.###", CultureInfo.InvariantCulture),
                                  path.Main,
                                  info.HasFixes && !info.HasVortex ? (info.HasOneFixes ? "1" : "") + (info.HasTwoMoreFixes ? "2+" : "") + "fixes " : "",
                                  info.HasVortex ? "vortex " : "",
                                  info.Points,
                                  info.Diff.ToString("+#;-#;0", CultureInfo.InvariantCulture),
                                  info.Stars,
                                  info.Wins ? " wins" : "");
            }
            Console.WriteLine();
        }
        private static bool Ignore(List<Tuple<double, PlayInfo, PlayPath>> weighted, List<Tuple<double, PlayInfo, PlayPath>> previous)
        {
            if (weighted.Count != previous.Count)
                return false;
            for (int i = 0; i < weighted.Count; i++)
                if (weighted[i].Item3 != previous[i].Item3)
                    return false;
            return true;
        }
        #endregion
        public MoveFinder(WordGraph graph, Board board, PlayGraph play)
        {
            this.graph = graph;
            this.board = board;
            this.play = play;
        }
        public Tuple<PlayInfo, PlayPath> GetBestMove()
        {
            var comparers = new List<IComparer<PlayInfo>>();
            comparers.Add(new PlayInfoComparer(ScoreStrategy.MaxDiff, WordStrategy.NoFixes));
            comparers.Add(new PlayInfoComparer(ScoreStrategy.MaxDiff, WordStrategy.NoOneFixes));
            comparers.Add(new PlayInfoComparer(ScoreStrategy.MaxDiff, WordStrategy.None));

            var moves = Progress.Wait<List<Tuple<PlayInfo, PlayPath>>>(GetAllMoves);

            Console.WriteLine();

            var previous = new List<Tuple<double, PlayInfo, PlayPath>>();
            var weighted = new List<Tuple<double, PlayInfo, PlayPath>>();
            for (int i = comparers.Count - 1; i >= 0; i--)
            {
                var reverse = new ReverseComparer<PlayInfo>(comparers[i]);
                moves.Sort((x, y) => reverse.Compare(x.Item1, y.Item1));

                while (weighted.Count < moves.Count)
                {
                    Tuple<PlayInfo, PlayPath> tuple = moves[weighted.Count];
                    if (weighted.Count == 0 || reverse.Compare(tuple.Item1, moves[weighted.Count - 1].Item1) == 0)
                    {
                        var word = new string((from lp in tuple.Item2.Played select lp.Letter).ToArray());
                        weighted.Add(new Tuple<double, PlayInfo, PlayPath>(graph.GetWeight(word), tuple.Item1, tuple.Item2));
                    }
                    else
                        break;
                }
                weighted.Sort((x, y) => x.Item1.CompareTo(y.Item1));

                if (!Ignore(weighted, previous))
                    WriteMoves(comparers[i], weighted);

                previous.Clear();
                previous.AddRange(weighted);
                if (i > 0)
                    weighted.Clear();
            }
            return new Tuple<PlayInfo, PlayPath>(weighted[0].Item2, weighted[0].Item3);
        }
    }
    class Game
    {
        #region class CellFinder
        private sealed class CellFinder
        {
            #region Fields
            private readonly RandomValues random;
            private readonly ValidWord valid;
            private readonly Direction direction;
            #endregion
            #region Private stuff
            private Cell GetRandomCell()
            {
                Cell first = new Cell(Board.Center, Board.Center);
                if (valid.Word.Length == 6)
                    first = direction == Direction.Right ? first.Left : first.Up;
                int range = valid.Word.Length == 6 ? 3 : valid.Word.Length - 1;
                int d = valid.Word.Length >= 5 ? random.GetInt(2) * range : random.GetInt(range);
                for (int i = 0; i < d; i++)
                    first = direction == Direction.Right ? first.Left : first.Up;
                return first;
            }
            private Cell GetPrefixCell()
            {
                if (valid.Word.Length >= 5)
                    return direction == Direction.Right ? new Cell(Board.Center, 0) : new Cell(0, Board.Center);
                return GetRandomCell();
            }
            private Cell GetSuffixCell()
            {
                int length = valid.Word.Length;
                if (length >= 5)
                    return direction == Direction.Right ? new Cell(Board.Center, Board.Width - length) : new Cell(Board.Height - length, Board.Center);
                return GetRandomCell();
            }
            #endregion
            public CellFinder(RandomValues random, ValidWord valid, Direction direction)
            {
                this.random = random;
                this.valid = valid;
                this.direction = direction;
            }
            public Cell Run()
            {
                switch (valid.OneFixes)
                {
                    case Fix.None:
                        return GetRandomCell();
                    case Fix.Prefix:
                        return GetPrefixCell();
                    case Fix.Suffix:
                        return GetSuffixCell();
                    case Fix.All:
                        if (random.Bool)
                            return GetPrefixCell();
                        return GetSuffixCell();
                    default:
                        throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Unknown fix : {0}", valid.OneFixes));
                }
            }
        }
        #endregion
        #region Fields
        private readonly RandomValues random;
        private readonly WordGraph graph;
        #endregion
        #region Private stuff
        internal WordPart GetFirstWord(Rack rack)
        {
            var ai = new AI(graph);
            List<ValidWord> words = ai.FindLongest(rack.Value).ToList();
            var validWordComparer = new ValidWordComparer(WordStrategy.NoOneFixes);
            words.Sort(validWordComparer);
            words.Reverse();
            Console.WriteLine("{0} word{1}", words.Count, words.Count > 1 ? "s" : "");
            for (int i = 0; i < words.Count; i++)
            {
                ValidWord word = words[i];
                Console.WriteLine("{0}> {1} 1:{2} 2+:{3}", i + 1, word.Word, word.OneFixes, word.TwoMoreFixes);
            }

            var weighted = new List<Tuple<double, ValidWord>>();
            foreach (ValidWord valid in words)
                if (weighted.Count == 0 || validWordComparer.Compare(valid, weighted[weighted.Count - 1].Item2) == 0)
                    weighted.Add(new Tuple<double, ValidWord>(graph.GetWeight(valid.Word), valid));
            weighted.Sort((x, y) => x.Item1.CompareTo(y.Item1));

            Direction direction = random.Bool ? Direction.Right : Direction.Down;
            ValidWord selected = weighted[0].Item2;
            var cellFinder = new CellFinder(random, selected, direction);
            Cell first = cellFinder.Run();
            return new WordPart(selected.Word, first, direction);
        }
        private PlayGraph GetPlayGraph(Board board, Rack rack)
        {
            return new PlayGraph(graph, board, rack);
        }
        #endregion
        public Game()
            : this(new RandomValues(), WordGraph.French)
        {
        }
        public Game(RandomValues random, WordGraph graph)
        {
            this.random = random;
            this.graph = graph;
        }
        public void Play()
        {
            var board = new Board();
            var rack = new Rack(graph.GetRandom());
            Console.WriteLine("rack: " + rack.Value);
            WordPart part = GetFirstWord(rack);
            Console.WriteLine("word: {0}", part);
            board = board.Play(part);
            var letters = new List<char>(rack.Letters);
            foreach (char c in part.Word)
                letters.Remove(c);
            board.Write();

            while (!board.Score.Other.Wins)
            {
                var time = new Chrono();

                Console.Write("------------------------------\n");
                rack = new Rack(graph.GetRandom(new string(letters.ToArray())));
                Console.WriteLine("rack: " + rack.Value);

                if (board.IsEmpty)
                {
                    part = GetFirstWord(rack);
                    Console.WriteLine("word: {0}", part);
                    board = board.Play(part);
                    letters = new List<char>(rack.Letters);
                    foreach (char c in part.Word)
                        letters.Remove(c);
                    board.Write();
                }
                else
                {
                    var chrono = new Chrono();
                    PlayGraph play = GetPlayGraph(board, rack);
                    double seconds = .1 * Convert.ToInt32(chrono.Elapsed.TotalSeconds * 10);
                    Console.WriteLine("{0} moves in {1} s", play.Valids.Count, seconds);
                    chrono = new Chrono();
                    var moveFinder = new MoveFinder(graph, board, play);
                    Tuple<PlayInfo, PlayPath> best = moveFinder.GetBestMove();
                    seconds = .1 * Convert.ToInt32(chrono.Elapsed.TotalSeconds * 10);
                    Console.WriteLine("Analyzed {0} moves in {1} s", play.Valids.Count, seconds);
                    Console.WriteLine();
                    {
                        PlayInfo info = best.Item1;
                        PlayPath path = best.Item2;
                        path.Write();
                        board = board.Play(path);
                        board.Write();
                        if (info.HasVortex)
                        {
                            board = board.Clear();
                            letters.Clear();
                        }
                        else
                        {
                            letters = new List<char>(rack.Letters);
                            foreach (LetterPlay lp in path.Played)
                                letters.Remove(lp.Letter);
                        }
                    }
                    if (time.Elapsed.TotalSeconds > 40)
                        throw new TimeoutException();
                }
            }
        }
    }
}
