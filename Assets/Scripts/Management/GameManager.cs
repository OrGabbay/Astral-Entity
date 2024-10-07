using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Player")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private float respawnDelay = 0.3f;
    public Player player;

    [Header("Fruits Managment")]
    public bool coinshaveRandomLook;
    public int coinsCollected;

    private void Awake()
    {
        Instance = this;
    }

    public void RespawnPlayer() => StartCoroutine(RespawnCorutine());

    private IEnumerator RespawnCorutine()
    {
        yield return new WaitForSeconds(respawnDelay);

        GameObject newplayer = Instantiate(playerPrefab,respawnPoint.position,Quaternion.identity);
        player = newplayer.GetComponent<Player>();
    }

    public void AddCoin() => coinsCollected++;
    public bool CoinsHaveRandomLook() => coinshaveRandomLook;
}
