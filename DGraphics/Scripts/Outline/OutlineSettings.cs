using System;
using System.Collections.Generic;
using DGraphics.Inspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace DGraphics.PostProcessing.Outline
{
    [ExecuteAlways]
    public class OutlineSettings : MonoBehaviour
    {
        [ReadOnly] 
        public List<OutlineSetter> OutlineSetters = new();
        private static OutlineSettings _instance;
        public static OutlineSettings Instance
        {
            get
            {
                if (_instance == null)
                    _instance = CreateInstance();
                return _instance;
            }
        }
        
        public bool AddOutlineSetter(OutlineSetter setter)
        {
            if (OutlineSetters.Contains(setter))
                return false;

            OutlineSetters.Add(setter);
            UpdateIndexes();
            return true;
        }

        public void RemoveOutlineSetter(OutlineSetter setter)
        {
            if (!OutlineSetters.Contains(setter))
                return;
            
            OutlineSetters.Remove(setter);
            UpdateIndexes();
        }

        public void UpdateIndexes()
        {
            var comparer = Comparer<int>.Create((a, b) =>
            {
                if (a >= b)
                {
                    return 1;
                }
                else
                {
                    return -1;
                }
            });
            OutlineSetters.Sort((a, b) => comparer.Compare(a.Index, b.Index));
            
            for (int i = 0; i < OutlineSetters.Count; i++)
            {
                OutlineSetters[i].UpdateIndex(i + 1);
            }
        }
        
        private static OutlineSettings CreateInstance()
        {
            if (_instance != null) return _instance;
            var findResult = FindObjectOfType<OutlineSettings>();
            if (findResult != null) return findResult;
            if (!Application.isPlaying) 
                return null;
            var go = new GameObject("OutlineSettings");
                DontDestroyOnLoad(go);
            var instance = go.AddComponent<OutlineSettings>();
            return instance;
        }

        private void Reset()
        {
            var setters = FindObjectsOfType<OutlineSetter>();
            foreach (var setter in setters)
            {
                setter.Init();
                AddOutlineSetter(setter);
            }
        }

        private void OnDisable()
        {
            if (_instance == this) _instance = null;
        }
    }
}