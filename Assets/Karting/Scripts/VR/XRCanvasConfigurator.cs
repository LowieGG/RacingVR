using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR;

namespace KartGame.KartSystems
{
    [DefaultExecutionOrder(-1000)]
    public class XRCanvasConfigurator : MonoBehaviour
    {
        [Header("Head-Locked HUD")]
        public bool UseWorldSpaceHeadLockedCanvases = true;
        public Vector3 CanvasLocalPosition = new Vector3(0f, 0f, 1.2f);
        public Vector3 CanvasLocalEulerAngles = Vector3.zero;
        public Vector2 CanvasSize = new Vector2(1280f, 720f);
        public float CanvasScale = 0.00125f;
        public int SortingOrder = 5000;

        [Header("World-Space Exceptions")]
        public string[] KeepWorldSpaceCanvasNames = { "DashboardCanvas" };

        [Header("Fallback Screen Space Camera")]
        public float PlaneDistance = 0.75f;

        [Header("VR Button Submit")]
        public XRNode ControllerNode = XRNode.RightHand;
        public QuestControllerButton SubmitButton = QuestControllerButton.Trigger;
        public QuestControllerButton SubmitAltButton = QuestControllerButton.PrimaryButton;
        public float ButtonThreshold = 0.2f;

        InputDevice m_Controller;
        bool m_SubmitWasPressed;
        float m_NextRefreshTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void CreateRuntimeInstance()
        {
            if (FindObjectOfType<XRCanvasConfigurator>() != null)
            {
                return;
            }

            GameObject instance = new GameObject("XRCanvasConfigurator");
            DontDestroyOnLoad(instance);
            instance.AddComponent<XRCanvasConfigurator>();
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            ConfigureCanvases();
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void Update()
        {
            if (Time.unscaledTime >= m_NextRefreshTime)
            {
                m_NextRefreshTime = Time.unscaledTime + 0.25f;
                ConfigureCanvases();
            }

            HandleVrSubmit();
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ConfigureCanvases();
        }

        void ConfigureCanvases()
        {
            Camera targetCamera = FindTargetCamera();
            if (targetCamera == null)
            {
                return;
            }

            Canvas[] canvases = FindObjectsOfType<Canvas>(true);
            int configuredIndex = 0;
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                if (canvas == null || IsNestedCanvas(canvas))
                {
                    continue;
                }

                if (ShouldKeepInSceneWorldSpace(canvas))
                {
                    continue;
                }

                ConfigureCanvas(canvas, targetCamera, configuredIndex);
                configuredIndex++;
            }
        }

        bool ShouldKeepInSceneWorldSpace(Canvas canvas)
        {
            if (KeepWorldSpaceCanvasNames == null)
            {
                return false;
            }

            for (int i = 0; i < KeepWorldSpaceCanvasNames.Length; i++)
            {
                string canvasName = KeepWorldSpaceCanvasNames[i];
                if (!string.IsNullOrEmpty(canvasName) && canvas.name == canvasName)
                {
                    canvas.renderMode = RenderMode.WorldSpace;
                    canvas.worldCamera = null;
                    return true;
                }
            }

            return false;
        }

        void ConfigureCanvas(Canvas canvas, Camera targetCamera, int configuredIndex)
        {
            canvas.worldCamera = targetCamera;
            canvas.overrideSorting = true;
            canvas.sortingOrder = SortingOrder + configuredIndex;

            RectTransform rect = canvas.GetComponent<RectTransform>();
            if (rect == null)
            {
                return;
            }

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = CanvasSize;
                scaler.matchWidthOrHeight = 0.5f;
                scaler.dynamicPixelsPerUnit = 10f;
            }

