using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public enum GameState { Waiting, RoundStart, DecisionPhase, RevealWinner, RoundEnd }

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance;
    public GameState currentState = GameState.Waiting;
    public float decisionTime = 10f;
    public int defaultChips = 100;
    public int betAmount = 10;

    private Dictionary<Player, int> playerNumbers = new Dictionary<Player, int>();
    private Dictionary<int, string> playerDecisions = new Dictionary<int, string>();

    private float roundStartTime;

    private void Awake() => Instance = this;
    

    public void RoundStart()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(StartRound());
        }
    }

    public void InitializePlayerChips()
    {
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (!player.CustomProperties.ContainsKey("Chips"))
            {
                Hashtable chipInit = new Hashtable { { "Chips", defaultChips } };
                player.SetCustomProperties(chipInit);
            }
        }
    }


     IEnumerator StartRound()
    {
        currentState = GameState.RoundStart;
        playerNumbers.Clear();
        playerDecisions.Clear();

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            int randomNum = UnityEngine.Random.Range(1, 101);
            playerNumbers[player] = randomNum;
           
            Hashtable props = new Hashtable { { "Number", randomNum }, { "Decision", "Undecided" } };
            player.SetCustomProperties(props);
        }
        
        yield return new WaitForSeconds(1f);
       
        photonView.RPC("BeginDecisionPhase", RpcTarget.AllBuffered, PhotonNetwork.Time);
    }


    [PunRPC]
    void BeginDecisionPhase(double networkTime)
    {
        UIManager.Instance.SetNumber((int)PhotonNetwork.LocalPlayer.CustomProperties["Number"]);
        UIManager.Instance.StartRound();
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


     public void SubmitDecision(string decision)
    {
        int actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;

        if (!playerDecisions.ContainsKey(actorNumber))
        {
            playerDecisions[actorNumber] = decision;

            Hashtable props = new Hashtable { { "Decision", decision } };

            if (decision == "Contest")
            {
                if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("Chips", out object chipObj))
                {
                    int chips = (int)chipObj;
                    props["Chips"] = Mathf.Max(0, chips - betAmount);
                }
            }

            PhotonNetwork.LocalPlayer.SetCustomProperties(props);

            photonView.RPC("RegisterDecision", RpcTarget.MasterClient, actorNumber, decision);
        }
    }

    [PunRPC]
    public void RegisterDecision(int actorNumber, string decision)
    {
        playerDecisions[actorNumber] = decision;
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
                Debug.Log(" ++++++  "+player.ActorNumber+" "+decisionObj);
                if (player.CustomProperties.TryGetValue("Number", out object numberObj))
                {
                    Debug.Log(" @@@@@  "+player.ActorNumber+" "+decisionObj+" "+numberObj);
                    contenders.Add(player);
                    int number = (int)numberObj;
                    if (number > highestNum)
                    {
                        highestNum = number;
                        winner = player;
                    }
                }
            }
        }

        if (winner != null)
        {
            int pot = betAmount * contenders.Count;
            if (winner.CustomProperties.TryGetValue("Chips", out object winChipsObj))
            {
                int winnerChips = (int)winChipsObj;
                winner.SetCustomProperties(new Hashtable { { "Chips", winnerChips + pot } });
            }
        }

        photonView.RPC("AnnounceWinner", RpcTarget.AllBuffered, winner?.NickName ?? "No Winner", highestNum);
    }


    [PunRPC]
    void AnnounceWinner(string winnerName, int winningNumber)
    {
        Debug.Log("Winner: " + winnerName + " with number " + winningNumber);
        if(winnerName.Equals(PhotonNetwork.LocalPlayer.ActorNumber))
            UIManager.Instance.SetWinnerText("You're the Winner "+ "with number " + winningNumber);
        else
            UIManager.Instance.SetWinnerText("The Winner is " + winnerName + " with number " + winningNumber);
        StartCoroutine(RoundCooldown());
    }

    IEnumerator RoundCooldown()
    {
        currentState = GameState.RoundEnd;
        yield return new WaitForSeconds(15f);

        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(StartRound());
        }
    }
}
