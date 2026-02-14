using System;
using System.Collections.Generic;
using System.Dynamic;
using UnityEngine;

namespace Unity.HLODSystem
{
    /// <summary>
    /// [설명]: 유니티 인스펙터에서 직렬화 가능한 동적 객체(DynamicObject) 구현체입니다.
    /// 런타임에 유연하게 속성을 추가하거나 관리할 수 있으며, 에디터 상에서의 직렬화를 지원합니다.
    /// </summary>
    [Serializable]
    public class SerializableDynamicObject : DynamicObject, ISerializationCallbackReceiver
    {

        interface ISerializeItem
        {
            void SetName(string name);
            string GetName();

            object GetData();
        }
        [Serializable]
        class SerializeItem<T> : ISerializeItem
        {
            [SerializeField]
            public string Name;
            [SerializeField]
            public T Data;


            public void SetName(string name)
            {
                Name = name;
            }
            public string GetName()
            {
                return Name;
            }


            public void SetData(T data)
            {
                Data = data;
            }
            public object GetData()
            {
                return Data;
            }
        }

        [Serializable]
        class JsonSerializedData
        {
            [SerializeField]
            public string Type;
            [SerializeField]
            public string Data;
        }

        [SerializeField]
        private List<JsonSerializedData> m_serializeItems = new List<JsonSerializedData>();

        /// <summary>
        /// [설명]: 런타임 동적 데이터를 저장하는 딕셔너리입니다.
        /// </summary>
        private Dictionary<string, object> m_dynamicContext = new Dictionary<string, object>();

        public bool ContainsKey(string key)
        {            
            return m_dynamicContext.ContainsKey(key);
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            if (m_dynamicContext.ContainsKey(binder.Name) == false)
            {
                m_dynamicContext.Add(binder.Name, value);
            }
            else
            {
                m_dynamicContext[binder.Name] = value;
            }

            return true;
        }

        public object this[string key]
        {
            get
            {
                if (m_dynamicContext.TryGetValue(key, out object result))
                    return result;
                return null;
            }
            set
            {
                if (m_dynamicContext.ContainsKey(key))
                    m_dynamicContext[key] = value;
                else
                    m_dynamicContext.Add(key, value);
            }
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (m_dynamicContext.TryGetValue(binder.Name, out result))
            {
                return true;
            }

            return false;
        }

        public void OnBeforeSerialize()
        {
            m_serializeItems.Clear();
                
            foreach (var pair in m_dynamicContext)
            {
                if (pair.Value == null)
                    continue;

                Type genericClass = typeof(SerializeItem<>);
                Type constructedClass = genericClass.MakeGenericType(pair.Value.GetType());

                ISerializeItem item = Activator.CreateInstance(constructedClass) as ISerializeItem;
                if (item == null)
                    continue;

                var methodInfo = constructedClass.GetMethod("SetData");
                methodInfo.Invoke(item, new object[]{pair.Value});

                item.SetName(pair.Key);

                JsonSerializedData data = new JsonSerializedData();
                data.Type = item.GetType().AssemblyQualifiedName;
                data.Data = JsonUtility.ToJson(item);

                m_serializeItems.Add(data);

            }
        }

        public void OnAfterDeserialize()
        {
            m_dynamicContext.Clear();

            for (int i = 0; i < m_serializeItems.Count; ++i)
            {
                if (string.IsNullOrEmpty(m_serializeItems[i].Type))
                    continue;

                Type type = Type.GetType(m_serializeItems[i].Type);
                if (type == null)
                    continue;

                var data = JsonUtility.FromJson(m_serializeItems[i].Data, type) as ISerializeItem;
                if (data == null)
                    continue;

                m_dynamicContext.Add(data.GetName(), data.GetData());
            }

            m_serializeItems.Clear();
        }

        public SerializableDynamicObject()
        {
        }


    }

}