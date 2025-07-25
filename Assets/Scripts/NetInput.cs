using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public enum InputButton
{
    Jump,
    Grab,
    Climb
}

public struct NetInput : INetworkInput
{
    public NetworkButtons Buttons;
    public Vector2 Direction;
    public Vector2 LookDelta;
}