using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using Leap;

public class LeapManager : MonoBehaviour 
{
	// minimum frames to skip between gesture recognitions and other discrete leap actions
	public int FramesToSkip = 10;
	
	// false if the user is right-handed, true if the user is left-handed
	public bool LeftHandedUser = false;

	// List of extra gestures to detect
	public List<LeapExtraGestures.ExtraGestures> ExtraGestures;
	
	// Minimum time between gesture detections
	public float MinTimeBetweenGestures = 1f;
	
	// the debug camera, if available, tracking the fingers and hands
	public Camera DebugCamera = null;
	
	// the leap sensor prefab used to display the sensor in debug window
	public GameObject LeapSensorPrefab = null;
	
	// the line renderer prefab used to display fingers and hands in debug window
	public LineRenderer LineFingerPrefab = null;
	
	// true if finger and hand IDs need to be displayed in Debug window, false otherwise
	public bool DisplayLeapIds = false;
	
	// the central position for tracked fingers and hands to be displayed for the debug camera
	public Vector3 DisplayFingerPos = Vector3.zero;
	
	// scale used for displaying fingers and hands
	public float DisplayFingerScale = 5;
	
	// leap controller parameters
	private Leap.Controller leapController = null;
	private Leap.Frame leapFrame = null;
	private Int64 lastFrameID = 0;
	private Int64 leapFrameCounter = 0;
	
	// leap pointable parameters
	private Leap.Pointable leapPointable = null;
	private int leapPointableID = 0;
	private int leapPointableHandID;
	private Vector3 leapPointablePos;
	private Vector3 leapPointableDir;
	//private Quaternion leapPointableQuat;
	
	// leap hand parameters
	private Leap.Hand leapHand = null;
	private int leapHandID = 0;
	private Vector3 leapHandPos;
	private int leapHandFingersCount;
	private int leapHandLFingerId;
	private int leapHandRFingerId;
	
	// fingers count parameters
	private SingleNumberFilter fingersCountFilter = new SingleNumberFilter();
	private int fingersCountHandID;
	private float fingersCountFiltered;
	private int fingersCountPrev = -1;
	private int fingersCountPrevPrev = -1;
	
	// hand pinch parameters
	private bool handGripDetected = false;
	private bool handGripReported = false;
	private int handGripFingersCount;
	private Int64 handGripFrameCounter;
	
	// swipe parameters
	private Vector3 leapSwipeDir;
	private Vector3 leapSwipeSpeed;
	
	// gesture ID
	private int iCircleGestureID;
	private int iSwipeGestureID;
	private int iKeyTapGestureID;
	private int iScreenTapGestureID;
	
	// last gesture frame counter
	private Int64 iCircleFrameCounter;
	private Int64 iSwipeFrameCounter;
	private Int64 iKeyTapFrameCounter;
	private Int64 iScreenTapFrameCounter;
	
	// gesture progress
	private float fCircleProgress;
	private float fSwipeProgress;
	private float fKeyTapProgress;
	private float fScreenTapProgress;
	
	// Bool to keep track of whether LeapMotion has been initialized
	private bool leapInitialized = false;

	// cursor position, texture and touch status
	private GameObject handCursor;
	private Texture selectHandTexture;
	private Texture touchHandTexture;
	private Texture normalHandTexture;
	private Vector3 cursorNormalPos = Vector3.zero;
	private Vector3 cursorScreenPos = Vector3.zero;
	private Pointable.Zone leapPointableZone = Pointable.Zone.ZONENONE;

	// general gesture tracking time start
	private float gestureTrackingAtTime;
	
	// Lists of extra gesture data
	private List<LeapExtraGestures.ExtraGestureData> extraGesturesData = new List<LeapExtraGestures.ExtraGestureData>();
	
	// The single instance of LeapManager
	private static LeapManager instance;

	private GameObject debugText;
	private Dictionary<int, LineRenderer> dictFingerLines = new Dictionary<int, LineRenderer>();
	
	
	// returns the single LeapManager instance
    public static LeapManager Instance
    {
        get
        {
            return instance;
        }
    }
	
	// returns true if Leap-sensor is successfully initialized, false otherwise
	public bool IsLeapInitialized()
	{
		return leapInitialized;
	}
	
	// returns true if the left hand is set as primary hand, false otherwise
	public bool IsLeftHandPrimary()
	{
		return LeftHandedUser;
	}
	
	// returns true if the right hand is set as primary hand, false otherwise
	public bool IsRightHandPrimary()
	{
		return !LeftHandedUser;
	}
	
	// returns the current leap frame counter
	public Int64 GetLeapFrameCounter()
	{
		return leapFrameCounter;
	}
	
	// returns true if there is a valid pointable found, false otherwise
	public bool IsPointableValid()
	{
		return (leapPointable != null) && leapPointable.IsValid;
	}
	
	// returns the tracked leap pointable, or null if no pointable is being tracked
	public Leap.Pointable GetLeapPointable()
	{
		return leapPointable;
	}
	
	// returns the currently tracked pointable ID
	public int GetPointableID()
	{
		if((leapPointable != null) && leapPointable.IsValid)
			return leapPointableID;
		else
			return 0;
	}
	
	// returns the position of the currently tracked pointable
	public Vector3 GetPointablePos()
	{
		if((leapPointable != null) && leapPointable.IsValid)
			return leapPointablePos;
		else
			return Vector3.zero;
	}
	
