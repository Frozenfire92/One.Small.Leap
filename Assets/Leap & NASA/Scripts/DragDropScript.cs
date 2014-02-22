using UnityEngine;
using System.Collections;

public class DragDropScript : MonoBehaviour 
{
	public GameObject[] draggableObjects;
	public float dragSpeed = 3.0f;
	public Material selectedObjectMaterial;
	public float hitSpeed = 300f;
	
	private LeapManager manager;
	
	private GameObject draggedObject;
	private float draggedObjectDepth;
	private Vector3 draggedObjectOffset;
	private Vector3 newObjectPos;
	
	private Material[] objectMaterials;
	private Vector3[] objectPositions;
	private GameObject selectedObject;
	
	private GameObject infoGUI;
	private string detectedGesture;
	
	
	void Awake() 
	{
		// get needed objects´ references
		manager = Camera.mainCamera.GetComponent<LeapManager>();
		infoGUI = GameObject.Find("HandGuiText");
		
		// save original materials
		objectMaterials = new Material[draggableObjects.Length];
		objectPositions = new Vector3[draggableObjects.Length];
		
		for(int i = 0; i < draggableObjects.Length; i++)
		{
			if(draggableObjects[i] && draggableObjects[i].renderer)
			{
				objectMaterials[i] = new Material(draggableObjects[i].renderer.material);
				objectPositions[i] = draggableObjects[i].transform.position;
			}
		}
	}
	
	
	void Update() 
	{
		if(manager != null && manager.IsLeapInitialized())
		{
			Vector3 screenNormalPos = Vector3.zero;
			
			// pinch-release to drag-drop
			if(draggedObject == null)
			{
				// no object is currently selected or dragged.
				// if there is a hand pinch, try to select the underlying object and start dragging it.
				if(manager.IsHandPinchDetected())
				{
					screenNormalPos = manager.GetCursorNormalizedPos();
					
					// check if there is an underlying object to be selected
					Vector3 hitPoint;
					draggedObject = GetSelectedObject(screenNormalPos, out hitPoint);
					
					if(draggedObject)
					{
						if(selectedObject != null)
						{
							// unselect the previously selected object
							selectedObject = null;
							RestoreObjectMaterials();
						}
						
						draggedObjectDepth = draggedObject.transform.position.z - Camera.main.transform.position.z;
						draggedObjectOffset = hitPoint - draggedObject.transform.position;
						
						// set selection material
						draggedObject.renderer.material = selectedObjectMaterial;
					}
					else
					{
						// ignore the hand pinch
						manager.ClearHandPinch();
					}
				}
				
			}
			else
			{
				// continue dragging the object
				screenNormalPos = manager.GetCursorNormalizedPos();
				
				// convert the normalized screen pos to 3D-world pos
				Vector3 screenPixelPos = Vector3.zero;
				screenPixelPos.x = (int)(screenNormalPos.x * Camera.mainCamera.pixelWidth);
				screenPixelPos.y = (int)(screenNormalPos.y * Camera.mainCamera.pixelHeight);
				screenPixelPos.z = draggedObjectDepth;
				
				newObjectPos = Camera.mainCamera.ScreenToWorldPoint(screenPixelPos) - draggedObjectOffset;
				draggedObject.transform.position = Vector3.Lerp(draggedObject.transform.position, newObjectPos, dragSpeed * Time.deltaTime);
				
				// check if the object (hand pinch) was released
				bool isReleased = manager.IsHandReleaseDetected();
				
				if(isReleased)
				{
					// restore the object's material and stop dragging the object
					selectedObject = draggedObject;
					draggedObject = null;
				}
			}
			
			// object-selection functionality
			if(draggedObject == null)
			{
				float fClickProgress = manager.GetGestureProgress(LeapExtraGestures.ExtraGestures.Click);
				if(fClickProgress > 0.1f)
				{
					screenNormalPos = manager.GetCursorNormalizedPos();

					Vector3 hitPoint;
					GameObject selObject = GetSelectedObject(screenNormalPos, out hitPoint);
					
					// restore all original materials
					RestoreObjectMaterials();
					
					// set selection material
					if(selectedObject && selectedObject.renderer)
						selectedObject.renderer.material = selectedObjectMaterial;
					
					if(selObject && selObject.renderer)
					{
						Color colObject = Color.Lerp(selObject.renderer.material.color, selectedObjectMaterial.color, fClickProgress);
						selObject.renderer.material.color = colObject;
					}
					
				}
				
				if(manager.IsGestureComplete(LeapExtraGestures.ExtraGestures.Click, true))
				{
					screenNormalPos = manager.GetCursorNormalizedPos();

					Vector3 hitPoint;
					GameObject selObject = GetSelectedObject(screenNormalPos, out hitPoint);
					
					if(selObject)
					{
						selectedObject = selObject;
						
						// restore all original materials
						RestoreObjectMaterials();
						
						// set selection material
						selectedObject.renderer.material = selectedObjectMaterial;
					}
				}
			}
			else
			{
				// ignore gesture progress
				manager.IsGestureComplete(LeapExtraGestures.ExtraGestures.Click, true);
			}
				
			
			// object-rotation functionality
			if(selectedObject != null && selectedObject.rigidbody != null)
			{
				if(manager.IsGestureComplete(LeapExtraGestures.ExtraGestures.HandSwipe, true))
				{
					Vector3 gestureDir = manager.GetGestureDirection(LeapExtraGestures.ExtraGestures.HandSwipe);
					LeapExtraGestures.SwipeDirection swipeDir = LeapExtraGestures.GetSwipeDirection(gestureDir);
					
					switch(swipeDir)
					{
						case LeapExtraGestures.SwipeDirection.Right:
							detectedGesture = "SwipeRight";
							selectedObject.rigidbody.AddForce(Vector3.right * hitSpeed);
							break;

						case LeapExtraGestures.SwipeDirection.Left:
							detectedGesture = "SwipeLeft";
							selectedObject.rigidbody.AddForce(Vector3.left * hitSpeed);
							break;

						case LeapExtraGestures.SwipeDirection.Up:
							detectedGesture = "SwipeUp";
							selectedObject.rigidbody.AddForce(Vector3.up * hitSpeed);
							break;

						case LeapExtraGestures.SwipeDirection.Down:
							detectedGesture = "SwipeDown";
							selectedObject.rigidbody.AddForce(Vector3.down * hitSpeed);
							break;
						
						case LeapExtraGestures.SwipeDirection.Forward:
							detectedGesture = "SwipeForward";
							selectedObject.rigidbody.AddForce(Vector3.forward * hitSpeed);
							break;

						case LeapExtraGestures.SwipeDirection.Back:
							detectedGesture = "SwipeBack";
							selectedObject.rigidbody.AddForce(Vector3.back * hitSpeed);
							break;
					}
				}
				
				// make fist to put objects back in place
				if(manager.IsGestureComplete(LeapExtraGestures.ExtraGestures.Fist, true))
				{
					for(int i = 0; i < draggableObjects.Length; i++)
					{
						if(draggableObjects[i] != null && objectPositions[i] != null)
						{
							draggableObjects[i].transform.position = objectPositions[i];
							draggableObjects[i].transform.rotation = Quaternion.identity;
						}
					}
				}
			}
			else
			{
				// ignore gesture progress
				manager.IsGestureComplete(LeapExtraGestures.ExtraGestures.HandSwipe, true);
				manager.IsGestureComplete(LeapExtraGestures.ExtraGestures.Fist, true);
			}
			
		}
	} 
	
