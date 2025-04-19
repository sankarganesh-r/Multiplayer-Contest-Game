using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviourPunCallbacks
{
    public static UIManager Instance;
    public Button createRoomButton, JoinRoomButton, startRoundButton, foldButton, contestButton;
    public TMP_InputField createRoomInput, JoinRoomInput;
    public GameObject mainMenuPanel, lobbyPanel, decisionPanel, resultPanel;
    public TMP_Text warningText, numberText, timerText, userNameText, roomNameText, decisionShowText, 
        winnerText;
    public GameObject lobbyPlayerNamePrefab, playerNameParent,playerNumberParent;

    List<GameObject> playerList = new List<GameObject>();

    void Awake()
    {
        Instance = this;
        string username = "Player" + Random.Range(1000, 9999);
        PhotonNetwork.NickName = username;
        PhotonNetwork.ConnectUsingSettings();
        
    }

    // Start is called before the first frame update
    void Start()
    {
        createRoomButton.onClick.AddListener(CreateRoom);
        JoinRoomButton.onClick.AddListener(JoinRoom);
        startRoundButton.onClick.AddListener(()=> GameManager.Instance.RoundStart());
        foldButton.onClick.AddListener(OnFoldPressed);
        contestButton.onClick.AddListener(OnContestPressed);
    }


    public void StartRound()
    {
        decisionPanel.SetActive(true);
        lobbyPanel.SetActive(false);
        resultPanel.SetActive(false);
        decisionShowText.text="";
    }


    public void CreateRoom()
    {
        RoomOptions roomOptions= new RoomOptions();
        roomOptions.MaxPlayers=4;
        if(!string.IsNullOrEmpty(createRoomInput.text))
            PhotonNetwork.CreateRoom(createRoomInput.text, roomOptions);
    }


    public void JoinRoom()
    {
        if(!string.IsNullOrEmpty(JoinRoomInput.text))
            PhotonNetwork.JoinRoom(JoinRoomInput.text);

    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        mainMenuPanel.SetActive(false);
        lobbyPanel.SetActive(true);
        //warningText.text = "Room Joined \n" + "Username "+PhotonNetwork.LocalPlayer.NickName;
        SetUserName(PhotonNetwork.LocalPlayer.NickName);
        SetRoomName(PhotonNetwork.CurrentRoom.Name);
        ShowPlayerName(PhotonNetwork.LocalPlayer.NickName);
        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (player.CustomProperties.TryGetValue("Number", out object number))
            {
                Debug.Log($"Player {player.NickName} has number {number}");
                GameObject playersName = Instantiate(lobbyPlayerNamePrefab, playerNameParent.transform);
                playersName.GetComponent<TMP_Text>().text = player.NickName;
                playerList.Add(playersName);
            }
        }
        startRoundButton.gameObject.SetActive(PhotonNetwork.IsMasterClient ? true : false);

        GameManager.Instance.InitializePlayerChips();
    }


    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Photon as " + PhotonNetwork.NickName);
        //PhotonNetwork.JoinLobby(); // or go straight to room creation
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log("Player joined: " + newPlayer.NickName);
        ShowPlayerName(newPlayer.NickName);
    }


    void ShowPlayerName(string username)
    {
        GameObject playersName = Instantiate(lobbyPlayerNamePrefab,playerNameParent.transform);
        playersName.GetComponent<TMP_Text>().text = username;
        playerList.Add(playersName);
    }
    

    public void OnFoldPressed()
    {
        if (GameManager.Instance.currentState == GameState.DecisionPhase)
        {
            decisionShowText.text="You're Selected FOLD!!!!";
            GameManager.Instance.SubmitDecision("Fold");
        }
    }

    public void OnContestPressed()
    {
        if (GameManager.Instance.currentState == GameState.DecisionPhase)
        {
            decisionShowText.text="You're Selected CONTEST!!!!";
            GameManager.Instance.SubmitDecision("Contest");
        }
    }

    public void SetNumber(int num)
    {
        numberText.text=$"Your Number is {num}";
    }

    public void SetUserName(string userName)
    {
        userNameText.text=userName;
    }

    public void SetRoomName(string roomName)
    {
        roomNameText.text=roomName;
    }

    public void SetWinnerText(string winnerName)
    {
        decisionPanel.SetActive(false);
        resultPanel.SetActive(true);
        winnerText.text=winnerName;
        foreach(var players in playerList){
            Destroy(players);
        }
        playerList.Clear();
        
        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (player.CustomProperties.TryGetValue("Number", out object number))
            {
                Debug.Log($"Player {player.NickName} has number {number}");
                GameObject playersName = Instantiate(lobbyPlayerNamePrefab, playerNumberParent.transform);
                playersName.GetComponent<TMP_Text>().text = player.NickName + "  "+ number;
                playerList.Add(playersName);
            }
        }
    }

    public IEnumerator StartCountdown(float time)
    {
        while (time > 0)
        {
            timerText.text = Mathf.Ceil(time).ToString()+"s";
            yield return new WaitForSeconds(1f);
            time -= 1f;
        }

        timerText.text = "Time's up!";
        // Optionally, do something else here
    }
}
