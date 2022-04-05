using GLTFast;
using GLTFast.Loading;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
public class GLTFLoader : MonoBehaviour
{
    [SerializeField]
    string _url;
    [SerializeField]
    Transform _tm;
    [SerializeField]
    Transform _gridRoot;
    [SerializeField]
    RawImage _rawImagePrefab;

    // Start is called before the first frame update
    void Start()
    {
        LoadAsync();
        //StartCoroutine(LoadCo());
    }

    IEnumerator LoadCo()
    {
        UnityWebRequest request;
        request = UnityWebRequest.Get(_url);
        yield return request.SendWebRequest();
        Debug.Log("SendWebRequest isNetworkError: " + request.isNetworkError);
        Debug.Log("SendWebRequest isHttpError: " + request.isHttpError);
        Debug.Log("SendWebRequest text: " + request.downloadHandler.text);
        Debug.Log("SendWebRequest data: " + request.downloadHandler.data);

    }

    private void LoadAsync()
    {

        GLTFast.GLTFast gltf = new GLTFast.GLTFast(this, new MyDownloadProvider());
        gltf.onLoadComplete += (succ) =>
        {
            Debug.Log("onLoadComplete: " + succ);
            if (succ)
            {
                gltf.InstantiateGltf(_tm);
                CheckShader(_tm);
            }
        };
        gltf.Load(_url);


        //var gltf = new GltfImport();

        //// Create a settings object and configure it accordingly
        //var settings = new ImportSettings
        //{
        //    generateMipMaps = true,
        //    anisotropicFilterLevel = 3,
        //    nodeNameMethod = ImportSettings.NameImportMethod.OriginalUnique
        //};

        //// Load the glTF and pass along the settings
        //var success = await gltf.Load(_url, settings);

        //if (success)
        //{
        //    gltf.InstantiateMainScene(new GameObject("glTF").transform);
        //}
        //else
        //{
        //    Debug.LogError("Loading glTF failed!");
        //}
    }

    private void CheckShader(Transform tm)
    {
        var renderers = tm.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            string name = renderer.gameObject.name;
            var mtls = renderer.materials;
            foreach (var mtl in mtls)
            {
                var texNames = mtl.GetTexturePropertyNames();
                foreach (var texName in texNames)
                {
                    Debug.Log("TexName: " + texName);
                    var tex = mtl.GetTexture(texName);
                    Debug.Log("Tex: " + tex);
                    Debug.Log("TexW: " + tex.width);
                    Debug.Log("TexH: " + tex.height);
                    if (tex != null)
                    {
                        var obj = Instantiate(_rawImagePrefab.gameObject,_gridRoot);
                        obj.GetComponent<RawImage>().texture = tex;
                        obj.SetActive(true);
                    }
                }
                var shader = mtl.shader;
                string shaderName = shader.name;
                string shaderStr = shader.ToString();
                Debug.Log("shaderName: " + shaderName);
                Debug.Log("shaderStr: " + shaderStr);
                Debug.Log("shaderisSupported: " + shader.isSupported);
                Debug.Log("shaderisInstanceID: " + shader.GetInstanceID());

            }
        }
    }

    private class MyDownloadProvider : IDownloadProvider
    {
        public IDownload Request(string url)
        {
            return new MyAwaitableDownload(url);
        }

        public ITextureDownload RequestTexture(string url)
        {
            return new MyAwaitableTextureDownload(url);
        }
    }


    public class MyAwaitableDownload : IDownload
    {
        protected UnityWebRequest request;
        protected UnityWebRequestAsyncOperation asynOperation;


        public MyAwaitableDownload() { }

        public MyAwaitableDownload(string url)
        {
            Init(url);
        }

        protected virtual void Init(string url)
        {
            Debug.Log("MyAwaitableDownload::Init: " + url);
            request = UnityWebRequest.Get(url);
            asynOperation = request.SendWebRequest();
            asynOperation.completed += (action) =>
            {
                Debug.Log("MyAwaitableDownload::SendWebRequest url: " + url);
                Debug.Log("MyAwaitableDownload::SendWebRequest responseCode: " + request.responseCode);
                Debug.Log("MyAwaitableDownload::SendWebRequest isNetworkError: " + request.isNetworkError);
                Debug.Log("MyAwaitableDownload::SendWebRequest isHttpError: " + request.isHttpError);
                Debug.Log("MyAwaitableDownload::SendWebRequest text: " + request.downloadHandler.text);
            };
        }

        public object Current { get { return asynOperation; } }
        public bool MoveNext() { return !asynOperation.isDone; }
        public void Reset() { }

        public bool success
        {
            get
            {
                return request.isDone && !request.isNetworkError && !request.isHttpError;
            }
        }

        public string error { get { return request.error; } }
        public byte[] data { get { return request.downloadHandler.data; } }
        public string text { get { return request.downloadHandler.text; } }
    }

    public class MyAwaitableTextureDownload : AwaitableDownload, ITextureDownload
    {

        public MyAwaitableTextureDownload() : base() { }
        public MyAwaitableTextureDownload(string url) : base(url) { }

        protected static UnityWebRequest CreateRequest(string url)
        {
            return UnityWebRequestTexture.GetTexture(url
                /// TODO: Loading non-readable here would save memory, but
                /// breaks texture instantiation in case of multiple samplers:
                // ,true // nonReadable
                );
        }

        protected override void Init(string url)
        {
            request = CreateRequest(url);
            asynOperation = request.SendWebRequest();
        }

        public Texture2D texture
        {
            get
            {
                return (request.downloadHandler as DownloadHandlerTexture).texture;
            }
        }
    }
}
