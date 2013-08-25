using NUnit.Framework;
using System;
using System.IO;
using System.Linq;

namespace Ded.Wordox
{
    [TestFixture]
    public class WordDownloaderTest
    {
        [Test] public void TestGetCount()
        {
            Assert.Throws<FormatException>(() => WordDownloader.GetCount(string.Empty));
            int count = WordDownloader.GetCount("Il y a 2 mots");
            Assert.AreEqual(2, count);
            int one = WordDownloader.GetCount("Il y a un seul mot... mots");
            Assert.AreEqual(1, one);
        }
        [Test] public void TestGetWords()
        {
            Assert.Throws<FormatException>(() => WordDownloader.GetWords(string.Empty));
            Assert.Throws<FormatException>(() => WordDownloader.GetWords("<span class='mot'>"));
            var words = WordDownloader.GetWords("<span class='mot'>UN DEUX</span>");
            Assert.AreEqual(2, words.Count);
            Assert.IsTrue(words.Contains("UN"));
            Assert.IsTrue(words.Contains("DEUX"));
        }
        [Test] public void TestParseContent()
        {
            Assert.Throws<FormatException>(() => WordDownloader.ParseContent(string.Empty));
            Assert.Throws<FormatException>(() => WordDownloader.ParseContent("<span class='mot'>"));
            Assert.Throws<FormatException>(() => WordDownloader.ParseContent("<span class='mot'></span>"));
            var wc = WordDownloader.ParseContent("Il y a 2 mots <span class='mot'>UN DEUX</span>");
            Assert.AreEqual(2, wc.Count);
            Assert.AreEqual(2, wc.Words.Count);
            Assert.IsTrue(wc.Words.Contains("UN"));
            Assert.IsTrue(wc.Words.Contains("DEUX"));
            Assert.IsTrue(wc.IsFull);
        }
        [Test] public void TestParseContent6()
        {
            const string ResourcePath = "Ded.Wordox.Resources.";
            var resources = new AssemblyResources(typeof(WordDownloaderTest), ResourcePath);
            string content = resources.GetContent("mots6lettres.htm");
            WordContent wc = WordDownloader.ParseContent(content);
            Assert.AreEqual(17318, wc.Count);
            Assert.AreEqual(17318 - 10175, wc.Words.Count);
            Assert.IsFalse(wc.IsFull);
            foreach (string w in wc.Words)
                Assert.AreEqual(6, w.Length);
        }
        [Test] public void TestParseContent7A()
        {
            const string ResourcePath = "Ded.Wordox.Resources.";
            var resources = new AssemblyResources(typeof(WordDownloaderTest), ResourcePath);
            string content = resources.GetContent("mots7lettresdebutanta.htm");
            WordContent wc = WordDownloader.ParseContent(content);
            Assert.AreEqual(2401, wc.Count);
            Assert.AreEqual(2401, wc.Words.Count);
            Assert.IsTrue(wc.IsFull);
            foreach (string w in wc.Words)
                Assert.AreEqual(7, w.Length);
        }
        [Test] public void TestParseContentCN()
        {
            const string ResourcePath = "Ded.Wordox.Resources.";
            var resources = new AssemblyResources(typeof(WordDownloaderTest), ResourcePath);
            string content = resources.GetContent("mots9lettresdebutantcn.htm");
            WordContent wc = WordDownloader.ParseContent(content);
            Assert.AreEqual(1, wc.Count);
            Assert.AreEqual(1, wc.Words.Count);
            Assert.IsTrue(wc.IsFull);
            foreach (string w in wc.Words)
                Assert.AreEqual(9, w.Length);
        }
    }
    [TestFixture]
    public class English
    {
        private static int DicoCompare(string first, string second)
        {
            int result = first.Length.CompareTo(second.Length);
            if (result == 0)
                result = first.CompareTo(second);
            return result;
        }
        [Test] public void TestUnraw()
        {
            const string rawPath = @"D:\home\prog\git\hub\dedale\wordox\Prod\Resources\en-raw.txt";
            const string dicoPath = @"D:\home\prog\git\hub\dedale\wordox\Prod\Resources\en.txt";
            if (!File.Exists(dicoPath))
            {
                var rawLines = File.ReadAllLines(rawPath);
                var up = (from l in rawLines select l.ToUpperInvariant()).ToList();
                up.Sort(DicoCompare);
                File.WriteAllLines(dicoPath, up.ToArray());
            }
        }
    }
}

