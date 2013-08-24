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
        [Test] public void Test()
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
    }
}
