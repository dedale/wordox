using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ded.Wordox
{
    [Flags]
    public enum Fix
    {
        None = 0,
        Prefix = 1,
        Suffix = 2,
        All = Prefix | Suffix,
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
            if (fix != Fix.Prefix && fix != Fix.Suffix)
                throw new ArgumentException(string.Format("Invalid fixes : {0}", fix), "fix");
            this.letter = letter;
            this.fix = fix;
            this.vertex = vertex;
        }
        public char Letter { get { return letter; } }
        public Fix Fix { get { return fix; } }
        public WordVertex Vertex { get { return vertex; } }
    }

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
        public Fix Fixes
        {
            get
            {
                var fixes = Fix.None;
                foreach (Fix f in edges.Keys)
                    if (edges[f].Count > 0)
                        fixes |= f;
                return fixes;
            }
        }
        public IEnumerable<FixEdge> Edges
        {
            get
            {
                foreach (var map in edges.Values)
                    foreach (FixEdge edge in map.Values)
                        yield return edge;
            }
        }
        public ConstantSet<char> GetLetters(Fix fix)
        {
            if (fix != Fix.Prefix && fix != Fix.Suffix)
                throw new ArgumentException(string.Format("Bad fix : {0}", fix), "fix");
            Dictionary<char, FixEdge> map;
            if (!edges.TryGetValue(fix, out map))
                return new ConstantSet<char>();
            return new ConstantSet<char>(map.Keys);
        }
    }
    class ValidWord
    {
        #region Fields
        private readonly string word;
        private readonly Fix fixes;
        #endregion
        public ValidWord(string word, Fix fixes)
        {
            this.word = word;
            this.fixes = fixes;
        }
        public string Word { get { return word; } }
        public Fix Fixes { get { return fixes; } }
        public override string ToString()
        {
            return word;
        }
        public override int GetHashCode()
        {
            return word.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as ValidWord;
            if (other == null)
                return false;
            return word == other.word;
        }
    }
    class WordGraph
    {
        #region Fields
        private static readonly Lazy<WordGraph> french = new Lazy<WordGraph>(GetFrench);
        private readonly Dictionary<string, HashSet<string>> valids; 
        private readonly Dictionary<string, WordVertex> vertices;
        #endregion
        #region Private stuff
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
            {
                if (valid)
                    throw new ArgumentException(string.Format("{0} already found but not valid (are words sorted by size?)", word), "valid");
                return vertex;
            }
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
        internal static bool Select(Fix fixes, Fix allowed)
        {
            switch (fixes)
            {
                case Fix.None:
                    return true;
                case Fix.Prefix:
                    return (allowed & Fix.Prefix) == Fix.Prefix;
                case Fix.Suffix:
                    return (allowed & Fix.Suffix) == Fix.Suffix;
                case Fix.All:
                    return allowed == Fix.All;
                default:
                    throw new ArgumentException(string.Format("Unknown {0} fixes", fixes), "fixes");
            }
        }
        #endregion
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
        public ConstantSet<ValidWord> GetValids(string letters)
        {
            var sorted = letters.Sort();
            HashSet<string> set;
            if (valids.TryGetValue(sorted, out set))
            {
                var result = new HashSet<ValidWord>();
                foreach (var word in set)
                    result.Add(new ValidWord(word, vertices[word].Fixes));
                return result.ToConstant();
            }
            return new ConstantSet<ValidWord>();
        }
        public ConstantSet<ValidWord> GetValids(string letters, Fix fixes)
        {
            var sorted = letters.Sort();
            HashSet<string> set;
            if (valids.TryGetValue(sorted, out set))
            {
                var result = new HashSet<ValidWord>();
                foreach (string word in set)
                    if (Select(vertices[word].Fixes, fixes))
                        result.Add(new ValidWord(word, vertices[word].Fixes));
                return result.ToConstant();
            }
            return new ConstantSet<ValidWord>();
        }
        public Fix GetFixes(string word)
        {
            WordVertex vertex;
            if (vertices.TryGetValue(word, out vertex))
                return vertex.Fixes;
            return Fix.None;
        }
        public ConstantSet<char> GetLetters(string part, Fix fix)
        {
            WordVertex vertex;
            if (!vertices.TryGetValue(part, out vertex))
                throw new ArgumentException(string.Format("Unknown word part : {0}", part), "part");
            //if (fix != Fix.Prefix && fix != Fix.Suffix)
            //    throw new ArgumentException(string.Format("Bad fix : {0}", fix), "fix");
            return vertex.GetLetters(fix);
        }
        public bool IsValid(string word)
        {
            if (string.IsNullOrEmpty(word) || word.Length == 1)
                throw new ArgumentException(string.Format("{0} is too short", word), "word");
            WordVertex vertex;
            if (vertices.TryGetValue(word, out vertex))
                return vertex.IsValid;
            return false;
        }
        public static WordGraph French
        {
            get { return french.Value; }
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