	// returns the selected object or null
	private GameObject GetSelectedObject(Vector3 screenNormalPos, out Vector3 hitPoint)
	{
		// convert the normalized screen pos to pixel pos
		Vector3 screenPixelPos = Vector3.zero;
		screenPixelPos.x = (int)(screenNormalPos.x * Camera.mainCamera.pixelWidth);
		screenPixelPos.y = (int)(screenNormalPos.y * Camera.mainCamera.pixelHeight);
		Ray ray = Camera.mainCamera.ScreenPointToRay(screenPixelPos);
		
		// check for underlying objects
		RaycastHit hit;
		hitPoint = Vector3.zero;
		
		if(Physics.Raycast(ray, out hit))
		{
			
			foreach(GameObject obj in draggableObjects)
			{
				if(hit.collider.gameObject == obj)
				{
					hitPoint = hit.point;
					return obj;
				}
			}
		}

		return null;
	}
	
	// restores original object materials
	private void RestoreObjectMaterials()
	{
		for(int i = 0; i < draggableObjects.Length; i++)
		{
			if(draggableObjects[i] && draggableObjects[i].renderer)
			{
				draggableObjects[i].renderer.material = objectMaterials[i];
			}
		}
	}
	
	void OnGUI()
	{
		if(infoGUI != null && manager != null && manager.IsLeapInitialized())
		{
			string sInfo = string.Empty;
			
			int userID = manager.GetHandID();
			if(userID != 0)
			{
				if(draggedObject != null)
					sInfo = "Dragging the " + draggedObject.name + " around.";
				else if(selectedObject != null && selectedObject.rigidbody != null && selectedObject.rigidbody.velocity != Vector3.zero)
					sInfo = detectedGesture + " detected.";
				else if(selectedObject != null)
					sInfo = "You selected " + selectedObject.name + ". Swipe to move it or make Fist to reset the objects.";
				else
					sInfo = "Pinch and drag an object around, or hold the cursor over an object to select it.";
			}
			else
			{
				sInfo = "Waiting for Users...";
			}
			
			infoGUI.guiText.text = sInfo;
		}
	}
	
}
