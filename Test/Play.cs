using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ded.Wordox
{
    [TestFixture]
    public class PlayInfoTest
    {
        [Test] public void TestCompareToSame([Values(false, true)] bool vortex,
                                             [Values(19, 20)] int p1,
                                             [Values(0, 1)] int s1,
                                             [Values(24, 25)] int p2,
                                             [Values(0, 1)] int s2,
                                             [Values(false, true)] bool oneFixes,
                                             [Values(false, true)] bool twoMoreFixes)
        {
            var score = new Score(new PlayerScore(p1, s1), new PlayerScore(p2, s2));
            var info = new PlayInfo(vortex, score, oneFixes, twoMoreFixes);
            Assert.AreEqual(0, info.CompareTo(info));
        }
        [Test] public void TestCompareToWinsFirst()
        {
            var wonScore = new Score(new PlayerScore(20), new PlayerScore(25));
            var won = new PlayInfo(false, wonScore, false, false);
            var otherScore = new Score(new PlayerScore(20), new PlayerScore(24));
            var other = new PlayInfo(false, otherScore, false, false);
            Assert.Less(0, won.CompareTo(other));
            Assert.Greater(0, other.CompareTo(won));
        }
    }
    [TestFixture]
    public class PlayInfoComparerTest
    {
        [Test] public void TestCompareWinsFirst()
        {
            foreach (ScoreStrategy score in Enum.GetValues(typeof(ScoreStrategy)))
            {
                foreach (WordStrategy word in Enum.GetValues(typeof(WordStrategy)))
                {
                    var wonScore = new Score(new PlayerScore(20), new PlayerScore(25));
                    var won = new PlayInfo(false, wonScore, false, false);
                    var otherScore = new Score(new PlayerScore(20), new PlayerScore(24));
                    var other = new PlayInfo(false, otherScore, false, false);
                    var comparer = new PlayInfoComparer(score, word);
                    Assert.Less(0, comparer.Compare(won, other));
                    Assert.Greater(0, comparer.Compare(other, won));
                }
            }
        }
        [CLSCompliant(false)]
        [TestCase(10, 20, 10, 20, ScoreStrategy.None, 0)]
        [TestCase(10, 20, 10, 20, ScoreStrategy.MaxDiff, 0)]
        [TestCase(10, 20, 10, 20, ScoreStrategy.MaxPoints, 0)]
        [TestCase(10, 21, 10, 20, ScoreStrategy.None, 0)]
        [TestCase(10, 21, 5, 20, ScoreStrategy.None, 0)]
        [TestCase(10, 21, 10, 20, ScoreStrategy.MaxDiff, 1)]
        [TestCase(10, 20, 19, 21, ScoreStrategy.MaxDiff, 1)]
        [TestCase(10, 21, 10, 20, ScoreStrategy.MaxPoints, 1)]
        [TestCase(19, 21, 10, 20, ScoreStrategy.MaxPoints, 1)]
        public void TestCompareScores(int c1, int p1, int c2, int p2, ScoreStrategy strategy, int expected)
        {
            var current = new PlayerScore(10);
            var firstScore = new Score(new PlayerScore(c1), new PlayerScore(p1));
            var secondScore = new Score(new PlayerScore(c2), new PlayerScore(p2));
            foreach (bool vortex in new[] { false, true })
                foreach (WordStrategy word in Enum.GetValues(typeof(WordStrategy)))
                    foreach (bool oneFixes in new[] { false, true })
                        foreach (bool twoMoreFixes in new[] { false, true })
                        {
                            var comparer = new PlayInfoComparer(strategy, word);
                            var first = new PlayInfo(vortex, firstScore, oneFixes, twoMoreFixes);
                            var second = new PlayInfo(vortex, secondScore, oneFixes, twoMoreFixes);
                            Assert.AreEqual(expected, comparer.Compare(first, second));
                            Assert.AreEqual(-expected, comparer.Compare(second, first));
                        }
        }
    }
}
