using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GameCheckPlayer : NetworkBehaviour // เปลี่ยนชื่อคลาสที่นี่
{
    public static GameCheckPlayer Instance { get; private set; } // เปลี่ยนชื่อคลาสที่นี่

    [SerializeField] private GameObject youWinPanel; // UI Panel แสดง "You Win"
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float delayBeforeChecking = 60f; // หน่วงเวลา 1 นาที (60 วินาที)
    [SerializeField] private float checkInterval = 1f; // ตรวจสอบทุก 1 วินาทีหลังจากหน่วงเวลา

    private bool canCheckForWinner = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // หน่วงเวลาการเริ่มตรวจสอบผู้เล่น
            Invoke(nameof(StartCheckingForWinner), delayBeforeChecking);
        }
    }

    private void StartCheckingForWinner()
    {
        if (IsServer)
        {
            canCheckForWinner = true;
            // เริ่มการตรวจสอบผู้เล่นซ้ำๆ ทุกช่วงเวลา
            InvokeRepeating(nameof(CheckRemainingPlayers), 0f, checkInterval);
        }
    }

    private void CheckRemainingPlayers()
    {
        if (!IsServer || !canCheckForWinner) return;

        // ค้นหา GameObject ทั้งหมดใน Scene ที่มี Tag "Player"
        GameObject[] players = GameObject.FindGameObjectsWithTag(playerTag);

        // นับจำนวนผู้เล่นที่ยัง Active อยู่
        int alivePlayersCount = players.Count(player => player != null && player.activeSelf);

        if (alivePlayersCount == 1 && NetworkManager.Singleton.ConnectedClients.Count > 0)
        {
            // พบผู้ชนะ
            GameObject winner = players.FirstOrDefault(player => player != null && player.activeSelf);

            if (winner != null && winner.TryGetComponent<NetworkObject>(out var winnerNetworkObject))
            {
                ShowYouWinClientRpc(winnerNetworkObject.OwnerClientId);
                // อาจจะหยุดการตรวจสอบเมื่อพบผู้ชนะแล้ว
                CancelInvoke(nameof(CheckRemainingPlayers));
            }
        }
        else if (alivePlayersCount <= 0 && NetworkManager.Singleton.ConnectedClients.Count > 0)
        {
            // อาจจะเสมอ หรือไม่มีผู้เล่นเหลือ
            Debug.Log("No winner or draw.");
            CancelInvoke(nameof(CheckRemainingPlayers));
            // อาจจะแสดง UI เสมอ หรือทำอย่างอื่น
        }
        // ถ้ามีผู้เล่นมากกว่า 1 คน จะยังคงตรวจสอบต่อไปตามช่วงเวลา
    }

    [ClientRpc]
    private void ShowYouWinClientRpc(ulong winnerClientId)
    {
        if (NetworkManager.Singleton.LocalClientId == winnerClientId)
        {
            Debug.Log("You are the winner!");
            if (youWinPanel != null)
            {
                youWinPanel.SetActive(true);
            }
            else
            {
                Debug.LogError("You Win Panel ไม่ได้ถูกกำหนดใน GameCheckPlayer!"); // เปลี่ยนชื่อคลาสใน Error Log ด้วย
            }
        }
        // เอา else ออก เพื่อไม่ให้คนอื่นเห็น
    }
}