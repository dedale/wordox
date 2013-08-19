using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ded.Wordox
{
    [TestFixture]
    public class CellTest
    {
        [TestCase(-1, 0)]
        [TestCase(-1, -1)]
        [TestCase(0, -1)]
        [TestCase(Board.Height, 0)]
        [TestCase(0, Board.Width)]
        [TestCase(Board.Height, Board.Width)]
        public void TestNewOutOfRange(int row, int column)
        {
            Assert.Throws<OutOfBoardException>(() => new Cell(row, column));
        }
        [Test] public void TestEquals()
        {
            var cell_0_0 = new Cell(0, 0);
            Assert.AreNotEqual(null, cell_0_0);
            Assert.AreNotEqual(cell_0_0, null);
            Assert.AreEqual(cell_0_0, cell_0_0);
            Assert.AreEqual(cell_0_0, new Cell(0, 0));
            Assert.AreNotEqual(cell_0_0, new Cell(0, 1));
            Assert.AreNotEqual(cell_0_0, new Cell(1, 0));
        }
        [Test] public void TestGetHashCode()
        {
            for (int r = 0; r < Board.Height; r++)
            {
                for (int c = 0; c < Board.Width; c++)
                {
                    var hash = new Cell(r, c).GetHashCode();
                    var mirror = new Cell(c, r).GetHashCode();
                    if (r == c)
                        Assert.AreEqual(hash, mirror);
                    else
                        Assert.AreNotEqual(hash, mirror);
                }
            }
        }
        [Test] public void TestOperators()
        {
            Assert.IsFalse(new Cell(0, 0) == null);
            Assert.IsTrue(new Cell(0, 0) != null);
            Assert.IsTrue(new Cell(0, 0) == new Cell(0, 0));
            Assert.IsFalse(new Cell(0, 0) != new Cell(0, 0));
            Assert.IsFalse(new Cell(1, 0) == new Cell(0, 0));
            Assert.IsTrue(new Cell(1, 0) != new Cell(0, 0));
        }
        [Test] public void TestTryGetNext()
        {
            for (int row = 0; row < Board.Height; row++)
            {
                var cell = new Cell(row, 0);
                for (int col = 1; col < Board.Width; col++)
                    Assert.IsTrue(cell.TryGetNext(Direction.Right, out cell));
                Assert.IsFalse(cell.TryGetNext(Direction.Right, out cell));
            }
            for (int col = 0; col < Board.Width; col++)
            {
                var cell = new Cell(0, col);
                for (int row = 1; row < Board.Height; row++)
                    Assert.IsTrue(cell.TryGetNext(Direction.Bottom, out cell));
                Assert.IsFalse(cell.TryGetNext(Direction.Bottom, out cell));
            }
        }
        [Test] public void TestIsStar()
        {
            var stars = new HashSet<Cell>();
            var tuples = new List<Tuple<Cell, int, int>>();
            tuples.Add(new Tuple<Cell, int, int>(new Cell(4, 0), 1, 1));
            tuples.Add(new Tuple<Cell, int, int>(new Cell(8, 4), -1, 1));
            tuples.Add(new Tuple<Cell, int, int>(new Cell(4, 8), -1, -1));
            tuples.Add(new Tuple<Cell, int, int>(new Cell(0, 4), 1, -1));
            for (int i = 0; i < 4; i++)
            {
                for (int c = 0; c < tuples.Count; c++)
                {
                    Cell cell = tuples[c].Item1;
                    int dr = tuples[c].Item2;
                    int dc = tuples[c].Item3;
                    stars.Add(cell);
                    tuples[c] = new Tuple<Cell, int, int>(new Cell(cell.Row + dr, cell.Column + dc), dr, dc);
                }
            }
            for (int r = 0; r < Board.Height; r++)
            {
                for (int c = 0; c < Board.Width; c++)
                {
                    var cell = new Cell(r, c);
                    bool star = stars.Contains(cell);
                    Assert.AreEqual(star, cell.IsStar, string.Format(CultureInfo.InvariantCulture, "{0} should{1} be a star", cell, star ? string.Empty : " not"));
                }
            }
        }
        [Test] public void TestIsVortex()
        {
            var vortexes = new HashSet<Cell>();
            for (int r = 0; r <= 1; r++)
                for (int c = 0; c <= 1; c++)
                    vortexes.Add(new Cell(r * (Board.Height - 1), c * (Board.Width - 1)));
            for (int r = 0; r < Board.Height; r++)
            {
                for (int c = 0; c < Board.Width; c++)
                {
                    var cell = new Cell(r, c);
                    Assert.AreEqual(vortexes.Contains(cell), cell.IsVortex);
                }
            }
        }
    }
    [TestFixture]
    public class BoardTest
    {
        [Test] public void TestGetStartCellsFirst()
        {
            var board = new Board();
            var start = board.GetStartCells().ToList();
            Assert.AreEqual(1, start.Count);
            Assert.AreEqual(Board.Center, start[0].Row);
            Assert.AreEqual(Board.Center, start[0].Column);
        }
        private bool OutOfBoard(string word, int row, int column, Direction direction)
        {
            switch (direction)
            {
                case Direction.Bottom:
                    return row + word.Length > Board.Height;
                case Direction.Right:
                    return column + word.Length > Board.Width;
                default:
                    throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Unknown direction : {0}", direction), "direction");
            }
        }
        private bool CanPlay(string word, int row, int column, Direction direction)
        {
            switch (direction)
            {
                case Direction.Bottom:
                    return column == Board.Center
                        && row >= Math.Max(Board.Center - word.Length + 1, 0)
                        && row <= Board.Center + Math.Min(Board.Center - word.Length + 1, 0);
                case Direction.Right:
                    return row == Board.Center
                        && column >= Math.Max(Board.Center - word.Length + 1, 0)
                        && column <= Board.Center + Math.Min(Board.Center - word.Length + 1, 0);
                default:
                    throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Unknown direction : {0}", direction), "direction");
            }
        }
        [CLSCompliant(false)]
        [Test] public void TestPlayFirst([Values("ME", "MOT", "MOTS", "MOTTE", "MOTTES")] string word,
                                         [Range(0, Board.Height - 1)] int row,
                                         [Range(0, Board.Height - 1)] int column,
                                         [Values(Direction.Bottom, Direction.Right)] Direction direction)
        {
            var board = new Board();
            bool outOfBoard = OutOfBoard(word, row, column, direction);
            bool canPlay = CanPlay(word, row, column, direction);
            Action action = () => board = board.Play(new WordPart(word, new Cell(row, column), direction));
            if (outOfBoard)
                Assert.Throws<OutOfBoardException>(new TestDelegate(action));
            else if (!canPlay)
                Assert.Throws<ArgumentException>(new TestDelegate(action));
            else
                action();
        }
        [CLSCompliant(false)]
        [TestCase("ME", 4, 4, Direction.Right, 6)]
        [TestCase("LETTRE", 2, 4, Direction.Bottom, 14)]
        [TestCase("LETTRE", 3, 4, Direction.Bottom, 13)]
        [TestCase("LETTRE", 4, 2, Direction.Right, 14)]
        [TestCase("LETTRE", 4, 3, Direction.Right, 13)]
        public void TestGetStartCellsSecond(string word, int row, int column, Direction direction, int count)
        {
            var board = new Board();
            var part = new WordPart(word, new Cell(row, column), direction);
            board = board.Play(part);
            var cells = board.GetStartCells();
            Assert.AreEqual(count, cells.Count);
        }
        [Test] public void TestPlayAll()
        {
            var graph = WordGraph.French;
            var board = new Board();
            var score = new Score();
            var part = new WordPart("LETTRE", new Cell(4, 2), Direction.Right);
            board = board.Play(part);
            score = score.Play(part);
            var rack = new Rack("MOTEUR");
            var play = new PlayGraph(graph, board, rack);
            for (int i = 0; i < 5; i++)
                play = play.Next();
            Assert.AreEqual(701, play.Valids.Count);
        }
        [CLSCompliant(false)]
        [TestCase(4, 3, null, Direction.Right)]
        [TestCase(4, 7, "MER", Direction.Right)]
        [TestCase(3, 4, null, Direction.Bottom)]
        [TestCase(5, 4, "M", Direction.Bottom)]
        public void TestGetBeforePart(int row, int column, string word, Direction direction)
        {
            var board = new Board();
            var part = new WordPart("MER", new Cell(4, 4), Direction.Right);
            board = board.Play(part);
            WordPart before = board.GetBeforePart(new Cell(row, column), direction);
            if (word != null)
                Assert.AreEqual(new WordPart(word, new Cell(4, 4), direction), before);
            else
                Assert.IsNull(before);
        }
        [CLSCompliant(false)]
        [TestCase(4, 3, "MER", Direction.Right)]
        [TestCase(4, 7, null, Direction.Right)]
        [TestCase(3, 4, "M", Direction.Bottom)]
        [TestCase(5, 4, null, Direction.Bottom)]
        public void TestGetAfterPart(int row, int column, string word, Direction direction)
        {
            var board = new Board();
            var part = new WordPart("MER", new Cell(4, 4), Direction.Right);
            board = board.Play(part);
            WordPart after = board.GetAfterPart(new Cell(row, column), direction);
            if (word != null)
                Assert.AreEqual(new WordPart(word, new Cell(4, 4), direction), after);
            else
                Assert.IsNull(after);
        }
    }
    [TestFixture]
    public class AITest
    {
        [Test] public void TestFind()
        {
            var graph = WordGraph.French;
            var ai = new AI(graph);
            var words = ai.Find("OTM").ToDictionary(vw => vw.Word);
            Assert.AreEqual(2, words.Count);
            Assert.IsTrue(words.ContainsKey("MOT"));
            Assert.IsTrue(words.ContainsKey("TOM"));
        }
        [Test] public void TestFindLongest([Range(0, 3)] int n)
        {
            var graph = WordGraph.French;
            var ai = new AI(graph);
            var letters = "NEZ" + new string('Z', n);
            var words = ai.FindLongest(letters).ToDictionary(vw => vw.Word);
            Assert.AreEqual(2, words.Count, "FindLongest failed for " + letters);
            Assert.IsTrue(words.ContainsKey("NEZ"), "NEZ not found for " + letters);
            Assert.IsTrue(words.ContainsKey("ZEN"), "ZEN not found for " + letters);
        }
    }
    [TestFixture]
    public class WordPartTest
    {
        [CLSCompliant(false)]
        [TestCase("MER", Direction.Right, 4, 3, 'A', "AMER", 4, 3)]
        [TestCase("MER", Direction.Right, 4, 7, 'E', "MERE", 4, 4)]
        [TestCase("MER", Direction.Bottom, 3, 4, 'A', "AMER", 3, 4)]
        [TestCase("MER", Direction.Bottom, 7, 4, 'E', "MERE", 4, 4)]
        public void TestPlay(string word, Direction direction, int row, int column, char letter, string newWord, int newRow, int newColumn)
        {
            var part = new WordPart(word, new Cell(4, 4), direction);
            var played = part.Play(new Cell(row, column), letter);
            Assert.AreEqual(newWord, played.Word);
            Assert.AreEqual(new Cell(newRow, newColumn), played.First);
            Assert.AreEqual(direction, played.Direction);
        }
        [Test] public void TestPlayUnrelated()
        {
            var part = new WordPart("MER", new Cell(4, 4), Direction.Right);
            var played = part.Play(new Cell(0, 0), 'E');
            Assert.AreEqual(part, played);
        }
        [Test] public void TestMerge()
        {
            var part = new WordPart("MER", new Cell(4, 4), Direction.Right);
            Assert.Throws<ArgumentException>(() => part.Merge(new WordPart("RE", new Cell(0, 0), Direction.Right)));
            Assert.Throws<ArgumentException>(() => part.Merge(new WordPart("RE", new Cell(4, 6), Direction.Bottom)));
            Assert.Throws<ArgumentException>(() => part.Merge(new WordPart("ER", new Cell(4, 6), Direction.Right)));
            var merge = part.Merge(new WordPart("RE", new Cell(4, 6), Direction.Right));
            Assert.AreEqual(part.First, merge.First);
            Assert.AreEqual("MERE", merge.Word);
            Assert.AreEqual(Direction.Right, merge.Direction);
        }
    }
    [TestFixture]
    public class WordPartCollectionTest
    {
        [Test] public void TestPlaySimple()
        {
            var list = new List<WordPart>();
            list.Add(new WordPart("MOT", new Cell(4, 4), Direction.Right));
            var parts = new WordPartCollection(list.ToConstant());
            var newParts = parts.Play(new Cell(4, 7), 'S');
            Assert.AreEqual(1, newParts.Count);
            Assert.AreEqual("MOTS", newParts[0].Word);
            Assert.AreEqual(new Cell(4, 4), newParts[0].First);
            Assert.AreEqual(Direction.Right, newParts[0].Direction);
        }
        [Test] public void TestPlayBetween()
        {
            var list = new List<WordPart>();
            list.Add(new WordPart("LET", new Cell(4, 0), Direction.Right));
            list.Add(new WordPart("RE", new Cell(4, 4), Direction.Right));
            var parts = new WordPartCollection(list.ToConstant());
            var played = parts.Play(new Cell(4, 3), 'T');
            Assert.AreEqual(1, played.Count);
            Assert.AreEqual(new WordPart("LETTRE", new Cell(4, 0), Direction.Right), played[0]);
        }
    }
}
