using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;

public static class TokenMetadata
{
    private const string BaseUrl = "https://shrouded-brushlands-49806.herokuapp.com";
    
    public static IEnumerator TokenMetadataRequest(string contractAddress, string walletAddress, System.Action<string> onSuccess, System.Action<string> onFailure)
    {
        string url = $"{BaseUrl}/tokenMetadata?contractAddress={contractAddress}&walletAddress={walletAddress}";
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onFailure?.Invoke(request.error);
            }
            else
            {
                string responseText = request.downloadHandler.text;
                JArray responseJson = JArray.Parse(responseText);
                string formattedJson = responseJson.ToString();
                Debug.Log(formattedJson);
                onSuccess?.Invoke(formattedJson);
            }
        }
    }
}
