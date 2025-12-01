using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Playables;

public class SubtitleManager : MonoBehaviour
{
    public static SubtitleManager Instance{get; private set;}
    [SerializeField] TMP_Text subText;

    void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    void OnPlayableDirectorStopped(PlayableDirector director)
    {
        ResetSubtitles();
    }
    public void ResetSubtitles()
    {
        subText.text = "";
    }
    public void SetSubtitles(string s)
    {
        subText.text = s;
    }
}
