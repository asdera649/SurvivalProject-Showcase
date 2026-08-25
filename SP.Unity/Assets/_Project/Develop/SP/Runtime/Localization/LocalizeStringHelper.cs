using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.SmartFormat.PersistentVariables;
using UnityEngine.Localization.Tables;
#if UNITY_EDITOR
using UnityEditor.Localization;
#endif

namespace SP.Runtime.Localization
{
    #region Structs
    
    public class EntryContainer
    {
        public EntryContainer(string entryName, IReadOnlyList<LocalVariable> localVariables)
        {
            EntryName = entryName;
            LocalVariables = localVariables;
        }

        public string EntryName { get; }
        public IReadOnlyList<LocalVariable> LocalVariables { get; }
    }

    [Serializable]
    public struct LocalVariable
    {
        public LocalVariable(string name, object value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; }
        public object Value { get; }
    }

    #endregion
    
    [RequireComponent(typeof(LocalizeStringEvent), typeof(TMP_Text))]
    public class LocalizeStringHelper : MonoBehaviour
    {
#if UNITY_EDITOR
        [Header("Settings")]
        [SerializeField] private List<StringTableCollection> _tables = new();
#endif
        
        [SerializeField, HideInInspector] private string[] _tablesGuid;

        private const string _pattern = @"\{([^}]*)\}";
        
        private readonly Dictionary<string, Guid> _cachedGuids = new();
        private string _cachedEntryName;

        private LocalizeStringEvent _localizeString;
        private LocalizeStringEvent LocalizeString
        {
            get
            {
                if (_localizeString == null)
                {
                    _localizeString = GetComponent<LocalizeStringEvent>();
                }

                return _localizeString;
            }
        }

        private TMP_Text _text;
        private TMP_Text Text
        {
            get
            {
                if (_text == null)
                {
                    _text = GetComponent<TMP_Text>();
                }

                return _text;
            }
        }

        private void Awake()
        {
            Initialize();
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            _tablesGuid = new string[_tables.Count];

            for (var i = 0; i < _tables.Count; i++)
            {
                _tablesGuid[i] = _tables[i]?.TableCollectionNameReference.TableCollectionNameGuid.ToString();
            }
#endif
        }

        private void Initialize()
        {
            foreach (var g in _tablesGuid)
            {
                _cachedGuids.Add(g, new Guid(g));
            }
        }

        public void SetEntry(EntryContainer entryContainer)
        {
            if (entryContainer == null)
            {
                ClearEntry();
                return;
            }

            if (_cachedEntryName != entryContainer.EntryName)
            {
                ClearEntry();
            }
            
            foreach (var v in entryContainer.LocalVariables)
            {
                var temp = (IVariable)v.Value;

                if (LocalizeString.StringReference.ContainsKey(v.Name))
                {
                    LocalizeString.StringReference[v.Name] = temp;
                }
                else
                {
                    LocalizeString.StringReference.Add(v.Name, temp);
                }
            }
            
            LocalizeString.SetEntry(entryContainer.EntryName);
            
            LocalizeString.RefreshString();
            
            _cachedEntryName = entryContainer.EntryName;
        }

        public void SetEntry(string text)
        {
            var result = Regex.Replace(text, _pattern, match =>
            {
                var key = match.Groups[1].Value;
                
                foreach (var t in _tablesGuid)
                {
                    var entry = GetEntry(_cachedGuids[t], key);

                    if (entry == null)
                    {
                        continue;
                    }
                
                    return entry.Value;
                }
                
                return match.Value;
            });
                
            Text.text = result;
        }
        
        public void ClearEntry()
        {
            LocalizeString.SetEntry(null);
            LocalizeString.StringReference.Clear();
            
            Text.text = null;
        }

        #region Utilities

        private StringTableEntry GetEntry(TableReference tableReference, string key)
        {
            var table = LocalizationSettings.StringDatabase.GetTable(tableReference);
            
            return table.GetEntry(key);
        }

        #endregion
    }
}