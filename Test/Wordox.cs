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
    public class GameTest
    {
        #region Private stuff
        private static Board TestPlayIgnoreExtra(IList<Tuple<string, string, Cell, Direction>> plays)
        {
            var graph = WordGraph.French;
            var board = new Board();
            var rack = new Rack(plays[0].Item1);

            var part1 = new WordPart(plays[0].Item2, plays[0].Item3, plays[0].Item4);
            board = board.Play(part1);

            for (int i = 1; i < plays.Count; i++)
            {
                rack = new Rack(plays[i].Item1);
                PlayGraph play = new PlayGraph(graph, board, rack);
                var moveFinder = new MoveFinder(graph, board, play);
                List<Tuple<PlayInfo, PlayPath>> moves = moveFinder.GetAllMoves();
                foreach (Tuple<PlayInfo, PlayPath> move in moves)
                {
                    PlayPath path = move.Item2;
                    if (plays[i].Item2 != path.Main.Word
                        || plays[i].Item3 != path.Main.First
                        || plays[i].Item4 != path.Main.Direction)
                        continue;
                    board = board.Play(path);
                    if (i != plays.Count - 1)
                        continue;
                    Assert.GreaterOrEqual(board.Score.Other.Points, 0);
                    Assert.GreaterOrEqual(board.Score.Current.Points, 0);
                    return board;
                }
            }
            throw new InvalidOperationException("Move not found");
        }
        #endregion
        [Test] public void TestPlay()
        {
            for (int i = 0; i < 100; i++)
            {
                var game = new Game();
                game.Play();
                if (i == 0)
                    break;
            }
        }
        [Test] public void TestFixes1()
        {
            var graph = WordGraph.French;
            var board = new Board();
            var rack = new Rack("ASSDHS");

            var part1 = new WordPart("DAHS", new Cell(4, 4), Direction.Down);
            board = board.Play(part1);

            rack = new Rack("SSEEEA");
            PlayGraph play = new PlayGraph(graph, board, rack);
            var moveFinder = new MoveFinder(graph, board, play);
            List<Tuple<PlayInfo, PlayPath>> moves = moveFinder.GetAllMoves();
            foreach (Tuple<PlayInfo, PlayPath> move in moves)
            {
                if ("ES" != move.Item2.Main.Word
                    || new Cell(7, 5) != move.Item2.Main.First
                    || Direction.Down != move.Item2.Main.Direction)
                    continue;
                Assert.IsTrue(move.Item1.HasFixes);
                Assert.IsTrue(move.Item1.HasTwoMoreFixes);
            }
        }
        [Test] public void TestFixes2()
        {
            var graph = WordGraph.French;
            var board = new Board();
            var rack = new Rack("DELRIC");

            var part1 = new WordPart("DECRI", new Cell(4, 4), Direction.Right);
            board = board.Play(part1);

            rack = new Rack("LTREIG");
            PlayGraph play = new PlayGraph(graph, board, rack);
            var moveFinder = new MoveFinder(graph, board, play);
            List<Tuple<PlayInfo, PlayPath>> moves = moveFinder.GetAllMoves();
            foreach (Tuple<PlayInfo, PlayPath> move in moves)
            {
                if ("GILET" != move.Item2.Main.Word
                    || new Cell(5, 1) != move.Item2.Main.First
                    || Direction.Right != move.Item2.Main.Direction)
                    continue;
                Assert.IsTrue(move.Item1.HasFixes);
                Assert.IsTrue(move.Item1.HasTwoMoreFixes);
            }
        }
        [Test] public void TestPlayIgnoreExtra1()
        {
            var plays = new List<Tuple<string, string, Cell, Direction>>();
            plays.Add(new Tuple<string, string, Cell, Direction>("ASSDHS", "DAHS", new Cell(4, 4), Direction.Down));
            plays.Add(new Tuple<string, string, Cell, Direction>("SSEEEA", "ES", new Cell(7, 5), Direction.Down));
            plays.Add(new Tuple<string, string, Cell, Direction>("SEEAIA", "AIES", new Cell(5, 5), Direction.Down));

            TestPlayIgnoreExtra(plays);
        }
        [Test] public void TestPlayIgnoreExtra2()
        {
            var plays = new List<Tuple<string, string, Cell, Direction>>();
            plays.Add(new Tuple<string, string, Cell, Direction>("LATAEE", "ETALA", new Cell(0, 4), Direction.Down));
            plays.Add(new Tuple<string, string, Cell, Direction>("ESAOIC", "COIS", new Cell(5, 1), Direction.Right));
            plays.Add(new Tuple<string, string, Cell, Direction>("EAEIES", "AAS", new Cell(4, 3), Direction.Right));
            plays.Add(new Tuple<string, string, Cell, Direction>("EEIESA", "SAIES", new Cell(0, 5), Direction.Down));

            TestPlayIgnoreExtra(plays);
        }
        [CLSCompliant(false)]
        [TestCase("IAOUEF", "FOUIE", Fix.Suffix, Fix.Prefix)]
        [TestCase("NUIONS", "NUIONS", Fix.None, Fix.Prefix)]
        public void TestFirstFixes(string letters, string word, Fix one, Fix twoMore)
        {
            var graph = WordGraph.French;
            var rack = new Rack(letters);
            var ai = new AI(graph);
            List<ValidWord> words = ai.FindLongest(rack.Value).ToList();
            foreach (ValidWord valid in words)
            {
                if (valid.Word != word)
                    continue;
                Assert.AreEqual(one, valid.OneFixes);
                Assert.AreEqual(twoMore, valid.TwoMoreFixes);
                break;
            }
        }
    }
}
