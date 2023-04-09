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
    public Button getMetadataButton; 
    public InputField walletAddress;

    private DidimoMinter customApi;
    
    private const string ContractAddress = "0xe3515d63bce48059146134176dbb18b9db0d80d8";
    //private const string WalletAddress = "0x98000edf79B0eb598085721D57d93B5865c87751";


    public class Metadata
    {
        [JsonProperty("description")] public string Description { get; set; }

        [JsonProperty("external_url")] public string ExternalUrl { get; set; }

        [JsonProperty("image")] public string Image { get; set; }

        [JsonProperty("name")] public string Name { get; set; }

        [JsonProperty("animation_url")] public string AnimationUrl { get; set; }
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
        getMetadataButton.onClick.AddListener(OnGetMetadataButtonClick);
    }
    
        private void OnGetMetadataButtonClick()
        {
            StartCoroutine(TokenMetadata.TokenMetadataRequest(ContractAddress, walletAddress.text, OnTokenMetadataSuccess, OnTokenMetadataFailure));
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
        string animationUrl = await UploadFile(correctedPath);

        // Log the full file URL to the console
        Debug.Log("Avatar uploaded to: " + animationUrl);

        string imageUrl = await UploadScreenshot();

        Debug.Log("Image uploaded to: " + imageUrl);

        string metadataFileUrl = await UploadMetadataFile(animationUrl, imageUrl);

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

    private async Task<string> UploadMetadataFile(string animationUrl, string imageUrl)
    {
        // Create a metadata object
        var metadata = new Metadata
        {
            Description = "An NFT avatar for metaverse",
            ExternalUrl = "https://ailand.app/",
            Image = imageUrl,
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

    private async Task<string> UploadScreenshot()
    {
        // Capture a screenshot from the main camera and save it to a temporary file
        string tempScreenshotPath = Path.Combine(Application.persistentDataPath, "tempScreenshot.png");
        ScreenCapture.CaptureScreenshot(tempScreenshotPath);
        Debug.Log($"Captured screenshot at {tempScreenshotPath}");

        // Wait for a short time to ensure that the file is fully written to disk
        await Task.Delay(50);

        // Load the image from the temporary file
        byte[] imageData;
        using (FileStream fs = new FileStream(tempScreenshotPath, FileMode.Open, FileAccess.Read))
        {
            imageData = new byte[fs.Length];
            await fs.ReadAsync(imageData, 0, (int)fs.Length);
        }

        Debug.Log($"Read {imageData.Length} bytes from {tempScreenshotPath}");

        // Create a Texture2D object from the image data
        Texture2D texture = new Texture2D(1, 1);
        texture.LoadImage(imageData);

        // Crop the texture to a square aspect ratio, centered around its center
        int size = Mathf.Min(texture.width, texture.height);
        texture = Crop(texture, new Rect(0, 0, size, size));

        // Save the cropped image to another temporary file
        string tempCroppedPath = Path.Combine(Application.persistentDataPath, "tempCropped.png");
        File.WriteAllBytes(tempCroppedPath, texture.EncodeToPNG());

        Debug.Log($"Saved cropped image to {tempCroppedPath}");

        // Upload the cropped image file to NFTStorage using the UploadFile method
        string fileUrl = await UploadFile(tempCroppedPath);

        // Delete both temporary files after uploading
        File.Delete(tempScreenshotPath);
        File.Delete(tempCroppedPath);

        Debug.Log($"Deleted {tempScreenshotPath} and {tempCroppedPath}");

        return fileUrl;
    }


    public static Texture2D Crop(Texture2D sourceTexture, Rect rect)
    {
        // Calculate the coordinates and size of the cropped area
        int x = Mathf.RoundToInt(rect.x);
        int y = Mathf.RoundToInt(rect.y);
        int width = Mathf.RoundToInt(rect.width);
        int height = Mathf.RoundToInt(rect.height);

        // Make sure the cropped area is within the bounds of the source texture
        if (x < 0) x = 0;
        if (y < 0) y = 0;
        if (x + width > sourceTexture.width) width = sourceTexture.width - x;
        if (y + height > sourceTexture.height) height = sourceTexture.height - y;

        // Create a new texture for the cropped area
        Texture2D croppedTexture = new Texture2D(width, height);

        // Calculate the center of the source texture
        int centerX = sourceTexture.width / 2;
        int centerY = sourceTexture.height / 2;

        // Calculate the offset to center the cropped area around the center of the source texture
        int offsetX = centerX - x - width / 2;
        int offsetY = centerY - y - height / 2;

        // Copy the pixels from the source texture to the cropped texture
        Color[] pixels = sourceTexture.GetPixels(x, y, width, height);
        croppedTexture.SetPixels(pixels);

        // Shift the cropped texture to center it around the center of the source texture
        Color[] shiftedPixels = new Color[croppedTexture.width * croppedTexture.height];
        for (int yIndex = 0; yIndex < croppedTexture.height; yIndex++)
        {
            for (int xIndex = 0; xIndex < croppedTexture.width; xIndex++)
            {
                int sourceX = xIndex + offsetX;
                int sourceY = yIndex + offsetY;
                int sourceIndex = sourceY * sourceTexture.width + sourceX;
                int destinationIndex = yIndex * croppedTexture.width + xIndex;
                shiftedPixels[destinationIndex] = sourceTexture.GetPixel(sourceX, sourceY);
            }
        }

        croppedTexture.SetPixels(shiftedPixels);

        // Apply the changes and return the cropped texture
        croppedTexture.Apply();
        return croppedTexture;
    }
    
    private void OnTokenMetadataSuccess(string responseText)
    {
        Debug.Log(responseText);

        // Parse the response JSON and process the metadata as needed
        // ...
    }

    private void OnTokenMetadataFailure(string error)
    {
        Debug.LogError(error);
    }
}