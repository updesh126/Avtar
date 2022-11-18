using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ReadyPlayerMe
{
    public class RPMRuntime : MonoBehaviour
    {
        [SerializeField] private Button displayButton;
        [SerializeField] private WebView webView;
        private bool keepBrowserSessionAlive = true;
        #region Main Settings
        [Space]
        [Header("Base Settings")]
        [Space]
        [Tooltip("Starter Asset basemodel")]
        public GameObject baseModel;
        private GameObject avatar;

        [Space]
        [Header("UI Settings")]
        [Space]
        [Tooltip("Setting for UI")]
        [HideInInspector] public bool avatarSelection;
        public GameObject RPMAvatarMenu;
        public GameObject RPMChangeAvatarUI;
        public GameObject RPMLoadAvatarUI;
        public GameObject RPMErrorUI;

        [Space]
        [Header("UI Message Settings")]
        [Space]
        [Tooltip("UI Message Setting")]
        public string loadAvatarText = "Load Ready Player Me avatar. Please wait...";
        public string loadErrorText = "Timeout after 2000ms, avatar failed to load. Please try again";
        public string urlErrorText = "Given url is invalid or is not Ready Player Me avatar. Please check again";
        public float timeToShowErrorMessage = 3f;
        #endregion

        #region Events
        [Space]
        [Header("Event Setting")]
        public bool usingEvent;
        [Space]
        public UnityEvent eventToCallOnLoadAvatar = new UnityEvent();
        public UnityEvent eventToCallOnLoadCompleted = new UnityEvent();
        public UnityEvent eventToCallOnLoadFailed = new UnityEvent();
        public UnityEvent eventToCallOnUrlError = new UnityEvent();
        #endregion

        #region DebugLog
        [Space]
        [Header("Debug Log Setting")]
        public GameObject DebugLog;
        public bool enableDebugLog = false;
        #endregion

        private void Start()
        {
            displayButton.onClick.AddListener(DisplayWebView);
            if (webView == null)
            {
                webView = FindObjectOfType<WebView>();
            }

            webView.KeepSessionAlive = keepBrowserSessionAlive;
            avatarSelection = true;
            ApplicationData.Log();

            if (enableDebugLog)
            {
                if (DebugLog == null)
                {
                    Debug.LogWarning("Please assign Debug Log Panel Game Object", DebugLog);
                }
                else
                {
                    DebugLog.SetActive(true);
                }
            }

            avatarSelection = true;
            AvatarSelection();
        }
        private void OnDestroy()
        {
            if (avatar != null) Destroy(avatar);
            CancelInvoke();
        }

        #region Load Avatar
        private void AvatarSelection()
        {
            if (avatarSelection || enableDebugLog)
            {
                RPMLoadAvatarUI.GetComponentInChildren<Text>().text = loadAvatarText;
                RPMAvatarMenu.SetActive(true);
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            else
            {
                RPMAvatarMenu.SetActive(false);
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
        public void UILoadAvatar()
        {
            string RPMInputFieldText = RPMChangeAvatarUI.GetComponentInChildren<InputField>().text;
            var checkURL = RPMInputFieldText.Contains(".glb");
            if (RPMInputFieldText != null && checkURL)
            {
                LoadAvatar(RPMInputFieldText);
                RPMChangeAvatarUI.SetActive(false);
            }
            else
            {
                UrlError(RPMInputFieldText);
            }
        }
        public void LoadAvatar(string avatarUrls)
        {
            var avatarLoader = new AvatarLoader();
            avatarLoader.OnCompleted += (_, args) =>
            {
                avatar = args.Avatar;
                avatarUrls = null;

                if (usingEvent)
                {
                    eventToCallOnLoadCompleted.Invoke();
                }
                webView.SetVisible(false);
                RPMChangeAvatarUI.SetActive(false);
                RPMLoadAvatarUI.SetActive(false);
                avatarSelection = false;
                AvatarSelection();
                baseModel.SetActive(false);
            };
            avatarLoader.OnFailed += (_, args) =>
            {
                RPMLoadAvatarUI.SetActive(false);

                if (usingEvent)
                {
                    eventToCallOnLoadFailed.Invoke();
                }

                if (enableDebugLog)
                {
                    SDKLogger.Log(tag, loadErrorText);
                }

                StartCoroutine(ErrorShow(loadErrorText));
                RPMChangeAvatarUI.SetActive(true);
            };
            avatarLoader.LoadAvatar(avatarUrls);

            if (usingEvent)
            {
                eventToCallOnLoadAvatar.Invoke();
            }

            RPMLoadAvatarUI.SetActive(true);
        }
        private void UrlError(String ErrorField)
        {
            if (usingEvent)
            {
                eventToCallOnUrlError.Invoke();
            }

            StartCoroutine(ErrorShow(urlErrorText));

            if (enableDebugLog)
            {
                SDKLogger.Log(tag, ErrorField + " = " + urlErrorText);
            }
        }
        private IEnumerator ErrorShow(string errorMessage)
        {
            RPMErrorUI.GetComponentInChildren<Text>().text = errorMessage;
            RPMErrorUI.SetActive(true);
            yield return new WaitForSeconds(timeToShowErrorMessage);
            RPMErrorUI.SetActive(false);
        }
        private void DisplayWebView()
        {
            if (webView.Loaded)
            {
                webView.SetVisible(true);
            }
            else
            {
                webView.CreateWebView();
                webView.OnAvatarCreated = LoadAvatar;
            }
        }
        private void HideWebView()
        {
            webView.SetVisible(false);
            displayButton.gameObject.SetActive(true);
        }
        #endregion
    }
}