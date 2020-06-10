using UnityEngine;

// ADF Band: 190 Khz to 1750 Khz with 10 Khz increments
// AM Band: 540 Khz to 1600 Khz at 10 Khz increments
// FM Band: 88.0 Mhz to 108.0 Mhz with .2 Mhz increments
public class Radio_FM : MonoBehaviour
{
    public AudioClip[] audioFiles;
    public bool isPlaying;

    AudioSource audioSource;
    int currentSongIndex = -1;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void TurnOn()
    {
        isPlaying = true;

        if (currentSongIndex == -1)
            currentSongIndex = Random.Range(0, audioFiles.Length - 1);

        AudioClip a = audioFiles[currentSongIndex];
        audioSource.PlayOneShot(a);
    }

    public void TurnOff()
    {
        isPlaying = false;
        audioSource.Stop();
    }

    void ToggleRadio()
    {
        if (isPlaying) TurnOff(); else TurnOn();
    }

    public float Frequency(int index)
    {
        return (float) System.Math.Round(88f + (.2f * index), 2);
    }

    public string FrequencyString()
    {
        return Frequency(currentSongIndex).ToString("F2");
    }

    void IncrementStation()
    {
        TurnOff();
        currentSongIndex = (currentSongIndex + 1) % audioFiles.Length;
        TurnOn();
    }

    void DecrementStation()
    {
        TurnOff();
        currentSongIndex = (--currentSongIndex < 0) ? 0 : currentSongIndex;
        TurnOn();
    }

    void Update()
    {
        if (Input.GetButtonDown("Radio"))
        {
            ToggleRadio();
        }
        else if (Input.GetAxis("Dpad Horizontal") < 0)
        {
            DecrementStation();
        }
        else if (Input.GetAxis("Dpad Horizontal") > 0)
        {
            IncrementStation();
        }
    }
}
