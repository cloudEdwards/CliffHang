using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class GameLogic : NetworkBehaviour, IPlayerJoined, IPlayerLeft
{
    [SerializeField] private NetworkPrefabRef playerPrefab;
    [Header("Character Prefabs")]
    public List<NetworkPrefabRef> characterPrefabs; // assign your character prefabs here in order
    [Networked, Capacity(12)] private NetworkDictionary<PlayerRef, Player> Players => default;

    public void PlayerJoined(PlayerRef player)
    {
        if (HasStateAuthority) {
            int charIndex = PlayerPrefs.GetInt("SelectedCharacterIndex");
            Debug.Log("index " + charIndex);
            NetworkPrefabRef charPrefab = characterPrefabs[charIndex];
            Debug.Log("prefab ref " + characterPrefabs[charIndex]);
            NetworkPrefabRef chosenPlayerPrefab;
            if (charPrefab != null) {
                chosenPlayerPrefab = charPrefab;
            } else {
                // default player
                chosenPlayerPrefab = playerPrefab;
            }
            NetworkObject playerObject = Runner.Spawn(chosenPlayerPrefab, Vector3.up, Quaternion.identity, player);
            Players.Add(player, playerObject.GetComponent<Player>());
        }
    }

    public void PlayerLeft(PlayerRef player)
    {
        if (! HasStateAuthority) {
            return;
        }

        if (Players.TryGet(player, out Player playerBehaviour)) {
            Players.Remove(player);
            Runner.Despawn(playerBehaviour.Object);
        }
    }
}
