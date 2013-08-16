using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ded.Wordox
{
    enum Direction
    {
        Right,
        Bottom
    }
    class Cell
    {
        #region Fields
        private readonly int row;
        private readonly int column;
        #endregion
        public Cell(int row, int column)
        {
            this.row = row;
            this.column = column;
        }
        public int Row { get { return row; } }
        public int Column { get { return column; } }
        public override string ToString()
        {
            return string.Format("{0}:{1}", row, column);
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
    }
    [TestFixture]
    public class CellTest
    {
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
    }
    class Board
    {
        #region Constants
        public const int Height = 9;
        public const int Width = Height;
        private const int Center = Height / 2;
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
        public void Play(string word, Cell cell, Direction direction)
        {
            throw new NotImplementedException();
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
        public AI(WordGraph graph)
        {
            this.graph = graph;
        }
        public ConstantSet<string> Find(string word)
        {
            return graph.GetValids(word);
        }
        private void FindLongest(string word, HashSet<string> set, int pending)
        {
            if (pending == 0)
                set.AddRange(Find(word));
            else
                for (int i = 0; i < word.Length; i++)
                    FindLongest(word.Remove(i, 1), set, pending - 1);
        }
        public ConstantSet<string> FindLongest(string word)
        {
            ConstantSet<string> found = Find(word);
            if (found.Count > 0)
                return found;
            for (int pending = 1; pending <= word.Length - 2; pending++)
            {
                var set = new HashSet<string>();
                FindLongest(word, set, pending);
                if (set.Count > 0)
                    return set.ToConstant();
            }
            return new ConstantSet<string>();
        }
    }
    [TestFixture]
    public class AITest
    {
        [Test] public void TestFind()
        {
            var graph = WordGraph.French;
            var ai = new AI(graph);
            var words = ai.Find("OTM").ToList();
            Assert.AreEqual(2, words.Count);
            Assert.IsTrue(words.Contains("MOT"));
            Assert.IsTrue(words.Contains("TOM"));
        }
        private static void Perm(HashSet<string> set, string word, int pending)
        {
            if (pending == 0)
                set.Add(word);
            else
                for (int i = 0; i < word.Length; i++)
                    Perm(set, word.Remove(i, 1), pending - 1);
        }
        [Test] public void TestPerm()
        {
            var word = "1234";
            var set = new HashSet<string>();
            for (int n = 0; n <= word.Length - 2; n++)
                Perm(set, word, n);
            foreach (var s in set)
                Console.WriteLine(s);
        }
        [Test] public void TestFindLongest([Range(0, 3)] int n)
        {
            var graph = WordGraph.French;
            var ai = new AI(graph);
            var letters = "NEZ" + new string('Z', n);
            var words = ai.FindLongest(letters);
            Assert.AreEqual(2, words.Count, "FindLongest failed for " + letters);
            Assert.IsTrue(words.Contains("NEZ"), "NEZ not found for " + letters);
            Assert.IsTrue(words.Contains("ZEN"), "ZEN not found for " + letters);
        }
    }
}
