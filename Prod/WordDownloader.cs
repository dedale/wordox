using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace Ded.Wordox
{
    class WordContent
    {
        #region Fields
        private readonly int count;
        private readonly ConstantSet<string> words;
        #endregion
        public WordContent(int count, ConstantSet<string> words)
        {
            this.count = count;
            this.words = words;
        }
        public int Count { get { return count; } }
        public ConstantSet<string> Words { get { return words; } }
        public bool IsFull
        {
            get { return count == words.Count; }
        }
    }
    class WordDownloader
    {
        #region Fields
        private readonly WebClient wc;
        private readonly HashSet<string> allWords;
        #endregion
        #region Private stuff
        internal static int GetCount(string content)
        {
            const string one = "Il y a un seul mot";
            if (content.IndexOf(one) > -1)
                return 1;
            const string prefix = "Il y a ";
            const string suffix = " mots";
            int prefixPos = content.IndexOf(prefix);
            if (prefixPos == -1)
                throw new FormatException("Word count prefix not found");
            int suffixPos = content.IndexOf(suffix, prefixPos);
            if (suffixPos == -1)
                throw new FormatException("Word count suffix not found");
            int start = prefixPos + prefix.Length;
            return int.Parse(content.Substring(start, suffixPos - start));
        }
        internal static ConstantSet<string> GetWords(string content)
        {
            const string prefix = "<span class='mot'>";
            const string suffix = "</span>";
            int prefixPos = content.IndexOf(prefix);
            if (prefixPos == -1)
                throw new FormatException("Word list prefix not found");
            int suffixPos = content.IndexOf(suffix, prefixPos);
            if (suffixPos == -1)
                throw new FormatException("Word list suffix not found");
            int start = prefixPos + prefix.Length;
            var wordContent = content.Substring(start, suffixPos - start);
            var wordList = Regex.Split(wordContent, @"\s");
            return new ConstantSet<string>(from w in wordList where !string.IsNullOrEmpty(w) select w);
        }
        internal static WordContent ParseContent(string content)
        {
            int count = GetCount(content);
            ConstantSet<string> words = GetWords(content);
            return new WordContent(count, words);
        }
        private void GetAllWords(int length, char first, char second)
        {
            const string SecondAddressFormat = "http://www.listesdemots.fr/d/{1}/2/mots{0}lettresdebutant{1}{2}.htm";
            string content = null;
            try
            {
                content = wc.DownloadString(string.Format(SecondAddressFormat, length, first, second));
            }
            catch (WebException)
            {
            }
            if (content != null)
            {
                WordContent parsed = ParseContent(content);
                if (parsed.IsFull)
                    allWords.AddRange(parsed.Words);
                else
                    throw new NotImplementedException();
                Console.WriteLine(length + first.ToString() + second.ToString());
            }
        }
        private void GetAllWords(int length, char first)
        {
            const string FirstAddressFormat = "http://www.listesdemots.fr/d/{1}/1/mots{0}lettresdebutant{1}.htm";
            string content = null;
            try
            {
                content = wc.DownloadString(string.Format(FirstAddressFormat, length, first));
            }
            catch (WebException)
            {
            }
            if (content != null)
            {
                WordContent parsed = ParseContent(content);
                if (parsed.IsFull)
                    allWords.AddRange(parsed.Words);
                else
                    for (char second = 'A'; second <= 'Z'; second++)
                        GetAllWords(length, first, second);
                Console.WriteLine(length + first.ToString());
            }
        }
        private void GetAllWords(int length)
        {
            const string AllWordsAddressFormat = "http://www.listesdemots.fr/mots{0}lettres.htm";
            string content = null;
            try
            {
                content = wc.DownloadString(string.Format(AllWordsAddressFormat, length));
            }
            catch (WebException)
            {
            }
            if (content != null)
            {
                WordContent parsed = ParseContent(content);
                if (parsed.IsFull)
                    allWords.AddRange(parsed.Words);
                else
                    for (char first = 'A'; first <= 'Z'; first++)
                        GetAllWords(length, first);
                Console.WriteLine(length);
            }
        }
        #endregion
        public WordDownloader()
        {
            wc = new WebClient();
            allWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int length = 2; length <= 9; length++)
                GetAllWords(length);
        }
        public ISet<string> AllWords { get { return allWords; } }
    }
}
