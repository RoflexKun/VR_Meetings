using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils;

public class ClientPlayerMove : NetworkBehaviour
{
    [SerializeField] private XROrigin m_XROrigin;

    private void Awake()
    {
        m_XROrigin.enabled = false;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsOwner)
        {
            m_XROrigin.enabled = true;
        }
    }
}