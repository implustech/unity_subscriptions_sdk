using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Networking;
using UnityEngine.Purchasing;
using SimpleJson;
#if UNITY_IOS
using UnityEngine.iOS;
#endif
using System.Text;


namespace ApptilausSDK {

    public delegate void CompletionHandler(bool success, string error);

    public class ApptilausManager : MonoBehaviour {
        private const string _defaultURL = "https://api.apptilaus.com";
        private const string _sessionURL = "https://device.apptilaus.com/v1/device/";
        private const string _lastRegisteredSessionKey = "last_session_registered";
        private delegate void UnityWebResponseHandler(UnityWebRequest request);

        private static ApptilausManager _shared;
        public static ApptilausManager Shared {
            get {
                if (_shared == null) {
                    GameObject instanceObject = new GameObject("ApptilausManager");
                    DontDestroyOnLoad(instanceObject);
                    _shared = instanceObject.AddComponent<ApptilausManager>();
                }
                return _shared;
            }
        }

        private string _baseUrl = _defaultURL;
        public string BaseUrl {
            get {
                return _baseUrl;
            }
            set {
                _baseUrl = value;
                _baseUrl.TrimEnd('/', '\\');
            }
        }
        /// <summary>
        /// Custom user id to be sent to server
        /// </summary>
        public string UserId { get; set; } = "";

        private int _maxRequestRetryCount = 10;
        /// <summary>
        /// If a web request fails, the app will try to send it again, until it reaches the max retry count
        /// </summary>
        public int MaxRequestRetryCount {
            get {
               return  _maxRequestRetryCount;
            }
            set {
                if (value > 0) {
                    _maxRequestRetryCount = value;
                }
                else {
                    _maxRequestRetryCount = 0;
                }
            }
        }
        
        private string _sdkVersion;
        private string _appId = "";
        private string _appToken = "";
        private string _advertisingId = "";
        private bool _enableSessionTracking = false;

        #region InterfaceMethods
        /// <summary>
        /// Initialize Apptilaus Manager with given parameters
        /// </summary>
        /// <param name="appId">App ID from Apptilaus</param>
        /// <param name="appToken">App Token from Apptilaus</param>
        /// <param name="enableSessionTracking">Determines whether session tracking should be enabled</param>
        public void Setup(string appId, string appToken, bool enableSessionTracking = false) {
            _appId = appId;
            _appToken = appToken;
            _enableSessionTracking = enableSessionTracking;
            if (enableSessionTracking) {
                StartCoroutine(RegisterSessionIfNeeded());
            }
            TextAsset versionFile = Resources.Load("ApptilausSDKVersion", typeof(TextAsset)) as TextAsset;
            if (versionFile != null) {
                _sdkVersion = versionFile.text;
            }
        }

        /// <summary>
        /// Notify Apptilaus when a user has exercised their right to be forgotten
        /// </summary>
        /// <param name="completionHandler">Callback which is executed when server returns a responce</param>
        public void OptOut(CompletionHandler completionHandler = null) {
            if (!CheckSetup()) {
                return;
            }
            StartCoroutine(ProcessOptOut(completionHandler));
        }
        /// <summary>
        /// Notify Apptilaus of a successfull purchase
        /// </summary>
        /// <param name="purchase">PurchaseEventArgs object, received by your IStore listener object</param>
        /// <param name="customParams">Any custom parameters to pass to the server</param>
        public void Purchase(PurchaseEventArgs purchase, Dictionary<string, string> customParams = null) {
            if (!CheckSetup()) {
                return;
            }
            StartCoroutine(ProcessPurchase(purchase.purchasedProduct,customParams,0));
        }
        #endregion

        #region MainOperationCoroutines

