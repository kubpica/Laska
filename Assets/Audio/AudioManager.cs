using System;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviourSingleton<AudioManager>
{
	public AudioMixerGroup mixerGroup;

	public Sound[] sounds;

	public float volume = 2.5f;
	public bool isMuted;

	private void prepareSource(Sound s, GameObject go, bool destroyOldSource)
	{
		if(go == null)
		{
			go = gameObject;
		}

		if(s.source != null)
		{
			if(s.source.gameObject == go)
			{
				return;
			}
			else if(destroyOldSource)
			{
				Destroy(s.source);
			}
		}

		s.source = go.AddComponent<AudioSource>();
		s.source.clip = s.clip;
		s.source.loop = s.loop;
		s.source.spatialBlend = s.spatialBlend;

		s.source.outputAudioMixerGroup = mixerGroup;
	}

	private Sound getSound(string sound, GameObject sourceGO, bool destroyOldSource = false)
	{
		Sound s = Array.Find(sounds, item => item.name == sound);
		if (s == null)
		{
			Debug.LogWarning("Sound: " + sound + " not found!");
			return null;
		}
		prepareSource(s, sourceGO, destroyOldSource);

		s.source.volume = s.volume;
		s.source.pitch = s.pitch;
		return s;
	}

	public void Play(string sound, GameObject sourceGO = null)
	{
		if (isMuted)
			return;

		var s = getSound(sound, sourceGO, true);
		if (s != null)
		{
			s.source.volume = s.volume * volume;
			s.source.Play();
			s.source.time = s.startTime;
		}
	}

	public void PlayOneShot(string sound, GameObject sourceGO = null, float volumeScale = 1)
	{
		if (isMuted)
			return;

		var s = getSound(sound, sourceGO, false);
		if(s != null)
		{
			var audioSource = s.source;
			s.source.volume = s.volume * volume;
			audioSource.PlayOneShot(audioSource.clip, volumeScale);
			audioSource.time = s.startTime;
		}
	}

	public void PlayAtPoint(string sound, Vector3 position, float volumeScale = 1)
	{
		if (isMuted)
			return;

		var s = getSound(sound, null);
		if (s != null)
		{
			AudioSource.PlayClipAtPoint(s.clip, position, s.volume * volume * volumeScale);
		}
	}	
}
