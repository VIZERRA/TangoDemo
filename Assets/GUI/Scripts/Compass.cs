using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class Compass : MonoBehaviour {
	public Text text;
	public Scrollbar scrollbar;
	// Use this for initialization
	void Start () 
	{
		Input.compass.enabled = true;
		Input.location.Start ();
	}
	
	// Update is called once per frame
	void Update () 
	{
		if (text!=null)
		{
			text.text=Input.compass.trueHeading.ToString();
		}
		if (scrollbar!=null)
		{
			scrollbar.value=Input.compass.trueHeading/360;
		}
	}
}
