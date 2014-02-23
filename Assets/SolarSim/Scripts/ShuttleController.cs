using UnityEngine;
using System.Collections;
using Leap;

public class ShuttleController : MonoBehaviour 
{
	//Game Objects for accessing the fuel storage objects
	public GameObject fuelLeft;
	public GameObject fuelRight;
	public GameObject fuelMain;
	private bool stageOneReleased;
	private bool stageTwoReleased;

	//Fuel
	[Range(0, 100)]
	public float fuelAmt = 100f;
	public float fuelDrain = 0.001f;

	//The value of force to apply to thruster
	public float thrustPower = 20.0f;

	//The Leap Motion controller object
	private Leap.Controller leapController;

	//Flags for determining what area of atmosphere shuttle is in
	private bool inTropo;
	private bool inStrat;
	private bool inMeso;
	private bool inTherm;
	private bool inOrbit;

	//Countdown Timer
	private float countDownTimer;
	private bool countingDown;
	private int timeLeft;

	//GUI
	GUIStyle largeFont = new GUIStyle();

	//Use this for initialization
	void Start () 
	{
		largeFont.fontSize = 50;
		largeFont.alignment = TextAnchor.MiddleCenter;
		largeFont.normal.textColor = Color.white;

		//Get the leap motion controller
		leapController = new Controller();
		if (leapController != null)
		{
			Debug.Log ("Leap On!");
		}
		else
		{
			Debug.Log ("Leap Off :(");
		}

		//Start the countdown timer
		countDownTimer = Time.time + 10;
		countingDown = true;

		//Keep the fuel packets on
		stageOneReleased = false;
		stageTwoReleased = false;

		//The current atmosphere level
		inTropo = true;
		inStrat = false;
		inMeso = false;
		inTherm = false;
		inOrbit = false;
	}

	//Get the left hand, not 100% sure how it works yet
	Hand GetLeftMostHand(Frame f)
	{
		float smallestVal = float.MaxValue;
		Hand h = null;
		for (int i = 0; i < f.Hands.Count; ++i)
		{
			if (f.Hands[i].PalmPosition.ToUnity().x < smallestVal)
			{
				smallestVal = f.Hands[i].PalmPosition.ToUnity().x;
				h = f.Hands[i];
			}
		}
		return h;
	}

	//Get the right hand, not 100% sure how it works yet
	Hand GetRightMostHand(Frame f)
	{
		float largestVal = -float.MaxValue;
		Hand h = null;
		for (int i = 0; i < f.Hands.Count; ++i)
		{
			if (f.Hands[i].PalmPosition.ToUnity().x > largestVal)
			{
				largestVal = f.Hands[i].PalmPosition.ToUnity().x;
				h = f.Hands[i];
			}
		}
		return h;
	}

	//Returns the magnitude of distance the shuttle is from origin
	float OriginDisplacement()
	{
		return Mathf.Sqrt((transform.position.x * transform.position.x) + (transform.position.z * transform.position.z));
	}

	void ManageFuel()
	{
		//Game Over
		if (fuelAmt <= 0.0f)
		{
			//Game Over, fall to die
		}
		//Else manage fuel
		else
		{
			//Check displacement from origin
			float displacement = OriginDisplacement();

			//Subtract used fuel
			if (displacement < 10)
			{
				fuelAmt -= fuelDrain;
				Debug.Log("Regular Drain");
			}
			else
			{
				fuelAmt -= 2*fuelDrain;
				Debug.Log ("Big Drain");
			}

			//Check if time to remove module 1
			if (!stageOneReleased && transform.position.y > 50000)
			{
				fuelLeft.transform.parent = null;
				fuelRight.transform.parent = null;
				stageOneReleased = true;
			}
			if (!stageTwoReleased && transform.position.y > 75000)
			{
				fuelMain.transform.parent = null;
				stageTwoReleased = true;
			}
		}
	}