            if (UseWorldSpaceHeadLockedCanvases)
            {
                canvas.renderMode = RenderMode.WorldSpace;
                rect.SetParent(targetCamera.transform, false);
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = CanvasSize;
                rect.localPosition = CanvasLocalPosition;
                rect.localRotation = Quaternion.Euler(CanvasLocalEulerAngles);
                rect.localScale = Vector3.one * CanvasScale;
            }
            else
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.planeDistance = PlaneDistance;
                rect.localScale = Vector3.one;
            }
        }

        void HandleVrSubmit()
        {
            EnsureController();

            bool submitPressed = QuestControllerButtonUtility.IsPressed(m_Controller, SubmitButton, ButtonThreshold) ||
                                 QuestControllerButtonUtility.IsPressed(m_Controller, SubmitAltButton, ButtonThreshold);
            bool submitDown = submitPressed && !m_SubmitWasPressed;
            m_SubmitWasPressed = submitPressed;

            if (!submitDown || !ShouldSubmitUi())
            {
                return;
            }

            Button button = FindBestVisibleButton();
            if (button != null)
            {
                button.onClick.Invoke();
            }
        }

        void EnsureController()
        {
            if (!m_Controller.isValid)
            {
                m_Controller = InputDevices.GetDeviceAtXRNode(ControllerNode);
            }
        }

        bool ShouldSubmitUi()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            return Time.timeScale == 0f ||
                   sceneName.Contains("Win") ||
                   sceneName.Contains("Lose") ||
                   sceneName.Contains("Menu") ||
                   HasVisibleInteractableButton();
        }

        bool HasVisibleInteractableButton()
        {
            Button[] buttons = FindObjectsOfType<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (IsVisibleInteractableButton(buttons[i]))
                {
                    return true;
                }
            }

            return false;
        }

        Button FindBestVisibleButton()
        {
            if (EventSystem.current != null &&
                EventSystem.current.currentSelectedGameObject != null &&
                EventSystem.current.currentSelectedGameObject.TryGetComponent(out Button selectedButton) &&
                IsVisibleInteractableButton(selectedButton))
            {
                return selectedButton;
            }

            Button fallback = null;
            Button[] buttons = FindObjectsOfType<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (!IsVisibleInteractableButton(button))
                {
                    continue;
                }

                string label = GetButtonLabel(button).ToLowerInvariant();
                if (label.Contains("play again") || label.Contains("start") || label.Contains("continue"))
                {
                    return button;
                }

                if (fallback == null)
                {
                    fallback = button;
                }
            }

            return fallback;
        }

        static bool IsVisibleInteractableButton(Button button)
        {
            if (button == null || !button.gameObject.activeInHierarchy || !button.interactable)
            {
                return false;
            }

            CanvasGroup[] groups = button.GetComponentsInParent<CanvasGroup>(true);
            for (int i = 0; i < groups.Length; i++)
            {
                if (groups[i].alpha <= 0.05f || !groups[i].interactable)
                {
                    return false;
                }
            }

            return true;
        }

        static string GetButtonLabel(Button button)
        {
            TextMeshProUGUI tmp = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null)
            {
                return tmp.text;
            }

            Text text = button.GetComponentInChildren<Text>(true);
            return text != null ? text.text : button.gameObject.name;
        }

        static bool IsNestedCanvas(Canvas canvas)
        {
            Transform parent = canvas.transform.parent;
            return parent != null && parent.GetComponentInParent<Canvas>() != null;
        }

        static Camera FindTargetCamera()
        {
            Camera[] cameras = Camera.allCameras;
            for (int i = 0; i < cameras.Length; i++)
            {
                Camera camera = cameras[i];
                if (camera != null &&
                    camera.isActiveAndEnabled &&
                    camera.transform.root.name.ToLowerInvariant().Contains("xr"))
                {
                    return camera;
                }
            }

            Camera mainCamera = Camera.main;
            if (mainCamera != null && mainCamera.isActiveAndEnabled)
            {
                return mainCamera;
            }

            for (int i = 0; i < cameras.Length; i++)
            {
                Camera camera = cameras[i];
                if (camera != null && camera.isActiveAndEnabled)
                {
                    return camera;
                }
            }

            return null;
        }
    }
}
