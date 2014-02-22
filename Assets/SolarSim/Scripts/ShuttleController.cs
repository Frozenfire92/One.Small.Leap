using UnityEngine;
using System.Collections;
using Leap;

public class ShuttleController : MonoBehaviour 
{
	//The Leap Motion controller object
	Controller leapController;

	//Game Objects for accessing the fuel storage
	private GameObject fuelLeft;
	private GameObject fuelRight;
	private GameObject fuelMain;

	private bool inTropo;
	private bool inStrat;
	private bool inMeso;
	private bool inTherm;

	// Use this for initialization
	void Start () 
	{
		leapController = new Controller();
	}
	
	// Update is called once per frame
	void Update () 
	{
		Frame frame = leapController.Frame();
	}
}
