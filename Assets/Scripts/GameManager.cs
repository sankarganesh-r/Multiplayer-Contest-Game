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
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            int randomNum = UnityEngine.Random.Range(1, 101);
            Hashtable props = new Hashtable { { "Number", randomNum }, { "Decision", "Undecided" } };
            player.SetCustomProperties(props);
        }
        photonView.RPC("SyncChipsUI", RpcTarget.AllBuffered);
        yield return new WaitForSeconds(1f);
        photonView.RPC("BeginDecisionPhase", RpcTarget.AllBuffered, PhotonNetwork.Time);
    }


    [PunRPC]
    void BeginDecisionPhase(double networkTime)
    {
        currentState = GameState.DecisionPhase;
        roundStartTime = (float)networkTime;


        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("Number", out object num))
            UIManager.Instance.SetNumber((int)num);

        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("Chips", out object chips))
            UIManager.Instance.SetChipCount((int)chips);


        UIManager.Instance.decisionShowText.text = "Make your choice!";

        UIManager.Instance.StartRound();
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
        Debug.Log("Submit Decision");
        Hashtable props = new Hashtable { { "Decision", decision } };
        Debug.Log("Decision " + decision);

        if (decision == "Contest")
        {
            if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("Chips", out object chipObj))
            {
                int chips = (int)chipObj;
                int newChips = Mathf.Max(0, chips - betAmount);
                props["Chips"] = newChips;
                UIManager.Instance.SetChipCount(newChips);
            }
        }

        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        PhotonNetwork.LocalPlayer.CustomProperties["Decision"] = decision;
    }

    void DetermineWinner()
    {
        currentState = GameState.RevealWinner;
        List<Player> contenders = new List<Player>();
        int highestNum = -1;
        Player winner = null;

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (player.CustomProperties.TryGetValue("Decision", out object decisionObj))
            {
                Debug.Log("Final Decision " + player.NickName + " " + decisionObj);
                if (decisionObj.ToString() == "Contest")
                {
                    if (player.CustomProperties.TryGetValue("Number", out object numberObj))
                    {
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
        }

        if (winner != null && winner.CustomProperties.TryGetValue("Chips", out object winChipsObj))
        {
            int winnerChips = (int)winChipsObj;
            int pot = betAmount * contenders.Count;
            winner.SetCustomProperties(new Hashtable { { "Chips", winnerChips + pot } });
        }

        photonView.RPC("SyncChipsUI", RpcTarget.AllBuffered);
        photonView.RPC("AnnounceWinner", RpcTarget.AllBuffered, winner?.NickName ?? "No Winner", highestNum);
    }

    [PunRPC]
    void SyncChipsUI()
    {
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("Chips", out object chipObj))
        {
            UIManager.Instance.SetChipCount((int)chipObj);
        }
    }

    [PunRPC]
    void AnnounceWinner(string winnerName, int winningNumber)
    {
        Debug.Log("Winner: " + winnerName + " with number " + winningNumber);
        if (winningNumber > 0)
            UIManager.Instance.SetWinnerText(winnerName == PhotonNetwork.LocalPlayer.NickName ?
                $"You're the Winner with number {winningNumber}" :
                $"The Winner is {winnerName} with number {winningNumber}");
        else
            UIManager.Instance.SetWinnerText("Balanced Round");
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
