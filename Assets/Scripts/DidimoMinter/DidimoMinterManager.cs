using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Didimo.Networking;
using UnityEngine.UI;
using NFTStorage;
using NFTStorage.JSONSerialization;
using Newtonsoft.Json;
using System.IO;

public class DidimoMinterManager : MonoBehaviour
{
    private NFTStorageClient nftClient;

    public Button createDidimoButton;
    public InputField walletAddress;
    
    private DidimoMinter customApi;
    
    public class Metadata
    {
        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("external_url")]
        public string ExternalUrl { get; set; }

        [JsonProperty("image")]
        public string Image { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("animation_url")]
        public string AnimationUrl { get; set; }
    }

    // Start is called before the first frame update
    void Start()
    {
        // Instantiate a new NFTStorageClient object
        nftClient = new NFTStorageClient();

        // Set your nft.storage API key
        nftClient.SetApiToken(
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJkaWQ6ZXRocjoweEQ4RmZGNTk4ZjE5NjQ5NTY0RjRFZmY4RjBlNEVFYTU4NDQ2OUNGRjIiLCJpc3MiOiJuZnQtc3RvcmFnZSIsImlhdCI6MTY4MDY1Njc4MTQzNCwibmFtZSI6ImRpZGltb01pbnRlciJ9.4J1QeO-Is5gRFWJN0xOMXjBz7g0Lq10v3tD1UrnOjRI");

        ExportGlb.OnResultFilePathAssigned += HandleResultFilePathAssigned;

        customApi = new DidimoMinter();
        Didimo.Networking.Api.SetImplementation(customApi);

        Debug.Log("Ready to create a DIDIMO");

        // Set up the event listener to create a Didimo when the button is clicked
        createDidimoButton.onClick.AddListener(async () => await GenerateDidimo());
    }

    private async Task GenerateDidimo()
    {
// Find all game objects in the scene
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();

// Search for the first game object with the desired name pattern
        GameObject didimoObject = null;
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.StartsWith("didimo_"))
            {
                didimoObject = obj;
                break;
            }
        }

// Check if the object was found and destroy it
        if (didimoObject != null)
        {
            Debug.Log("Didimo object found. Destorying it");
            Destroy(didimoObject);
        }

        // Create a new Didimo and import it into the scene
        await customApi.GenerateDidimoImportInScene();
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
        
        string metadataFileUrl = await UploadMetadataFile(fileUrl);
        
        Debug.Log("Metadata file uploaded to: " + metadataFileUrl);
        
        string ownerAddress = walletAddress.text;
        string tokenURI = metadataFileUrl;

        StartCoroutine(NFTAPI.MintNFT(ownerAddress, tokenURI,
            (response) => Debug.Log("Minting successful: " + response),
            (error) => Debug.LogError("Minting error: " + error)));
        
    }

    private async Task<string> UploadFile(string filePath)
    {
        // Call the UploadDataFromStringHttpClient method to upload a file and get the file URL using CID
        NFTStorageUploadResponse response = await nftClient.UploadDataFromStringHttpClient(filePath);

        // Build the full file URL using the CID
        string fileUrl = $"https://{response.value.cid}.ipfs.dweb.link/";

        return fileUrl;
    }
    
    private async Task<string> UploadMetadataFile(string animationUrl)
    {
        // Create a metadata object
        var metadata = new Metadata
        {
            Description = "An NFT avatar for metaverse",
            ExternalUrl = "https://ailand.app/",
            Image = "https://bafkreib7v2xptv6idhv7vbnfoqb3gcrurxs3ew5u7aumo5aobpooieersq.ipfs.nftstorage.link/",
            Name = "Ailand Avatar #1",
            AnimationUrl = animationUrl
        };

        // Serialize the metadata object to a JSON string
        string jsonString = JsonConvert.SerializeObject(metadata);

        // Generate a random file name
        string randomFileName = Path.GetRandomFileName() + ".json";

        // Combine Application.persistentDataPath with the random file name
        string filePath = Path.Combine(Application.persistentDataPath, randomFileName);

        // Save the JSON string to the file
        File.WriteAllText(filePath, jsonString);

        // Call the UploadDataFromStringHttpClient method to upload the file and get the file URL using CID
        NFTStorageUploadResponse response = await nftClient.UploadDataFromStringHttpClient(filePath);

        // Build the full file URL using the CID
        string fileUrl = $"https://{response.value.cid}.ipfs.dweb.link/";

        // Delete the temporary file after uploading
        File.Delete(filePath);

        return fileUrl;
    }
    
    
}