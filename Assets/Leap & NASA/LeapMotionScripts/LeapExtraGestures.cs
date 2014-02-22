using UnityEngine;
using System.Collections;

public class LeapExtraGestures : MonoBehaviour 
{
	
	public static class Constants
	{
		public const float PoseCompleteDuration = 1.5f;
		public const float MinTimeBetweenGestures = 0.7f;
		public const float ClickStayDuration = 2.0f;
	}
	

	public enum ExtraGestures
	{
		None = 0,
		Fist,
		Click,
		HandSwipe
	}
	
	public enum SwipeDirection
	{
		None,
		Right,
		Left,
		Up,
		Down,
		Forward,
		Back
	}

	public struct ExtraGestureData
	{
		public ExtraGestures gesture;
		public int state;
		public float timestamp;
		public bool jointIsHand;
		public int jointId;
		public Vector3 jointPos;
		public Vector3 screenPos;
		public float tagFloat;
		public Vector3 tagVector;
		public Vector3 tagVector2;
		public float progress;
		public bool complete;
		public bool cancelled;
		public float completeTime;
		public Vector3 gestureDir;
		public float startTrackingAtTime;
	}
	
	private static void SetGestureJoint(ref ExtraGestureData gestureData, float timestamp, 
										bool jointIsHand, int jointId, Vector3 jointPos)
	{
		gestureData.jointIsHand = jointIsHand;
		gestureData.jointId = jointId;
		gestureData.jointPos = jointPos;
		gestureData.timestamp = timestamp;
		gestureData.state++;
	}
	
	private static void SetGestureCancelled(ref ExtraGestureData gestureData)
	{
		gestureData.state = 0;
		gestureData.progress = 0f;
		gestureData.cancelled = true;
	}
	
	private static void CheckPoseComplete(ref ExtraGestureData gestureData, float timestamp, Vector3 jointPos, bool isInPose, float durationToComplete)
	{
		if(isInPose)
		{
			float timeLeft = timestamp - gestureData.timestamp;
			gestureData.progress = durationToComplete > 0f ? Mathf.Clamp01(timeLeft / durationToComplete) : 1.0f;
	
			if(timeLeft >= durationToComplete)
			{
				gestureData.completeTime = timestamp - gestureData.timestamp;
				gestureData.gestureDir = jointPos - gestureData.jointPos;
				
				gestureData.timestamp = timestamp;
				gestureData.jointPos = jointPos;
				gestureData.state++;
				gestureData.complete = true;
			}
		}
		else
		{
			SetGestureCancelled(ref gestureData);
		}
	}
	
