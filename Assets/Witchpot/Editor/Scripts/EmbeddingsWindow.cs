using System;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine.UIElements;

#if INITIALIZED
using OpenAI_API;
#endif

namespace Witchpot.Editor
{
    public sealed class EmbeddingsWindow : EditorWindow
    {
        [Serializable]
        private class FoldoutItem
        {
            [SerializeField]
            private bool m_Foldout;
            public bool Foldout { get => m_Foldout; set => m_Foldout = value; }

            [SerializeField]
            private GUIContent m_Content;
            public GUIContent Content => m_Content;

            public FoldoutItem(bool fold, string content)
            {
                m_Foldout = fold;
                m_Content = new GUIContent(content);
            }
        }

        [MenuItem("Witchpot/Editor/Embeddings Window")]
        private static void Open()
        {
            GetWindow<EmbeddingsWindow>("Embeddings");
        }

        private const string m_Extension = ".asset";
        private GUILayoutOption m_Hight = GUILayout.Height(20);
        private GUILayoutOption m_LineHight = GUILayout.Height(3);


        private SerializedObject so_This;
        private SerializedProperty sp_SearchResult;

#if INITIALIZED
        private OpenAIAPI m_Api;
#endif

        private Vector2 m_ScrollPos;
        private bool m_TaskExist = false;
        private bool m_TaskCanceled = false;

        private bool UpdateEnabled =>
            Parameter.EmbeddingsWindow.EmbeddingsContainerForUpdate != null && 
            Parameter.EmbeddingsWindow.IsValidOutputFolderPath == Parameter.EFolderPathStatus.Valid && 
            Parameter.EmbeddingsWindow.IsValidIncludeFolder == Parameter.EFolderPathStatus.Valid && 
            !m_TaskExist;

        private bool SearchEnabled =>
            Parameter.EmbeddingsWindow.EmbeddingsContainerForSearch != null && 
            Parameter.EmbeddingsWindow.EmbeddingsForSearch != null && 
            !m_TaskExist;

        [SerializeField]
        private List<AssetBinder> m_SearchResult = new ();

        // Calclate Distance
        private float m_Distance = -1;

        // Debug
        private int m_Count = 0;
        private bool m_CreateDummy = false;

        private void OnEnable()
        {
#if INITIALIZED
            so_This = new SerializedObject(this);
            sp_SearchResult = so_This.FindProperty("m_SearchResult");

            m_Api = new OpenAIAPI(new APIAuthentication(Parameter.ApiKey, Parameter.Organization));
#else
            Debug.Log("Add INITIALIZED");
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, "INITIALIZED");

            Parameter.Save();
#endif
        }

        private void OnDisable()
        {
            // Debug.Log("Save on disable");

            Parameter.Save();
        }

        private void UpdateOpenAIAPI()
        {
#if INITIALIZED
            m_Api.Auth = new APIAuthentication(Parameter.ApiKey, Parameter.Organization);
#endif
        }

        private FoldoutItem m_General = new FoldoutItem(true, "General Settings");
        private FoldoutItem m_CreateSingle = new FoldoutItem(false, "Create single embedding");
        private FoldoutItem m_UpdateContainer = new FoldoutItem(false, "Update embeddings container");
        private FoldoutItem m_Search = new FoldoutItem(false, "Search in embeddings container");
        private FoldoutItem m_Others = new FoldoutItem(false, "Others");