        private IEnumerator ProcessPurchase(Product product, Dictionary<string, string> customParams, int tryCount) {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
#if UNITY_ANDROID
            parameters.Add("platform", "GooglePlay");
#elif UNITY_IOS
            parameters.Add("platform", "AppleAppStore");
#endif
            yield return InsertDeviceIdInfo(parameters);
            parameters.Add("price", String.Format("{0:f2}", product.metadata.localizedPrice));
            parameters.Add("currency", product.metadata.isoCurrencyCode);
            parameters.Add("sdk_version", _sdkVersion);
            if (!string.IsNullOrEmpty(UserId)) {
                parameters.Add("user_id", UserId);
            }
            if (customParams != null) {
                foreach (KeyValuePair<string, string> param in customParams) {
                    parameters.Add("dp_" + param.Key, param.Value);
                }
            }
            parameters.Add("item", product.definition.storeSpecificId);
            parameters.Add("transaction_id", product.transactionID);
            try {
#if UNITY_IOS
                string payload = JSON.Parse(product.receipt)["Payload"];
                parameters.Add("receipt", payload);
#elif UNITY_ANDROID
                string payload = JSON.Parse(product.receipt)["Payload"];
                JSONNode payloadNode = JSON.Parse(payload);
                parameters.Add("receipt", payloadNode["signature"]);
                JSONNode json = JSON.Parse(payloadNode["json"]);
                parameters.Add("purchase_token", json["purchaseToken"]);                
#endif
            }
            catch {
                Debug.LogError("[Apptilaus] Couldn't parse receipt json : " + product.receipt);
            }

            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("App-Token", _appToken);
            headers.Add("Content-Type", "application/json");
            string url = BaseUrl;
            url += "/v1/unity/" + _appId + "/";

            UnityWebResponseHandler onResponse = (request) => {
                if (request.error != null) {
                    if (request.isNetworkError) {
                        if (tryCount < MaxRequestRetryCount) {
                            StartCoroutine(ProcessPurchase(product, customParams, tryCount + 1));
                        }
                        else {
                            Debug.LogError("[Apptilaus] Could not send purchase request: retry limit is exceeded");
                        }
                    }
                    else {
                        Debug.LogError("[Apptilaus] Could not send purchase request due to error: " + request.error);
                    }
                    return;
                }
                Debug.Log("[Apptilaus]:server response: " + request.downloadHandler.text);
                Debug.Log("[Apptilaus]: Purchase Processed");
            };
            Debug.Log("[Apptilaus]: sending purchase to " + url);
            yield return PostRequest(url, onResponse, JsonDictionaryWriter.Serialize(parameters), headers);
        }
        

        private IEnumerator ProcessOptOut(CompletionHandler completionHandler = null) {            

            Dictionary<string, string> requestHeaders = new Dictionary<string, string>();
            yield return InsertDeviceIdInfo(requestHeaders);

            string url = BaseUrl;
            url+= "/v1/optout";
            requestHeaders.Add("App-Bundle", Application.identifier);
            UnityWebResponseHandler onResponse = (request) => {
                if (request.error != null) {
                    Debug.LogError("[Apptilaus]: optOut was not registered, due to error:\n" + request.error);
                    if (completionHandler != null) {
                        completionHandler(false, request.error);
                    }
                    return;
                }
                if (completionHandler != null) {
                    completionHandler(true, "");
                }
                Debug.Log("[Apptilaus]: optOut processed");
            };
            yield return GetRequest(url, onResponse, requestHeaders);
        }


        private IEnumerator RegisterSessionIfNeeded() {
            if (!CheckSetup()) {
                yield break;
            }
            Dictionary<string, string> parameters = new Dictionary<string, string>();

            DateTime now = DateTime.Now;
            TimeSpan nowTimeSpan = (now - new DateTime(1970, 1, 1, 0, 0, 0));
            double nowMilliseconds = (Int64)Math.Floor(nowTimeSpan.TotalMilliseconds);
            string timeParameterName;
            string lastRegisteredSessionValue = PlayerPrefs.GetString(_lastRegisteredSessionKey, "");
            double lastCallMilliseconds;
            if (!string.IsNullOrEmpty(lastRegisteredSessionValue) && double.TryParse(lastRegisteredSessionValue, out lastCallMilliseconds)) {
                //Session had already been registered, check the day of the last session
                TimeSpan lastCall = TimeSpan.FromMilliseconds(lastCallMilliseconds);
                if (Math.Floor(nowTimeSpan.TotalDays) == Math.Floor(lastCall.TotalDays)) {
                    Debug.Log("[Apptilaus]: already registered session today");
                    yield break;
                }
                timeParameterName = "dp_session";
                parameters.Add("dp_activity","session");
            }
            else {
                //This is the first time this app registers session
                timeParameterName = "dp_install";
                parameters.Add("dp_activity", "install");
            }
            parameters.Add(timeParameterName, String.Format("{0:0}",nowMilliseconds));
            yield return InsertDeviceIdInfo(parameters);
            //Encode parameters to URL
            string url = _sessionURL +_appId+"/?";
            int counter = 0;
            foreach (KeyValuePair<string, string> param in parameters) {
                url += param.Key + "=" + param.Value;
                counter++;
                if (counter < parameters.Count) {
                    url += "&";
                }
            }
            Debug.Log("[Apptilaus]: sending session request to " + url);
            UnityWebResponseHandler onResponse = (request) => {
                if (request.error != null) {
                    Debug.LogError("[Apptilaus]: session was not registered, due to error:\n" + request.error);
                    return;
                }
                PlayerPrefs.SetString(_lastRegisteredSessionKey,nowMilliseconds.ToString());
                PlayerPrefs.Save();
                Debug.Log("[Apptilaus]: session registered");
            };
            yield return GetRequest(url, onResponse, null);
        }

