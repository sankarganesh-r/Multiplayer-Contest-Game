using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;


public class PlayerData
{
    public int number;
    public Decision decision;
    public int chips;
}

public enum Decision
{
    None,
    Fold,
    Contest
}

public enum GameState { Waiting, RoundStart, DecisionPhase, RevealWinner, RoundEnd }

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance;
    public GameState currentState = GameState.Waiting;
    public float decisionTime = 10f;

    private Dictionary<Player, int> playerNumbers = new Dictionary<Player, int>();
    private Dictionary<Player, string> playerDecisions = new Dictionary<Player, string>();

    private float roundStartTime;

    private void Awake() => Instance = this;
    

    public void RoundStart()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(StartRound());
        }
    }


     IEnumerator StartRound()
    {
        currentState = GameState.RoundStart;
        playerNumbers.Clear();
        playerDecisions.Clear();

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            int randomNum = Random.Range(1, 101);
            playerNumbers[player] = randomNum;
           
            Hashtable props = new Hashtable { { "Number", randomNum }, { "Decision", "Undecided" } };
            player.SetCustomProperties(props);
        }
        
        yield return new WaitForSeconds(1f);
        UIManager.Instance.SetNumber(playerNumbers[PhotonNetwork.LocalPlayer]);
        photonView.RPC("BeginDecisionPhase", RpcTarget.AllBuffered, PhotonNetwork.Time);
    }


    [PunRPC]
    void BeginDecisionPhase(double networkTime)
    {
        currentState = GameState.DecisionPhase;
        roundStartTime = (float)networkTime;
        StartCoroutine(WaitForDecisionEnd());
    }


    IEnumerator WaitForDecisionEnd()
    {
        StartCoroutine(UIManager.Instance.StartCountdown(decisionTime));
        yield return new WaitForSeconds(decisionTime);

        if (PhotonNetwork.IsMasterClient)
        {
            DetermineWinner();
        }
    }


    [PunRPC]
    public void SubmitDecision(string decision)
    {
        Player player = PhotonNetwork.LocalPlayer;
        playerDecisions[player] = decision;

        Hashtable props = new Hashtable { { "Decision", decision } };
        player.SetCustomProperties(props);
    }


    void DetermineWinner()
    {
        currentState = GameState.RevealWinner;
        List<Player> contenders = new List<Player>();
        int highestNum = -1;
        Player winner = null;

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            object decisionObj;
            if (player.CustomProperties.TryGetValue("Decision", out decisionObj) && decisionObj.ToString() == "Contest")
            {
                contenders.Add(player);
                int number = (int)player.CustomProperties["Number"];
                if (number > highestNum)
                {
                    highestNum = number;
                    winner = player;
                }
            }
        }

        photonView.RPC("AnnounceWinner", RpcTarget.AllBuffered, winner?.NickName ?? "No Winner", highestNum);
    }


    [PunRPC]
    void AnnounceWinner(string winnerName, int winningNumber)
    {
        Debug.Log("Winner: " + winnerName + " with number " + winningNumber);
        StartCoroutine(RoundCooldown());
    }

    IEnumerator RoundCooldown()
    {
        currentState = GameState.RoundEnd;
        yield return new WaitForSeconds(5f);

        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(StartRound());
        }
    }
   
}
