using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThrerDUImanger : MonoBehaviour {

    private Camera _camera;
	// Use this for initialization
	void Start () {
        _camera = GetComponent<Camera>();
        _camera.depth = 200;
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