        #endregion

        #region UtilityMethods

        private IEnumerator GetRequest(string url, UnityWebResponseHandler onResponse, Dictionary<string, string> headers = null) {
            UnityWebRequest request = UnityWebRequest.Get(url);
            if (headers != null) {
                foreach (KeyValuePair<string, string> header in headers) {
                    request.SetRequestHeader(header.Key, header.Value);
                }
            }
            yield return request.SendWebRequest();
            if (onResponse != null) {
                onResponse(request);
            }
        }

        
        private IEnumerator PostRequest(string url, UnityWebResponseHandler onResponse, string body, Dictionary<string, string> headers = null) {
            byte[] data = Encoding.UTF8.GetBytes(body);
            Debug.Log("[Apptilaus]: sending POST request. Body :\n" + body);
            UnityWebRequest request = new UnityWebRequest(url, "POST");
            request.uploadHandler = (UploadHandler)new UploadHandlerRaw(data);
            request.downloadHandler = new DownloadHandlerBuffer();

            if (headers != null) {
                foreach (KeyValuePair<string, string> header in headers) {
                    request.SetRequestHeader(header.Key, header.Value);
                }
            }
            request.downloadHandler = new DownloadHandlerScript();
            yield return request.SendWebRequest();
            if (onResponse != null) {
                onResponse(request);
            }
        }
        
        private IEnumerator InsertDeviceIdInfo(Dictionary<string, string> parametersCollection) {
            string advertisingIdName="";
#if UNITY_IOS
            advertisingIdName = "ios_idfa";
            if (!string.IsNullOrEmpty(Device.vendorIdentifier)) {
                parametersCollection.Add("ios_idfv", Device.vendorIdentifier);
            }
#elif UNITY_ANDROID
            advertisingIdName = "android_gps";
            parametersCollection.Add("android_id", SystemInfo.deviceUniqueIdentifier);
#endif
            yield return GetAdvertisingId();
            if (!string.IsNullOrEmpty(_advertisingId)) {
                parametersCollection.Add(advertisingIdName, _advertisingId);
            }
        }

        private IEnumerator GetAdvertisingId() {
            bool requestAdIdCompleted = false;
            if (string.IsNullOrEmpty(_advertisingId)) {
                if (!Application.RequestAdvertisingIdentifierAsync(
                (string adId, bool trackingEnabled, string error) => { _advertisingId = adId; requestAdIdCompleted = true; })) {
                    Debug.LogWarning("[Apptilaus]:Advertising ID is not available");
                    requestAdIdCompleted = true;
                }
            }
            else {
                requestAdIdCompleted = true;
            }
            yield return new WaitUntil(() => requestAdIdCompleted);
        }

        private bool CheckSetup() {
            if (string.IsNullOrEmpty(_appId)) {
                Debug.LogError("[Apptilaus]: appId not set");
                return false;
            }
            if (string.IsNullOrEmpty(_appToken)){
                Debug.Log("[Apptilaus]: appToken is not et");
                return false;
            }
            if (string.IsNullOrEmpty(BaseUrl)) {
                Debug.LogError("[Apptilaus]: base URL is not set");
                return false;
            }
            return true;
        }

        #endregion
    }
}