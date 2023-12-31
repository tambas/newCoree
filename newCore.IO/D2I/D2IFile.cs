﻿using Giny.Core.DesignPattern;
using Giny.Core.Extensions;
using Giny.Core.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Giny.IO.D2I
{
    /// <summary>
    /// Dofus Language File *.d2i
    /// </summary>
    [WIP("make an D2IManager (as D2O) for tools.")]
    public class D2IFile
    {
        private readonly Dictionary<int, D2IEntry<int>> m_entries = new Dictionary<int, D2IEntry<int>>();
        private readonly Dictionary<string, D2IEntry<string>> m_textEntries = new Dictionary<string, D2IEntry<string>>();

        private readonly List<int> m_textSortIndexes = new List<int>();
        private int textCount = 0;
        private readonly string m_uri;

        public D2IFile(string uri)
        {
            m_uri = uri;
            Initialize();
        }

        public string FilePath
        {
            get { return m_uri; }
        }

        private void Initialize()
        {
            using (var reader = new BigEndianReader(File.ReadAllBytes(m_uri)))
            {
                int indexPos = reader.ReadInt();
                reader.Seek(indexPos, SeekOrigin.Begin);
                int indexLen = reader.ReadInt();

                for (int i = 0; i < indexLen; i += 9)
                {
                    int key = reader.ReadInt();
                    bool undiacritical = reader.ReadBoolean();
                    int dataPos = reader.ReadInt();
                    int pos = (int)reader.Position;
                    reader.Seek(dataPos, SeekOrigin.Begin);
                    var text = reader.ReadUTF();
                    reader.Seek(pos, SeekOrigin.Begin);

                    if (undiacritical)
                    {
                        var criticalPos = reader.ReadInt();
                        i += 4;

                        pos = (int)reader.Position;
                        reader.Seek(criticalPos, SeekOrigin.Begin);
                        var undiacriticalText = reader.ReadUTF();
                        reader.Seek(pos, SeekOrigin.Begin);

                        m_entries.Add(key, new D2IEntry<int>(key, text, undiacriticalText));
                    }
                    else
                        m_entries.Add(key, new D2IEntry<int>(key, text));

                }
                indexLen = reader.ReadInt();

                while (indexLen > 0)
                {
                    var pos = reader.Position;
                    string key = reader.ReadUTF();
                    int dataPos = reader.ReadInt();
                    indexLen -= ((int)reader.Position - (int)pos);

                    textCount++;
                    pos = reader.Position;
                    reader.Seek(dataPos, SeekOrigin.Begin);
                    m_textEntries.Add(key, new D2IEntry<string>(key, reader.ReadUTF()));
                    reader.Seek((int)pos, SeekOrigin.Begin);
                }

                indexLen = reader.ReadInt();
                while (indexLen > 0)
                {
                    m_textSortIndexes.Add(reader.ReadInt());
                    indexLen -= 4;
                }
            }
        }

        public string GetText(int id)
        {
            if (m_entries.ContainsKey(id))
                return m_entries[id].Text;
            return string.Format("[UNKNOWN_TEXT_ID_{0}]", id);
        }

        public string GetText(string id)
        {
            if (m_textEntries.ContainsKey(id))
                return m_textEntries[id].Text;
            return string.Format("[UNKNOWN_TEXT_ID_{0}]", id);
        }

        public void SetText(int id, string value)
        {
            D2IEntry<int> entry;
            if (!m_entries.TryGetValue(id, out entry))
                m_entries.Add(id, entry = new D2IEntry<int>(id, value));
            else
                entry.Text = value;

            if (value.HasAccents() || value.Any(char.IsUpper))
            {
                entry.UnDiactricialText = value.RemoveAccents().ToLower();
                entry.UseUndiactricalText = true;
            }
            else
                entry.UseUndiactricalText = false;
        }

        public void SetText(string id, string value)
        {
            D2IEntry<string> entry;
            if (!m_textEntries.TryGetValue(id, out entry))
                m_textEntries.Add(id, new D2IEntry<string>(id, value));
            else
                entry.Text = value;
        }

        public bool DeleteText(int id)
        {
            return m_entries.Remove(id);
        }

        public bool DeleteText(string id)
        {
            return m_textEntries.Remove(id);
        }

        public IEnumerable<D2IEntry<int>> GetAllText()
        {
            return m_entries.Values;
        }

        public IEnumerable<D2IEntry<string>> GetAllUiText()
        {
            return m_textEntries.Values;
        }

        public int FindFreeId()
        {
            return m_entries.Keys.Max() + 1;
        }

        public void Save()
        {
            Save(m_uri);
        }

        public void Save(string uri)
        {
            using (var contentWriter = new BigEndianWriter(new StreamWriter(uri).BaseStream))
            {
                var headerWriter = new BigEndianWriter();
                contentWriter.Seek(4, SeekOrigin.Begin);

                foreach (var index in m_entries.Where(x => x.Value.Text != null))
                {
                    headerWriter.WriteInt(index.Key);
                    headerWriter.WriteBoolean(index.Value.UseUndiactricalText);

                    headerWriter.WriteInt((int)contentWriter.Position);
                    contentWriter.WriteUTF(index.Value.Text);

                    if (index.Value.UseUndiactricalText)
                    {
                        headerWriter.WriteInt((int)contentWriter.Position);
                        contentWriter.WriteUTF(index.Value.UnDiactricialText);
                    }
                }

                var indexLen = (int)headerWriter.Position;

                headerWriter.WriteInt(0);

                foreach (var index in m_textEntries.Where(x => x.Value.Text != null))
                {
                    headerWriter.WriteUTF(index.Key);
                    headerWriter.WriteInt((int)contentWriter.Position);
                    contentWriter.WriteUTF(index.Value.Text);
                }

                var textIndexLen = headerWriter.Position - indexLen - 4;
                var searchTablePos = headerWriter.Position;
                headerWriter.Seek(indexLen);
                headerWriter.WriteInt((int)textIndexLen);
                headerWriter.Seek((int)searchTablePos);

                headerWriter.WriteInt(0);
                var sortedEntries = m_entries.Values.OrderBy(x => x.Text);
                foreach (var entry in sortedEntries)
                {
                    headerWriter.WriteInt(entry.Key);
                }

                var searchTableLen = headerWriter.Position - searchTablePos - 4;
                headerWriter.Seek((int)searchTablePos);
                headerWriter.WriteInt((int)searchTableLen);

                var indexPos = (int)contentWriter.Position;

                byte[] indexData = headerWriter.Data;
                contentWriter.WriteInt(indexLen);
                contentWriter.WriteBytes(indexData);

                contentWriter.Seek(0, SeekOrigin.Begin);
                contentWriter.WriteInt(indexPos);
            }
        }
    }
    public class D2IEntry<T> : ID2IEntry
    {

        public D2IEntry(T key, string text)
        {
            Key = key;
            Text = text;
        }

        public D2IEntry(T key, string text, string undiactricalText)
        {
            Key = key;
            Text = text;
            UnDiactricialText = undiactricalText;
            UseUndiactricalText = true;
        }

        public T Key
        {
            get;
            set;
        }

        public string Text
        {
            get;
            set;
        }

        public bool UseUndiactricalText;

        public string UnDiactricialText
        {
            get;
            set;
        }

        public string GetText()
        {
            return UseUndiactricalText ? UnDiactricialText : Text;
        }
    }
    public interface ID2IEntry
    {
        string GetText();
    }
}