	//Checks the current y position relative to the ground and determines which layer of atmosphere the shuttle is in
	void AtmosphereCheck()
	{
		//Troposphere check
		if (transform.position.y <= 3425)
		{
			inTropo = true;
			inStrat = false;
			inMeso = false;
			inTherm = false;
			inOrbit = false;
		}

		//Stratosphere check
		if (transform.position.y > 3425 && transform.position.y <= 14285)
		{
			inTropo = false;
			inStrat = true;
			inMeso = false;
			inTherm = false;
			inOrbit = false;
		}

		//Mesosphere check
		if (transform.position.y > 14285 && transform.position.y <= 22857)
		{
			inTropo = false;
			inStrat = false;
			inMeso = true;
			inTherm = false;
			inOrbit = false;
		}

		//Thermosphere check
		if (transform.position.y > 22857 && transform.position.y < 75000)
		{
			inTropo = false;
			inStrat = false;
			inMeso = false;
			inTherm = true;
			inOrbit = false;
		}

		//Orbit check
		if (transform.position.y >= 75000)
		{
			inTropo = false;
			inStrat = false;
			inMeso = false;
			inTherm = true;
			inOrbit = true;
		}
	}

	//Physics frame updates
	void FixedUpdate()
	{
		//If counting down before player can play
		if (countingDown)
		{
			timeLeft = (int)(countDownTimer - Time.time);
			if (timeLeft <= 0)
				countingDown = false;
			transform.rigidbody.useGravity = false;

		}
		//Else, game on!
		else
		{
			transform.rigidbody.useGravity = true;

			//Update where the shuttle is relative to ground
			AtmosphereCheck();
			
			//If shuttle has reached orbit threshold, end the level
			if (inOrbit)
			{
				//End level
			}
			
			ManageFuel();
			
			//Get the frame info from the leap motion controller
			Frame frame = leapController.Frame();
			
			//If there are 2 hands update leap logic
			if (frame.Hands.Count >= 2)
			{
				//Assign the hands to variables
				Hand leftHand = GetLeftMostHand(frame);
				Hand rightHand = GetRightMostHand(frame);
				
				//Takes the average forward vector of palms, used for x rotation
				Vector3 avgPalmForward = (frame.Hands[0].Direction.ToUnity() + frame.Hands[1].Direction.ToUnity()) * 0.5f;
				
				//Gets the Vector difference between the palm positions
				Vector3 handDiff = leftHand.PalmPosition.ToUnityScaled() - rightHand.PalmPosition.ToUnityScaled();
				
				//Get the current shuttle rotation, then applies hand difference (y) to shuttle's z rotation
				Vector3 newRot = transform.localRotation.eulerAngles;
				newRot.z = -handDiff.y * 20.0f;
				
				// adding the rot.z as a way to use banking (rolling) to turn.
				newRot.y += handDiff.z * 3.0f - newRot.z * 0.03f * transform.rigidbody.velocity.magnitude;
				newRot.x = -(avgPalmForward.y - 0.1f) * 100.0f;
				
				// if closed fist, then stop the plane and slowly go backwards.
				if (frame.Fingers.Count < 3)
				{
					//thrustPower = -3.0f;
				}
				
				transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.Euler(newRot), 0.1f);
				//transform.rigidbody.velocity = transform.up * thrustPower;
				transform.rigidbody.AddForce(transform.up * thrustPower, ForceMode.Force);
			}
			//Else Fall to deaths
			else
			{
				
			}
		}
	}

	void OnGUI()
	{
		if (inTropo)
			GUI.Label(new Rect(10, 10, 100, 50), "Current Level: Troposphere");
		if (inStrat)
			GUI.Label(new Rect(10, 10, 100, 50), "Current Level: Stratosphere");
		if (inMeso)
			GUI.Label(new Rect(10, 10, 100, 50), "Current Level: Mesosphere");
		if (inTherm)
			GUI.Label(new Rect(10, 10, 100, 50), "Current Level: Thermosphere");

		if (timeLeft > 0)
			GUI.Label(new Rect(550, 400, 0, 0), "" + timeLeft, largeFont);

		GUI.Label(new Rect(10, 50, 100, 50), "Speed: " + transform.rigidbody.velocity.magnitude);
		GUI.Label (new Rect(10, 70, 100, 50), "Height: " + transform.position.y);
		GUI.Label(new Rect(10, 90, 100, 50), "Fuel: " + fuelAmt);
	}
}