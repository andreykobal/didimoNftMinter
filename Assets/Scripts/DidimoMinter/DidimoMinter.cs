using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Didimo;
using Didimo.Builder;
using Didimo.Networking;
using Didimo.Core.Deformables;
using Didimo.Core.Inspector;
using Didimo.Core.Utility;
using Didimo.Extensions;
using Didimo.Speech;
using Object = UnityEngine.Object;

using UnityGLTF;

public class DidimoMinter : IApi, ListToPopupAttribute.IListToPopup
{
    private string gameObjectName;
    // Wait this amount of milliseconds between calls to checking didimo creation progress
    private const int CHECK_STATUS_WAIT_TIME = 2500;

    [SerializeField, Readonly] protected string didimoKey;

    [SerializeField, Readonly] protected DidimoComponents didimoComponents;

    private List<string> didimoList;

    [ListToPopup] public int selectedDidimo;

    public List<string> ListToPopupGetValues() => didimoList;

#if UNITY_EDITOR

    public void ListToPopupSetSelectedValue(int i)
    {
        selectedDidimo = i;
    }

    public string ProgressMessage { get; private set; }
    public float Progress { get; private set; }

    private void OnValidate()
    {
        ProgressMessage = null;
        Progress = 0f;
    }

    [Button]
    void OpenDeveloperPortalDocumentation()
    {
        Application.OpenURL(UsefulLinks.CREATING_A_DIDIMO_DEVELOPER_PORTAL);
    }

    [Button("Create didimo And Import")]
    public async Task GenerateDidimoImportInScene()
    {
        Debug.Log("Starting GenerateDidimoImportInScene");

        if (ProgressMessage != null)
        {
            EditorUtility.DisplayDialog("Error", "Please wait for the current request to complete.", "OK");
            Debug.LogError("Error: Please wait for the current request to complete.");
            return;
        }

        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Error", "To use this feature you must first enter play mode", "OK");
            Debug.LogError("Error: To use this feature you must first enter play mode.");
            return;
        }

        string photoFilePath = EditorUtility.OpenFilePanel("Choose a photo", "", "png,jpg");
        if (string.IsNullOrEmpty(photoFilePath))
        {
            Debug.LogWarning("Warning: Photo file path is null or empty.");
            return;
        }

        ProgressMessage = "Creating your didimo";
        Progress = 0f;
        Debug.Log("Creating didimo");

        Task<(bool success, DidimoComponents didimo)> createDidimoTask = Api.Instance.CreateDidimoAndImportGltf(
            photoFilePath, null, progress =>
            {
                Progress = progress;
                Debug.Log($"Creating didimo progress: {progress}");
            });
        
        Debug.Log("awaiting createDidimoTask");
        await createDidimoTask;
        Debug.Log("createDidimoTask completed");
        ProgressMessage = null;

        if (!createDidimoTask.Result.success)
        {
            EditorUtility.DisplayDialog("Failed to create didimo",
                "Failed to create your didimo. Please check the console for logs.", "OK");
            Debug.LogError("Error: Failed to create didimo");
            return;
        }

        //EditorUtility.DisplayDialog("Created didimo",
        //    "Created and imported your didimo with success. You can inspect it in the scene.", "OK");
        didimoKey = createDidimoTask.Result.didimo.DidimoKey;
        didimoComponents = createDidimoTask.Result.didimo;
        Debug.Log("Didimo created!!!!!!!!!!!!!!");

        GameObject foundGameObject = GameObject.Find(gameObjectName);

        if (foundGameObject != null)
        {
            Debug.Log("GameObject with the name '" + gameObjectName + "' exists.");
        }
        else
        {
            Debug.Log("GameObject with the name '" + gameObjectName + "' does not exist.");
        }
            
