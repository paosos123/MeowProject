using UnityEngine;
using Unity.Netcode.Components;
public class ClientNetworkTranform : NetworkTransform
{
  protected override bool OnIsServerAuthoritative()
  {
    return false;
  }
}
