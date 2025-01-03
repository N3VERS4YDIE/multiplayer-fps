using FishNet.Managing;
using UnityEngine;

public class NetworkStarter : MonoBehaviour
{
    void Start()
    {
        NetworkManager networkManager = FindFirstObjectByType<NetworkManager>();

        if (!networkManager.ServerManager.Started)
            networkManager.ServerManager.StartConnection();
            
        networkManager.ClientManager.StartConnection();
    }
}
