using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ded.Wordox
{
    // TODO CHOOSE LOWEST PROBA FOR COMPARABLE MOVES
    public class Chrono
    {
        #region Fields
        private readonly DateTime start = DateTime.Now;
        #endregion
        public TimeSpan Elapsed { get { return DateTime.Now - start; } }
    }
    class Game
    {
        #region class RandomValues
        private sealed class RandomValues
        {
            #region Fields
            private readonly Random random;
            #endregion
            public RandomValues()
            {
                random = new Random();
            }
            public bool Bool
            {
                get { return GetInt(2) == 0; }
            }
            public int GetInt(int range)
            {
                return random.Next() % range;
            }
        }
        #endregion
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
                    first = direction == Direction.Right ? first.Left : first.Top;
                int range = valid.Word.Length == 6 ? 3 : valid.Word.Length - 1;
                int d = valid.Word.Length >= 5 ? random.GetInt(2) * range : random.GetInt(range);
                for (int i = 0; i < d; i++)
                    first = direction == Direction.Right ? first.Left : first.Top;
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
        private WordPart GetFirstWord(Rack rack)
        {
            var ai = new AI(graph);
            List<ValidWord> words = ai.FindLongest(rack.Value).ToList();
            var validWordComparer = new ValidWordComparer(WordStrategy.NoOneFixes);
            words.Sort(validWordComparer);
            words.Reverse();
            Console.WriteLine("{0} words", words.Count);
            foreach (var word in words)
                Console.WriteLine("{0} 1:{1}", word.Word, word.OneFixes);

            var weighted = new List<Tuple<double, ValidWord>>();
            foreach (ValidWord valid in words)
                if (weighted.Count == 0 || validWordComparer.Compare(valid, weighted[weighted.Count - 1].Item2) == 0)
                    weighted.Add(new Tuple<double, ValidWord>(graph.GetWeight(valid.Word), valid));
            weighted.Sort((x, y) => x.Item1.CompareTo(y.Item1));

            Direction direction = random.Bool ? Direction.Right : Direction.Bottom;
            ValidWord selected = weighted[0].Item2;
            var cellFinder = new CellFinder(random, selected, direction);
            Cell first = cellFinder.Run();
            return new WordPart(selected.Word, first, direction);
        }
        private static void Debug(PlayPath path)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("{0} {1} {2}", path.Main.First, path.Main.Direction, path.Main.Word);
            if (path.Extras.Count > 0)
                foreach (WordPart extra in path.Extras)
                    sb.AppendFormat(" [{0} {1} {2}]", extra.First, extra.Direction, extra.Word);
            sb.AppendFormat(" +{0}", string.Join(string.Empty, (from lp in path.Played select lp.Letter).ToArray()));
            if (path.Pending.Count > 0)
                sb.AppendFormat(" [{0}]", new string(path.Pending.ToArray()));
            Console.WriteLine(sb);
        }
        public Game()
        {
            random = new RandomValues();
            graph = WordGraph.French;
        }

        private PlayGraph GetPlayGraph(Board board, Rack rack)
        {
            var play = new PlayGraph(graph, board, rack);
            for (int i = 1; i < Rack.Size; i++)
                play = play.Next();
            return play;
        }
        private Tuple<PlayInfo, PlayPath> GetBestPlay(PlayGraph play, Score score, List<Tuple<PlayInfo, PlayPath>> infos)
        {
            if (play != null)
                throw new NotImplementedException("Find 2+ fixes");
            var valids = new List<PlayPath>(play.Valids);
            var comparer = new PlayInfoComparer(ScoreStrategy.MaxDiff, WordStrategy.NoFixes);
            var reverse = new ReverseComparer<PlayInfo>(comparer);
            foreach (PlayPath path in valids)
            {
                bool vortex = false;
                foreach (LetterPlay lp in path.Played)
                    vortex |= lp.Cell.IsVortex;
                Tuple<Fix, Fix> oneTwoFixes = play.GetFixes(path);
                var info = new PlayInfo(vortex, score.Play(path), oneTwoFixes.Item1 != Fix.None, oneTwoFixes.Item2 != Fix.None);
                infos.Add(new Tuple<PlayInfo, PlayPath>(info, path));
            }
            infos.Sort((x, y) => reverse.Compare(x.Item1, y.Item1));
            var weighted = new List<Tuple<double, PlayInfo, PlayPath>>();
            while (weighted.Count < infos.Count)
            {
                Tuple<PlayInfo, PlayPath> tuple = infos[weighted.Count];
                if (weighted.Count == 0 || comparer.Compare(tuple.Item1, infos[weighted.Count - 1].Item1) == 0)
                {
                    var word = new string((from lp in tuple.Item2.Played select lp.Letter).ToArray());
                    weighted.Add(new Tuple<double, PlayInfo, PlayPath>(graph.GetWeight(word), tuple.Item1, tuple.Item2));
                }
                else
                    break;
            }
            weighted.Sort((x, y) => x.Item1.CompareTo(y.Item1));
            return new Tuple<PlayInfo, PlayPath>(weighted[0].Item2, weighted[0].Item3);
        }

        public void Play()
        {
            var board = new Board();
            var score = new Score();
            var rack = new Rack(graph.GetRandom());
            Console.WriteLine("rack: " + rack.Value);
            WordPart part = GetFirstWord(rack);
            Console.WriteLine("word: {0} @ {1} {2}", part.Word, part.First, part.Direction);
            board = board.Play(part);
            score = score.Play(part);
            var letters = new List<char>(rack.Letters);
            foreach (char c in part.Word)
                letters.Remove(c);
            Console.WriteLine("score: " + score);
            board.Write();

            while (!score.Other.Wins)
            {
                Console.Write("------------------------------\n");
                rack = new Rack(graph.GetRandom(new string(letters.ToArray())));
                Console.WriteLine("rack: " + rack.Value);
                var chrono = new Chrono();
                PlayGraph play = GetPlayGraph(board, rack);
                Console.WriteLine("{0} moves in {1} s", play.Valids.Count, Convert.ToInt32(chrono.Elapsed.TotalSeconds));
                var infos = new List<Tuple<PlayInfo, PlayPath>>();
                chrono = new Chrono();
                Tuple<PlayInfo, PlayPath> best = GetBestPlay(play, score, infos);
                Console.WriteLine("Analyzed {0} moves in {1} s", play.Valids.Count, Convert.ToInt32(chrono.Elapsed.TotalSeconds));
                {
                    PlayInfo info = best.Item1;
                    PlayPath path = best.Item2;
                    Debug(path);
                    board = board.Play(path.Main);
                    score = score.Play(path);
                    Console.WriteLine("score: " + score);
                    board.Write();
                    if (info.HasVortex)
                    {
                        board = new Board();
                        letters.Clear();
                    }
                    else
                    {
                        letters = new List<char>(rack.Letters);
                        foreach (LetterPlay lp in path.Played)
                            letters.Remove(lp.Letter);
                    }
                }
                int max = 0;
                foreach (Tuple<PlayInfo, PlayPath> tuple in infos)
                {
                    PlayInfo info = tuple.Item1;
                    PlayPath path = tuple.Item2;
                    if (max == 0)
                        max = info.Points;
                    if (info.Points >= max)
                        Console.WriteLine("word: {0} @ {1} {2} - {3}{4}{5}[{6}] ({7}){8}",
                                          path.Main.Word,
                                          path.Main.First,
                                          path.Main.Direction,
                                          info.HasFixes && !info.HasVortex ? (info.HasOneFixes ? "1" : "") + (info.HasTwoMoreFixes ? "2+" : "") + "fixes " : "",
                                          info.HasVortex ? "vortex " : "",
                                          info.Points,
                                          info.Diff.ToString("+#;-#;0"),
                                          info.Stars,
                                          info.Wins ? " wins" : "");
                }
            }
        }
    }
    [TestFixture]
    public class GameTest
    {
        [Test] public void Test()
        {
            for (int i = 0; i < 100; i++)
            {
                var game = new Game();
                game.Play();
                if (i == 0)
                    break;
            }
        }


        private static PlayGraph GetPlayGraph(WordGraph graph, Board board, Rack rack)
        {
            var play = new PlayGraph(graph, board, rack);
            for (int i = 1; i < Rack.Size; i++)
                play = play.Next();
            return play;
        }
        private static Tuple<PlayInfo, PlayPath> GetBestPlay(WordGraph graph, PlayGraph play, Score score, List<Tuple<PlayInfo, PlayPath>> infos)
        {
            var valids = new List<PlayPath>(play.Valids);
            var comparer = new PlayInfoComparer(ScoreStrategy.MaxDiff, WordStrategy.NoFixes);
            var reverse = new ReverseComparer<PlayInfo>(comparer);
            foreach (PlayPath path in valids)
            {
                bool vortex = false;
                foreach (LetterPlay lp in path.Played)
                    vortex |= lp.Cell.IsVortex;
                Tuple<Fix, Fix> oneTwoFixes = play.GetFixes(path);
                var info = new PlayInfo(vortex, score.Play(path), oneTwoFixes.Item1 != Fix.None, oneTwoFixes.Item2 != Fix.None);
                infos.Add(new Tuple<PlayInfo, PlayPath>(info, path));
            }
            infos.Sort((x, y) => reverse.Compare(x.Item1, y.Item1));
            var weighted = new List<Tuple<double, PlayInfo, PlayPath>>();
            while (weighted.Count < infos.Count)
            {
                Tuple<PlayInfo, PlayPath> tuple = infos[weighted.Count];
                if (weighted.Count == 0 || comparer.Compare(tuple.Item1, infos[weighted.Count - 1].Item1) == 0)
                {
                    var word = new string((from lp in tuple.Item2.Played select lp.Letter).ToArray());
                    weighted.Add(new Tuple<double, PlayInfo, PlayPath>(graph.GetWeight(word), tuple.Item1, tuple.Item2));
                }
                else
                    break;
            }
            weighted.Sort((x, y) => x.Item1.CompareTo(y.Item1));
            return new Tuple<PlayInfo, PlayPath>(weighted[0].Item2, weighted[0].Item3);
        }

        
        [Test] public void TestFixesBug()
        {
            var pending = new ConstantSet<char>();

            var graph = WordGraph.French;
            var board = new Board();
            var rack = new Rack("ASSDHS");
            var score = new Score();

            var part1 = new WordPart("DAHS", new Cell(4, 4), Direction.Bottom);
            board = board.Play(part1);
            score = score.Play(part1);
            //board.Write();
            //Console.WriteLine("score: " + score);
            //Console.WriteLine();

            rack = new Rack("SSEEEA");
            PlayGraph play = GetPlayGraph(graph, board, rack);
            var infos = new List<Tuple<PlayInfo, PlayPath>>();
            /*Tuple<PlayInfo, PlayPath> best =*/ GetBestPlay(graph, play, score, infos);
            foreach (Tuple<PlayInfo, PlayPath> info in infos)
            {
                if ("ES" != info.Item2.Main.Word
                    || new Cell(7, 5) != info.Item2.Main.First
                    || Direction.Bottom != info.Item2.Main.Direction)
                    continue;
                Assert.IsTrue(info.Item1.HasFixes);
                Assert.IsTrue(info.Item1.HasTwoMoreFixes);
            }
            //var part2 = new WordPart("ES", new Cell(7, 5), Direction.Bottom);
            //var extra2 = new WordPartCollection(new WordPart("SE", new Cell(7, 4), Direction.Right));
            //var path2 = new PlayPath(part2, extra2, LetterPlayTest.GetPlayed(part2, 0), pending);
            //board = board.Play(part2);
            //score = score.Play(part2);
            //board.Write();
            //Console.WriteLine("score: " + score);
            //Console.WriteLine();


        }
        [Test] public void TestFirstFixes()
        {
            var graph = WordGraph.French;
            var rack = new Rack("IAOUEF");
            var ai = new AI(graph);
            List<ValidWord> words = ai.FindLongest(rack.Value).ToList();
            foreach (ValidWord valid in words)
            {
                if (valid.Word != "FOUIE")
                    continue;
                Assert.AreEqual(Fix.None, valid.OneFixes);
                Assert.AreEqual(Fix.Prefix, Fix.None);//valid.TwoMoreFixes);
                break;
            }
        }
    }
}
