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
            var part = new WordPart(word, new Cell(4, 3), Direction.Right);
            var score = PlayerScore.Play(part);
            Assert.AreEqual(points, score.Points);
            Assert.AreEqual(stars, score.Stars);
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
            var part1 = new WordPart("LETTRE", new Cell(4, 2), Direction.Right);
            var first = score.Play(part1);
            var part2 = new WordPart("MOTEUR", new Cell(1, 8), Direction.Bottom);
            var played = LetterPlayTest.GetPlayed(part2);
            var extra = part1.Play(new Cell(4, 8), 'E');
            var play = new PlayPath(part2, new WordPartCollection(extra), played, new ConstantSet<char>());
            var second = first.Play(play);
            Assert.AreEqual(new PlayerScore(0, 0), second.Current);
            Assert.AreEqual(new PlayerScore(12, 1), second.Other);
        }
        [Test] public void TestPlaySecondSameDirection()
        {
            var score = new Score();
            var part1 = new WordPart("ET", new Cell(4, 3), Direction.Right);
            var first = score.Play(part1);
            var part2 = new WordPart("LETTRE", new Cell(4, 2), Direction.Right);
            var played = LetterPlayTest.GetPlayed(part2, 1, 2);
            var play = new PlayPath(part2, new WordPartCollection(), played, new ConstantSet<char>());
            var second = first.Play(play);
            Assert.AreEqual(new PlayerScore(0, 0), second.Current);
            Assert.AreEqual(new PlayerScore(6, 0), second.Other);
        }
        [Test] public void TestPlayVortexAddStars()
        {
            var score = new Score();
            var part1 = new WordPart("LETTRE", new Cell(4, 2), Direction.Right);
            var first = score.Play(part1);
            var part2 = new WordPart("JOUES", new Cell(0, 8), Direction.Bottom);
            var played = LetterPlayTest.GetPlayed(part2);
            var extra = part1.Play(new Cell(4, 8), 'S');
            var play = new PlayPath(part2, new WordPartCollection(extra), played.ToConstant(), new ConstantSet<char>());
            var second = first.Play(play);
            Assert.AreEqual(new PlayerScore(0, 0), second.Current);
            Assert.AreEqual(new PlayerScore(12, 0), second.Other);
        }
        [Test] public void TestPlayVortexResetStars()
        {
            var scores = new List<Score>();
            scores.Add(new Score());
            var parts = new List<WordPart>();
            parts.Add(new WordPart("ET", new Cell(4, 3), Direction.Right));
            scores.Add(scores[scores.Count - 1].Play(parts[parts.Count - 1]));
            var plays = new List<PlayPath>();
            parts.Add(new WordPart("LETTRE", new Cell(4, 2), Direction.Right));
            plays.Add(new PlayPath(parts[parts.Count - 1], new WordPartCollection(), LetterPlayTest.GetPlayed(parts[parts.Count - 1], 1, 2), new ConstantSet<char>()));
            scores.Add(scores[scores.Count - 1].Play(plays[plays.Count - 1]));
            parts.Add(new WordPart("LETTRES", new Cell(4, 2), Direction.Right));
            plays.Add(new PlayPath(parts[parts.Count - 1], new WordPartCollection(), LetterPlayTest.GetPlayed(parts[parts.Count - 1], 0, 1, 2, 3, 4, 5), new ConstantSet<char>()));
            scores.Add(scores[scores.Count - 1].Play(plays[plays.Count - 1]));
            Assert.AreEqual(new PlayerScore(7, 1), scores[scores.Count - 1].Other);
            Assert.AreEqual(new PlayerScore(), scores[scores.Count - 1].Current);
            parts.Add(new WordPart("JOUES", new Cell(0, 8), Direction.Bottom));
            plays.Add(new PlayPath(parts[parts.Count - 1], new WordPartCollection(), LetterPlayTest.GetPlayed(parts[parts.Count - 1], 4), new ConstantSet<char>()));
            scores.Add(scores[scores.Count - 1].Play(plays[plays.Count - 1]));
            Assert.AreEqual(new PlayerScore(5), scores[scores.Count - 1].Other);
            Assert.AreEqual(new PlayerScore(6), scores[scores.Count - 1].Current);
        }
    }
}
