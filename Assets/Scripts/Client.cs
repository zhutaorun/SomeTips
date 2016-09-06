using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Client:MonoBehaviour
{
    private static Client s_Instance;
    public static Client ins 
    {
        get
        {
            if (s_Instance == null)
            {
                s_Instance = new Client();
            }
            return s_Instance; 
        }
    }

    private void Awake()
    {
        s_Instance = this;
        
    }
    private void Start()
    {
        StartCoroutine(XMLConfig.loadAll());
        Debug.Log("PetMaxLevel" + GameConfig.ins.PetMaxLevel);
    }
}
