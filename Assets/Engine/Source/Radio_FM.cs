using UnityEngine;
using TMPro;

// ADF Band: 190 Khz to 1750 Khz with 10 Khz increments
// AM Band: 540 Khz to 1600 Khz at 10 Khz increments
// FM Band: 88.0 Mhz to 108.0 Mhz with .2 Mhz increments
public class Radio_FM : MonoBehaviour
{
    public AudioClip[] audioFiles;
    public TextMeshProUGUI text;

    int currentSongIndex;
    AudioSource audioSource;
    DPadButton dpad;
    AudioClip clip;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        dpad = GetComponent<DPadButton>();
        currentSongIndex = Random.Range(0, audioFiles.Length - 1);
        clip = audioFiles[currentSongIndex];
        audioSource.PlayOneShot(clip);
        text.text = FrequencyString();
    }

    public float Frequency(int index)
    {
        return (float) System.Math.Round(88f + (.2f * index), 2);
    }

    public string FrequencyString()
    {
        return Frequency(currentSongIndex).ToString("F1");
    }

    void IncrementStation()
    {
        audioSource.Stop();
        currentSongIndex++;
        currentSongIndex = currentSongIndex % audioFiles.Length;
        clip = audioFiles[currentSongIndex];
        audioSource.PlayOneShot(clip);
        text.text = FrequencyString();
    }

    void DecrementStation()
    {
        audioSource.Stop();
        currentSongIndex--;
        if (currentSongIndex < 0) currentSongIndex = audioFiles.Length - 1;
        clip = audioFiles[currentSongIndex];
        audioSource.PlayOneShot(clip);
        text.text = FrequencyString();
    }

    void RaiseVolume()
    {
        audioSource.volume += .1f;
    }

    void LowerVolume()
    {
        audioSource.volume -= .1f;
    }

    void Update()
    {
        if (dpad.left)
        {
            DecrementStation();
        }
        if (dpad.right)
        {
            IncrementStation();
        }
        if (dpad.down)
        {
            LowerVolume();
        }
        if (dpad.up)
        {
            RaiseVolume();
        }
    }
}
