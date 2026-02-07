using DG.Tweening;
using GridBuilder.Core;
using SimpleMan.CoroutineExtensions;
using TMPro;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }
    
    [SerializeField] LevelBuilder _LevelBuilder;
    [SerializeField] TMP_Text _LevelTitle;
    
    private int currentPar = 0;
    
    public int CurrentPar => currentPar;
    public int LevelPar => GameManager.Instance.GetCurrentLevelData()?.par ?? 0;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnEnable()
    {
        // Reset current par when level loads
        // Don't access GameManager here as it might not be initialized yet
        currentPar = 0;
    }

    void Start()
    {
        // Use Start() instead of OnEnable() to ensure GameManager is initialized
        // Check if GameManager is available
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("LevelManager: GameManager.Instance is null. Level data and title will not be set.");
            return;
        }
        
        LevelData levelData = GameManager.Instance.GetCurrentLevelData();
        if (levelData == null)
        {
            Debug.LogWarning("LevelManager: GetCurrentLevelData() returned null. Make sure the level index is valid.");
            return;
        }
        
        // Set level data for LevelBuilder
        if (_LevelBuilder != null)
        {
            _LevelBuilder.levelData = levelData;
        }
        
        // Set level title
        if (_LevelTitle != null)
        {
            _LevelTitle.text = levelData.levelName;
            // set transition fade
            _LevelTitle.DOFade(0f, 1.25f);
        }
    }
    
    /// <summary>
    /// Increments the current par count when a wave is placed
    /// </summary>
    public void IncrementPar()
    {
        currentPar++;
    }
}
