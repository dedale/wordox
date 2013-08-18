using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ded.Wordox
{
    [TestFixture]
    public class PlayerScoreTest
    {
        [TestCase("MOT", 3, 0)]
        [TestCase("LETTRE", 6, 1)]
        public void TestPlay(string word, int points, int stars)
        {
            var score = new PlayerScore();
            var part = new WordPart(word, new Cell(4, 3), Direction.Right);
            var newScore = score.Play(part);
            Assert.AreEqual(points, newScore.Points);
            Assert.AreEqual(stars, newScore.Stars);
        }
        [Test] public void TestGetHashCode()
        {
            Assert.AreNotEqual(new PlayerScore(1, 2).GetHashCode(), new PlayerScore(2, 1).GetHashCode());
        }
    }
    [TestFixture]
    public class ScoreTest
    {
        [Test] public void TestPlayFirst()
        {
            var score = new Score();
            var part = new WordPart("LETTRE", new Cell(4, 3), Direction.Right);
            Score newScore = score.Play(part);
            Assert.AreEqual(new PlayerScore(6, 1), newScore.Other);
            Assert.AreEqual(new PlayerScore(), newScore.Current);
        }
        [Test] public void TestPlaySecondNovice()
        {
            var score = new Score();
            var part1 = new WordPart("LETTRE", new Cell(4, 3), Direction.Right);
            var first = score.Play(part1);
            var part2 = new WordPart("LE", new Cell(4, 3), Direction.Bottom);
            var play = new PlayPath(part2, new LetterPlay(new Cell(5, 3), 'E'), new ConstantSet<char>("MOTUR"));
            var second = first.Play(play);
            Assert.AreEqual(new PlayerScore(5, 1), second.Current);
            Assert.AreEqual(new PlayerScore(2, 0), second.Other);
        }
        [Test] public void TestPlaySecondExpert()
        {
            var score = new Score();
            const string word = "MOTEUR";
            var part1 = new WordPart("LETTRE", new Cell(4, 2), Direction.Right);
            var first = score.Play(part1);
            var part2 = new WordPart(word, new Cell(1, 8), Direction.Bottom);
            var played = new List<LetterPlay>();
            var cell = new Cell(1, 8);
            for (int i = 0; i < word.Length; i++)
            {
                played.Add(new LetterPlay(cell, word[i]));
                if (i < word.Length - 1)
                    cell = cell.Bottom;
            }
            var extra = part1.Play(new Cell(4, 8), 'E');
            var play = new PlayPath(part2, new WordPartCollection(extra), played.ToConstant(), new ConstantSet<char>());
            var second = first.Play(play);
            Assert.AreEqual(new PlayerScore(0, 0), second.Current);
            Assert.AreEqual(new PlayerScore(12, 1), second.Other);
        }
    }
}
