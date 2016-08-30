using UnityEngine;
using System.Collections;

public class Rotation : MonoBehaviour {
	
	public string animationName;
	bool startRotation = false;
	// Update is called once per frame
	public bool StartRotation 
	{
		get
		{
			return startRotation;
		}
		set
		{
			startRotation = value;
			if (this.GetComponent<Animation>()!=null)
			{
				startRotation = value;
				if (startRotation)
				{
					if (this.GetComponent<Animation>().isPlaying==false)
					{
						this.GetComponent<Animation>().Play();
						this.GetComponent<Animation>()[animationName].speed=1;
					}
					else this.GetComponent<Animation>()[animationName].speed=1;
				}
				else 
				{
					this.GetComponent<Animation>()[animationName].speed=0;
					//this.animation.Stop();
				}
			}
		}	
	}
}
