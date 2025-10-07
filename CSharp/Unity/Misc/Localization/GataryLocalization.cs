using UnityEngine;
using System.Linq;
using System;

namespace GataryLabs.Localization
{
    /// <summary>
    /// 
    /// GataryLabs Localization
    /// 
    /// By Yves Scherdin
    /// 2021/03
    /// 
    /// </summary>
    static public class GataryLocalization
    {
        /// <summary>
        /// To Customize
        /// </summary>
        static internal string settingsPath = "Lang/LanguageSettings";
        static internal LocalizationMissingStrategy missingStrategy = LocalizationMissingStrategy.Indicated;

        // internal part
        static private LocalizationSettings settings;
        static private LanguageData currentData;
        static private LanguageTable currentTable;

        static public LanguageCode CurrentLanguage => currentData != null ? currentData.code : LanguageCode.None;
        static public bool IsReady() => currentTable != null;

        static public void SetSettings(LocalizationSettings settings)
        {
            GataryLocalization.settings = settings;

            if (settings.allLanguages != null && settings.current != LanguageCode.None)
                SetCurrentLanguage(settings.current);
        }

        static public void SetCurrentLanguage(LanguageCode langCode)
        {
            if (CurrentLanguage == langCode)
                return;
            
            LanguageData langData = settings.allLanguages.Where((LanguageData ld) => ld.code == langCode).FirstOrDefault();

            if (langData == null || langData.asset == null)
            {
                Debug.LogWarning("Language not prepared: " + langCode.ToString());

                if (settings != null)
                {
                    Debug.Log("Choosing fallback " + settings.fallback.ToString());
                    langData = settings.allLanguages.Where((LanguageData ld) => ld.code == settings.fallback).FirstOrDefault();
                }
            }

            currentData = langData;
            currentTable = DataToTable(langData, langCode);

            //currentTable.Log();
        }

        static internal void Reset()
        {
            currentTable = null;
        }

        static public bool HasText(string id, LanguageCategory category)
        {
            return HasText(ToKey(id, category));
        }

        private static bool HasText(string id)
        {
            return currentTable != null && currentTable.Contains(id);
        }

        static public string GetText(string id, LanguageCategory category)
        {
            return GetText(ToKey(id, category));
        }
        
        static public string GetText(string id, LanguageCategory category, LocalizationMissingStrategy missingStrategy)
        {
            return GetText(ToKey(id, category), missingStrategy);
        }

        static public string GetText(string id)
        {
            return currentTable != null && currentTable.Contains(id) ? currentTable.GetString(id) : GetMissingString(id);
        }
        
        static public string GetText(string id, LocalizationMissingStrategy missingStrategy)
        {
            return currentTable != null && currentTable.Contains(id) ? currentTable.GetString(id) : GetMissingString(id, missingStrategy);
        }
        
        static private string GetMissingString(string id)
        {
            switch (missingStrategy)
            {
                case LocalizationMissingStrategy.Raw:       return id;
                case LocalizationMissingStrategy.Empty:     return "";
                case LocalizationMissingStrategy.Indicated: return "__" + id + "__";
                default:                                    return id;
            }
        }
        
        static private string GetMissingString(string id, LocalizationMissingStrategy strategy)
        {
            switch (strategy)
            {
                case LocalizationMissingStrategy.Raw:       return id;
                case LocalizationMissingStrategy.Empty:     return "";
                case LocalizationMissingStrategy.Indicated: return "__" + id + "__";
                default:                                    return id;
            }
        }

        static public string ToKey(string id, LanguageCategory category) => category.ToString() + "." + id;


        static internal LanguageTable DataToTable(LanguageData langData, LanguageCode langCode)
        {
            string content = null;

            if (langData == null)
            {
                Debug.LogWarning("language does not exist: " + langCode);
                content = "";
            }
            else if (langData.asset == null)
            {
                Debug.LogWarning("Language file missing: " + langData.code);
                content = "";
            }
            else
            {
                content = langData.asset.text;
            }

            LanguageTable table = new LanguageTable();
            LanguageTableIO.Read(content, table.dictionary);
            return table;
        }

    }

    public enum LocalizationMissingStrategy
    {
        Raw=0,
        Indicated=1,
        Empty=2
    }
}