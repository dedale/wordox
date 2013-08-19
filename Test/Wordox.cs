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

            Direction direction = random.Bool ? Direction.Right : Direction.Bottom;
            ValidWord valid = words[0];
            var cellFinder = new CellFinder(random, valid, direction);
            Cell first = cellFinder.Run();
            return new WordPart(valid.Word, first, direction);
        }
        public Game()
        {
            random = new RandomValues();
            graph = WordGraph.French;
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
            //for (int t = 0; t < 4; t++)
            {
                Console.Write("------------------------------\n");
                rack = new Rack(graph.GetRandom(new string(letters.ToArray())));
                Console.WriteLine("rack: " + rack.Value);
                var play = new PlayGraph(graph, board, rack);
                for (int i = 1; i < Rack.Size; i++)
                    play = play.Next();
                Console.WriteLine("{0} moves", play.Valids.Count);
                var valids = new List<PlayPath>(play.Valids);
                var comparer = new PlayInfoComparer(ScoreStrategy.MaxDiff, WordStrategy.NoFixes);
                var reverse = new ReverseComparer<PlayInfo>(comparer);
                var infos = new List<Tuple<PlayInfo, PlayPath>>();
                foreach (PlayPath path in valids)
                {
                    bool vortex = false;
                    foreach (LetterPlay lp in path.Played)
                        vortex |= lp.Cell.IsVortex;
                    var info = new PlayInfo(vortex, score.Play(path), play.GetFixes(path) != Fix.None, false);
                    infos.Add(new Tuple<PlayInfo, PlayPath>(info, path));
                }
                infos.Sort((x, y) => reverse.Compare(x.Item1, y.Item1));
                int max = 0;
                foreach (Tuple<PlayInfo, PlayPath> tuple in infos)
                {
                    PlayInfo info = tuple.Item1;
                    PlayPath path = tuple.Item2;
                    if (max == 0)
                    {
                        max = info.Points;
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
                    if (info.Points >= max)
                        Console.WriteLine("word: {0} @ {1} {2} - {3}{4}{5}[{6}] ({7}){8}", path.Main.Word, path.Main.First, path.Main.Direction,
                                          info.HasFixes ? "fixes " : "", info.HasVortex ? "vortex " : "", info.Points, info.Diff.ToString("+#;-#;0"), info.Stars, info.Wins ? " wins" : "");
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
    }
}
