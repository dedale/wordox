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
        private static void TestSort(PlayInfoComparer comparer, PlayInfo first, PlayInfo second, int expected)
        {
            if (expected != 0)
            {
                var list = new List<PlayInfo> { first, second };
                list.Sort(comparer);
                list.Reverse();
                Assert.IsTrue(ReferenceEquals(expected < 0 ? second : first, list[0]));
                Assert.IsTrue(ReferenceEquals(expected < 0 ? first : second, list[1]));
            }
        }
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
                    TestSort(comparer, won, other, 1);
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
                            TestSort(comparer, first, second, expected);
                        }
        }
        [TestCase(0, 0, 0)]
        [TestCase(0, 1, -1)]
        [TestCase(1, 0, 1)]
        [TestCase(1, 1, 0)]
        public void TestCompareMaxStars(int s1, int s2, int expected)
        {
            var firstScore = new Score(new PlayerScore(10), new PlayerScore(15, s1));
            var secondScore = new Score(new PlayerScore(10), new PlayerScore(15, s2));
            foreach (ScoreStrategy scoreStrategy in Enum.GetValues(typeof(ScoreStrategy)))
                foreach (WordStrategy wordStrategy in Enum.GetValues(typeof(WordStrategy)))
                    foreach (bool vortex in new[] { false, true })
                        foreach (bool oneFixes in new[] { false, true })
                            foreach (bool twoMoreFixes in new[] { false, true })
                            {
                                var comparer = new PlayInfoComparer(scoreStrategy, wordStrategy);
                                var first = new PlayInfo(vortex, firstScore, oneFixes, twoMoreFixes);
                                var second = new PlayInfo(vortex, secondScore, oneFixes, twoMoreFixes);
                                Assert.AreEqual(expected, comparer.Compare(first, second));
                                Assert.AreEqual(-expected, comparer.Compare(second, first));
                                TestSort(comparer, first, second, expected);
                            }
        }
        [Test] public void TestCompareWordsNone([Values(false, true)] bool one1,
                                                [Values(false, true)] bool two1,
                                                [Values(false, true)] bool one2,
                                                [Values(false, true)] bool two2)
        {
            var score = new Score(new PlayerScore(10), new PlayerScore(15));
            foreach (bool vortex1 in new[] { false, true })
                foreach (bool vortex2 in new[] { false, true })
                    foreach (ScoreStrategy scoreStrategy in Enum.GetValues(typeof(ScoreStrategy)))
                    {
                        var comparer = new PlayInfoComparer(scoreStrategy, WordStrategy.None);
                        var first = new PlayInfo(vortex1, score, one1, two1);
                        var second = new PlayInfo(vortex2, score, one2, two2);
                        Assert.AreEqual(0, comparer.Compare(first, second));
                        Assert.AreEqual(0, comparer.Compare(second, first));
                        TestSort(comparer, first, second, 0);
                    }
        }
        private static int GetExpected(bool vortex1, bool vortex2, bool first, bool second)
        {
            if (!vortex1 && !vortex2)
            {
                if (first ^ second)
                    return first ? -1 : 1;
                return 0;
            }
            if (vortex1 ^ vortex2)
                return vortex1 ? 1 : -1;
            return 0;
        }
        [Test] public void TestCompareWordsNoOneFixes([Values(false, true)] bool vortex1,
                                                      [Values(false, true)] bool vortex2,
                                                      [Values(false, true)] bool one1,
                                                      [Values(false, true)] bool two1,
                                                      [Values(false, true)] bool one2,
                                                      [Values(false, true)] bool two2)
        {
            var score = new Score(new PlayerScore(10), new PlayerScore(15));
            foreach (ScoreStrategy scoreStrategy in Enum.GetValues(typeof(ScoreStrategy)))
            {
                var comparer = new PlayInfoComparer(scoreStrategy, WordStrategy.NoOneFixes);
                var first = new PlayInfo(vortex1, score, one1, two1);
                var second = new PlayInfo(vortex2, score, one2, two2);
                int expected = GetExpected(vortex1, vortex2, one1, one2);
                Assert.AreEqual(expected, comparer.Compare(first, second));
                Assert.AreEqual(-expected, comparer.Compare(second, first));
                TestSort(comparer, first, second, expected);
            }
        }
        [Test] public void TestCompareWordsNoTwoMoreFixes([Values(false, true)] bool vortex1,
                                                          [Values(false, true)] bool vortex2,
                                                          [Values(false, true)] bool one1,
                                                          [Values(false, true)] bool two1,
                                                          [Values(false, true)] bool one2,
                                                          [Values(false, true)] bool two2)
        {
            var score = new Score(new PlayerScore(10), new PlayerScore(15));
            foreach (ScoreStrategy scoreStrategy in Enum.GetValues(typeof(ScoreStrategy)))
            {
                var comparer = new PlayInfoComparer(scoreStrategy, WordStrategy.NoTwoMoreFixes);
                var first = new PlayInfo(vortex1, score, one1, two1);
                var second = new PlayInfo(vortex2, score, one2, two2);
                int expected = GetExpected(vortex1, vortex2, two1, two2);
                Assert.AreEqual(expected, comparer.Compare(first, second));
                Assert.AreEqual(-expected, comparer.Compare(second, first));
                TestSort(comparer, first, second, expected);
            }
        }
        [Test] public void TestCompareWordsNoFixes([Values(false, true)] bool vortex1,
                                                   [Values(false, true)] bool vortex2,
                                                   [Values(false, true)] bool one1,
                                                   [Values(false, true)] bool two1,
                                                   [Values(false, true)] bool one2,
                                                   [Values(false, true)] bool two2)
        {
            var score = new Score(new PlayerScore(10), new PlayerScore(15));
            foreach (ScoreStrategy scoreStrategy in Enum.GetValues(typeof(ScoreStrategy)))
            {
                var comparer = new PlayInfoComparer(scoreStrategy, WordStrategy.NoFixes);
                var first = new PlayInfo(vortex1, score, one1, two1);
                var second = new PlayInfo(vortex2, score, one2, two2);
                int expected = GetExpected(vortex1, vortex2, one1 || two1, one2 || two2);
                Assert.AreEqual(expected, comparer.Compare(first, second));
                Assert.AreEqual(-expected, comparer.Compare(second, first));
                TestSort(comparer, first, second, expected);
            }
        }
    }
}
