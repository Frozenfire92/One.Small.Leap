using UnityEngine;
using System.Collections;

public class GameControlScript : MonoBehaviour 
{
	public GameObject cratePrefab;
	public Rect guiWindowRect = new Rect(80, 40, 262, 420);
	public GUISkin guiSkin;
	
	
	void Start () 
	{
		Quaternion quatRot90 = Quaternion.Euler(new Vector3(0, 90, 0));
		
		for(int i = -50; i <= 50; i++)
		{
			GameObject.Instantiate(cratePrefab, new Vector3(i, 0.32f, 50), Quaternion.identity);
			GameObject.Instantiate(cratePrefab, new Vector3(i, 0.32f, -50), Quaternion.identity);
			GameObject.Instantiate(cratePrefab, new Vector3(50, 0.32f, i), quatRot90);
			GameObject.Instantiate(cratePrefab, new Vector3(-50, 0.32f, i), quatRot90);
		}
	}

	
	private void ShowGuiWindow(int windowID) 
	{
		GUILayout.BeginVertical();
		//GUILayout.Label(System.Environment.CurrentDirectory);
		GUILayout.Label("<b>* Point a finger in a direction to walk.</b>");
		GUILayout.Label("<b>* Make a fist or take your hand back to stop.</b>");
		GUILayout.Space(15);
		GUILayout.Label("<b>* Use <i>Circle</i> gesture to jump and run.</b>");
		GUILayout.Label("<b>* Use <i>Keytap</i> gesture to stop running.</b>");
		GUILayout.Label("<b>* Use <i>Screentap</i> gesture to start running again.</b>");
		GUILayout.Label("<b>* Use <i>Swipe</i> gesture to wave.</b>");
		GUILayout.EndVertical();
		
		// Make the window draggable.
		GUI.DragWindow();
	}
	
	
	void OnGUI()
	{
		Rect windowRect = guiWindowRect;
		if(windowRect.x < 0)
			windowRect.x += Screen.width;
		if(windowRect.y < 0)
			windowRect.y += Screen.height;
		
		GUI.skin = guiSkin;
		guiWindowRect = GUI.Window(0, windowRect, ShowGuiWindow, "Leap Commands");
	}
	
}
