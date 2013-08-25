using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ded.Wordox
{
    [TestFixture]
    public class ShellTest
    {
        [Test] public void TestGetPath()
        {
            var board = new Board();
            var part1 = new WordPart("PUIS", new Cell(4, 4), Direction.Down);
            board = board.Play(part1);
            var part2 = new WordPart("PIED", new Cell(4, 4), Direction.Right);
            var path2 = Shell.GetPath(board, new Rack("PIEDQZ"), part2);
            Assert.AreEqual(3, path2.Played.Count);
            Assert.AreEqual(0, path2.Extras.Count);
            board = board.Play(path2);
            Assert.AreEqual(new PlayerScore(3), board.Score.Current);
            Assert.AreEqual(new PlayerScore(4), board.Score.Other);
        }
        [Test] public void TestGetNewRackNull()
        {
            var graph = WordGraph.French;
            Assert.IsNull(Shell.GetNewRack(graph, new List<char>(), null));
        }
        [TestCase("A")]
        [TestCase("AB")]
        [TestCase("ABC")]
        [TestCase("ABCD")]
        [TestCase("ABCDE")]
        public void TestGetNewRackRandom(string current)
        {
            var graph = WordGraph.French;
            var list = new List<char>();
            list.AddRange(current);
            string rack = Shell.GetNewRack(graph, list, "*");
            Assert.IsTrue(Rack.Check(rack));
        }
        [TestCase("", "ABCDE", false)]
        [TestCase("", "ABCDEF", true)]
        [TestCase("", "ABCDEFG", false)]
        [TestCase("ABC", "DE", false)]
        [TestCase("ABC", "DEF", true)]
        [TestCase("ABC", "DEFG", false)]
        [TestCase("ABC", "ABCDEF", false)]
        public void TestAppend(string current, string append, bool ok)
        {
            var graph = WordGraph.French;
            var list = new List<char>();
            list.AddRange(current);
            string rack = Shell.GetNewRack(graph, list, append);
            Assert.AreEqual(ok, Rack.Check(rack));
        }
        [Test] public void TestGetPathArgumentException()
        {
            var board = new Board();
            var part1 = new WordPart("YEWS", new Cell(4, 4), Direction.Right);
            board = board.Play(part1);
            var rack = new Rack("ESZZZZ");
            PlayPath path2 = Shell.GetPath(board, rack, "YES", 4, 4, null);
            Assert.IsNotNull(path2);
        }
    }
}
