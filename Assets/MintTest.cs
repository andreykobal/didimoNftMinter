using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MintTest : MonoBehaviour
{
    // Start is called before the first frame update
    private void Start()
    {
        string ownerAddress = "0x95883a8bB3Fc36B9bdf3D937be96a7901306cE13";
        string tokenURI = "https://bafkreiefuyqk4tsxw4fbs7v45lqh2iudr2bxvpinmsdqiv72sqbigs76qe.ipfs.nftstorage.link/";

        StartCoroutine(NFTAPI.MintNFT(ownerAddress, tokenURI,
            (response) => Debug.Log("Minting successful: " + response),
            (error) => Debug.LogError("Minting error: " + error)));
    }
}
