using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Didimo.Networking;
using UnityEngine.UI;

public class DidimoMinterManager : MonoBehaviour
{
    public Button createDidimoButton;
    // Start is called before the first frame update
    void Start()
    {
        DidimoMinter customApi = new DidimoMinter();
        Didimo.Networking.Api.SetImplementation(customApi);
        
        Debug.Log("Creating didimo");
        //on click button call await customApi.CreateDidimoAndImport();   
        createDidimoButton.onClick.AddListener(async () => await customApi.GenerateDidimoImportInScene());
    }
    
}
