using System.Collections;
using System.Collections.Generic;
using GridBuilder.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance{get; private set;}
    // Current Level
    int _CurrentLevel = 0;
    public int CurrentLevel{get {return _CurrentLevel;} private set{_CurrentLevel = value;}}
    [SerializeField] List<LevelData> Levels;

    public LevelData this[int index]
    {
        get { if(index < 0 || index >= Levels.Count) return Levels[index]; else return null;}
    }

    public LevelData GetCurrentLevelData()
    {
        if(CurrentLevel < 0 || CurrentLevel >= Levels.Count)
        {
            Debug.LogError("CurrentLevel index is out of range!");
            return null;
        }
        return Levels[CurrentLevel];
    }
    void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }
    void OnEnable()
    {
        Ship.OnWinConditionEvent += HandleWinCondition;
    }

    void OnDisable()
    {
        Ship.OnWinConditionEvent -= HandleWinCondition;
    }

    void HandleWinCondition()
    {
        CurrentLevel++;
        if(CurrentLevel >= Levels.Count)
        {
            Debug.Log("All levels completed!");
            SceneManager.LoadScene("WonScreen");
            return;
        }
        SceneManager.LoadScene("LevelScene");
    }
}
