using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public static class NFTAPI
{
    private const string BaseUrl = "https://shrouded-brushlands-49806.herokuapp.com/mint";
    private const string ContentTypeHeader = "Content-Type";
    private const string ApplicationJson = "application/json";

    public static IEnumerator MintNFT(string ownerAddress, string tokenURI, System.Action<string> onSuccess, System.Action<string> onFailure)
    {
        string jsonData = JsonUtility.ToJson(new MintRequest { ownerAddress = ownerAddress, tokenURI = tokenURI });

        using (UnityWebRequest www = new UnityWebRequest(BaseUrl, "POST"))
        {
            www.SetRequestHeader(ContentTypeHeader, ApplicationJson);
            www.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonData));
            www.downloadHandler = new DownloadHandlerBuffer();

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                onFailure?.Invoke(www.error);
            }
            else
            {
                onSuccess?.Invoke(www.downloadHandler.text);
            }
        }
    }

    [System.Serializable]
    private class MintRequest
    {
        public string ownerAddress;
        public string tokenURI;
    }
}