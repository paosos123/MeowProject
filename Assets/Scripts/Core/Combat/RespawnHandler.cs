using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class RespawnHandler : NetworkBehaviour
{
    [SerializeField] private MovementPlayer playerPrefab;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) { return; }
        MovementPlayer[] players = FindObjectsByType<MovementPlayer>(FindObjectsSortMode.None);
        foreach (MovementPlayer player in players)
        {
            HandlePlayerSpawned(player);
        }

        MovementPlayer.OnPlayerSpawned += HandlePlayerSpawned;
        MovementPlayer.OnPlayerDespawned += HandlePlayerDespawned;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) { return; }
        MovementPlayer.OnPlayerSpawned -= HandlePlayerSpawned;
        MovementPlayer.OnPlayerDespawned -= HandlePlayerDespawned;
    }

    private void HandlePlayerSpawned(MovementPlayer player)
    {
        HandlePlayerDie(player);
    }

    private void HandlePlayerDespawned(MovementPlayer player)
    {
        HandlePlayerDie(player);
    }

    private void HandlePlayerDie(MovementPlayer player)
    {
        Destroy(player.gameObject);

        StartCoroutine(RespawnPlayer(player.OwnerClientId));
    }

    private IEnumerator RespawnPlayer(ulong ownerClientId)
    {
        yield return null;

        MovementPlayer playerInstance = Instantiate(
            playerPrefab, SpawnPoint.GetRandomSpawnPos(), Quaternion.identity);

        playerInstance.NetworkObject.SpawnAsPlayerObject(ownerClientId);

    }
}