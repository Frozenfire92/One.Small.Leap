using UnityEngine;
using System.Collections;
using System;

public class windController : MonoBehaviour {

	System.Random random = new System.Random();
	public int chanceWind = 0;
	//public GameObject wind;
	public int time = 0;
	public GameObject shuttle;
	int stopTime = 0;
	int xValue = 0;
	int yValue = 0;  // never incremented
	int zValue = 0;
	
	
	// Use this for initialization
	void Start () {

	}
	
	// Update is called once per frame
	void Update () {
		stopTime = random.Next (1, 7);
		xValue = random.Next (0, 1000);
		zValue = random.Next (0, 1000);

		chanceWind = random.Next (0, 100);
		if (chanceWind > 66) {
			StartCoroutine("wind");
		}

	}

	IEnumerator wind () {
		 

		bool windy = false;


			shuttle.transform.rigidbody.AddForce(xValue, yValue, zValue);
			/* if (time >= stopTime) {
			 	
			} */

		yield return;

	}
}
