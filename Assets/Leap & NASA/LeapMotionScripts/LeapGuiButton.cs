using UnityEngine;
using System.Collections;

public class LeapGuiButton : MonoBehaviour 
{
	public bool toggleButton = false;
	public float selectionTime = 0.5f;
	public Texture normalTexture;
	public Texture hoverTexture;
	public Texture pressedTexture;

	private bool bBtnPressed = false;
	private bool bPressReported = false;
	private Vector3 vLastCursorPos = Vector3.zero;
	private float fSelectionTimer = 0f;
	private LeapManager leapManager;
	
	
	public bool IsButtonPressed()
	{
		Rect btnRect = this.guiTexture.GetScreenRect();
		
		if(!toggleButton && bBtnPressed && !bPressReported)
		{
			DepressButton();
			
			bPressReported = true;
			return true;
		}

		return bBtnPressed;
	}
	
	
	public void DepressButton()
	{
		bBtnPressed = false;
		this.guiTexture.texture = normalTexture;
		
		vLastCursorPos = Vector3.zero;
		fSelectionTimer = 0;
	}
	

	void Start () 
	{
		leapManager = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<LeapManager>();
	}
	
	void OnGUI()
	{
		// LeapGuiButton must be used on a GUI texture
		if(!this.guiTexture)
			return;
		
		if(leapManager && leapManager.IsPointableValid() //&& 
			/**(leapManager.GetPointableTouchStatus() == Leap.Pointable.Zone.ZONEHOVERING || leapManager.GetPointableTouchStatus() == Leap.Pointable.Zone.ZONETOUCHING)*/)
		{
			Vector3 posCursor = leapManager.GetCursorScreenPos();
			Leap.Pointable.Zone touchStatus = leapManager.GetPointableTouchStatus();
			
			Rect btnRect = this.guiTexture.GetScreenRect();
			Texture texture = !bBtnPressed ? normalTexture : pressedTexture;
			
			if(!bBtnPressed)
			{
				// selection
				if(!bPressReported && btnRect.Contains(posCursor))
				{
					texture = hoverTexture;
					
					if(!btnRect.Contains(vLastCursorPos))
					{
						fSelectionTimer = Time.realtimeSinceStartup + selectionTime;
					}
					else if((fSelectionTimer > 0) && (Time.realtimeSinceStartup >= fSelectionTimer))
					{
						bBtnPressed = true;
						texture = pressedTexture;
						fSelectionTimer = 0;
					}
				}
				else if(bPressReported && !btnRect.Contains(posCursor))
				{
					// can report the press again
					bPressReported = false;
				}
			}
			else if(toggleButton)
			{
				// unselection
				if(btnRect.Contains(posCursor))
				{
					if(!btnRect.Contains(vLastCursorPos))
					{
						fSelectionTimer = Time.realtimeSinceStartup + selectionTime;
					}
					else if((fSelectionTimer > 0) && (Time.realtimeSinceStartup >= fSelectionTimer))
					{
						bBtnPressed = false;
						texture = normalTexture;
						fSelectionTimer = 0;
					}
				}
			}
			
			this.guiTexture.texture = texture;
			vLastCursorPos = posCursor;
		}
	}
	
}