        private void OnGUI()
        {
#if INITIALIZED
            using (var scrollView = new EditorGUILayout.ScrollViewScope(m_ScrollPos, GUILayout.Height(position.height)))
            {
                m_ScrollPos = scrollView.scrollPosition;

                //GUILayout.Label("General Settings", EditorStyles.boldLabel);
                //GUILayout.Space(10);

                if (m_General.Foldout = EditorGUILayout.Foldout(m_General.Foldout, m_General.Content))
                {
                    using (new EditorGUILayout.HorizontalScope(m_Hight))
                    {
                        GUILayout.Label("ApiKey", GUILayout.Width(100), m_Hight);

                        Parameter.ApiKey = EditorGUILayout.TextField(Parameter.ApiKey, m_Hight);
                    }

                    using (new EditorGUILayout.HorizontalScope(m_Hight))
                    {
                        GUILayout.Label("Organization", GUILayout.Width(100), m_Hight);

                        Parameter.Organization = EditorGUILayout.TextField(Parameter.Organization, m_Hight);
                    }

                    // GUILayout.Label($"Count : {m_Count}");

                    // m_CreateDummy = GUILayout.Toggle(m_CreateDummy, "Create Dummy Embeddings", m_Hight);

                    using (new EditorGUILayout.HorizontalScope(m_Hight))
                    {
                        GUILayout.Label("Output Folder", GUILayout.Width(100), m_Hight);

                        Parameter.EmbeddingsWindow.OutputFolder = EditorGUILayout.TextField(Parameter.EmbeddingsWindow.OutputFolder, m_Hight);

                        if (Parameter.EmbeddingsWindow.IsValidOutputFolderPath == Parameter.EFolderPathStatus.StartIsNotAsset)
                        {
                            using (new GUIColorScope(Color.red))
                            {
                                GUILayout.Label("Output Folder must start from Assets/", m_Hight);
                            }
                        }
                    }
                }

                GUILayout.Box(string.Empty, GUILayout.ExpandWidth(true), m_LineHight); // ----------

                //GUILayout.Label("Create single embedding", EditorStyles.boldLabel);

                if (m_CreateSingle.Foldout = EditorGUILayout.Foldout(m_CreateSingle.Foldout, m_CreateSingle.Content))
                {
                    GUILayout.Space(10);
                    
                    using (new EditorGUILayout.HorizontalScope(m_Hight))
                    {
                        GUILayout.Label("Prompt", GUILayout.Width(100), m_Hight);

                        Parameter.EmbeddingsWindow.Prompt = EditorGUILayout.TextField(Parameter.EmbeddingsWindow.Prompt, m_Hight);
                    }

                    bool buffer = Parameter.EmbeddingsWindow.UsePromptForFileName;
                    Parameter.EmbeddingsWindow.UsePromptForFileName = GUILayout.Toggle(Parameter.EmbeddingsWindow.UsePromptForFileName, "Use prompt for file name", m_Hight);

                    const string m_FileNameTextField = "FileNameTextField";

                    if (buffer != Parameter.EmbeddingsWindow.UsePromptForFileName)
                    {
                        if (string.Equals(GUI.GetNameOfFocusedControl(), m_FileNameTextField))
                        {
                            GUI.FocusControl(string.Empty);
                        }
                    }

                    bool isValidFileName;

                    using (new EditorGUILayout.HorizontalScope(m_Hight))
                    {
                        GUILayout.Label("File Name", GUILayout.Width(100), m_Hight);

                        GUI.SetNextControlName(m_FileNameTextField);

                        if (Parameter.EmbeddingsWindow.UsePromptForFileName)
                        {
                            using (new EditorGUI.DisabledScope(true))
                            {
                                Parameter.EmbeddingsWindow.FileName = EditorGUILayout.TextField(Parameter.EmbeddingsWindow.Prompt, m_Hight);
                            }
                        }
                        else
                        {
                            Parameter.EmbeddingsWindow.FileName = EditorGUILayout.TextField(Parameter.EmbeddingsWindow.FileName, m_Hight);
                        }

                        isValidFileName = Parameter.EmbeddingsWindow.ValidteFileName(Parameter.EmbeddingsWindow.FileName);

                        if (!isValidFileName)
                        {
                            using (new GUIColorScope(Color.red))
                            {
                                GUILayout.Label("File Name is invalid", m_Hight);
                            }
                        }
                    }

                    bool CreateEnabled =
                        isValidFileName &&
                        Parameter.EmbeddingsWindow.IsValidOutputFolderPath == Parameter.EFolderPathStatus.Valid &&
                        !m_TaskExist;

                    using (new EditorGUI.DisabledScope(!CreateEnabled))
                    {
                        if (GUILayout.Button("Create", m_Hight))
                        {
                            CreateEmbeddingsAsync(Parameter.EmbeddingsWindow.FileName, Parameter.EmbeddingsWindow.Prompt, Parameter.EmbeddingsWindow.OutputFolder).Forget();
                        }
                    }

                }

                GUILayout.Box(string.Empty, GUILayout.ExpandWidth(true), m_LineHight); // ----------

                //GUILayout.Label("Update embeddings container", EditorStyles.boldLabel);

                if (m_UpdateContainer.Foldout = EditorGUILayout.Foldout(m_UpdateContainer.Foldout, m_UpdateContainer.Content))
                {
                    GUILayout.Space(10);
                    
                    Parameter.EmbeddingsWindow.EmbeddingsContainerForUpdate = (EmbeddingsContainer)EditorGUILayout.ObjectField(Parameter.EmbeddingsWindow.EmbeddingsContainerForUpdate, typeof(EmbeddingsContainer), true);

                    using (new EditorGUILayout.HorizontalScope(m_Hight))
                    {
                        GUILayout.Label("Include Folder", GUILayout.Width(100), m_Hight);

                        Parameter.EmbeddingsWindow.IncludeFolder = EditorGUILayout.TextField(Parameter.EmbeddingsWindow.IncludeFolder, m_Hight);

                        if (Parameter.EmbeddingsWindow.IsValidIncludeFolder == Parameter.EFolderPathStatus.StartIsNotAsset)
                        {
                            using (new GUIColorScope(Color.red))
                            {
                                GUILayout.Label("Include Folder must start from Assets/", m_Hight);
                            }
                        }
                        else if (Parameter.EmbeddingsWindow.IsValidIncludeFolder == Parameter.EFolderPathStatus.FolderNotExist)
                        {
                            using (new GUIColorScope(Color.red))
                            {
                                GUILayout.Label("Include Folder not Exist", m_Hight);
                            }
                        }
                    }

                    Parameter.EmbeddingsWindow.LimitProcessSpeed = GUILayout.Toggle(Parameter.EmbeddingsWindow.LimitProcessSpeed, "Limit process speed (Trial account limited 60 access/min)", m_Hight);

                    using (new EditorGUI.DisabledScope(!UpdateEnabled))
                    {
                        if (GUILayout.Button("Update", m_Hight))
                        {
                            UpdateEmbeddingsContainerAsync(Parameter.EmbeddingsWindow.IncludeFolder, Parameter.EmbeddingsWindow.OutputFolder).Forget();
                        }
                    }

                    using (new EditorGUI.DisabledScope(!m_TaskExist))
                    {
                        if (GUILayout.Button("Cancel", m_Hight))
                        {
                            m_TaskCanceled = true;
                        }
                    }
                }


                GUILayout.Box(string.Empty, GUILayout.ExpandWidth(true), m_LineHight); // ----------

                //GUILayout.Label("Search in embeddings container", EditorStyles.boldLabel);

                if (m_Search.Foldout = EditorGUILayout.Foldout(m_Search.Foldout, m_Search.Content))
                {
                    GUILayout.Space(10);
                    
                    Parameter.EmbeddingsWindow.EmbeddingsContainerForSearch = (EmbeddingsContainer)EditorGUILayout.ObjectField(Parameter.EmbeddingsWindow.EmbeddingsContainerForSearch, typeof(EmbeddingsContainer), true);
                    Parameter.EmbeddingsWindow.EmbeddingsForSearch = (EmbeddingsData)EditorGUILayout.ObjectField(Parameter.EmbeddingsWindow.EmbeddingsForSearch, typeof(EmbeddingsData), true);

                    using (new EditorGUILayout.HorizontalScope(m_Hight))
                    {
                        GUILayout.Label("Search Count", GUILayout.Width(100), m_Hight);

                        Parameter.EmbeddingsWindow.SearchCount = EditorGUILayout.IntField(Parameter.EmbeddingsWindow.SearchCount);
                    }

                    using (new EditorGUI.DisabledScope(!SearchEnabled))
                    {
                        if (GUILayout.Button("Search", m_Hight))
                        {
                            m_SearchResult = Parameter.EmbeddingsWindow.EmbeddingsContainerForSearch.SearchCloseAsset(Parameter.EmbeddingsWindow.EmbeddingsForSearch, Parameter.EmbeddingsWindow.SearchCount);
                        }
                    }

                    so_This.Update();

                    if (sp_SearchResult != null)
                    {
                        EditorGUILayout.PropertyField(sp_SearchResult, true);
                    }

                    so_This.ApplyModifiedProperties();
                }

                GUILayout.Box(string.Empty, GUILayout.ExpandWidth(true), m_LineHight); // ----------

                if (m_Others.Foldout = EditorGUILayout.Foldout(m_Others.Foldout, m_Others.Content))
                {
                    GUILayout.Space(10);

                    GUILayout.Label("Calculate distance", EditorStyles.boldLabel);

                    Parameter.EmbeddingsWindow.Embeddings1 = (EmbeddingsData) EditorGUILayout.ObjectField(Parameter.EmbeddingsWindow.Embeddings1, typeof(EmbeddingsData), true);
                    Parameter.EmbeddingsWindow.Embeddings2 = (EmbeddingsData) EditorGUILayout.ObjectField(Parameter.EmbeddingsWindow.Embeddings2, typeof(EmbeddingsData), true);

                    using (new EditorGUILayout.HorizontalScope(m_Hight))
                    {
                        if (GUILayout.Button("Calc", GUILayout.Width(50), m_Hight))
                        {
                            m_Distance = EmbeddingsData.CalcEuclideanDistance(Parameter.EmbeddingsWindow.Embeddings1, Parameter.EmbeddingsWindow.Embeddings2);
                        }

                        m_Distance = EditorGUILayout.FloatField(m_Distance, m_Hight);
                    }
                }
            }
#else
            using (new GUIColorScope(Color.red))
            {
                GUILayout.Label("Initializing...", m_Hight);
            }
#endif
        }

