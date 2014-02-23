using UnityEngine;
using System.Collections; 
using System;

public class wind : MonoBehaviour {

	System.Random random = new System.Random();
	public GameObject shuttle;
	public int stopTime = 0; 
	public int timer = 0;
	bool applyForce = true;
	public int xValue = 0;
	public int yValue = 0;  // never incremented
	public int zValue = 0;
	bool windy = false;


	// Use this for initialization
	void Start () {
	
		if (windy == false) {
						xValue = random.Next (0, 1000);
						zValue = random.Next (0, 1000);
						stopTime = random.Next (30, 2000);
					 	shuttle.transform.rigidbody.AddForce(xValue, yValue, zValue);
				windy = true;
				}

	}
	
	// Update is called once per frame
	void Update () {
	
		if (windy == true) {
			if (timer >= stopTime){

			}
			timer ++;
		}


	}
}
