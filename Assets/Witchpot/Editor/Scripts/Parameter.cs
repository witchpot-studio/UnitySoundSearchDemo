using System;
using UnityEditor;
using UnityEngine;

namespace Witchpot.Editor
{
    [FilePath("Assets/Witchpot/Editor/Scripts/Parameter.asset", FilePathAttribute.Location.ProjectFolder)]
    public class Parameter : ScriptableSingleton<Parameter>
    {
        public enum EFolderPathStatus
        {
            Unknown,
            Valid,
            StartIsNotAsset,
            FolderNotExist,
        }

        private const string FirstApiKey = "Your api key";

        // OpenAI
        [SerializeField]
        private string m_ApiKey = FirstApiKey;
        public static string ApiKey { get => instance.m_ApiKey; set => instance.m_ApiKey = value; }

        public static bool ApiKeyIsReady => !string.IsNullOrEmpty(instance.m_ApiKey) && !string.Equals(instance.m_ApiKey, FirstApiKey);

        [SerializeField]
        private string m_Organization = string.Empty;
        public static string Organization { get => instance.m_Organization; set => instance.m_Organization = value; }

        public static void Save() { instance.Save(true); }

        // EmbeddingsWindow
        [SerializeField]
        private EmbeddingsWindow m_EmbeddingsWindow;
        public EmbeddingsWindow Embeddings => m_EmbeddingsWindow;

        [Serializable]
        public class EmbeddingsWindow
        {
            // Create Single Embeddings
            [SerializeField]
            private string m_OutputFolder = "Assets/Witchpot/Editor/Embeddings";
            public static string OutputFolder { get => instance.Embeddings.m_OutputFolder; set => instance.Embeddings.m_OutputFolder = value; }

            public static EFolderPathStatus IsValidOutputFolderPath => ValidateFolderPath(instance.Embeddings.m_OutputFolder);

            [SerializeField]
            private string m_Prompt = string.Empty;
            public static string Prompt { get => instance.Embeddings.m_Prompt; set => instance.Embeddings.m_Prompt = value; }

            [SerializeField]
            private bool m_FileNameUsingPrompt = true;
            public static bool UsePromptForFileName { get => instance.Embeddings.m_FileNameUsingPrompt; set => instance.Embeddings.m_FileNameUsingPrompt = value; }

            [SerializeField]
            private string m_FileName = string.Empty;
            public static string FileName { get => instance.Embeddings.m_FileName; set => instance.Embeddings.m_FileName = value; }

            // Update Embeddings Container
            [SerializeField]
            private EmbeddingsContainer m_EmbeddingsContainerForUpdate;
            public static EmbeddingsContainer EmbeddingsContainerForUpdate { get => instance.Embeddings.m_EmbeddingsContainerForUpdate; set => instance.Embeddings.m_EmbeddingsContainerForUpdate = value; }

            [SerializeField]
            private string m_IncludeFolder = "Assets/Include";
            public static string IncludeFolder { get => instance.Embeddings.m_IncludeFolder; set => instance.Embeddings.m_IncludeFolder = value; }

            public static EFolderPathStatus IsValidIncludeFolder => ValidateFolderPath(instance.Embeddings.m_IncludeFolder);

            [SerializeField]
            private bool m_LimitProcessSpeed = true;
            public static bool LimitProcessSpeed { get => instance.Embeddings.m_LimitProcessSpeed; set => instance.Embeddings.m_LimitProcessSpeed = value; }

            // Serch in Embeddings Container
            [SerializeField]
            private EmbeddingsContainer m_EmbeddingsContainerForSearch;
            public static EmbeddingsContainer EmbeddingsContainerForSearch { get => instance.Embeddings.m_EmbeddingsContainerForSearch; set => instance.Embeddings.m_EmbeddingsContainerForSearch = value; }

            [SerializeField]
            private EmbeddingsData m_EmbeddingsForSearch;
            public static EmbeddingsData EmbeddingsForSearch { get => instance.Embeddings.m_EmbeddingsForSearch; set => instance.Embeddings.m_EmbeddingsForSearch = value; }

            [SerializeField]
            private int m_SearchCount = 3;
            public static int SearchCount
            {
                get => instance.Embeddings.m_SearchCount;
                set
                {
                    if (value >= 0)
                    {
                        instance.Embeddings.m_SearchCount = value;
                    }
                    else
                    {
                        instance.Embeddings.m_SearchCount = 0;
                    }
                }
            }

            // Calclate Distance
            [SerializeField]
            private EmbeddingsData m_Embeddings1;
            public static EmbeddingsData Embeddings1 { get => instance.Embeddings.m_Embeddings1; set => instance.Embeddings.m_Embeddings1 = value; }

            [SerializeField]
            private EmbeddingsData m_Embeddings2;
            public static EmbeddingsData Embeddings2 { get => instance.Embeddings.m_Embeddings2; set => instance.Embeddings.m_Embeddings2 = value; }

            private static EFolderPathStatus ValidateFolderPath(string path)
            {
                if (!path.StartsWith("Assets/"))
                {
                    return EFolderPathStatus.StartIsNotAsset;
                }

                if (!System.IO.Directory.Exists(path))
                {
                    return EFolderPathStatus.FolderNotExist;
                }

                return EFolderPathStatus.Valid;
            }

            public static bool ValidteFileName(string filename)
            {
                if (string.IsNullOrWhiteSpace(filename)) { return false; }
                char[] target = System.IO.Path.GetInvalidFileNameChars();
                return (filename.IndexOfAny(target) < 0);
            }
        }

        [SerializeField]
        private SearchWindow m_SearchWindow;
        public SearchWindow Search => m_SearchWindow;

        [Serializable]
        public class SearchWindow
        {
            [SerializeField]
            private EmbeddingsContainer m_EmbeddingsContainerForSearch;
            public static EmbeddingsContainer EmbeddingsContainerForSearch { get => instance.Search.m_EmbeddingsContainerForSearch; set => instance.Search.m_EmbeddingsContainerForSearch = value; }

            [SerializeField]
            private int m_SearchCount = 3;
            public static int SearchCount
            {
                get => instance.Search.m_SearchCount;
                set
                {
                    if (value >= 0)
                    {
                        instance.Search.m_SearchCount = value;
                    }
                    else
                    {
                        instance.Search.m_SearchCount = 0;
                    }
                }
            }

            [SerializeField]
            private string m_Prompt = string.Empty;
            public static string Prompt { get => instance.Search.m_Prompt; set => instance.Search.m_Prompt = value; }
        }
    }
}
