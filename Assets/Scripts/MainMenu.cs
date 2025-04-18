using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviourPunCallbacks
{

    public Button createRoomButton, JoinRoomButton;
    public TMP_InputField createRoomInput, JoinRoomInput;
    public GameObject mainMenuPanel;
    public TMP_Text warningText;



// Start is called before the first frame update
    void Start()
    {
        PhotonNetwork.ConnectUsingSettings();
        createRoomButton.onClick.AddListener(CreateRoom);
        JoinRoomButton.onClick.AddListener(JoinRoom);
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
        warningText.text = "Room Joined";
    }
    
    // Update is called once per frame
    void Update()
    {
        
    }
}