	// returns the direction of the currently tracked pointable
	public Vector3 GetPointableDir()
	{
		if((leapPointable != null) && leapPointable.IsValid)
			return leapPointableDir;
		else
			return Vector3.zero;
	}
	
	// returns the 3D rotation of the currently tracked pointable
	public Quaternion GetPointableQuat()
	{
		if((leapPointable != null) && leapPointable.IsValid)
			//return leapPointableQuat;
			return Quaternion.LookRotation(leapPointableDir);
		else
			return Quaternion.identity;
	}
	
	// returns true if there is a valid hand found, false otherwise
	public bool IsHandValid()
	{
		return (leapHand != null) && leapHand.IsValid;
	}
	
	// returns tracked leap hand, or null if no hand is being tracked
	public Leap.Hand GetLeapHand()
	{
		return leapHand;
	}
	
	// returns the currently tracked hand ID
	public int GetHandID()
	{
		if((leapHand != null) && leapHand.IsValid)
			return leapHandID;
		else
			return 0;
	}
	
	// returns the position of the currently tracked hand
	public Vector3 GetHandPos()
	{
		if((leapHand != null) && leapHand.IsValid)
			return leapHandPos;
		else
			return Vector3.zero;
	}
	
	// returns the count of fingers of the tracked hand
	public int GetFingersCount()
	{
		//return (int)(fingersCountFiltered + 0.5f);
		return leapHandFingersCount;
	}

	// returns ID of the leftmost/rightmost finger, or 0 if not valid
	public int GetThumbID()
	{
		if((leapHand != null) && leapHand.IsValid)
		{
			if(!LeftHandedUser)
				return leapHand.Fingers.Leftmost.IsValid ? leapHand.Fingers.Leftmost.Id : 0;
			else
				return leapHand.Fingers.Rightmost.IsValid ? leapHand.Fingers.Rightmost.Id : 0;
		}
		
		return 0;
	}
	
	// returns true if hand pinch has been detected, false otherwise
	public bool IsHandPinchDetected()
	{
		if(handGripDetected && !handGripReported)
		{
			handGripReported = true;
			return true;
		}
		
		return false;
	}
	
	// returns true if hand release has been detected, false otherwise
	public bool IsHandReleaseDetected()
	{
		if(!handGripDetected && handGripReported)
		{
			handGripReported = false;
			return true;
		}
		
		return false;
	}
	
	// clears currently detected hand pinch
	public void ClearHandPinch()
	{
		handGripDetected = false;
		handGripReported = false;
	}
	
	// returns true if gesture Circle has been detected, false otherwise
	public bool IsGestureCircleDetected()
	{
		bool bDetected = fCircleProgress >= 1f;
		
		if(bDetected)
		{
			//iCircleGestureID = 0;
			fCircleProgress = 0f;
		}
		
		return bDetected;
	}

	// returns the ID of the last Circle gesture
	public int GetGestureCircleID()
	{
		return iCircleGestureID;
	}
	
	// returns true if gesture Swipe has been detected, false otherwise
	public bool IsGestureSwipeDetected()
	{
		bool bDetected = fSwipeProgress >= 1f;
		
		if(bDetected)
		{
			//iSwipeGestureID = 0;
			fSwipeProgress = 0f;
		}
		
		return bDetected;
	}
	
	// returns the ID of the last Swipe gesture
	public int GetGestureSwipeID()
	{
		return iSwipeGestureID;
	}
	
	// returns the last swipe direction
	public Vector3 GetSwipeDir()
	{
		return leapSwipeDir;
	}
	
	// returns the last swipe speed
	public Vector3 GetSwipeSpeed()
	{
		return leapSwipeDir;
	}
	
	// returns true if gesture Key-tap has been detected, false otherwise
	public bool IsGestureKeytapDetected()
	{
		bool bDetected = fKeyTapProgress >= 1f;
		
		if(bDetected)
		{
			//iKeyTapGestureID = 0;
			fKeyTapProgress = 0f;
		}
		
		return bDetected;
	}
	
	// returns the ID of the last Keytap gesture
	public int GetGestureKeytapID()
	{
		return iKeyTapGestureID;
	}
	
	// returns true if gesture Screen-tap has been detected, false otherwise
	public bool IsGestureScreentapDetected()
	{
		bool bDetected = fScreenTapProgress >= 1f;
		
		if(bDetected)
		{
			//iScreenTapGestureID = 0;
			fScreenTapProgress = 0f;
		}
		
		return bDetected;
	}
	
	// returns the ID of the last Screentap gesture
	public int GetGestureScreentapID()
	{
		return iScreenTapGestureID;
	}
	
	// returns the cursor position in normalized coordinates
	public Vector3 GetCursorNormalizedPos()
	{
		if((leapHand != null) && leapHand.IsValid)
			return cursorNormalPos;
		else
			return Vector3.zero;
	}
	
	// returns the cursor position in screen coordinates
	public Vector3 GetCursorScreenPos()
	{
		if((leapHand != null) && leapHand.IsValid)
			return cursorScreenPos;
		else
			return Vector3.zero;
	}
	
	// returns the touch status of the pointable
	public Pointable.Zone GetPointableTouchStatus()
	{
		if((leapPointable != null) && leapPointable.IsValid)
			return leapPointableZone;
		else
			return Pointable.Zone.ZONENONE;
	}

