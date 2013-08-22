using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ded.Wordox
{
    [TestFixture]
    public class FixEdgeTest
    {
        [TestCase(Fix.None)]
        [TestCase(Fix.All)]
        public void TestNew(Fix fixes)
        {
            Assert.Throws<ArgumentException>(() => new FixEdge('L', fixes, new WordVertex("W", false)));
        }
    }
    [TestFixture]
    public class WordGraphTest
    {
        [Test] public void TestNewSorted()
        {
            Assert.Throws<ArgumentException>(() => new WordGraph(new[] { "ABC", "AB", "A" }));
        }
        [Test] public void TestNewCompletion()
        {
            var words = new[] { "MER", "AMER", "MERE", "AMERE" };
            var graph = new WordGraph(words);
            var map = new Dictionary<string, WordVertex>();
            foreach (WordVertex vertex in graph.Vertices)
                map.Add(vertex.Word, vertex);
            foreach (string word in words)
                Assert.IsTrue(map.ContainsKey(word));
            var valid = new HashSet<string>(words);
            foreach (WordVertex vertex in graph.Vertices)
                Assert.AreEqual(valid.Contains(vertex.Word), vertex.IsValid);
            var mer = map["MER"];
            var merEdges = mer.Edges.ToList();
            Assert.AreEqual(2, merEdges.Count);
            foreach (FixEdge edge in merEdges)
            {
                switch (edge.Vertex.Word)
                {
                    case "MERE":
                        Assert.AreEqual(Fix.Suffix, edge.Fix);
                        Assert.AreEqual('E', edge.Letter);
                        break;
                    case "AMER":
                        Assert.AreEqual(Fix.Prefix, edge.Fix);
                        Assert.AreEqual('A', edge.Letter);
                        break;
                    default:
                        throw new InvalidOperationException();
                }
            }
        }
        [Test] public void TestFrench()
        {
            var graph = WordGraph.French;
            Assert.AreEqual(543714, graph.Count);
            int valid = 0;
            foreach (WordVertex vertex in graph.Vertices)
                if (vertex.IsValid)
                    valid++;
            Assert.AreEqual(163028, valid);
        }
        [Test] public void TestGetValids()
        {
            var graph = new WordGraph(new[] { "ET", "TE", "TA", "EN" });
            var valids = graph.GetValids("ET").ToDictionary(vw => vw.Word);
            Assert.AreEqual(2, valids.Count);
            Assert.IsTrue(valids.ContainsKey("ET"));
            Assert.IsTrue(valids.ContainsKey("TE"));
        }
        [TestCase("MER", Fix.All, Fix.None)]
        [TestCase("AMER", Fix.Suffix, Fix.None)]
        [TestCase("MERE", Fix.Prefix, Fix.None)]
        [TestCase("AMERE", Fix.None, Fix.None)]
        [TestCase("RA", Fix.None, Fix.Suffix)]
        [TestCase("MI", Fix.None, Fix.Prefix)]
        [TestCase("TI", Fix.None, Fix.All)]
        public void TestGetValids(string word, Fix one, Fix twoMore)
        {
            var graph = new WordGraph(new[] { "MI", "RA", "TI", "MER", "AMER", "MERE", "RAMI", "TIRE", "YETI", "AMERE" });
            var valids = graph.GetValids(word).ToList();
            Assert.AreEqual(1, valids.Count);
            Assert.AreEqual(word, valids[0].Word);
            Assert.AreEqual(one, valids[0].OneFixes);
            Assert.AreEqual(twoMore, valids[0].TwoMoreFixes);
        }
        [Test] public void TestGetLetters()
        {
            var graph = WordGraph.French;
            var letters = graph.GetLetters("KIW", Fix.Suffix);
            Assert.AreEqual(1, letters.Count);
            Assert.AreEqual('I', letters.ToList()[0]);
        }
        [TestCase("LETTREE", true)]
        [TestCase("ELETTRE", false)]
        public void TestIsValid(string word, bool valid)
        {
            var graph = WordGraph.French;
            Assert.AreEqual(valid, graph.IsValid(word));
        }
        [TestCase(1e-10, 'A')]
        [TestCase(1 - 1e-10, 'Z')]
        public void TestGetLetter(double d, char letter)
        {
            var graph = WordGraph.French;
            Assert.AreEqual(letter, graph.GetLetter(d));
        }
        [TestCase("P")]
        [TestCase("PE")]
        [TestCase("PEN")]
        [TestCase("PEND")]
        [TestCase("PENDI")]
        [TestCase("PENDIN")]
        public void TestGetRandomAppend(string pending)
        {
            var graph = WordGraph.French;
            for (int i = 0; i < 10000; i++)
                Assert.AreEqual(pending, graph.GetRandom(pending).Substring(0, pending.Length));
        }
        [Test] public void TestGetRandom()
        {
            var graph = WordGraph.French;
            for (int i = 0; i < 10000; i++)
                Assert.AreEqual(Rack.Size, graph.GetRandom().Length);
        }
    }
    [TestFixture]
    public class WordVertexTest
    {
        //[Test] public void TestOneFixes()
        //{
        //    var graph = new WordGraph(new[] { "MOT", "MOTS", "DEUX", "MER", "AMER", "MERE", "NE", "ANE" });
        //    Assert.AreEqual(Fix.None, graph.GetOneFixes("MOTS"));
        //    Assert.AreEqual(Fix.Suffix, graph.GetOneFixes("MOT"));
        //    Assert.AreEqual(Fix.Prefix, graph.GetOneFixes("NE"));
        //    Assert.AreEqual(Fix.All, graph.GetOneFixes("MER"));
        //}
    }
    [TestFixture]
    public class ValidWordTest
    {
        private static void TestSort(ValidWordComparer comparer, ValidWord x, ValidWord y, int expected)
        {
            if (expected != 0)
            {
                var list = new List<ValidWord> { x, y };
                list.Sort(comparer);
                list.Reverse();
                Assert.IsTrue(ReferenceEquals(expected < 0 ? y : x, list[0]));
                Assert.IsTrue(ReferenceEquals(expected < 0 ? x : y, list[1]));
            }
        }
        [TestCase("MOT", "MOTS", -1)]
        [TestCase("MOT", "MOT", 0)]
        public void TestCompareLengthSameFixes(string first, string second, int expected)
        {
            foreach (WordStrategy strategy in Enum.GetValues(typeof(WordStrategy)))
                foreach (Fix one in Enum.GetValues(typeof(Fix)))
                    foreach (Fix two in Enum.GetValues(typeof(Fix)))
                    {
                        var comparer = new ValidWordComparer(strategy);
                        var x = new ValidWord(first, one, two);
                        var y = new ValidWord(second, one, two);
                        Assert.AreEqual(expected, comparer.Compare(x, y));
                        Assert.AreEqual(-expected, comparer.Compare(y, x));
                        TestSort(comparer, x, y, expected);
                    }
        }
        [CLSCompliant(false)]
        [TestCase("MOT", Fix.None, "MOT", Fix.None, 0)]
        [TestCase("MOT", Fix.None, "MOTS", Fix.None, -1)]
        [TestCase("MOT", Fix.None, "MOTS", Fix.Prefix, 1)]
        public void TestCompareNoOneFixes(string w1, Fix f1, string w2, Fix f2, int expected)
        {
            foreach (Fix two1 in Enum.GetValues(typeof(Fix)))
                foreach (Fix two2 in Enum.GetValues(typeof(Fix)))
                {
                    var comparer = new ValidWordComparer(WordStrategy.NoOneFixes);
                    var x = new ValidWord(w1, f1, two1);
                    var y = new ValidWord(w2, f2, two2);
                    Assert.AreEqual(expected, comparer.Compare(x, y));
                    Assert.AreEqual(-expected, comparer.Compare(y, x));
                    TestSort(comparer, x, y, expected);
                }
        }
    }
}
