using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Cysharp.Threading.Tasks;

#if INITIALIZED
using OpenAI_API;
#endif


namespace Witchpot.Editor
{
    public sealed class SearchWindow : EditorWindow
    {
        [MenuItem("Witchpot/Editor/Search Window")]
        private static void Open()
        {
            GetWindow<SearchWindow>("SearchWindow");
        }

        private GUILayoutOption m_Hight = GUILayout.Height(20);

#if INITIALIZED
        private OpenAIAPI m_Api;
#endif

        private SerializedObject so_This;
        private SerializedProperty sp_SearchResult;

        private string m_Prompt = string.Empty;

        [SerializeField]
        private List<AssetBinder> m_SearchResult = new();

        private bool SearchEnabled =>
            Parameter.EmbeddingsWindow.EmbeddingsContainerForSearch != null &&
            !string.IsNullOrEmpty(m_Prompt) &&
            !m_Searching;

        private bool m_Searching = false;

        private void OnEnable()
        {
#if !INITIALIZED
            Debug.Log("Add INITIALIZED");
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, "INITIALIZED");
#endif
            so_This = new SerializedObject(this);
            sp_SearchResult = so_This.FindProperty("m_SearchResult");

#if INITIALIZED
            m_Api = new OpenAIAPI(new APIAuthentication(Parameter.ApiKey, Parameter.Organization));
#endif
        }

        private void OnDisable()
        {
            // Debug.Log("Save on disable");

            Parameter.Save();
        }

        private void OnGUI()
        {
            GUILayout.Label("Search in embeddings container", EditorStyles.boldLabel);
            GUILayout.Space(10);

            Parameter.SearchWindow.EmbeddingsContainerForSearch = (EmbeddingsContainer)EditorGUILayout.ObjectField(Parameter.SearchWindow.EmbeddingsContainerForSearch, typeof(EmbeddingsContainer), true);

            using (new EditorGUILayout.HorizontalScope(m_Hight))
            {
                GUILayout.Label("Search Prompt", GUILayout.Width(100), m_Hight);

                m_Prompt = EditorGUILayout.TextField(m_Prompt, m_Hight);
            }

            using (new EditorGUILayout.HorizontalScope(m_Hight))
            {
                GUILayout.Label("Search Count", GUILayout.Width(100), m_Hight);

                Parameter.SearchWindow.SearchCount = EditorGUILayout.IntField(Parameter.SearchWindow.SearchCount);
            }

            using (new EditorGUI.DisabledScope(!SearchEnabled))
            {
                if (GUILayout.Button("Search", m_Hight))
                {
                    Search().Forget();
                }
            }

            so_This.Update();

            if (sp_SearchResult != null)
            {
                EditorGUILayout.PropertyField(sp_SearchResult, true);
            }

            so_This.ApplyModifiedProperties();
        }

        private async UniTask Search()
        {
            if (m_Searching)
            {
                return;
            }

            try
            {
                m_Searching = true;

                float[] result;

#if INITIALIZED
                m_Api.Auth = new APIAuthentication(Parameter.ApiKey, Parameter.Organization);
                result = await m_Api.Embeddings.GetEmbeddingsAsync(m_Prompt);
    #else
                await Task.CompletedTask;
                result = new float[1536];
    #endif

                var obj = ScriptableObject.CreateInstance<EmbeddingsData>();

                obj.Prompt = m_Prompt;
                obj.Embeddings = result;

                m_SearchResult = Parameter.SearchWindow.EmbeddingsContainerForSearch.SearchCloseAsset(obj, Parameter.SearchWindow.SearchCount);
            }
            finally
            {
                m_Searching = false;
            }
        }
    }
}