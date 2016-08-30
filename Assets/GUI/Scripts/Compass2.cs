using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class Compass2 : MonoBehaviour {
	public Text text;
	public Text text2;
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
		float value = Input.compass.trueHeading / 360;
		float value2 = value * 10;
		if (text!=null)
		{
			text.text=value.ToString();
		}
		if (text2!=null)
		{
			text2.text=value2.ToString("F2");
		}
		if (scrollbar!=null)
		{
			scrollbar.value=value;
		}
	}
}
