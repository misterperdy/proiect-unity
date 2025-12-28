using UnityEngine;

namespace UI
{
    public class MainMenuAnimation : MonoBehaviour
    {
        [Header("Position Settings")]
        [SerializeField, Range(0f, 5f)] private float speedPos = 0.73f;
        [SerializeField, Range(0f, 2f)] private float posXVariation = 0f;
        [SerializeField, Range(0f, 2f)] private float posYVariation = 0f;
        [SerializeField, Range(0f, 2f)] private float posZVariation = 0.17f;

        [Header("Rotation Settings")]
        [SerializeField, Range(0f, 5f)] private float speedRot = 0.41f;
        [SerializeField, Range(0f, 10f)] private float rotXVariation = 0.62f;
        [SerializeField, Range(0f, 10f)] private float rotYVariation = 10f;
        [SerializeField, Range(0f, 10f)] private float rotZVariation = 3.86f;

        private Vector3 _startPosition;
        private Quaternion _startRotation;

        private void Start()
        {
            _startPosition = transform.position;
            _startRotation = transform.rotation;
        }   

        private void Update()
        {
            // Calculate sine waves
            float sinPos = Mathf.Sin(Time.time * speedPos);
            float sinRot = Mathf.Sin(Time.time * speedRot);

            // Apply Position
            Vector3 posOffset = new Vector3(
                sinPos * posXVariation,
                sinPos * posYVariation,
                sinPos * posZVariation
            );
            transform.position = _startPosition + posOffset;

            // Apply Rotation
            Vector3 rotOffset = new Vector3(
                sinRot * rotXVariation,
                sinRot * rotYVariation,
                sinRot * rotZVariation
            );
            
            // Use localEulerAngles or global? Usually rotation animations are relative to start.
            // Using Quaternion * Quaternion to rotate relative to start rotation
            transform.rotation = Quaternion.Euler(_startRotation.eulerAngles + rotOffset);
        }
    }
}