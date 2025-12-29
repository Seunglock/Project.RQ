using UnityEngine;
using UnityEngine.InputSystem;

namespace GuildReceptionist
{
    /// <summary>
    /// Input abstraction layer for cross-platform input handling
    /// Wraps Unity's Input System to provide consistent interface across platforms
    /// </summary>
    public class InputAbstraction : MonoBehaviour
    {
        private static InputAbstraction _instance;
        public static InputAbstraction Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("InputAbstraction");
                    _instance = go.AddComponent<InputAbstraction>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private PlayerInput _playerInput;
        private InputAction _selectAction;
        private InputAction _cancelAction;
        private InputAction _navigateAction;
        private InputAction _submitAction;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeInputActions();
        }

        private void InitializeInputActions()
        {
            // Create input actions programmatically for platform independence
            var inputActionAsset = Resources.Load<InputActionAsset>("InputSystem_Actions");
            
            if (inputActionAsset != null)
            {
                _selectAction = inputActionAsset.FindAction("Select");
                _cancelAction = inputActionAsset.FindAction("Cancel");
                _navigateAction = inputActionAsset.FindAction("Navigate");
                _submitAction = inputActionAsset.FindAction("Submit");

                _selectAction?.Enable();
                _cancelAction?.Enable();
                _navigateAction?.Enable();
                _submitAction?.Enable();
            }
            else
            {
                Debug.LogWarning("InputSystem_Actions asset not found. Creating default input actions.");
                CreateDefaultInputActions();
            }
        }

        private void CreateDefaultInputActions()
        {
            // Create default input actions if asset is missing
            var map = new InputActionMap("UI");
            
            _selectAction = map.AddAction("Select", binding: "<Mouse>/leftButton");
            _selectAction.AddBinding("<Touchscreen>/primaryTouch/tap");
            _selectAction.AddBinding("<Gamepad>/buttonSouth");
            
            _cancelAction = map.AddAction("Cancel", binding: "<Keyboard>/escape");
            _cancelAction.AddBinding("<Gamepad>/buttonEast");
            
            _navigateAction = map.AddAction("Navigate", binding: "<Keyboard>/arrows");
            _navigateAction.AddBinding("<Gamepad>/leftStick");
            
            _submitAction = map.AddAction("Submit", binding: "<Keyboard>/enter");
            _submitAction.AddBinding("<Gamepad>/buttonSouth");

            map.Enable();
        }

        /// <summary>
        /// Check if select action was performed this frame
        /// </summary>
        public bool SelectPressed => _selectAction?.WasPressedThisFrame() ?? false;

        /// <summary>
        /// Check if cancel action was performed this frame
        /// </summary>
        public bool CancelPressed => _cancelAction?.WasPressedThisFrame() ?? false;

        /// <summary>
        /// Get navigation input as Vector2
        /// </summary>
        public Vector2 NavigationInput => _navigateAction?.ReadValue<Vector2>() ?? Vector2.zero;

        /// <summary>
        /// Check if submit action was performed this frame
        /// </summary>
        public bool SubmitPressed => _submitAction?.WasPressedThisFrame() ?? false;

        /// <summary>
        /// Get mouse/touch position in screen space
        /// </summary>
        public Vector2 PointerPosition
        {
            get
            {
#if UNITY_STANDALONE || UNITY_EDITOR
                return Mouse.current?.position.ReadValue() ?? Vector2.zero;
#elif UNITY_ANDROID || UNITY_IOS
                return Touchscreen.current?.primaryTouch.position.ReadValue() ?? Vector2.zero;
#elif UNITY_SWITCH
                // Switch uses controller by default
                return Vector2.zero;
#else
                return Vector2.zero;
#endif
            }
        }

        private void OnDestroy()
        {
            _selectAction?.Disable();
            _cancelAction?.Disable();
            _navigateAction?.Disable();
            _submitAction?.Disable();
        }
    }
}
