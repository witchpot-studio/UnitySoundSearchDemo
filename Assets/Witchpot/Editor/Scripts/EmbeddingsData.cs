using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Witchpot.Editor
{
    public class EmbeddingsData : ScriptableObject
    {
        public static float CalcEuclideanDistance(EmbeddingsData a, EmbeddingsData b)
        {
            if (a.Embeddings.Length != b.Embeddings.Length)
            {
                Debug.LogError($"Embeddings has defferent Length. Aborting.");
                return -1;
            }

            var sum = 0f;

            for (int i = 0; i < a.Embeddings.Length; i++)
            {
                var dif = b.Embeddings[i] - a.Embeddings[i];
                sum += dif * dif;
            }

            return MathF.Sqrt(sum);
        }

        [SerializeField]
        private string m_Prompt;

        public string Prompt
        {
            get => m_Prompt;
            set => m_Prompt = value;
        }

        [SerializeField]
        private float[] m_Embeddings;

        public float[] Embeddings
        {
            get => m_Embeddings;
            set => m_Embeddings = value;
        }
    }
}