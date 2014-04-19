/* 
 * This script has been adapted from Leap Motion's Fly.cs.  
 * It controls our Shuttle as it attempts to break orbit.
 * It uses leap motion to control the shuttle's direction as thrust is
 * a default action while there is fuel remaining and hands are detected. 
 * A countdown will occur to allow the user to position their hands over the sensor. 
 * It will drop the fuel modules when the shuttle has surpassed certain y coordinates. 
 * Once a height of 75000 unity units is reached the shuttle will be considered to be in orbit
 * and the level will end. The fuel will be removed based on how far you are from
 * the origin or launch point.
 * 
 * Controls:
 * Two Hands flat over the Leap Motion Controller, thumbs in seems to work best
 * Left Down / Right Up - turn to the left
 * Right Down / Left Up - turn to the right
 * Tilt both hands forward / backwards - forward/backwards tilt
 * One hand towards sceen the other towards player - twist the shuttle
 * 
 * Made with <3 during McHacks 2014
 * Joel Kuntz, Ray Higgins, Jon Maclellan
 */

using UnityEngine;
using System.Collections;
using Leap;

public class ShuttleController : MonoBehaviour 
{
	//The Leap Motion controller object
	private Leap.Controller leapController;

	//Explosion prefab
	public GameObject explosionPrefab;
	public bool explosionHeightReached;

	//Camera Reference
	public GameObject camera;

	//Thruster Reference
	public ParticleSystem leftThruster;
	public ParticleSystem rightThruster;
	public ParticleSystem mainThruster;
	public float thrustPower = 20.0f;
	public Transform thrustPos;

	//Fuel
	public GameObject fuelLeft;
	public GameObject fuelRight;
	public GameObject fuelMain;
	private bool stageOneReleased;
	private bool stageTwoReleased;	
	[Range(0, 300)]
	public float fuelAmt = 100f;
	public float fuelDrain = 0.001f;
	private bool outOfFuel;
	private int outOfFuelCount;

	//Current Atmosphere Layer
	private bool inTropo;
	private bool inStrat;
	private bool inMeso;
	private bool inTherm;
	private bool inOrbit;

	//Countdown Timer
	public float countDownTime;
	private float countDownTimer;
	private bool countingDown;
	private int timeLeft;

	//GUI
	GUIStyle largeFont = new GUIStyle();
	GUIStyle mediumFont = new GUIStyle();

	//Object/script initialization
	void Start () 
	{
		//Countdown Text
		largeFont.fontSize = 50;
		largeFont.alignment = TextAnchor.MiddleCenter;
		largeFont.normal.textColor = Color.red;

		//Stats Text
		mediumFont.fontSize = 30;
		mediumFont.alignment = TextAnchor.UpperLeft;
		mediumFont.normal.textColor = Color.white;

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
		countDownTimer = Time.time + countDownTime;
		countingDown = true;

		//Keep the fuel packets on
		stageOneReleased = false;
		stageTwoReleased = false;
		outOfFuel = false;
		outOfFuelCount = 0;

		//The current atmosphere level
		inTropo = true;
		inStrat = false;
		inMeso = false;
		inTherm = false;
		inOrbit = false;
	}

	//Updates game logic based on frame changes
	void Update()
	{
		if (Input.GetButtonDown("Restart")) Application.LoadLevel(Application.loadedLevel);
	}

