using UnityEngine;
using System.Collections;

public class MoonOrbit : MonoBehaviour 
{
	public Transform earth;
	[Range(0,25)]
	public float orbitSpeed = 5f;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () 
	{
		transform.RotateAround(earth.transform.position, transform.up, orbitSpeed*Time.deltaTime);
	}
}