	// adds a gesture to the list of detected extra gestures
	public void DetectGesture(LeapExtraGestures.ExtraGestures gesture)
	{
		int index = GetGestureIndex(gesture);
		if(index >= 0)
			DeleteGesture(gesture);
		
		LeapExtraGestures.ExtraGestureData gestureData = new LeapExtraGestures.ExtraGestureData();
		
		gestureData.gesture = gesture;
		gestureData.state = 0;
		gestureData.jointId = 0;
		gestureData.progress = 0f;
		gestureData.complete = false;
		gestureData.cancelled = false;
		
		extraGesturesData.Add(gestureData);
	}
	
	// resets the gesture-data state for the given extra gesture
	public bool ResetGesture(LeapExtraGestures.ExtraGestures gesture)
	{
		int index = GetGestureIndex(gesture);
		if(index < 0)
			return false;
		
		LeapExtraGestures.ExtraGestureData gestureData = extraGesturesData[index];
		
		gestureData.state = 0;
		gestureData.jointId = 0;
		gestureData.progress = 0f;
		gestureData.complete = false;
		gestureData.cancelled = false;
		gestureData.startTrackingAtTime = Time.realtimeSinceStartup + LeapExtraGestures.Constants.MinTimeBetweenGestures;

		extraGesturesData[index] = gestureData;
		
		return true;
	}
	
	// deletes the given gesture from the list of detected extra gestures
	public bool DeleteGesture(LeapExtraGestures.ExtraGestures gesture)
	{
		int index = GetGestureIndex(gesture);
		if(index < 0)
			return false;
		
		extraGesturesData.RemoveAt(index);
		return true;
	}
	
	// clears detected extra gestures list
	public void ClearGestures()
	{
		extraGesturesData.Clear();
	}
	
	// returns true, if the given extra gesture is in the list of detected extra gestures
	public bool IsGestureDetected(LeapExtraGestures.ExtraGestures gesture)
	{
		int index = GetGestureIndex(gesture);
		return index >= 0;
	}
	
	// returns true, if the given extra gesture is complete
	public bool IsGestureComplete(LeapExtraGestures.ExtraGestures gesture, bool bResetOnComplete)
	{
		int index = GetGestureIndex(gesture);

		if(index >= 0)
		{
			LeapExtraGestures.ExtraGestureData gestureData = extraGesturesData[index];
			
			if(bResetOnComplete && gestureData.complete)
			{
				ResetGesture(gesture);
				return true;
			}
			
			return gestureData.complete;
		}
		
		return false;
	}
	
	// returns true, if the given extra gesture is cancelled
	public bool IsGestureCancelled(LeapExtraGestures.ExtraGestures gesture)
	{
		int index = GetGestureIndex(gesture);

		if(index >= 0)
		{
			LeapExtraGestures.ExtraGestureData gestureData = extraGesturesData[index];
			return gestureData.cancelled;
		}
		
		return false;
	}
	
	// returns the progress in range [0, 1] of the given extra gesture
	public float GetGestureProgress(LeapExtraGestures.ExtraGestures gesture)
	{
		int index = GetGestureIndex(gesture);

		if(index >= 0)
		{
			LeapExtraGestures.ExtraGestureData gestureData = extraGesturesData[index];
			return gestureData.progress;
		}
		
		return 0f;
	}
	
	// returns the normalized screen position of the given extra gesture
	public Vector3 GetGestureScreenPos(LeapExtraGestures.ExtraGestures gesture)
	{
		int index = GetGestureIndex(gesture);

		if(index >= 0)
		{
			LeapExtraGestures.ExtraGestureData gestureData = extraGesturesData[index];
			return gestureData.screenPos;
		}
		
		return Vector3.zero;
	}
	
	// returns the normalized direction of the given extra gesture
	public Vector3 GetGestureDirection(LeapExtraGestures.ExtraGestures gesture)
	{
		int index = GetGestureIndex(gesture);

		if(index >= 0)
		{
			LeapExtraGestures.ExtraGestureData gestureData = extraGesturesData[index];
			return gestureData.gestureDir.normalized;
		}
		
		return Vector3.zero;
	}
	
	// returns the normalized velocity of the given extra gesture
	public Vector3 GetGestureVelocity(LeapExtraGestures.ExtraGestures gesture)
	{
		int index = GetGestureIndex(gesture);

		if(index >= 0)
		{
			LeapExtraGestures.ExtraGestureData gestureData = extraGesturesData[index];
			
			if(gestureData.completeTime > 0)
			{
				Vector3 gestureVel = gestureData.gestureDir / gestureData.completeTime;
				return gestureVel.normalized;
			}
		}
		
		return Vector3.zero;
	}
	

	//----------------------------------- end of public functions --------------------------------------//
	
	void Awake()
	{
		debugText = GameObject.Find("DebugText");
		handCursor = GameObject.Find("HandCursor");
		
		if(LeapSensorPrefab)
		{
			Instantiate(LeapSensorPrefab, DisplayFingerPos, Quaternion.identity);
		}
		
		// ensure the needed dlls are in place
		if(CheckLibsPresence())
		{
			// reload the same level
			Application.LoadLevel(Application.loadedLevel);
		}
	}