	//Updates the game logic based on discrete physics updates
	void FixedUpdate()
	{
		//If counting down before player can play, disable movement
		if (countingDown)
		{
			timeLeft = (int)(countDownTimer - Time.time);
			if (timeLeft <= 0)
				countingDown = false;
			transform.rigidbody.useGravity = false;
		}
		//if out of fuel
		else if (outOfFuel)
		{
			//Add a downward force for five frames to help speed up the slow down process
			if (outOfFuelCount < 5)
			{
				transform.rigidbody.AddForce(-transform.up * thrustPower, ForceMode.Force);
				outOfFuelCount++;
			}
		}
		//Else, game on!
		else
		{
			//Turn gravity on
			transform.rigidbody.useGravity = true;

			//Update where the shuttle is relative to ground
			AtmosphereCheck();
			
			//If shuttle has reached orbit threshold, end the level
			if (inOrbit)
			{
				//Application.loadedLevel(0);
			}

			//Manage fuel use
			ManageFuel();
			
			//Get the frame info from the leap motion controller
			Frame frame = leapController.Frame();
			
			//If there are 2 hands in the current Leap Frame update leap logic
			if (frame.Hands.Count >= 2)
			{
				//Turn on thrusters
				leftThruster.Play();
				rightThruster.Play();
				mainThruster.Play();

				//Assign the hands to variables
				Hand leftHand = GetLeftMostHand(frame);
				Hand rightHand = GetRightMostHand(frame);
				
				//Takes the average forward tilt of palms, used for x rotation
				Vector3 avgPalmForward = (frame.Hands[0].Direction.ToUnity() + frame.Hands[1].Direction.ToUnity()) * 0.5f;
				
				//Gets the Vector difference between the palm positions
				Vector3 handDiff = leftHand.PalmPosition.ToUnityScaled() - rightHand.PalmPosition.ToUnityScaled();
				
				//Get the current shuttle rotation, then apply the hand's height difference to rotate about the shuttle's z axis
				Vector3 newRot = transform.localRotation.eulerAngles;
				//newRot.z = -handDiff.y * 10.0f;
				newRot.z = -handDiff.y * 20.0f;
				
				//Uses the difference between the hands z(depth) and the z rotation to rotate the shuttle about the y axis
				//newRot.y += handDiff.z * 1.5f - newRot.z * 0.09f * transform.rigidbody.velocity.magnitude;
				newRot.y += handDiff.z * 3.0f - newRot.z * 0.03f * transform.rigidbody.velocity.magnitude;

				//Uses the palm's tilt to rotate the shuttle about the x axis
				//newRot.x = -(avgPalmForward.y - 0.1f) * 100.0f;
				newRot.x = -(avgPalmForward.y - 0.1f) * 100.0f;
				
				//Apply the rotation & force to the transform
				transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.Euler(newRot), 0.1f);
//				transform.rigidbody.AddForce(transform.up * thrustPower, ForceMode.Force);
				transform.rigidbody.AddForceAtPosition(transform.up * thrustPower, thrustPos.position, ForceMode.Force);
				//transform.rigidbody.velocity = transform.up * thrustPower;
			}
			//Else Fall to deaths
			else
			{
				//Turn off thrusters
				leftThruster.Stop();
				rightThruster.Stop();
				mainThruster.Stop();
			}
		}
	}

	//If the shuttle collides with the ground collider and has reached a grace height, explode, detach the camera and destroy the shuttle
	void OnTriggerEnter(Collider other)
	{
		if (other.tag == "Ground" && explosionHeightReached)
		{
			Instantiate(explosionPrefab, transform.position, transform.rotation);
			camera.transform.parent = null;
			Destroy (this);
		}
	}

	//Get the left hand from Leap. Uses the palm's x position to determine which is smallest and therefore the left hand
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

	//Get the right hand from Leap. Uses the palm's x position to determine which is largest and therefore the right hand
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

	//Manages Fuel & related operations
	void ManageFuel()
	{
		//Game Over
		if (fuelAmt <= 0.0f)
		{
			if (fuelAmt < 0.0f) fuelAmt = 0.0f;
			outOfFuel = true;
			leftThruster.Stop();
			rightThruster.Stop();
			mainThruster.Stop();
		}
		//Else manage fuel
		else
		{
			//Check displacement from origin
			float displacement = OriginDisplacement();

			//Subtract used fuel
			if (displacement < 30)
			{
				fuelAmt -= fuelDrain;
				Debug.Log("Minimal Fuel Usage");
			}
			else if (displacement >= 30 && displacement < 60)
			{
				fuelAmt -= 2*fuelDrain;
				Debug.Log("Medium Fuel Usage");
			}
			else
			{
				fuelAmt -= 3*fuelDrain;
				Debug.Log ("Large Fuel Usage");
			}

			//Check if time to remove sub fuel modules
			if (!stageOneReleased && transform.position.y > 40000)
			{
				fuelLeft.transform.parent = null;
				fuelRight.transform.parent = null;
				leftThruster.Stop();
				rightThruster.Stop();
				stageOneReleased = true;
			}
			//Check if time to remove main fuel module
			if (!stageTwoReleased && transform.position.y > 60000)
			{
				fuelMain.transform.parent = null;
				mainThruster.Stop();
				stageTwoReleased = true;
			}
		}
	}

	//Checks the current y position relative to the ground and determines which layer of atmosphere the shuttle is in
	void AtmosphereCheck()
	{
		//Explosion check
		if (transform.position.y > 50 && !explosionHeightReached)
		{
			explosionHeightReached = true;
		}

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

	//Draws GUI
	void OnGUI()
	{
		if (inTropo)
			GUI.Label(new Rect(10, 10, 200, 100), "Current Level: Troposphere", mediumFont);
		if (inStrat)
			GUI.Label(new Rect(10, 10, 200, 100), "Current Level: Stratosphere", mediumFont);
		if (inMeso)
			GUI.Label(new Rect(10, 10, 200, 100), "Current Level: Mesosphere", mediumFont);
		if (inTherm)
			GUI.Label(new Rect(10, 10, 200, 100), "Current Level: Thermosphere", mediumFont);

		if (timeLeft > 0)
			GUI.Label(new Rect(620, 400, 0, 0), "" + timeLeft, largeFont);

		if (!inOrbit && outOfFuel)
			GUI.Label(new Rect(UnityEngine.Screen.width / 2, UnityEngine.Screen.height/2, 100, 30), "Out of Fuel!\nPress R to retry!", largeFont);

		if (inOrbit)
			GUI.Label(new Rect(595, 400, 100, 30), "Orbit Reached!", largeFont);

		GUI.Label(new Rect(10, 50, 200, 100), "Speed: " + (int)transform.rigidbody.velocity.magnitude, mediumFont);
		GUI.Label (new Rect(10, 90, 200, 100), "Height: " + (int)transform.position.y, mediumFont);
		GUI.Label(new Rect(10, 130, 200, 100), "Fuel: " + (int)fuelAmt, mediumFont);
	}
}