        Debug.Log("Calling ExportGLBByName with " + gameObjectName);
        ExportGlb.ExportGLBByName(gameObjectName);

    }

    public async Task<(bool success, DidimoComponents didimo)> CreateDidimoAndImportGltf(string photoPath,
        Configuration configuration = null, Action<float> creationProgress = null)
    {
        Debug.Log("Starting CreateDidimoAndImportGltf");

        Task<(bool success, string path, string key)> createNewDidimoTask = CreateDidimoAndDownload(photoPath,
            null, null, creationProgress);
        await createNewDidimoTask;

        Debug.Log("createNewDidimoTask completed");

        return await Import(createNewDidimoTask.Result.key, createNewDidimoTask.Result.path, configuration);
    }

    public async Task<(bool success, string filePath, string key)> CreateDidimoAndDownload(string photoPath,
        string downloadPath, Configuration configuration = null, Action<float> creationProgress = null,
        DidimoDetailsResponse.DownloadTransferFormatType format = DidimoDetailsResponse.DownloadTransferFormatType.Gltf)
    {
        Debug.Log("Starting CreateDidimoAndDownload");

        DidimoNetworkingResources.NetworkConfig.DownloadRoot = downloadPath;

        Task<(bool success, string didimoKey)> createNewDidimoTask = CreateNewDidimo(photoPath);
        await createNewDidimoTask;

        Debug.Log("createNewDidimoTask completed");

        if (!createNewDidimoTask.Result.success) return (false, null, null);

        Task<(bool success, DidimoDetailsResponse status, string errorCode)> checkForStatusUntilCompletionTask =
            CheckForStatusUntilCompletion(createNewDidimoTask.Result.didimoKey, creationProgress);
        await checkForStatusUntilCompletionTask;

        Debug.Log("checkForStatusUntilCompletionTask completed");

        Downloadable downloadable =
            checkForStatusUntilCompletionTask.Result.status.GetDownloadableForTransferFormat(format);
        if (downloadable == null)
        {
            Debug.LogError(
                $"Failed to get downloadable of type {DidimoDetailsResponse.DownloadTransferFormatType.Gltf}");
            return (false, null, null);
        }

        (bool success, string path) downloadResult = await downloadable.DownloadToDisk(true);

        if (!downloadResult.success)
        {
            Debug.LogError($"Failed to download to disk");
            return (false, null, null);
        }
        else
        {
            Debug.Log("Downloaded to " + downloadResult.path);
            Debug.Log("Didimo key is " + createNewDidimoTask.Result.didimoKey);
            
            gameObjectName = "didimo_" + createNewDidimoTask.Result.didimoKey;
            
            
            return (downloadResult.success, downloadResult.path, createNewDidimoTask.Result.didimoKey);
        }
    }

    [Button("Delete didimo")]
    public async Task DeleteDidimo()
    {
        Task<bool> deleteTask = Api.Instance.DeleteDidimo(didimoKey);
        await deleteTask;
        if (deleteTask.Result)
        {
            EditorUtility.DisplayDialog("Deleted didimo", $"Deleted didimo with key: {didimoKey}", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Failed to delete didimo", $"Failed to delete didimo with key {didimoKey}",
                "OK");
        }
    }

    [Button("List didimos")]
    public async Task ListDidimos()
    {
        didimoList = new List<string>();
        Task<(bool success, List<DidimoDetailsResponse> didimos)> listTask = Api.Instance.GetAllDidimos();
        await listTask;
        if (!listTask.Result.success) return;

        didimoList.AddRange(listTask.Result.didimos.Select(item => item.DidimoKey));
    }

    [Button]
    public async Task ImportFromKey()
    {
        if (ProgressMessage != null)
        {
            EditorUtility.DisplayDialog("Error", "Please wait for the current request to complete.", "OK");
            return;
        }

        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Error", "To use this feature you must first enter play mode", "OK");
            return;
        }

        string chosenKey = didimoList[selectedDidimo];

        if (string.IsNullOrEmpty(chosenKey)) return;

        ProgressMessage = "Importing your didimo";
        Progress = 0f;
        Task<(bool success, DidimoComponents didimo)> importFromKeyTask =
            Api.Instance.DidimoFromKey(chosenKey, null, progress => { Progress = progress; });
        await importFromKeyTask;
        ProgressMessage = null;

        if (!importFromKeyTask.Result.success)
        {
            EditorUtility.DisplayDialog("Failed to import didimo",
                "Failed to import your didimo. Please check the console for logs.", "OK");
            return;
        }

        EditorUtility.DisplayDialog("Imported didimo",
            "Imported your didimo with success. You can inspect it in the scene.", "OK");
        didimoKey = importFromKeyTask.Result.didimo.DidimoKey;
        didimoComponents = importFromKeyTask.Result.didimo;
    }

    [Button]
    protected async Task AddRandomHair()
    {
        if (ProgressMessage != null)
        {
            EditorUtility.DisplayDialog("Error", "Please wait for the current request to complete.", "OK");
            return;
        }

        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Error", "To use this feature you must first enter play mode", "OK");
            return;
        }

        if (didimoComponents == null)
        {
            EditorUtility.DisplayDialog("Error", "First, create a didimo", "OK");
            return;
        }

        var deformableDatabase = UnityEngine.Resources.Load<DeformableDatabase>("DeformableDatabase");
        string deformableId = deformableDatabase.AllIDs.RandomOrDefault();
        if (!didimoComponents.Deformables.TryCreate(deformableId, out Deformable deformable))
        {
            EditorUtility.DisplayDialog("Error", "Failed to create deformable", "OK");
            return;
        }

        deformable.gameObject.SetActive(false);
        byte[] undeformedMeshData = deformable.GetUndeformedMeshData();
        Progress = 0.0f;
        ProgressMessage = "Deforming asset";
        (bool success, byte[] deformedMeshData) = await Api.Instance.Deform(didimoComponents.DidimoKey,
            undeformedMeshData, progress => { Progress = progress; });
        if (!success)
        {
            EditorUtility.DisplayDialog("Error", "Error with api call", "OK");
            ProgressMessage = null;
            return;
        }

        ProgressMessage = null;

        deformable.SetDeformedMeshData(deformedMeshData);
        deformable.gameObject.SetActive(true);
    }

    [Button]
    private void ChangeHairColor()
    {
        if (didimoComponents == null)
        {
            EditorUtility.DisplayDialog("Error", "First, create a didimo", "OK");
            return;
        }

        if (didimoComponents.Deformables.TryFind(out Hair hair))
        {
            Selection.objects = new Object[] {hair};
            // You can also set a preset with:
            // hair.SetPreset(0);
        }
        else
        {
            EditorUtility.DisplayDialog("Error", "Please add a hairstyle to your didimo first", "OK");
        }
    }

    [Button]
    private void RemoveHair()
    {
        if (didimoComponents.Deformables.TryFind(out Hair hair))
        {
            //if (Application.isPlaying) Destroy(hair.gameObject);
            //else DestroyImmediate(hair.gameObject);
        }
        else
        {
            Debug.Log("Could not find hair to remove.");
        }
    }