	void Start()
	{
		try 
		{
			leapController = new Leap.Controller();
			
//			if(leapController.Devices.Count == 0)
//				throw new Exception("Please connect the LeapMotion sensor!");

			leapController.EnableGesture(Gesture.GestureType.TYPECIRCLE);
			leapController.EnableGesture(Gesture.GestureType.TYPEKEYTAP);
			leapController.EnableGesture(Gesture.GestureType.TYPESCREENTAP);
			leapController.EnableGesture(Gesture.GestureType.TYPESWIPE);
			
			// add the extra gestures to detect, if any
			foreach(LeapExtraGestures.ExtraGestures gesture in ExtraGestures)
			{
				DetectGesture(gesture);
			}
			
			// load cursor textures once
			normalHandTexture = (Texture)Resources.Load("NormalCursor");
			touchHandTexture = (Texture)Resources.Load("TouchCursor");
			selectHandTexture = (Texture)Resources.Load("SelectCursor");
			
			instance = this;
			leapInitialized = true;
			
			DontDestroyOnLoad(gameObject);
			
			// show the ready-message
			string sMessage = leapController.Devices.Count > 0 ? "Ready." : "Please make sure the Leap-sensor is connected.";
			Debug.Log(sMessage);
			
			if(debugText != null)
				debugText.guiText.text = sMessage;
		}
		catch(System.TypeInitializationException ex)
		{
			Debug.LogError(ex.ToString());
			if(debugText != null)
				debugText.guiText.text = "Please check the LeapMotion installation.";
		}
		catch (System.Exception ex) 
		{
			Debug.LogError(ex.ToString());
			if(debugText != null)
				debugText.guiText.text = ex.Message;
		}
	}
	
	void OnApplicationQuit()
	{
		leapPointable = null;
		leapFrame = null;
		
		if(leapController != null)
		{
			leapController.Dispose();
			leapController = null;
		}
		
		leapInitialized = false;
		instance = null;
	}
	
