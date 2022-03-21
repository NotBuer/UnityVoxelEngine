using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour {
    public static GameManager Instance { get; private set; }


    public static event Action<Player> OnNewPlayerConnected;


    [Header("Active Players")]
    [SerializeField] private List<Player> playersList;

    public List<Player> GetPlayers { get => playersList; }

    private void Awake() {
        if (Instance == null) { Instance = this; } else { DestroyImmediate(gameObject); }
    }

    private void Start() {
        playersList.Add(FindObjectOfType<Player>());
        for (int i = 0; i < playersList.Count; i++) {
            if (playersList[i] == null) { continue; }
            OnNewPlayerConnected?.Invoke(playersList[i]);
        }
    }

}
