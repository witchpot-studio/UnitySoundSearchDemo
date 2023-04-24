using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace Witchpot.Editor
{
    [CreateAssetMenu(menuName = "Witchpt/EmbeddingsContainer", fileName = "EmbeddingsContainer")]
    public class EmbeddingsContainer : ScriptableObject
    {
        [ContextMenu("Validate AssetBinder")]
        public void ValidateAssetBinder()
        {
            foreach (var item in m_AssetBinderList)
            {
                item.OnValidate();
            }
        }

        [SerializeField]
        private List<AssetBinder> m_AssetBinderList;

        public IList<AssetBinder> AssetBinderList => m_AssetBinderList;

        public List<AssetBinder> SearchCloseAsset(EmbeddingsData data, int size)
        {
            foreach (var binder in m_AssetBinderList)
            {
                binder.Distance = EmbeddingsData.CalcEuclideanDistance(data, binder.Embedding);
            }

            return m_AssetBinderList.OrderBy(x => x.Distance).Take(size).ToList();
        }
    }

    [Serializable]
    public class AssetBinder
    {
        [SerializeField]
        private string m_Name = default;

        [SerializeField]
        private string m_FolderName;

        public string FolderName
        {
            get => m_FolderName;
            set => m_FolderName = value;
        }

        [SerializeField]
        private UnityEngine.Object m_Asset;

        public UnityEngine.Object Asett
        {
            get => m_Asset;
            set => m_Asset = value;
        }

        [SerializeField]
        private EmbeddingsData m_Embedding;

        public EmbeddingsData Embedding
        {
            get => m_Embedding;
            set => m_Embedding = value;
        }

        // For Sorting
        private float m_Distance;

        public float Distance
        {
            get => m_Distance;
            set => m_Distance = value;
        }

        public void OnValidate()
        {
            ValidateName();
            ValidateFolderName();
        }

        public void ValidateName()
        {
            if (!string.IsNullOrEmpty(m_Name)) { return; }

            if (m_Asset != null)
            {
                m_Name = m_Asset.name;
            }
            else if (m_Embedding != null)
            {
                m_Name = m_Embedding.Prompt;
            }
            else
            {
                m_Name = string.Empty;
            }
        }

        public void ValidateFolderName()
        {
            if (m_Asset == null) { return; }

            var path = AssetDatabase.GetAssetPath(m_Asset);

            if (string.IsNullOrEmpty(path)) { return; }

            path = System.IO.Path.GetDirectoryName(path);

            if (string.IsNullOrEmpty(path)) { return; }

            path = System.IO.Path.GetFileName(path);

            if (string.IsNullOrEmpty(path)) { return; }

            m_FolderName = path;
        }
    }
}