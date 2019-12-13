using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using TSR.Utils;
using UnityEditor;
using UnityEngine;

namespace Development.JSONEditor.Editor
{
    public class JsonEditor : EditorWindow
    {
        [MenuItem("Tools/Language JSON editor")]
        public static void ShowWindow()
        {
            GetWindow<JsonEditor>(false, "Language JSON Editor", true);
        }

        private Language[] languages;

        private Dictionary<KeyValuePair<string, Language>, string> mainDictionary = new Dictionary<KeyValuePair<string, Language>, string>();

        private List<string> keys;

        private string keyToAdd;
        private Dictionary<Language, string> valuesToAddDictionary = new Dictionary<Language, string>();

        private Vector2 verticalScrollPosition;
        private Vector2 horizontalScrollPosition;

        private string searchText;

        private void OnGUI()
        {
            if (GUILayout.Button("Load language files", GUILayout.Height(40)))
            {
                LoadLanguageFiles();
            }

            GUILayout.Space(20);

            if (keys == null)
                return;

            horizontalScrollPosition = EditorGUILayout.BeginScrollView(horizontalScrollPosition, GUI.skin.horizontalScrollbar, GUIStyle.none);

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();

            GUILayout.Space(100);

            EditorGUILayout.LabelField("Key");

            foreach (var value in languages)
            {
                EditorGUILayout.LabelField(value.ToString());
            }

            GUILayout.Space(10);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Search: ", GUILayout.Width(100));
            searchText = EditorGUILayout.TextField("", searchText);
            EditorGUILayout.EndHorizontal();

            verticalScrollPosition = EditorGUILayout.BeginScrollView(verticalScrollPosition, GUIStyle.none, GUI.skin.verticalScrollbar);

            for (var i = 0; i < keys.Count; i++)
            {
                if(!string.IsNullOrEmpty(searchText) && !keys[i].ToUpper().Contains(searchText.ToUpper()))
                    continue;

                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Delete", GUILayout.Width(100)))
                {
                    keys.RemoveAt(i);
                    EditorGUILayout.EndHorizontal();
                    return;
                }

                EditorGUILayout.LabelField(keys[i]);
                
                foreach (var value in languages)
                {
                    var tmpKey = new KeyValuePair<string, Language>(keys[i], value);

                    if (!mainDictionary.ContainsKey(tmpKey))
                    {
                        mainDictionary[tmpKey] = "";
                    }

                    mainDictionary[tmpKey] = EditorGUILayout.TextField("", mainDictionary[tmpKey]);
                }
                
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.BeginVertical("box");
            GUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Add", GUILayout.Width(96)))
            {
                if (string.IsNullOrEmpty(keyToAdd))
                    return;

                foreach (var value in languages)
                {
                    mainDictionary[new KeyValuePair<string, Language>(keyToAdd, value)] = valuesToAddDictionary[value];
                }

                if (!keys.Contains(keyToAdd))
                    keys.Add(keyToAdd);

                keyToAdd = string.Empty;
                valuesToAddDictionary.Clear();

                Repaint();
            }

            keyToAdd = EditorGUILayout.TextField("", keyToAdd);
            foreach (var value in languages)
            {
                if (!valuesToAddDictionary.ContainsKey(value))
                {
                    valuesToAddDictionary[value] = "";
                }

                valuesToAddDictionary[value] = EditorGUILayout.TextField("", valuesToAddDictionary[value]);
            }

            GUILayout.Space(10);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);
            EditorGUILayout.EndVertical();
            GUILayout.Space(10);
            EditorGUILayout.EndScrollView();

            GUILayout.Space(20);

            if (GUILayout.Button("Save language files", GUILayout.Height(40)))
            {
                SaveLanguageFiles();
            }
        }

        private void LoadLanguageFiles()
        {
            mainDictionary.Clear();

            languages = (Language[])Enum.GetValues(typeof(Language));

            var languageStructs = new Dictionary<Language, LanguageStruct>();

            keys = new List<string>();

            foreach (var value in languages)
            {
                languageStructs[value] =
                    JsonConvert.DeserializeObject<LanguageStruct>(Resources.Load<TextAsset>("Languages/" + value).text);
            }

            foreach (var languageStruct in languageStructs)
            {
                foreach (var valuePair in languageStruct.Value.languageFile)
                {
                    var tmp = new KeyValuePair<string, Language>(valuePair.Key, languageStruct.Key);
                    mainDictionary[tmp] = valuePair.Value;
                    
                    if (keys.All(k => k != tmp.Key))
                    {
                        keys.Add(tmp.Key);
                    }
                }
            }
        }

        private void SaveLanguageFiles()
        {
            var languageStructs = new Dictionary<Language, LanguageStruct>();

            foreach (var value in languages)
            {
                var ls = new LanguageStruct()
                {
                    languageFile = new List<KeyValuePair<string, string>>()
                };

                languageStructs[value] = ls;
            }

            foreach (var languageStructPair in languageStructs)
            {
                foreach (var key in keys)
                {
                    languageStructPair.Value.languageFile.Add(new KeyValuePair<string, string>(key, mainDictionary[new KeyValuePair<string, Language>(key, languageStructPair.Key)]));
                }

                JsonSerializerSettings serializeSetting = new JsonSerializerSettings() { Formatting = Formatting.Indented };
                var fileToSave = JsonConvert.SerializeObject(languageStructPair.Value, serializeSetting);
                File.WriteAllText("Assets/Resources/Languages/" + languageStructPair.Key + ".json", fileToSave, Encoding.UTF8);
            }

            AssetDatabase.Refresh();
        }
    }
}
