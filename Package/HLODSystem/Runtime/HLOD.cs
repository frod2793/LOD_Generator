using System;
using System.Collections;
using System.Collections.Generic;
using Unity.HLODSystem.SpaceManager;
using Unity.HLODSystem.Streaming;
using Unity.HLODSystem.Utils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.HLODSystem
{
    public class HLOD : MonoBehaviour, ISerializationCallbackReceiver, IGeneratedResourceManager
    {
        public const string k_HLODLayerStr = "HLOD";

        [SerializeField]
        private float m_chunkSize = 30.0f;
        [SerializeField]
        private float m_lodDistance = 0.3f;
        [SerializeField]
        private float m_cullDistance = 0.01f;
        [SerializeField]
        private float m_minObjectSize = 0.0f;

        private Type m_spaceSplitterType;
        private Type m_batcherType;
        private Type m_simplifierType;
        private Type m_streamingType;
        private Type m_userDataSerializerType;


        [SerializeField] 
        private string m_spaceSplitterTypeStr;
        [SerializeField]
        private string m_batcherTypeStr;        //< unity serializer is not support serialization with System.Type
                                                //< So, we should convert to string to store value.
        [SerializeField]
        private string m_simplifierTypeStr;
        [SerializeField]
        private string m_streamingTypeStr;
        [SerializeField]
        private string m_userDataSerializerTypeStr;

        [SerializeField]
        private SerializableDynamicObject m_spaceSplitterOptions = new SerializableDynamicObject();
        [SerializeField]
        private SerializableDynamicObject m_simplifierOptions = new SerializableDynamicObject();
        [SerializeField]
        private SerializableDynamicObject m_batcherOptions = new SerializableDynamicObject();
        [SerializeField]
        private SerializableDynamicObject m_streamingOptions = new SerializableDynamicObject();
        
        [SerializeField]
        private List<Object> m_generatedObjects = new List<Object>();
        [SerializeField]
        private List<GameObject> m_convertedPrefabObjects = new List<GameObject>();


        public float ChunkSize
        {
            get { return m_chunkSize; }
        }

        public float LODDistance
        {
            get { return m_lodDistance; }
        }
        public float CullDistance
        {
            set { m_cullDistance = value; }
            get { return m_cullDistance; }
        }

        public Type SpaceSplitterType
        {
            set { m_spaceSplitterType = value; }
            get { return m_spaceSplitterType; }
        }

        public Type BatcherType
        {
            set { m_batcherType = value; }
            get { return m_batcherType; }
        }

        public Type SimplifierType
        {
            set { m_simplifierType = value; }
            get { return m_simplifierType; }
        }

        public Type StreamingType
        {
            set { m_streamingType = value; }
            get { return m_streamingType; }
        }

        public Type UserDataSerializerType
        {
            set { m_userDataSerializerType = value; }
            get { return m_userDataSerializerType; }
        }

        public SerializableDynamicObject SpaceSplitterOptions
        {
            get { return m_spaceSplitterOptions; }
        }
        public SerializableDynamicObject BatcherOptions
        {
            get { return m_batcherOptions; }
        }

        public SerializableDynamicObject StreamingOptions
        {
            get { return m_streamingOptions; }
        }

        public SerializableDynamicObject SimplifierOptions
        {
            get { return m_simplifierOptions; }
        }

        public float MinObjectSize
        {
            set { m_minObjectSize = value; }
            get { return m_minObjectSize; }
        }

        
#if UNITY_EDITOR
        public List<Object> GeneratedObjects
        {
            get { return m_generatedObjects; }
        }

        public List<GameObject> ConvertedPrefabObjects
        {
            get { return m_convertedPrefabObjects; }
        }

        public List<HLODControllerBase> GetHLODControllerBases()
        {
            List<HLODControllerBase> controllerBases = new List<HLODControllerBase>();

            foreach (Object obj in m_generatedObjects)
            {
                var controllerBase = obj as HLODControllerBase;
                if ( controllerBase != null )
                    controllerBases.Add(controllerBase);
            }
            
            //if controller base doesn't exists in the generated objects, it was created from old version.
            //so adding controller base manually.
            if (controllerBases.Count == 0)
            {
                var controller = GetComponent<Streaming.HLODControllerBase>();
                if (controller != null)
                {
                    controllerBases.Add(controller);
                }
            }
            return controllerBases;
        }
#endif
        public Bounds GetBounds()
        {
            Bounds ret = new Bounds();
            var renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                ret.center = Vector3.zero;
                ret.size = Vector3.zero;
                return ret;
            }

            Bounds bounds = Utils.BoundsUtils.CalcLocalBounds(renderers[0], transform);
            for (int i = 1; i < renderers.Length; ++i)
            {
                bounds.Encapsulate(Utils.BoundsUtils.CalcLocalBounds(renderers[i], transform));
            }

            ret.center = bounds.center;
            float max = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            ret.size = new Vector3(max, max, max);  

            return ret;
        }

    

        public void OnBeforeSerialize()
        {
            if (m_spaceSplitterType != null)
                m_spaceSplitterTypeStr = m_spaceSplitterType.AssemblyQualifiedName;
            if ( m_batcherType != null )
                m_batcherTypeStr = m_batcherType.AssemblyQualifiedName;
            if (m_simplifierType != null)
                m_simplifierTypeStr = m_simplifierType.AssemblyQualifiedName;
            if (m_streamingType != null)
                m_streamingTypeStr = m_streamingType.AssemblyQualifiedName;
            if (m_userDataSerializerType != null)
                m_userDataSerializerTypeStr = m_userDataSerializerType.AssemblyQualifiedName;
        }

        public void OnAfterDeserialize()
        {
            if (string.IsNullOrEmpty(m_spaceSplitterTypeStr))
            {
                m_spaceSplitterType = null;
            }
            else
            {
                m_spaceSplitterType = Type.GetType(m_spaceSplitterTypeStr);
            }
            
            if (string.IsNullOrEmpty(m_batcherTypeStr))
            {
                m_batcherType = null;
            }
            else
            {
                m_batcherType = Type.GetType(m_batcherTypeStr);
            }

            if (string.IsNullOrEmpty(m_simplifierTypeStr))
            {
                m_simplifierType = null;
            }
            else
            {
                m_simplifierType = Type.GetType(m_simplifierTypeStr);
            }

            if (string.IsNullOrEmpty(m_streamingTypeStr))
            {
                m_streamingType = null;
            }
            else
            {
                m_streamingType = Type.GetType(m_streamingTypeStr);
            }

            if (string.IsNullOrEmpty(m_userDataSerializerTypeStr))
            {
                m_userDataSerializerType = null;
            }
            else
            {
                m_userDataSerializerType = Type.GetType(m_userDataSerializerTypeStr);
            }
            
        }

        public void AddGeneratedResource(Object obj)
        {
            m_generatedObjects.Add(obj);
        }

        public bool IsGeneratedResource(Object obj)
        {
            return m_generatedObjects.Contains(obj);
        }

        public void AddConvertedPrefabResource(GameObject obj)
        {
            m_convertedPrefabObjects.Add(obj);
        }

    }

}