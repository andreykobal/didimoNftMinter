using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Didimo.Networking;
using UnityEngine.UI;
using NFTStorage;
using NFTStorage.JSONSerialization;

public class DidimoMinterManager : MonoBehaviour
{
    private NFTStorageClient nftClient;

    public Button createDidimoButton;
    // Start is called before the first frame update
    void Start()
    {
        // Instantiate a new NFTStorageClient object
        nftClient = new NFTStorageClient();

        // Set your nft.storage API key
        nftClient.SetApiToken("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJkaWQ6ZXRocjoweEQ4RmZGNTk4ZjE5NjQ5NTY0RjRFZmY4RjBlNEVFYTU4NDQ2OUNGRjIiLCJpc3MiOiJuZnQtc3RvcmFnZSIsImlhdCI6MTY4MDY1Njc4MTQzNCwibmFtZSI6ImRpZGltb01pbnRlciJ9.4J1QeO-Is5gRFWJN0xOMXjBz7g0Lq10v3tD1UrnOjRI");
        
        ExportGlb.OnResultFilePathAssigned += HandleResultFilePathAssigned;

        DidimoMinter customApi = new DidimoMinter();
        Didimo.Networking.Api.SetImplementation(customApi);
        
        Debug.Log("Ready to create a DIDIMO");
        //on click button call await customApi.CreateDidimoAndImport();   
        createDidimoButton.onClick.AddListener(async () => await customApi.GenerateDidimoImportInScene());
    }
    
    private async void HandleResultFilePathAssigned(string resultFilePath)
    {
        string correctedPath = resultFilePath.Replace("\\", "/");
        Debug.Log("Result file path assigned: " + correctedPath);

        // Call your method here that needs to be triggered when ResultFilePath is assigned
        // Call the UploadFile method to upload a file and get the file URL using CID
        string fileUrl = await UploadFile(correctedPath);

        // Log the full file URL to the console
        Debug.Log("Avatar uploaded to: " + fileUrl);
    }
    
    private async Task<string> UploadFile(string filePath)
    {
        // Call the UploadDataFromStringHttpClient method to upload a file and get the file URL using CID
        NFTStorageUploadResponse response = await nftClient.UploadDataFromStringHttpClient(filePath);

        // Build the full file URL using the CID
        string fileUrl = $"https://{response.value.cid}.ipfs.dweb.link/";

        return fileUrl;
    }
    
}
