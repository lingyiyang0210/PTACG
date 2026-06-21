using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class TrashCounter : BaseCounter
{
    public static event EventHandler OnAnyObjectTrashed;

    new public static void ResetStaticData()
    {
        OnAnyObjectTrashed = null;
    }

    public override void Interact(Player player)
    {
        if (player.HasKitchenObject())
        {
            KitchenObject.DestroyKitchenObject(player.GetKitchenObject());

            InteractLogicServerRPC();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void InteractLogicServerRPC()
    {
        InteractLogicClientRPC();
    }

    [ClientRpc]
    private void InteractLogicClientRPC()
    {
        OnAnyObjectTrashed?.Invoke(this, EventArgs.Empty);
    }
}
