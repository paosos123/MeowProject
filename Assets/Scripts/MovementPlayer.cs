using System;
using Unity.Cinemachine;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;


public class MovementPlayer :  NetworkBehaviour
{
   
    private Rigidbody2D rb; // เพิ่มตัวแปร Rigidbody2D
    [SerializeField] float moveSpeed = 10f;

    [field: SerializeField] public Health Health { get; private set; }

    [SerializeField] private CinemachineCamera virtualCamera;
    public NetworkVariable<FixedString32Bytes> PlayerName = new NetworkVariable<FixedString32Bytes>();
    public NetworkVariable<int> PlayerColorIndex = new NetworkVariable<int>();
    public NetworkVariable<int> TeamIndex = new NetworkVariable<int>();

    [SerializeField] private int ownerPriority = 15;

    public static event Action<MovementPlayer> OnPlayerSpawned;
    public static event Action<MovementPlayer> OnPlayerDespawned;

    void Start()
    {
        // GetComponent Rigidbody2D เมื่อ GameObject ถูกสร้าง
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody2D component not found on this GameObject!");
            enabled = false; // ปิดการทำงานของสคริปต์ถ้าไม่มี Rigidbody2D
        }
    }
    void Update() // ใช้ FixedUpdate สำหรับการเคลื่อนที่ที่เกี่ยวข้องกับฟิสิกส์
    {
        if(!IsOwner)
            return;
       // Move();
        
    }
    /*  void FixedUpdate() // ใช้ FixedUpdate สำหรับการเคลื่อนที่ที่เกี่ยวข้องกับฟิสิกส์
      {
          if(!IsOwner)
              return;
          Move();

      }*/

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            UserData userData = null;
            if (IsHost)
            {
                userData =
                HostSingleton.Instance.GameManager.NetworkServer.GetUserDataByClientId(OwnerClientId);
            }
            else
            {
                userData =
                    ServerSingleton.Instance.GameManager.NetworkServer.GetUserDataByClientId(OwnerClientId);
            }


            PlayerName.Value = userData.userName;
            PlayerColorIndex.Value = userData.userColorIndex;
            TeamIndex.Value = userData.teamIndex;
          
            if (IsOwner)
            {
                virtualCamera.Priority = ownerPriority;
            }

            OnPlayerSpawned?.Invoke(this);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            OnPlayerDespawned?.Invoke(this);
        }
    }

    void Move()
    {
        float moveInputX = Input.GetAxisRaw("Horizontal");
        float moveInputY = Input.GetAxisRaw("Vertical");
        Vector2 movement = new Vector2(moveInputX, moveInputY).normalized;

        // ใช้ velocity ของ Rigidbody2D ในการเคลื่อนที่
        if (rb != null)
        {
            rb.linearVelocity = movement * moveSpeed;
        }
    }
}
