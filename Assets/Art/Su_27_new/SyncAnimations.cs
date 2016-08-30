using UnityEngine;
using System.Collections;

public class SyncAnimations : MonoBehaviour {
	
	public Animation animation;
	// Update is called once per frame
	void Rewind ()
	{
		animation.Rewind();
		animation.Play();
		animation.Sample();
		animation.Stop();
		animation.Play();
	}
}
