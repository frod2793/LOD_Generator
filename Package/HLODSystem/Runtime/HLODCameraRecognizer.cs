using System;
using UnityEngine;

namespace Unity.HLODSystem
{
    public class HLODCameraRecognizer : MonoBehaviour
    {
        private Camera m_recognizedCamera;

        private static Camera s_recognizedCamera;
        public static Camera RecognizedCamera => s_recognizedCamera;

        public Camera LocalRecognizedCamera => m_recognizedCamera;

        [SerializeField]
        private int m_id;
        [SerializeField]
        private int m_priority;


        public int ID
        {
            get
            {
                return m_id;
            }
        }

        public int Priority
        {
            get
            {
                return m_priority;
            }
        }
        
        

        private void Awake()
        {
            m_recognizedCamera = GetComponent<Camera>();
            s_recognizedCamera = m_recognizedCamera;
        }
        private void OnEnable()
        {
            HLODCameraRecognizerManager.Instance.RegisterRecognizer(this);
        }

        private void OnDisable()
        {
            HLODCameraRecognizerManager.Instance.UnregisterRecognizer(this);            
        }
        
        public void Active()
        {
            if (enabled == false)
            {
                Debug.LogError("Failed to active HLODCameraRecognizer. It is not Enabled.");
                return;
            }

            HLODCameraRecognizerManager.Instance.Active(this);
        }
    }
}