	void Update() 
	{
		if(leapInitialized && leapController != null)
		{
			Leap.Frame frame = leapController.Frame();
			
			if(frame.IsValid && (frame.Id != lastFrameID))
			{
				leapFrame = frame;
				lastFrameID = leapFrame.Id;
				leapFrameCounter++;
				
				// fix unfinished leap gesture progress
				if(fCircleProgress > 0f && fCircleProgress < 1f)
					fCircleProgress = 0f;
				if(fSwipeProgress > 0f && fSwipeProgress < 1f)
					fSwipeProgress = 0f;
				if(fKeyTapProgress > 0f && fKeyTapProgress < 1f)
					fKeyTapProgress = 0f;
				if(fScreenTapProgress > 0f && fScreenTapProgress < 1f)
					fScreenTapProgress = 0f;
				
				// get a suitable pointable
				leapPointable = leapFrame.Pointable(leapPointableID);
				
				if(!leapPointable.IsValid)
					leapPointable = leapFrame.Pointables.Frontmost;

				Leap.Vector stabilizedPosition = Leap.Vector.Zero;
				Leap.Hand handPrim = leapFrame.Hands.Count > 0 ? leapFrame.Hands[leapFrame.Hands.Count - 1] : null;
				
				if(leapPointable != null && leapPointable.IsValid && 
					leapPointable.Hand != null && leapPointable.Hand.IsValid &&
					handPrim != null && leapPointable.Hand.Id == handPrim.Id)
				{
					leapPointableID = leapPointable.Id;
					leapPointableHandID = leapPointable.Hand != null && leapPointable.Hand.IsValid ? leapPointable.Hand.Id : 0;
					
					leapPointablePos = LeapToUnity(leapPointable.StabilizedTipPosition, true);
					leapPointableDir = LeapToUnity(leapPointable.Direction, false);
					//leapPointableQuat = Quaternion.LookRotation(leapPointableDir);
					
					leapPointableZone = leapPointable.TouchZone;
					//stabilizedPosition = leapPointable.StabilizedTipPosition;
					
					leapHand = leapPointable.Hand;
					leapHandID = leapHand.Id;
				}
				else 
				{
					leapPointableID = 0;
					leapPointable = null;
					
					// get leap hand
					leapHand = leapFrame.Hand(leapHandID);
					if(leapHand == null || !leapHand.IsValid)
					{
						leapHandID = 0;

						if(leapFrame.Hands.Count > 0)
						{
							for(int i = leapFrame.Hands.Count - 1; i >= 0; i--)
							{
								leapHand = leapFrame.Hands[i];
								
								if(leapHand.IsValid /**&& leapHand.Fingers.Count > 0*/)
								{
									leapHandID = leapHand.Id;
									break;
								}
							}
						}
					}
					
				}
					
				if(leapHandID != 0)
				{
					leapHandPos = LeapToUnity(leapHand.StabilizedPalmPosition, true);
					stabilizedPosition = leapHand.StabilizedPalmPosition;
					leapHandFingersCount = leapHand.Fingers.Count;
				}
				
				// estimate the cursor coordinates
				if(stabilizedPosition != Leap.Vector.Zero)
				{
				    Leap.InteractionBox iBox = frame.InteractionBox;
				    Leap.Vector normalizedPosition = iBox.NormalizePoint(stabilizedPosition);
					
				    cursorNormalPos.x = normalizedPosition.x;
				    cursorNormalPos.y = normalizedPosition.y;
					cursorScreenPos.x = cursorNormalPos.x * UnityEngine.Screen.width;
					cursorScreenPos.y = cursorNormalPos.y * UnityEngine.Screen.height;
				}
				
				// do fingers count filter
				if(leapHandID != fingersCountHandID)
				{
					fingersCountHandID = leapHandID;
					fingersCountPrev = -1;
					fingersCountPrevPrev = -1;
					
					handGripDetected = false;
					fingersCountFilter.Reset();
				}
				
				if(leapHandID != 0)
				{
					fingersCountFiltered = leapHandFingersCount;
					fingersCountFilter.UpdateFilter(ref fingersCountFiltered);
					
					if((leapFrameCounter - handGripFrameCounter) >= FramesToSkip)
					{
						handGripFrameCounter = leapFrameCounter;
						int fingersCountNow = (int)(fingersCountFiltered + 0.5f);
						//int fingersCountNow = leapHandFingersCount;
						
						if(fingersCountPrev == fingersCountPrevPrev)
						{
							if(!handGripDetected)
							{
								if(fingersCountNow < fingersCountPrev)
								{
									Finger leftFinger = leapHand.Finger(leapHandLFingerId);
									Finger rightFinger = leapHand.Finger(leapHandRFingerId);
									bool bThumbOff = !LeftHandedUser ? leapHandLFingerId != 0 && (leftFinger == null || !leftFinger.IsValid) :
										leapHandRFingerId != 0 && (rightFinger == null || !rightFinger.IsValid);
									
									if(bThumbOff)
									{
										handGripDetected = true;
										handGripFingersCount = fingersCountPrev;
									}
								}
								else
								{
									leapHandLFingerId = leapHand != null && leapHand.Fingers.Count > 0 ? leapHand.Fingers.Leftmost.Id : 0;
									leapHandRFingerId = leapHand != null && leapHand.Fingers.Count > 0 ? leapHand.Fingers.Rightmost.Id : 0;
								}
							}
							else
							{
								if(fingersCountNow >= fingersCountPrev/**handGripFingersCount*/)
								{
									Finger leftFinger = leapHand.Finger(leapHandLFingerId);
									Finger rightFinger = leapHand.Finger(leapHandRFingerId);
									
									bool bThumbOn = !LeftHandedUser ? (leftFinger != null && leftFinger.IsValid) :
										(rightFinger != null && rightFinger.IsValid);
									
									if(bThumbOn || fingersCountNow >= handGripFingersCount)
									{
										handGripDetected = false;
									}
								}
								else if(leapHand == null || !leapHand.IsValid)
								{
									// stop pinching if the hand is lost
									handGripDetected = false;
								}
							}
						}
						
						fingersCountPrevPrev = fingersCountPrev;
						fingersCountPrev = fingersCountNow;
					}
				}
				
				if(Time.realtimeSinceStartup >= gestureTrackingAtTime)
				{
					GestureList gestures = frame.Gestures ();
					for (int i = 0; i < gestures.Count; i++) 
					{
						Gesture gesture = gestures[i];
						
						switch (gesture.Type) 
						{
							case Gesture.GestureType.TYPECIRCLE:
								CircleGesture circle = new CircleGesture(gesture);
							
								if((leapFrameCounter - iCircleFrameCounter) >= FramesToSkip &&
									iCircleGestureID != circle.Id && 
									circle.State == Gesture.GestureState.STATESTOP)
								{
									iCircleFrameCounter = leapFrameCounter;
									iCircleGestureID = circle.Id;
									fCircleProgress = 1f;
								
									gestureTrackingAtTime = Time.realtimeSinceStartup + MinTimeBetweenGestures;
								}
								else if(circle.Progress < 1f)
								{
									fCircleProgress = circle.Progress;
								}
								break;
							
							case Gesture.GestureType.TYPESWIPE:
								SwipeGesture swipe = new SwipeGesture(gesture);
							
								if((leapFrameCounter - iSwipeFrameCounter) >= FramesToSkip &&
									iSwipeGestureID != swipe.Id &&
									swipe.State == Gesture.GestureState.STATESTOP)
								{
									iSwipeFrameCounter = leapFrameCounter;
									iSwipeGestureID = swipe.Id;
									fSwipeProgress = 1f;  // swipe.Progress
								
									gestureTrackingAtTime = Time.realtimeSinceStartup + MinTimeBetweenGestures;
								
									leapSwipeDir = LeapToUnity(swipe.Direction, false);
									leapSwipeSpeed = LeapToUnity(swipe.Position - swipe.StartPosition, true);
								
									if(swipe.DurationSeconds != 0)
										leapSwipeSpeed /= swipe.DurationSeconds;
									else
										leapSwipeSpeed = Vector3.zero;
								}
								else if(swipe.State != Gesture.GestureState.STATESTOP)
								{
									fSwipeProgress = 0.5f;
								}
								break;
							
							case Gesture.GestureType.TYPEKEYTAP:
								KeyTapGesture keytap = new KeyTapGesture (gesture);
							
								if((leapFrameCounter - iKeyTapFrameCounter) >= FramesToSkip &&
									iKeyTapGestureID != keytap.Id && 
									keytap.State == Gesture.GestureState.STATESTOP)
								{
									iKeyTapFrameCounter = leapFrameCounter;
									iKeyTapGestureID = keytap.Id;
									fKeyTapProgress = 1f;
								
									gestureTrackingAtTime = Time.realtimeSinceStartup + MinTimeBetweenGestures;
								}
								else if(keytap.Progress < 1f)
								{
									fKeyTapProgress = keytap.Progress;
								}
								break;
							
							case Gesture.GestureType.TYPESCREENTAP:
								ScreenTapGesture screentap = new ScreenTapGesture (gesture);
							
								if((leapFrameCounter - iScreenTapFrameCounter) >= FramesToSkip &&
									iScreenTapGestureID != screentap.Id && 
									screentap.State == Gesture.GestureState.STATESTOP)
								{
									iScreenTapFrameCounter = leapFrameCounter;
									iScreenTapGestureID = screentap.Id;
									fScreenTapProgress = 1f;
								
									gestureTrackingAtTime = Time.realtimeSinceStartup + MinTimeBetweenGestures;
								}
								else if(screentap.Progress < 1f)
								{
									fScreenTapProgress = screentap.Progress;
								}
								break;
							
							default:
								Debug.LogError("Unknown gesture type.");
								break;
						}
					}

					// check for extra gestures
					int listGestureSize = extraGesturesData.Count;
					float timestampNow = Time.realtimeSinceStartup;
					
					for(int g = 0; g < listGestureSize; g++)
					{
						LeapExtraGestures.ExtraGestureData gestureData = extraGesturesData[g];
						
						if(timestampNow >= gestureData.startTrackingAtTime)
						{
							LeapExtraGestures.CheckForGesture(ref gestureData, Time.realtimeSinceStartup, this);
							extraGesturesData[g] = gestureData;
							
							if(gestureData.complete)
							{
								gestureTrackingAtTime = Time.realtimeSinceStartup + MinTimeBetweenGestures;
							}
						}
					}
				}
				
				if(DebugCamera)
				{
					DoDisplayFingers();
				}
			}
		}
	}
	
	
	void OnGUI()
	{	
		// display the cursor status and position
		if(handCursor != null)
		{
			Texture texture = null;
			
//			if((leapPointable != null) && leapPointable.IsValid)
//			{
//				switch(leapPointableZone)
//				{
//					case Pointable.Zone.ZONENONE:
//						texture = normalHandTexture;
//						break;
//					
//					case Pointable.Zone.ZONEHOVERING:
//						texture = touchHandTexture;
//						break;
//					
//					case Pointable.Zone.ZONETOUCHING:
//						texture = selectHandTexture;
//						break;
//				}
//			}
			
			if(handGripDetected)
			{
				texture = selectHandTexture;
			}
			
			if(texture == null)
			{
				texture = normalHandTexture;
			}
			
			handCursor.guiTexture.texture = texture;
			handCursor.transform.position = Vector3.Lerp(handCursor.transform.position, cursorNormalPos, 3 * Time.deltaTime);
		}
		
		if(leapFrame != null && DebugCamera)
		{
			string sDebug = String.Empty;
			Rect rectDebugCamera = DebugCamera.pixelRect;
			Rect rectDebugFinger = new Rect(0, 0, 100, 100);
			
			if(DisplayLeapIds)
			{
				// show finger Ids
				foreach(Pointable finger in leapFrame.Pointables)
				{
					if(finger.IsValid)
					{
						if(finger.Id == leapPointableID)
							sDebug = "<b>" + finger.Id.ToString() + "</b>";
						else if((finger.Id == leapHandLFingerId) || (finger.Id == leapHandRFingerId))
							sDebug = "<i>" + finger.Id.ToString() + "</i>";
						else
							sDebug = finger.Id.ToString();
						
						Vector3 vFingerPos = LeapToUnity(finger.StabilizedTipPosition, true) * DisplayFingerScale + DisplayFingerPos;
						Vector3 vScreenPos = DebugCamera.WorldToScreenPoint(vFingerPos);
						//vScreenPos.y = UnityEngine.Screen.height - vScreenPos.y;
						
						rectDebugFinger.x = vScreenPos.x;
						rectDebugFinger.y = UnityEngine.Screen.height - vScreenPos.y; //vScreenPos.y;
						
						if(rectDebugCamera.Contains(vScreenPos))
							GUI.Label(rectDebugFinger, sDebug);
					}
				}
				
				// show hand Ids
				int handPrimId = leapFrame.Hands.Count > 0 ? leapFrame.Hands[leapFrame.Hands.Count - 1].Id : 0;
				
				foreach(Hand hand in leapFrame.Hands)
				{
					if(hand.IsValid)
					{
						if(hand.Id == handPrimId)
							sDebug = "<b>" + hand.Id.ToString() + "</b>";
						else
							sDebug = hand.Id.ToString();
						
						Leap.Vector handBase = hand.StabilizedPalmPosition + (-hand.Direction * 100);
						Vector3 vHandPos = LeapToUnity(handBase, true) * DisplayFingerScale + DisplayFingerPos;
						Vector3 vScreenPos = DebugCamera.WorldToScreenPoint(vHandPos);
						//vScreenPos.y = UnityEngine.Screen.height - vScreenPos.y;
						
						rectDebugFinger.x = vScreenPos.x;
						rectDebugFinger.y = UnityEngine.Screen.height - vScreenPos.y; //vScreenPos.y;
						
						if(rectDebugCamera.Contains(vScreenPos))
							GUI.Label(rectDebugFinger, sDebug);
					}
				}
			}
			
//			// show finger Ids
//			List<int> alFingerIds = new List<int>(dictFingerLines.Keys);
//			alFingerIds.Sort();
//			
//			foreach(int fingerId in alFingerIds)
//			{
//				if(fingerId == leapPointableID)
//					sDebug += "<b>" + fingerId.ToString() + "</b> ";
//				else
//					sDebug += fingerId.ToString() + " ";
//			}
			
			Rect rectCamera = DebugCamera.pixelRect;
			rectCamera.x += 10;
			rectCamera.y = UnityEngine.Screen.height - rectCamera.height + 10;
			
//			GUI.Label(rectCamera, sDebug);
//			alFingerIds.Clear();
			
			// show pointable and hand info
//			rectCamera.y += 20;
			if(leapPointableID != 0)
			{
				sDebug = "Pointable " + leapPointableID + "/" + leapPointableHandID + ": " + leapPointablePos.ToString();
				GUI.Label(rectCamera, sDebug);
			}

			rectCamera.y += 20;
			if(leapHandID != 0)
			{
				sDebug = "Hand " + leapHandID + "/" + leapHandFingersCount + ": " + leapHandPos.ToString();
				GUI.Label(rectCamera, sDebug);
			}
			
			// show cursor coordinates
//			rectCamera.y += 20;
//			sDebug = "Cursor: " + cursorNormalPos;
//			GUI.Label(rectCamera, sDebug);

			// show touch and pinch status
//			rectCamera.y += 20;
//			sDebug = "Touch: " + leapPointableZone;
//			GUI.Label(rectCamera, sDebug);

			rectCamera.y += 20;
			if(leapFrameCounter != 0)
			{
				sDebug = "Pinch " + leapHandLFingerId + "/" + leapHandRFingerId + ": " + handGripDetected;
				GUI.Label(rectCamera, sDebug);
			}

//			// show gestures in progress
//			sDebug = "";
//			if(fCircleProgress > 0f)
//				sDebug += "Circling... ";
//			if(fSwipeProgress > 0f)
//				sDebug += "Swiping... ";
//			if(fKeyTapProgress > 0f)
//				sDebug += "KeyTapping... ";
//			if(fScreenTapProgress > 0f)
//				sDebug += "ScreenTapping... ";
//
			
			// show completed extra gestures
			int listGestureSize = extraGesturesData.Count;
			float timestampNow = Time.realtimeSinceStartup;
			sDebug = String.Empty;
			
			for(int g = 0; g < listGestureSize; g++)
			{
				LeapExtraGestures.ExtraGestureData gestureData = extraGesturesData[g];
				
				if(gestureData.complete || gestureData.startTrackingAtTime > timestampNow)
				{
					sDebug += gestureData.gesture.ToString() + " ";
				}
			}
			
			if(sDebug != String.Empty)
				sDebug = "Detected: " + sDebug;
		
			rectCamera.y += 20;
			GUI.Label(rectCamera, sDebug);
		}
	}
	
	
	private void DoDisplayFingers()
	{
		if(leapFrame == null || !leapFrame.IsValid || !LineFingerPrefab) 
			return;
		
		List<int> alFingerIds = new List<int>();
		
		foreach(Pointable finger in leapFrame.Pointables)
		{
			if(finger.IsValid)
			{
				alFingerIds.Add(finger.Id);
				
				LineRenderer line = null;
				if(dictFingerLines.ContainsKey(finger.Id))
				{
					line = dictFingerLines[finger.Id];
				}
				else
				{
					line = Instantiate(LineFingerPrefab) as LineRenderer;
					dictFingerLines[finger.Id] = line;
				}
				
				Leap.Vector fingerBase = finger.StabilizedTipPosition + (-finger.Direction * finger.Length);
				
				if(finger.Hand == null || !finger.Hand.IsValid)
					line.SetVertexCount(2);
				else
					line.SetVertexCount(4);
				
				line.SetPosition(0, LeapToUnity(finger.StabilizedTipPosition, true) * DisplayFingerScale + DisplayFingerPos);
				line.SetPosition(1, LeapToUnity(fingerBase, true) * DisplayFingerScale + DisplayFingerPos);
				
				if(finger.Hand != null && finger.Hand.IsValid)
				{
					Leap.Hand hand = finger.Hand;
					Leap.Vector handBase = hand.StabilizedPalmPosition + (-hand.Direction * 100);

					line.SetPosition(2, LeapToUnity(hand.StabilizedPalmPosition, true) * DisplayFingerScale + DisplayFingerPos);
					line.SetPosition(3, LeapToUnity(handBase, true) * DisplayFingerScale + DisplayFingerPos);
				}
			}
		}
		
		// cleapup fingers list
		List<int> alLostFingeIds = new List<int>();
		foreach(int fingerId in dictFingerLines.Keys)
		{
			if(!alFingerIds.Contains(fingerId))
			{
				alLostFingeIds.Add(fingerId);
			}
		}
		
		foreach(int fingerId in alLostFingeIds)
		{
			//Debug.Log("Destroying " + fingerId);
			Destroy(dictFingerLines[fingerId].gameObject);
			dictFingerLines.Remove(fingerId);
		}
		
		alFingerIds.Clear();
		alLostFingeIds.Clear();
	}
	

