using UnityEngine;
using System.Collections; 
using System.Random;

public class wind : MonoBehaviour {

	System.Random random = new Random();
	public GameObject shuttle;
	public int stopTime = 0; 
	public int timer = 0;
	bool applyForce = true;
	public int xValue = 0;
	public int yValue = 0;  // never incremented
	public int zValue = 0;


	// Use this for initialization
	void Start () {
	
		xValue = random.Next (0, 100);
		zValue = random.Next (0, 100);
		stopTime = random.Next (30, 2000);
		shuttle.transform.rigidbody.AddForce

		timer ++;
	}
	
	// Update is called once per frame
	void Update () {
	

		timer ++;
	}
}
