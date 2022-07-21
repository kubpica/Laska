using UnityEngine;
using UnityEngine.Audio;

[System.Serializable]
public class Sound {

	public string name;

	public AudioClip clip;

	[Range(0f, 1f)]
	public float volume = .75f;
	[Range(.1f, 3f)]
	public float pitch = 1f;
	[Range(0f, 1f)]
	public float spatialBlend;

	public float startTime;

	public bool loop = false;

	public AudioMixerGroup mixerGroup;

	[HideInInspector]
	public AudioSource source;

}
