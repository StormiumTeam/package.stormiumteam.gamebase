using UnityEngine;

namespace Runtime.Components
{
    [RequireComponent(typeof(Camera))]
    public class GameCamera : MonoBehaviour
    {
        public Camera Camera { get; private set; }

        private void OnEnable()
        {
            Camera = GetComponent<Camera>();
        }

        private void OnDisable()
        {
            Camera = null;
        }
    }
}