#endif

    public async Task<(bool success, AccountStatusResponse status)> AccountStatus()
    {
        AccountStatusQuery didimoDetailsQuery = new AccountStatusQuery();
        AccountStatusResponse result = await didimoDetailsQuery.ExecuteQuery();

        return (result != null, result);
    }

    public async Task<(bool success, byte[] data)> Deform(string didimoKey, byte[] data,
        Action<float> statusProgress = null)
    {
        Task<(bool success, DidimoDetailsResponse status)> didimoStatus = CheckDidimoStatus(didimoKey);
        await didimoStatus;
        if (!didimoStatus.Result.success)
        {
            Debug.LogWarning($"Failed to get status from didimo {didimoKey}.");
            return (false, null);
        }

        Downloadable downloadable =
            didimoStatus.Result.status.GetDownloadableArtifact(DidimoDetailsResponse.DownloadArtifactType.Deformer_dmx);
        if (downloadable == null)
        {
            Debug.LogError(
                $"Failed to get downloadable of type {DidimoDetailsResponse.DownloadArtifactType.Deformer_dmx}");
            return (false, null);
        }

        (bool success, byte[] bytes) downloadArtifact = await downloadable.Download();
        if (!downloadArtifact.success)
        {
            Debug.LogError("Failed to download deformation matrix");
            return (false, null);
        }

        DeformQuery deformQuery = new DeformQuery(didimoKey, downloadArtifact.bytes, data);
        DeformResponse deformResponse = await deformQuery.ExecuteQuery();

        string deformID = deformResponse.DeformedID;
        (bool success, DidimoDetailsResponse status, string errorCode) checkForStatusUntilCompletion =
            await CheckForStatusUntilCompletion(deformID, statusProgress, IApi.StatusType.Deform);

        if (!checkForStatusUntilCompletion.success) return (false, null);

        downloadable =
            checkForStatusUntilCompletion.status.GetDownloadableForTransferFormat(DidimoDetailsResponse
                .DownloadTransferFormatType.Package);
        if (downloadable == null)
        {
            Debug.LogError(
                $"Failed to get downloadable of type {DidimoDetailsResponse.DownloadTransferFormatType.Package}");
            return (false, null);
        }

        (bool success, string path) = await downloadable.DownloadToDisk(true);

        if (!success)
        {
            Debug.LogWarning($"Failed to download deformed data.");
            return (false, null);
        }

        string downloadPath = path + "/" + DeformQuery.DeformedAssetName;
        (success, data) = await DidimoDownloader.Download(downloadPath);

        if (!success)
        {
            Debug.LogError($"Failed to load deformed asset from {downloadPath}");
            return (false, null);
        }

        return (true, data);
    }

    public async Task<(bool success, DidimoDetailsResponse status, string errorCode)> CheckForStatusUntilCompletion(
        string statusKey, Action<float> statusProgress = null, IApi.StatusType statusType = IApi.StatusType.Didimo)
    {
        Task<(bool success, DidimoDetailsResponse status)> statusTask;
        do
        {
            statusTask = CheckDidimoStatus(statusKey, statusType);
            await statusTask;

            // TODO: we should be throwing errors, so they can then be caught.
            if (!statusTask.Result.success)
                return (false, statusTask.Result.status, statusTask.Result.status.ErrorCode);
            if (statusProgress != null)
            {
                statusProgress(statusTask.Result.status.Percent);
            }

            await Task.Delay(CHECK_STATUS_WAIT_TIME);
        } while (!statusTask.Result.status.IsDone);

        return (true, statusTask.Result.status, null);
    }

    public async Task<(bool success, string didimoKey)> CreateNewDidimo(string filePath)
    {
        NewDidimoQuery newDidimoQuery =
            new NewDidimoQuery(filePath, DidimoNetworkingResources.NetworkConfig.GetFeaturesForApi());
        NewDidimoResponse result = await newDidimoQuery.ExecuteQuery();

        return (result != null, result?.DidimoKey);
    }

    public async Task<(bool success, DidimoDetailsResponse status)> CheckDefaultDidimoStatus()
    {
        DefaultDidimoDetailsQuery didimoDetailsQuery = new DefaultDidimoDetailsQuery();
        return await CheckDidimoStatus(didimoDetailsQuery);
    }

    public async Task<(bool success, DidimoDetailsResponse status)> CheckDidimoStatus(string didimoKey,
        IApi.StatusType statusType = IApi.StatusType.Didimo)
    {
        switch (statusType)
        {
            case IApi.StatusType.Didimo:
                return await CheckDidimoStatus(new DidimoDetailsQuery(didimoKey));
            case IApi.StatusType.Deform:
                return await CheckDidimoStatus(new DeformDetailsQuery(didimoKey));
            default:
                throw new ArgumentOutOfRangeException(nameof(statusType), statusType, null);
        }
    }

    public async Task<(bool success, Phrase phrase)> TextToSpeech(string text, string voice)
    {
        TextToSpeechQuery unsetMetaQuery = new TextToSpeechQuery(text, voice);
        Task<TextToSpeechResponse> queryTask = unsetMetaQuery.ExecuteQuery();
        await queryTask;

        return (false, null);
    }

    public async Task<bool> DeleteMetadata(string didimoKey, string key)
    {
        DeleteMetaQuery deleteMetaQuery = new DeleteMetaQuery(didimoKey, key);
        Task<DidimoEmptyResponse> queryTask = deleteMetaQuery.ExecuteQuery();
        await queryTask;

        return true;
    }

    public async Task<(bool success, string value)> GetMetadata(string didimoKey, string key)
    {
        GetMetaQuery setMetaQuery = new GetMetaQuery(didimoKey, key);
        Task<MetaDataResponse> queryTask = setMetaQuery.ExecuteQuery();
        await queryTask;

        return (true, queryTask.Result.Value);
    }

    public async Task<(bool success, string value)> UpdateMetadata(string didimoKey, string value, string key)
    {
        UpdateMetaQuery updateMetaQuery = new UpdateMetaQuery(didimoKey, value, key);
        Task<MetaDataResponse> queryTask = updateMetaQuery.ExecuteQuery();
        await queryTask;

        return (true, queryTask.Result.Value);
    }

    public async Task<bool> SetMetadata(string didimoKey, string key, string value)
    {
        SetMetaQuery setMetaQuery = new SetMetaQuery(didimoKey, key, value);
        Task<DidimoResponse> queryTask = setMetaQuery.ExecuteQuery();
        await queryTask;

        return true;
    }

    public async Task<(bool success, List<string> didimoKeys)> GetAllDidimoKeys()
    {
        Task<(bool success, List<DidimoDetailsResponse> didimos)> queryTask = GetAllDidimos();
        await queryTask;

        if (!queryTask.Result.success) return (false, null);

        List<string> didimoKeys = queryTask.Result.didimos.Select(m => m.DidimoKey).ToList();
        return (true, didimoKeys);
    }

    public async Task<(bool success, List<DidimoDetailsResponse> didimos)> GetAllDidimos()
    {
        ListQuery listQuery = new ListQuery();
        Task<ListResponse> queryTask = listQuery.ExecuteQuery();
        await queryTask;

        if (!queryTask.Result.Didimos.Any())
        {
            Debug.LogWarning("GetAllDidimos API call succeeded but there were no didimos on the account.");
            return (false, null);
        }

        return (true, queryTask.Result.Didimos.ToList());
    }

    public async Task<(bool success, DidimoComponents didimo)> ImportDefaultDidimoGltf(
        Configuration configuration = null)
    {
        Task<DidimoDetailsResponse> detailsQuery = new DefaultDidimoDetailsQuery().ExecuteQuery();
        await detailsQuery;

        Downloadable downloadable =
            detailsQuery.Result.GetDownloadableForTransferFormat(DidimoDetailsResponse.DownloadTransferFormatType.Gltf);
        if (downloadable == null)
        {
            Debug.LogError(
                $"Failed to get downloadable of type {DidimoDetailsResponse.DownloadTransferFormatType.Gltf}");
            return (false, null);
        }

        (bool success, string path) = await downloadable.DownloadToDisk(true);
        if (!success)
        {
            Debug.LogError("Failed to download didimo.");
            return (false, null);
        }

        return await Import(detailsQuery.Result.DidimoKey, path, configuration);
    }

    public async Task<bool> DeleteDidimo(string didimoKey)
    {
        DeleteQuery deleteQuery = new DeleteQuery(didimoKey);
        await deleteQuery.ExecuteQuery();
        return true;
    }

    public async Task<(bool success, DidimoDetailsResponse status)> CheckDidimoStatus(
        GetQuery<DidimoDetailsResponse> detailsQuery)
    {
        Task<DidimoDetailsResponse> queryTask = detailsQuery.ExecuteQuery();
        await queryTask;

        if (queryTask.Result == null)
        {
            return (false, null);
        }

        return (true, queryTask.Result);
    }

    private static async Task<(bool success, DidimoComponents didimo)> Import(string didimoKey, string path,
        Configuration configuration = null)
    {
        DidimoComponents didimoComponents = await DidimoLoader.LoadDidimoInFolder(didimoKey, path, configuration);

        return (true, didimoComponents);
    }

    public async Task<(bool success, DidimoComponents didimo)> DidimoFromKey(string didimoKey,
        Configuration configuration = null, Action<float> creationProgress = null)
    {
        Task<(bool success, DidimoDetailsResponse status, string errorCode)> checkForStatusUntilCompletionTask =
            CheckForStatusUntilCompletion(didimoKey, creationProgress);
        await checkForStatusUntilCompletionTask;

        Downloadable downloadable =
            checkForStatusUntilCompletionTask.Result.status.GetDownloadableForTransferFormat(DidimoDetailsResponse
                .DownloadTransferFormatType.Gltf);
        if (downloadable == null)
        {
            Debug.LogError(
                $"Failed to get downloadable of type {DidimoDetailsResponse.DownloadTransferFormatType.Gltf}");
            return (false, null);
        }

        (bool success, string path) downloadResult = await downloadable.DownloadToDisk(true);
        return await Import(didimoKey, downloadResult.path, configuration);
    }
}