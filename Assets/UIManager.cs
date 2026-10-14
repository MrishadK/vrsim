using UnityEngine;
using TMPro;
using UnityEngine.Video;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public GameObject panel;
    public TextMeshProUGUI instructionText;
    public AutoMoveNav player;

    [Header("Video")]
    public VideoPlayer videoPlayer;
    public VideoClip[] clips;

    void Awake()
    {
        Instance = this;
        panel.SetActive(false);
    }

    public void ShowStep(int step)
    {
        panel.SetActive(true);

        // TEXT
        switch (step)
        {
            case 1: instructionText.text = "Submit your ticket at Check-in."; break;
            case 2: instructionText.text = "Complete Immigration."; break;
            case 3: instructionText.text = "Proceed to Security Check."; break;
            case 4: instructionText.text = "Go to Boarding Gate."; break;
        }

        // VIDEO
        if (videoPlayer != null && clips.Length > step)
        {
            videoPlayer.clip = clips[step];
            videoPlayer.Play();
        }
    }

    public void OnContinue()
    {
        panel.SetActive(false);

        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }

        player.ContinueMove();
    }
}