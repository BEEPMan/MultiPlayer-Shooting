using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public Player player;

    public InputManager input;

    public CinemachineCamera cinemachine;
    public Pathfinding path;
    public MapGenerator mapGenerator;

    public GameObject spawner;

    public GameObject startPanel;
    public GameObject lobbyPanel;
    public Text playerText;
    public Button gameStartButton;
    public Button hostButton;
    public Button clientButton;

    public int roomCount;

    private void Awake()
    {
        Instance = this;
    }

    public void StartHost()
    {
        NetworkManager.Singleton.StartHost();
        mapGenerator.GenerateLobby();
        input.Init();
        startPanel.SetActive(false);
        lobbyPanel.SetActive(true);
    }

    public void StartClient()
    {
        NetworkManager.Singleton.StartClient();
        mapGenerator.GenerateLobby();
        input.Init();
        startPanel.SetActive(false);
    }

    public void StartGame()
    {
        lobbyPanel.SetActive(false);
        mapGenerator.ClearRoom();
        mapGenerator.GenerateMap(roomCount);
    }
}
