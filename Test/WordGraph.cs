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
        [TestCase("MER", Fix.None, 0)]
        [TestCase("MER", Fix.Prefix, 0)]
        [TestCase("MER", Fix.Suffix, 0)]
        [TestCase("MER", Fix.All, 1)]
        [TestCase("AMER", Fix.None, 0)]
        [TestCase("AMER", Fix.Prefix, 0)]
        [TestCase("AMER", Fix.Suffix, 1)]
        [TestCase("AMER", Fix.All, 1)]
        [TestCase("MERE", Fix.None, 0)]
        [TestCase("MERE", Fix.Prefix, 1)]
        [TestCase("MERE", Fix.Suffix, 0)]
        [TestCase("MERE", Fix.All, 1)]
        [TestCase("AMERE", Fix.None, 1)]
        [TestCase("AMERE", Fix.Prefix, 1)]
        [TestCase("AMERE", Fix.Suffix, 1)]
        [TestCase("AMERE", Fix.All, 1)]
        public void TestGetValidsFixes(string word, Fix fixes, int count)
        {
            var graph = new WordGraph(new[] { "MER", "AMER", "MERE", "AMERE" });
            var valids = graph.GetValids(word, fixes);
            Assert.AreEqual(count, valids.Count);
        }
        [TestCase("MER", Fix.All)]
        [TestCase("MERE", Fix.Prefix)]
        [TestCase("AMER", Fix.Suffix)]
        [TestCase("AMERE", Fix.None)]
        public void TestGetFixes(string word, Fix fixes)
        {
            var graph = new WordGraph(new[] { "MER", "AMER", "MERE", "AMERE" });
            Assert.AreEqual(fixes, graph.GetFixes(word));
        }
        [TestCase(Fix.None, Fix.None, true)]
        [TestCase(Fix.None, Fix.Prefix, true)]
        [TestCase(Fix.None, Fix.Suffix, true)]
        [TestCase(Fix.None, Fix.All, true)]
        [TestCase(Fix.Prefix, Fix.None, false)]
        [TestCase(Fix.Prefix, Fix.Prefix, true)]
        [TestCase(Fix.Prefix, Fix.Suffix, false)]
        [TestCase(Fix.Prefix, Fix.All, true)]
        [TestCase(Fix.Suffix, Fix.None, false)]
        [TestCase(Fix.Suffix, Fix.Prefix, false)]
        [TestCase(Fix.Suffix, Fix.Suffix, true)]
        [TestCase(Fix.Suffix, Fix.All, true)]
        [TestCase(Fix.All, Fix.None, false)]
        [TestCase(Fix.All, Fix.Prefix, false)]
        [TestCase(Fix.All, Fix.Suffix, false)]
        [TestCase(Fix.All, Fix.All, true)]
        public void TestSelect(Fix fixes, Fix allowed, bool selected)
        {
            Assert.AreEqual(selected, WordGraph.Select(fixes, allowed));
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
        public void TestGetRandom(string pending)
        {
            var graph = WordGraph.French;
            for (int i = 0; i < 10000; i++)
                Assert.AreEqual(pending, graph.GetRandom(pending).Substring(0, pending.Length));
        }
    }
    [TestFixture]
    public class WordVertexTest
    {
        [Test] public void Test()
        {
            var m = new WordVertex("M", false);
            var me = new WordVertex("ME", true);
            var edge = new FixEdge('E', Fix.Suffix, me);
            m.Add(edge);
        }
    }
}