        private async UniTask<EmbeddingsData> CreateEmbeddingsInternal(string embeddingFileName, string prompt, string outputPath)
        {
            UpdateOpenAIAPI();

            m_TaskExist = true;
            m_TaskCanceled = false;

            float[] result;

            if (m_CreateDummy)
            {
                result = new float[1536];
            }
            else
            {
#if INITIALIZED
                result = await m_Api.Embeddings.GetEmbeddingsAsync(prompt);
#else
                await Task.CompletedTask;
                result = new float[1536];
#endif
            }

            CreateFolder(outputPath);

            var targetPath = Path.Combine(outputPath, embeddingFileName + m_Extension);

            EmbeddingsData obj;

            if (File.Exists(targetPath))
            {
                obj = AssetDatabase.LoadAssetAtPath<EmbeddingsData>(targetPath);
            }
            else
            {
                obj = ScriptableObject.CreateInstance<EmbeddingsData>();

                obj.Prompt = prompt;
                obj.Embeddings = result;

                AssetDatabase.CreateAsset(obj, targetPath);
                Debug.Log($"Asset({targetPath}) created.");
                AssetDatabase.Refresh();
            }

            return obj;
        }

        private UniTask<EmbeddingsData> CreateEmbeddingsAsync(string embeddingFileName, string prompt, string outputPath)
        {
            try
            {
                UpdateOpenAIAPI();

                m_TaskExist = true;
                m_TaskCanceled = false;

                return CreateEmbeddingsInternal(embeddingFileName, prompt, outputPath);
            }
            finally
            {
                m_TaskExist = false;
            }
        }

