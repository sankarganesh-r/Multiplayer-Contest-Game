using System.Collections;
using System.Collections.Generic;
using UnityEngine;


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

public class GameManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
