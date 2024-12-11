using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.UI;

namespace YellowPanda.Popup
{

    public class PopupManager : MonoBehaviour
    {
        public static PopupManager Instance;

        public bool printAssetLoading;
        public Color preloadCanvasColor;

        [HideInInspector] public PopupWindow LoadedPopup;
        [HideInInspector] public List<PopupWindow> LoadedPopups { get; set; }

        Dictionary<int, GameObject> _createdCanvases;
        GameObject _canvasPreset;
        bool _initialized = false;
        Dictionary<string, IResourceLocation> _resourceLocationMap;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                LoadedPopups = new List<PopupWindow>();
                _createdCanvases = new Dictionary<int, GameObject>();
                _canvasPreset = transform.GetChild(0).gameObject;

                StartCoroutine(Initialize());
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        public GameObject GenerateBackground(GameObject canvas)
        {
            GameObject background = new GameObject("BackgroundScreen");
            background.transform.SetParent(canvas.transform, false);
            RectTransform pretoRect = background.AddComponent<RectTransform>();
            pretoRect.anchorMin = Vector3.zero;
            pretoRect.anchorMax = Vector3.one;
            pretoRect.offsetMin = Vector3.zero;
            pretoRect.offsetMax = Vector3.zero;
            Image backgroundImage = background.AddComponent<Image>();
            backgroundImage.color = preloadCanvasColor;
            return background;
        }

        public IEnumerator OpenPopupRoutine(string assetId, int layerOrder)
        {
            while (!_initialized) yield return new WaitForEndOfFrame();

            GameObject canvasObject;
            if (_createdCanvases.ContainsKey(layerOrder))
            {
                canvasObject = _createdCanvases[layerOrder];
            }
            else
            {
                canvasObject = Instantiate(_canvasPreset, transform);
                canvasObject.SetActive(true);
                Canvas canvas = canvasObject.GetComponent<Canvas>();
                canvas.sortingOrder = layerOrder;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = Camera.main;
                _createdCanvases.Add(layerOrder, canvasObject);
            }

            GameObject background = GenerateBackground(canvasObject);

            Task<GameObject> instantiateTask = Addressables.InstantiateAsync(_resourceLocationMap[assetId]).Task;

            while (!instantiateTask.IsCompleted) yield return new WaitForEndOfFrame();

            instantiateTask.Result.transform.SetParent(canvasObject.transform, false);
            PopupWindow popup = instantiateTask.Result.GetComponent<PopupWindow>();
            popup.OpenPopup();
            LoadedPopup = popup;
            LoadedPopups.Add(popup);

            Destroy(background);
        }

        public IEnumerator OpenPopupRoutine(AssetReference assetReference, int layerOrder)
        {
            while (!_initialized) yield return new WaitForEndOfFrame();

            GameObject canvasObject;
            if (_createdCanvases.ContainsKey(layerOrder))
            {
                canvasObject = _createdCanvases[layerOrder];
            }
            else
            {
                canvasObject = Instantiate(_canvasPreset, transform);
                canvasObject.SetActive(true);
                Canvas canvas = canvasObject.GetComponent<Canvas>();
                canvas.sortingOrder = layerOrder;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = Camera.main;
                _createdCanvases.Add(layerOrder, canvasObject);
            }

            GameObject background = GenerateBackground(canvasObject);

            Task<GameObject> instantiateTask = Addressables.InstantiateAsync(assetReference).Task;

            while (!instantiateTask.IsCompleted) yield return null;

            instantiateTask.Result.transform.SetParent(canvasObject.transform, false);
            PopupWindow popup = instantiateTask.Result.GetComponent<PopupWindow>();
            popup.OpenPopup();
            LoadedPopup = popup;
            LoadedPopups.Add(popup);

            Destroy(background);
        }

        private IEnumerator Initialize()
        {
            Task<IList<IResourceLocation>> loadTask = Addressables.LoadResourceLocationsAsync("popups").Task;
            while (!loadTask.IsCompleted) yield return new WaitForEndOfFrame();

            _resourceLocationMap = new Dictionary<string, IResourceLocation>();
            foreach (IResourceLocation resource in loadTask.Result)
            {
                if (!_resourceLocationMap.ContainsKey(resource.PrimaryKey))
                {
                    _resourceLocationMap.Add(resource.PrimaryKey, resource);
                    if (printAssetLoading)
                        Debug.Log("Loaded Addressable " + resource.PrimaryKey);
                }
                else
                {
                    if (printAssetLoading)
                        Debug.Log("Could not load Addressable " + resource.PrimaryKey + " as it was already loaded");
                }

                if (!_resourceLocationMap.ContainsKey(resource.ToString()))
                    _resourceLocationMap.Add(resource.ToString(), resource);
            }
            _initialized = true;
        }

        private void Update()
        {
            for (int i = LoadedPopups.Count - 1; i >= 0; i--)
            {
                PopupWindow window = LoadedPopups[i];
                if (!window.InScene())
                {
                    LoadedPopups.RemoveAt(i);
                    Addressables.Release(window.gameObject);
                }
            }

            List<int> canvasesToRemove = new List<int>();
            foreach (int layerOrder in _createdCanvases.Keys)
            {
                if (_createdCanvases[layerOrder].transform.childCount == 0)
                {
                    canvasesToRemove.Add(layerOrder);
                }
            }
            foreach (int c in canvasesToRemove)
            {
                Destroy(_createdCanvases[c]);
                _createdCanvases.Remove(c);
            }
        }

        public void OpenPopup(AssetReference assetReference, int layerOrder, Action<PopupWindow> callback)
        {
            StartCoroutine(_OpenPopup(assetReference, layerOrder, callback));
        }
        public void OpenPopup(string assetKey, int layerOrder, Action<PopupWindow> callback)
        {
            StartCoroutine(_OpenPopup(assetKey, layerOrder, callback));
        }
        private IEnumerator _OpenPopup(AssetReference assetReference, int layerOrder, Action<PopupWindow> callback)
        {
            yield return OpenPopupRoutine(assetReference, layerOrder);
            callback?.Invoke(LoadedPopup);
        }

        private IEnumerator _OpenPopup(string assetKey, int layerOrder, Action<PopupWindow> callback)
        {
            yield return OpenPopupRoutine(assetKey, layerOrder);
            callback?.Invoke(LoadedPopup);
        }

        public bool HasInitialized()
        {
            return _initialized;
        }
    }
}