        private async UniTask UpdateEmbeddingsContainerAsync(string includeFolder, string outputFolder)
        {
            try
            {
                UpdateOpenAIAPI();

                m_TaskExist = true;
                m_TaskCanceled = false;

                Debug.Log($"Update Embeddings Container.");

                var objects = await FindAllAssetAsync<UnityEngine.Object>(includeFolder);

                Debug.Log($"Find {objects.Count} items");

                foreach (var obj in objects)
                {
                    if (Parameter.EmbeddingsWindow.LimitProcessSpeed)
                    {
                        await UniTask.Delay(1000);
                    }
                    else
                    {
                        await UniTask.Yield();
                    }

                    if (m_TaskCanceled)
                    {
                        Debug.LogWarning($"Task canceled.");
                        return;
                    }

                    var exist = false;

                    foreach (var binder in Parameter.EmbeddingsWindow.EmbeddingsContainerForUpdate.AssetBinderList)
                    {
                        if (obj.Equals(binder.Asett))
                        {
                            Debug.Log($"{obj.name} is already exist in container. skip it.");
                            exist = true;
                            break;
                        }
                    }

                    if (exist)
                    {
                        continue;
                    }

                    var assetFilePath = AssetDatabase.GetAssetPath(obj);
                    var assetFolderPath = Path.GetDirectoryName(assetFilePath);
                    var assetFolderName = Path.GetFileName(assetFolderPath);

                    var embeddingFileName = obj.name;
                    var embeddingFolderPath = Path.Combine(outputFolder, assetFolderName);
                    var embeddingFilePath = Path.Combine(embeddingFolderPath, embeddingFileName);

                    var embedding = AssetDatabase.LoadAssetAtPath<EmbeddingsData>(embeddingFilePath);

                    if (embedding != null)
                    {
                        Debug.Log($"find same name embeddings. use it. : {embeddingFilePath}");
                    }
                    else
                    {
                        var prompt = $"{assetFolderName} {obj.name}";

                        embedding = await CreateEmbeddingsInternal(embeddingFileName, prompt, embeddingFolderPath);
                    }

                    var add = new AssetBinder();

                    add.Asett = obj;
                    add.Embedding = embedding;

                    Parameter.EmbeddingsWindow.EmbeddingsContainerForUpdate.AssetBinderList.Add(add);

                    // await UniTask.Delay(1000);
                }

                Parameter.EmbeddingsWindow.EmbeddingsContainerForUpdate.ValidateAssetBinder();

                Debug.Log($"Update Embeddings Container finished");
            }
            finally
            {
                m_TaskExist = false;
                AssetDatabase.Refresh();
            }
        }

        private void CreateFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                var parent = Path.GetDirectoryName(path);
                var folder = Path.GetFileName(path);

                if (AssetDatabase.IsValidFolder(parent))
                {
                    AssetDatabase.CreateFolder(parent, folder);
                    Debug.Log($"Folder({path}) created.");
                    AssetDatabase.Refresh();
                }
                else
                {
                    CreateFolder(parent);
                }
            }
            else
            {
                return;
            }
        }

        private async UniTask<IReadOnlyList<T>> FindAllAssetAsync<T>(string directoryPath) where T : UnityEngine.Object
        {
            m_Count = 0;

            List<T> assets = new List<T>();
            var fileNames = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);

            await UniTask.Yield();

            foreach (var fileName in fileNames)
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(fileName);

                if (asset != null) { assets.Add(asset); m_Count++; }                

                await UniTask.Yield();
            }

            return assets;
        }
    }
}