	// estimate the next state and completeness of the gesture
	public static void CheckForGesture(ref ExtraGestureData gestureData, float timestamp, LeapManager leapManager)
	{
		if(gestureData.complete || !leapManager)
			return;
		
		switch(gestureData.gesture)
		{
			// check for Fist
			case ExtraGestures.Fist:
				switch(gestureData.state)
				{
					case 0:  // gesture detection - phase 1
						if(leapManager.IsHandValid() && leapManager.GetFingersCount() > 1)
						{
							SetGestureJoint(ref gestureData, timestamp, true, leapManager.GetHandID(), leapManager.GetHandPos());
							gestureData.tagFloat = leapManager.GetThumbID();
							gestureData.screenPos = leapManager.GetCursorNormalizedPos();
							gestureData.progress = 0.3f;
						}
						break;
				
					case 1:  // gesture phase 2 - complete
						if(leapManager.IsHandValid())
						{
							// check for stay-in-place
							Vector3 jointPos = leapManager.GetHandPos();
							bool bHandMatch = (leapManager.GetHandID() == gestureData.jointId);
							bool bThumbMatch = leapManager.GetFingersCount() > 0 ? (leapManager.GetThumbID() == (int)gestureData.tagFloat) : true;
							
							if(bHandMatch && bThumbMatch)
							{
								if(leapManager.GetFingersCount() <= 1)
								{
									gestureData.screenPos = leapManager.GetCursorNormalizedPos();
									CheckPoseComplete(ref gestureData, timestamp, jointPos, true, Constants.PoseCompleteDuration);
								}
							}
							else
							{
								// hand or thumb don´t match
								SetGestureCancelled(ref gestureData);
							}

//							Debug.Log(gestureData.complete.ToString() + " GestID: " + gestureData.jointId + 
//									" Fingers: " + leapManager.GetFingersCount() + 
//									" ThumbID: " + leapManager.GetThumbID().ToString());

						}
						break;
				}
				break;
			
			// check for Click
			case ExtraGestures.Click:
				switch(gestureData.state)
				{
					case 0:  // gesture detection - phase 1
						if(leapManager.IsHandValid())
						{
							SetGestureJoint(ref gestureData, timestamp, true, leapManager.GetHandID(), leapManager.GetHandPos());
							gestureData.screenPos = leapManager.GetCursorNormalizedPos();
							gestureData.progress = 0.3f;
						}
						break;
				
					case 1:  // gesture phase 2 - complete
						if(leapManager.IsHandValid())
						{
							// check for stay-in-place
							Vector3 jointPos = leapManager.GetHandPos();
							Vector3 distVector = jointPos - gestureData.jointPos;
							bool isInPose = leapManager.GetHandID() == gestureData.jointId && 
											distVector.magnitude < 0.05f;

							CheckPoseComplete(ref gestureData, timestamp, jointPos, isInPose, Constants.ClickStayDuration);
						}
						break;
				}
				break;
			
			// check for HandSwipe
			case ExtraGestures.HandSwipe:
				switch(gestureData.state)
				{
					case 0:  // gesture detection - phase 1
						if(leapManager.IsHandValid())
						{
							SetGestureJoint(ref gestureData, timestamp, true, leapManager.GetHandID(), leapManager.GetHandPos());
							gestureData.screenPos = leapManager.GetCursorNormalizedPos();
							gestureData.progress = 0.5f;
						}
						break;
				
					case 1:  // gesture phase 2 - complete
						if((timestamp - gestureData.timestamp) < 1.5f)
						{
							if(leapManager.IsHandValid())
							{
								Vector3 jointPos = leapManager.GetHandPos();
								Vector3 distVector = jointPos - gestureData.jointPos;
								bool isInPose = leapManager.GetHandID() == gestureData.jointId && 
												distVector.magnitude > 0.15f;
	
								if(isInPose)
								{
									CheckPoseComplete(ref gestureData, timestamp, jointPos, isInPose, 0f);
								}
							}
						}
						else
						{
							// cancel the gesture
							SetGestureCancelled(ref gestureData);
						}
						break;
				}
				break;
		}

		// here come more gesture-cases
	}
	
	// converts gesture direction to SwipeDirection-value
	public static SwipeDirection GetSwipeDirection(Vector3 swipeDir)
	{
		if(Mathf.Abs(swipeDir.x) > Mathf.Abs(swipeDir.y))
		{
			// |x| > |y|
			if(Mathf.Abs(swipeDir.x) > Mathf.Abs(swipeDir.z))
			{
				// x
				return swipeDir.x > 0 ? SwipeDirection.Right : SwipeDirection.Left;
			}
			else
			{
				// z
				return swipeDir.z > 0 ? SwipeDirection.Forward : SwipeDirection.Back;
			}
		}
		else
		{
			// |y| > |x|
			if(Mathf.Abs(swipeDir.y) > Mathf.Abs(swipeDir.z))
			{
				// y
				return swipeDir.y > 0 ? SwipeDirection.Up : SwipeDirection.Down;
			}
			else
			{
				// z
				return swipeDir.z > 0 ? SwipeDirection.Forward : SwipeDirection.Back;
			}
		}
		
		return SwipeDirection.None;
	}
	
}
