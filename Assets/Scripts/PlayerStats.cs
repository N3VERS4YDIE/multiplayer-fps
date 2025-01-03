using System.Collections;
using FishNet.Component.Spawning;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class PlayerStats : NetworkBehaviour
{
    public byte maxHealth;
    public float respawnTime;

    public byte Health
    {
        get => health.Value;

        [ServerRpc(RequireOwnership = false)]
        set
        {
            if (value <= maxHealth)
            {
                health.Value = value;
            }
        }
    }

    readonly SyncVar<byte> health = new(new SyncTypeSettings(0f));

    public override void OnStartClient()
    {
        base.OnStartClient();

        health.OnChange += OnHealthChanged;
        
        if (IsOwner)
        {
            Health = maxHealth;
        }
    }

    void OnHealthChanged(byte prev, byte next, bool asServer)
    {
        if (IsOwner)
        {
            GameManager.Instance.localPlayerHud.UpdateHealthBar(Mathf.Clamp(next, 0, maxHealth), maxHealth);
        }

        Debug.Log($"Health changed on client {Owner.ClientId}: {next}");

        if (next <= 0)
        {
            Respawn();
        }
    }

    public void Respawn()
    {
        StartCoroutine(IRespawn());
    }

    IEnumerator IRespawn()
    {
        PlayerController playerController = GetComponent<PlayerController>();
        CharacterController characterController = GetComponent<CharacterController>();

        characterController.enabled = false;

        if (IsOwner)
        {
            GameManager.Instance.localPlayerHud.UpdateHealthBar(0, maxHealth);
            Camera.main.transform.parent = null;
            playerController.enabled = false;
        }

        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false);
        }

        yield return new WaitForSeconds(respawnTime);

        Transform[] spawns = FindFirstObjectByType<PlayerSpawner>().Spawns;
        transform.position = spawns[Random.Range(0, spawns.Length)].position;

        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(true);
        }

        if (IsOwner)
        {
            GameManager.Instance.localPlayerHud.UpdateHealthBar(maxHealth, maxHealth);
            Camera.main.transform.parent = GetComponent<PlayerController>().cameraPivot;
            Camera.main.transform.localPosition = Vector3.zero;
            playerController.enabled = true;
        }

        characterController.enabled = true;
        Health = maxHealth;
    }
}
