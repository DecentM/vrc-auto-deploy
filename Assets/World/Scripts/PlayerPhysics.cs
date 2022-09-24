
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class PlayerPhysics : UdonSharpBehaviour
{
    void Start()
    {
        if (Networking.LocalPlayer == null || !Networking.LocalPlayer.IsValid())
            return;

        VRCPlayerApi player = Networking.LocalPlayer;

        player.SetJumpImpulse(3);
        player.SetWalkSpeed(2);
        player.SetRunSpeed(4);
        player.SetStrafeSpeed(2);
    }
}