	// converts leap vector to unity vector
	private Vector3 LeapToUnity(Leap.Vector leapVector, bool bScaled)	
	{
		if(bScaled)
			return new Vector3(leapVector.x, leapVector.y, -leapVector.z) * .001f; 
		else
			return new Vector3(leapVector.x, leapVector.y, -leapVector.z); 
	}
	
	
	// return the index of extra gesture in the list, or -1 if not found
	private int GetGestureIndex(LeapExtraGestures.ExtraGestures gesture)
	{
		int listSize = extraGesturesData.Count;

		for(int i = 0; i < listSize; i++)
		{
			if(extraGesturesData[i].gesture == gesture)
				return i;
		}
		
		return -1;
	}
	

	// copies the needed libraries in the project directory
	private bool CheckLibsPresence()
	{
		bool bOneCopied = false, bAllCopied = true;
		
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
		if(!File.Exists("Leap.dll"))
		{
			Debug.Log("Copying Leap library...");
			TextAsset textRes = Resources.Load("Leap.dll", typeof(TextAsset)) as TextAsset;
			
			if(textRes != null)
			{
				using (FileStream fileStream = new FileStream ("Leap.dll", FileMode.Create, FileAccess.Write, FileShare.Read))
				{
					fileStream.Write (textRes.bytes, 0, textRes.bytes.Length);
				}
				
				bOneCopied = File.Exists("Leap.dll");
				bAllCopied = bAllCopied && bOneCopied;
				
				if(bOneCopied)
					Debug.Log("Copied Leap library.");
			}
		}

		if(!File.Exists("LeapCSharp.dll"))
		{
			Debug.Log("Copying LeapCSharp library...");
			TextAsset textRes = Resources.Load("LeapCSharp.dll", typeof(TextAsset)) as TextAsset;
			
			if(textRes != null)
			{
				using (FileStream fileStream = new FileStream ("LeapCSharp.dll", FileMode.Create, FileAccess.Write, FileShare.Read))
				{
					fileStream.Write (textRes.bytes, 0, textRes.bytes.Length);
				}
				
				bOneCopied = File.Exists("LeapCSharp.dll");
				bAllCopied = bAllCopied && bOneCopied;
				
				if(bOneCopied)
					Debug.Log("Copied LeapCSharp library.");
			}
		}

		if(!File.Exists("msvcp100.dll"))
		{
			Debug.Log("Copying msvcp100 library...");
			TextAsset textRes = Resources.Load("msvcp100.dll", typeof(TextAsset)) as TextAsset;
			
			if(textRes != null)
			{
				using (FileStream fileStream = new FileStream ("msvcp100.dll", FileMode.Create, FileAccess.Write, FileShare.Read))
				{
					fileStream.Write (textRes.bytes, 0, textRes.bytes.Length);
				}
				
				bOneCopied = File.Exists("msvcp100.dll");
				bAllCopied = bAllCopied && bOneCopied;
				
				if(bOneCopied)
					Debug.Log("Copied msvcp100 library.");
			}
		}
		
		if(!File.Exists("msvcr100.dll"))
		{
			Debug.Log("Copying msvcr100 library...");
			TextAsset textRes = Resources.Load("msvcr100.dll", typeof(TextAsset)) as TextAsset;
			
			if(textRes != null)
			{
				using (FileStream fileStream = new FileStream ("msvcr100.dll", FileMode.Create, FileAccess.Write, FileShare.Read))
				{
					fileStream.Write (textRes.bytes, 0, textRes.bytes.Length);
				}
				
				bOneCopied = File.Exists("msvcr100.dll");
				bAllCopied = bAllCopied && bOneCopied;
				
				if(bOneCopied)
					Debug.Log("Copied msvcr100 library.");
			}
		}
#endif
		
#if UNITY_EDITOR || UNITY_STANDALONE_OSX
		if(!File.Exists("libLeap.dylib"))
		{
			Debug.Log("Copying Leap library...");
			TextAsset textRes = Resources.Load("libLeap.dylib", typeof(TextAsset)) as TextAsset;
			
			if(textRes != null)
			{
				using (FileStream fileStream = new FileStream ("libLeap.dylib", FileMode.Create, FileAccess.Write, FileShare.Read))
				{
					fileStream.Write (textRes.bytes, 0, textRes.bytes.Length);
				}
				
				bOneCopied = File.Exists("libLeap.dylib");
				bAllCopied = bAllCopied && bOneCopied;
				
				if(bOneCopied)
					Debug.Log("Copied Leap library.");
			}
		}

		if(!File.Exists("libLeapCSharp.dylib"))
		{
			Debug.Log("Copying LeapCSharp library...");
			TextAsset textRes = Resources.Load("libLeapCSharp.dylib", typeof(TextAsset)) as TextAsset;
			
			if(textRes != null)
			{
				using (FileStream fileStream = new FileStream ("libLeapCSharp.dylib", FileMode.Create, FileAccess.Write, FileShare.Read))
				{
					fileStream.Write (textRes.bytes, 0, textRes.bytes.Length);
				}
				
				bOneCopied = File.Exists("libLeapCSharp.dylib");
				bAllCopied = bAllCopied && bOneCopied;
				
				if(bOneCopied)
					Debug.Log("Copied LeapCSharp library.");
			}
		}
#endif

		return bOneCopied && bAllCopied;
	}
	
}
