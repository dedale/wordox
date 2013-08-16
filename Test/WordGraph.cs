using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ded.Wordox
{
    enum Fix
    {
        Prefix,
        Suffix
    }
    class FixEdge
    {
        #region Fields
        private readonly char letter;
        private readonly Fix fix;
        private readonly WordVertex vertex;
        #endregion
        public FixEdge(char letter, Fix fix, WordVertex vertex)
        {
            this.letter = letter;
            this.fix = fix;
            this.vertex = vertex;
        }
        public char Letter { get { return letter; } }
        public Fix Fix { get { return fix; } }
        public WordVertex Vertex { get { return vertex; } }
    }
    class WordVertex
    {
        #region Fields
        private readonly string word;
        private readonly bool valid;
        private readonly Dictionary<Fix, Dictionary<char, FixEdge>> edges;
        #endregion
        public WordVertex(string word, bool valid)
        {
            this.word = word;
            this.valid = valid;
            edges = new Dictionary<Fix, Dictionary<char, FixEdge>>();
        }
        public string Word { get { return word; } }
        public bool IsValid { get { return valid; } }
        public void Add(FixEdge edge)
        {
            Dictionary<char, FixEdge> map;
            if (!edges.TryGetValue(edge.Fix, out map))
            {
                map = new Dictionary<char, FixEdge>();
                edges.Add(edge.Fix, map);
            }
            map.Add(edge.Letter, edge);
        }
    }
    class WordGraph
    {
        #region Fields
        private static readonly Lazy<WordGraph> french = new Lazy<WordGraph>(GetFrench);
        private readonly Dictionary<string, HashSet<string>> valids; 
        private readonly Dictionary<string, WordVertex> vertices;
        #endregion
        private static IEnumerable<string> GetAllWords()
        {
            var resources = new AssemblyResources(typeof(WordDownloader), "Ded.Wordox.Resources.");
            string frenchContent = resources.GetContent("fr.txt");
            return frenchContent.Split(new[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        }
        private static WordGraph GetFrench()
        {
            var words = GetAllWords();
            return new WordGraph(words);
        }
        private void AddPrefixEdge(WordVertex vertex)
        {
            string prefix = vertex.Word.Substring(0, vertex.Word.Length - 1);
            WordVertex prefixVertex = GetVertex(prefix, false);
            prefixVertex.Add(new FixEdge(vertex.Word[prefix.Length], Fix.Suffix, vertex));
        }
        private void AddSuffixEdge(WordVertex vertex)
        {
            string suffix = vertex.Word.Substring(1);
            WordVertex suffixVertex = GetVertex(suffix, false);
            suffixVertex.Add(new FixEdge(vertex.Word[0], Fix.Prefix, vertex));
        }
        private void AddEdgesIfNeeded(WordVertex vertex)
        {
            if (vertex.Word.Length == 1)
                return;
            AddPrefixEdge(vertex);
            AddSuffixEdge(vertex);
        }
        private WordVertex GetVertex(string word, bool valid)
        {
            WordVertex vertex;
            if (vertices.TryGetValue(word, out vertex))
                return vertex;
            vertex = new WordVertex(word, valid);
            vertices.Add(word, vertex);
            AddEdgesIfNeeded(vertex);
            return vertex;
        }
        private void AddValid(string word)
        {
            var sorted = word.Sort();
            HashSet<string> set;
            if (!valids.TryGetValue(sorted, out set))
            {
                set = new HashSet<string>();
                valids.Add(sorted, set);
            }
            set.Add(word);
        }
        public WordGraph(IEnumerable<string> words)
        {
            vertices = new Dictionary<string, WordVertex>();
            valids = new Dictionary<string, HashSet<string>>();
            foreach (string word in words)
            {
                GetVertex(word, true);
                AddValid(word);
            }
        }
        public int Count { get { return vertices.Count; } }
        public IEnumerable<WordVertex> Vertices
        {
            get { return vertices.Values; }
        }
        public ConstantSet<string> GetValids(string letters)
        {
            var sorted = letters.Sort();
            HashSet<string> set;
            if (valids.TryGetValue(sorted, out set))
                return set.ToConstant();
            return new ConstantSet<string>();
        }
        public static WordGraph French
        {
            get { return french.Value; }
        }
    }

    [TestFixture]
    public class WordGraphTest
    {
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
            var graph = WordGraph.French;
            var valids = graph.GetValids("ET");
            Assert.AreEqual(2, valids.Count);
            Assert.IsTrue(valids.Contains("ET"));
            Assert.IsTrue(valids.Contains("TE"));
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
