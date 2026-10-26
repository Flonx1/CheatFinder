using System.Collections.Generic;
using System.Text;

namespace CheatFinder_RECODE.Utils
{
    // Aho-Corasick по байтам: ищет все сигнатуры за один проход по буферу,
    // вместо "вытащить строки -> для каждой строки перебрать все сигнатуры".
    internal class PatternMatcher
    {
        private readonly Automaton _exact = new Automaton(false);
        private readonly Automaton _nocase = new Automaton(true);
        private readonly List<string> _names = new List<string>();

        public int Count => _names.Count;
        public string GetName(int id) => _names[id];

        // ignoreCase работает только для ASCII; utf16 добавляет вариант в UTF-16LE (строки Java 8 в 1.16.5)
        // wholeWord: совпадение не должно быть частью другого слова (для коротких имён вроде "Wild", "ZOV")
        public void Add(string pattern, bool ignoreCase = true, bool utf16 = false, bool? wholeWord = null)
        {
            if (string.IsNullOrEmpty(pattern)) return;
            int id = _names.Count;
            _names.Add(pattern);
            bool word = wholeWord ?? pattern.Length <= ShortPatternLength;

            Automaton target = ignoreCase ? _nocase : _exact;
            target.Insert(Encoding.ASCII.GetBytes(pattern), id, 1, word);
            if (utf16) target.Insert(Encoding.Unicode.GetBytes(pattern), id, 2, word);
        }

        public void AddRange(IEnumerable<string> patterns, bool ignoreCase = true, bool utf16 = false, bool? wholeWord = null)
        {
            foreach (var p in patterns) Add(p, ignoreCase, utf16, wholeWord);
        }

        private const int ShortPatternLength = 6;

        // id найденной сигнатуры -> позиция конца первого вхождения
        public Dictionary<int, int> Search(byte[] buffer, int length)
        {
            var found = new Dictionary<int, int>();
            _exact.Search(buffer, length, found);
            _nocase.Search(buffer, length, found);
            return found;
        }

        public Dictionary<int, int> Search(byte[] buffer) => Search(buffer, buffer.Length);

        public Dictionary<int, int> Search(string text) => Search(Encoding.UTF8.GetBytes(text ?? ""));

        // Кусок текста вокруг совпадения для лога
        public static string Context(byte[] buffer, int length, int pos, int radius = 60)
        {
            int start = System.Math.Max(0, pos - radius);
            int end = System.Math.Min(length, pos + radius);
            var sb = new StringBuilder();
            for (int i = start; i < end; i++)
            {
                byte b = buffer[i];
                if (b >= 0x20 && b <= 0x7E) sb.Append((char)b);
                else if (b != 0) sb.Append('.');
            }
            return sb.ToString();
        }

        private class Automaton
        {
            private struct Output
            {
                public int Id;
                public int Length;
                public int CharSize;
                public bool WholeWord;
            }

            private class Node
            {
                public readonly Node[] Next = new Node[256];
                public Node Fail;
                public readonly List<Output> Outputs = new List<Output>();
            }

            private readonly bool _ignoreCase;
            private readonly Node _root = new Node();
            private bool _built;
            private bool _empty = true;

            public Automaton(bool ignoreCase)
            {
                _ignoreCase = ignoreCase;
            }

            public void Insert(byte[] bytes, int id, int charSize, bool wholeWord)
            {
                Node node = _root;
                foreach (byte raw in bytes)
                {
                    byte b = _ignoreCase ? Lower(raw) : raw;
                    if (node.Next[b] == null) node.Next[b] = new Node();
                    node = node.Next[b];
                }
                node.Outputs.Add(new Output { Id = id, Length = bytes.Length, CharSize = charSize, WholeWord = wholeWord });
                _built = false;
                _empty = false;
            }

            // Классическое построение: переходы превращаются в полный автомат (goto для всех 256 байт)
            private void Build()
            {
                var queue = new Queue<Node>();
                _root.Fail = _root;
                for (int b = 0; b < 256; b++)
                {
                    Node child = _root.Next[b];
                    if (child == null)
                    {
                        _root.Next[b] = _root;
                    }
                    else
                    {
                        child.Fail = _root;
                        queue.Enqueue(child);
                    }
                }

                while (queue.Count > 0)
                {
                    Node node = queue.Dequeue();
                    for (int b = 0; b < 256; b++)
                    {
                        Node child = node.Next[b];
                        if (child == null)
                        {
                            node.Next[b] = node.Fail.Next[b];
                            continue;
                        }
                        child.Fail = node.Fail.Next[b];
                        child.Outputs.AddRange(child.Fail.Outputs);
                        queue.Enqueue(child);
                    }
                }
                _built = true;
            }

            public void Search(byte[] buffer, int length, Dictionary<int, int> found)
            {
                if (_empty) return;
                lock (this)
                {
                    if (!_built) Build();
                }

                Node node = _root;
                for (int i = 0; i < length; i++)
                {
                    byte b = _ignoreCase ? Lower(buffer[i]) : buffer[i];
                    node = node.Next[b];
                    if (node.Outputs.Count == 0) continue;
                    foreach (var output in node.Outputs)
                    {
                        if (found.ContainsKey(output.Id)) continue;
                        if (output.WholeWord && !AtWordBoundary(buffer, length, i, output)) continue;
                        found[output.Id] = i;
                    }
                }
            }

            private static bool AtWordBoundary(byte[] buffer, int length, int end, Output output)
            {
                int before = end - output.Length + 1 - output.CharSize;
                int after = end + 1;
                if (before >= 0 && IsWordChar(buffer[before])) return false;
                if (after < length && IsWordChar(buffer[after])) return false;
                return true;
            }

            private static bool IsWordChar(byte b) => (b >= (byte)'a' && b <= (byte)'z') || (b >= (byte)'A' && b <= (byte)'Z') || (b >= (byte)'0' && b <= (byte)'9');

            private static byte Lower(byte b) => b >= (byte)'A' && b <= (byte)'Z' ? (byte)(b + 32) : b;
        }
    }
}
