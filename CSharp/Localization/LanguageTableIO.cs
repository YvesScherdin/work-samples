using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GataryLabs.Localization
{
    static public class LanguageTableIO
    {
        private const char delimeterKeyValue = '=';
        private const char delimeterEntries = '\n';
        private const string newLineReadReplacement = "☻";
        private const string newLineIndicator = "\\n";

        static public void Read(string rawContent, Dictionary<string, string> table, bool additive=false)
        {
            if(!additive)
                table.Clear();

            if (rawContent.Length == 0)
                return;

            // unify EOL
            rawContent = rawContent.Replace("\\n", newLineReadReplacement);

            if (rawContent.Contains("\r"))
            {
                if (rawContent.Contains("\r\n"))
                    rawContent = rawContent.Replace("\r\n", "\n");
                else
                    rawContent = rawContent.Replace("\r", "\n");
            }

            string[] kvps = rawContent.Split(delimeterEntries);

            for(int i=0; i< kvps.Length; i++)
            {
                string kvp = kvps[i];
                int endIndex = kvp.IndexOf(delimeterKeyValue);

                if (endIndex == -1)
                {
                    Debug.LogWarning("KVP buggy: " + kvp);
                    continue;
                }

                string key = kvp.Substring(0, endIndex);
                string value = PreProcessValueForReading(kvp.Substring(kvp.IndexOf(delimeterKeyValue)+1));
                table[key] = value;
            }
        }

        static private string PreProcessValueForReading(string value)
        {
            return value.Replace(newLineReadReplacement, Environment.NewLine);
        }

#if UNITY_EDITOR
        static public string Write(Dictionary<string, string> table)
        {
            List<string> keys = new List<string>(table.Keys);
            keys.Sort();

            StringBuilder b = new StringBuilder();

            int last = keys.Count - 1;
            for (int i=0; i<keys.Count; i++)
            {
                string key = keys[i];
                b.Append(key);
                b.Append(delimeterKeyValue);
                b.Append(PreProcessValueForWriting(table[key]));

                if (i != last)
                    b.Append(Environment.NewLine); // reverse unification of EOL again
            }

            return b.ToString();
        }

        static private string PreProcessValueForWriting(string value)
        {
            return value.Replace(Environment.NewLine, newLineIndicator);
        }
#endif
    }
}
