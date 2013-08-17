using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ded.Wordox
{
    class Cell
    {
        #region Fields
        private readonly int row;
        private readonly int column;
        #endregion
        #region Private stuff
        private Cell GetNext(Direction direction)
        {
            switch (direction)
            {
                case Direction.Bottom:
                    if (row == Board.Height - 1)
                        return null;
                    return new Cell(row + 1, column);
                case Direction.Right:
                    if (column == Board.Width - 1)
                        return null;
                    return new Cell(row, column + 1);
                default:
                    throw new ArgumentException(string.Format("Unknown direction : {0}", direction), "direction");
            }
        }
        #endregion
        public Cell(int row, int column)
        {
            if (row < 0
                || column < 0
                || row >= Board.Height
                || column >= Board.Width)
                throw new IndexOutOfRangeException(string.Format("Cannot create cell in {0},{1}", row, column));
            this.row = row;
            this.column = column;
        }
        public int Row { get { return row; } }
        public int Column { get { return column; } }
        public override string ToString()
        {
            return string.Format("{0},{1}", row, column);
        }
        public override int GetHashCode()
        {
            // 463 is 90th prime (90 > 9 x 9)
            return row ^ (column * 463);
        }
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as Cell;
            if (other == null)
                return false;
            return row == other.row && column == other.column;
        }
        public bool TryGetNext(Direction direction, out Cell next)
        {
            next = GetNext(direction);
            if (next != null)
            {
                if (next.row >= Board.Height || next.column >= Board.Width)
                    next = null;
            }
            return next != null;
        }
    }
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
            Assert.Throws<IndexOutOfRangeException>(() => new Cell(row, column));
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
    }
    class Board
    {
        #region Constants
        public const int Height = 9;
        public const int Width = Height;
        internal const int Center = Height / 2;
        #endregion
        #region Fields
        private readonly char[,] board = new char[Height, Width];
        #endregion
        public Board()
        {
        }
        public IEnumerable<Cell> GetStartCells()
        {
            return new[] { new Cell(Center, Center) };
        }
        private bool Contains(ConstantSet<Cell> start, ISet<Cell> played)
        {
            foreach (Cell cell in played)
                if (start.Contains(cell))
                    return true;
            return false;
        }
        public void Play(string word, Cell cell, Direction direction)
        {
            var start = new ConstantSet<Cell>(GetStartCells());
            var cells = new HashSet<Cell>();
            cells.Add(cell);
            var current = cell;
            for (int i = 1; i < word.Length; i++)
            {
                if (!current.TryGetNext(direction, out current))
                    throw new ArgumentException(string.Format("Cannot play {0} at {1}", word, cell));
                cells.Add(current);
            }
            if (!Contains(start, cells))
                throw new ArgumentException(string.Format("Cannot play {0} at {1}", word, cell));
        }
    }
    [TestFixture]
    public class BoardTest
    {
        [Test] public void TestGetStartCells()
        {
            var board = new Board();
            var start = board.GetStartCells().ToList();
            Assert.AreEqual(1, start.Count);
            Assert.AreEqual(Board.Center, start[0].Row);
            Assert.AreEqual(Board.Center, start[0].Column);
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
                    throw new ArgumentException(string.Format("Unknown direction : {0}", direction), "direction");
            }
        }
        [CLSCompliant(false)]
        [Test] public void TestPlayFirst([Values("ME", "MOT", "MOTS", "MOTTE", "MOTTES")] string word,
                                         [Range(0, Board.Height - 1)] int row,
                                         [Range(0, Board.Height - 1)] int column,
                                         [Values(Direction.Bottom, Direction.Right)] Direction direction)
        {
            var board = new Board();
            bool canPlay = CanPlay(word, row, column, direction);
            Action action = () => board.Play(word, new Cell(row, column), direction);
            if (!canPlay)
                Assert.Throws<ArgumentException>(new TestDelegate(action));
            else
                action();
        }
    }
    static class StringExtensions
    {
        public static string Sort(this string s)
        {
            var letters = new List<char>(s);
            letters.Sort();
            return new string(letters.ToArray());
        }
    }
    class AI
    {
        #region Fields
        private readonly WordGraph graph;
        #endregion
        #region Private stuff
        private void FindLongest(string word, HashSet<ValidWord> set, int pending)
        {
            if (pending == 0)
                set.AddRange(Find(word));
            else
                for (int i = 0; i < word.Length; i++)
                    FindLongest(word.Remove(i, 1), set, pending - 1);
        }
        #endregion
        public AI(WordGraph graph)
        {
            this.graph = graph;
        }
        public ConstantSet<ValidWord> Find(string word)
        {
            return graph.GetValids(word);
        }
        public ConstantSet<ValidWord> FindLongest(string word)
        {
            ConstantSet<ValidWord> found = Find(word);
            if (found.Count > 0)
                return found;
            for (int pending = 1; pending <= word.Length - 2; pending++)
            {
                var set = new HashSet<ValidWord>();
                FindLongest(word, set, pending);
                if (set.Count > 0)
                    return set.ToConstant();
            }
            return new ConstantSet<ValidWord>();
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
}
