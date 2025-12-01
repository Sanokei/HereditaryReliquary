using System.Collections;
using System.Collections.Generic;
using SimpleMan.CoroutineExtensions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using Unity.Cinemachine;
using UnityEngine.Rendering;
using CW.Common;
using Lean.Transition;

public class MenuCameraController : MonoBehaviour
{
    [SerializeField] CinemachineCamera _Start;
    [SerializeField] Volume _StartVolume;
    [SerializeField] CinemachineCamera _VideoStart;
    [SerializeField] Volume _VideoStartVolume;
    [SerializeField] GameObject _VideoStartFocusObject;
    [SerializeField] Camera _MainCamera;
    // [SerializeField] VideoPlayer _BubbleTransition;

    string _QueuedSceneName;
    void Start()
    {
        CinemachineCameraManager.Instance.SetCam(_Start);
    }
    public void GoToScene(string sceneName)
    {
        _Start.Priority = 0;
        _VideoStart.Priority = 1;
        _QueuedSceneName = sceneName;
        
        _VideoStart.transform
                .positionTransition_y(-20.739f, 1.25f, LeanEase.Smooth).JoinTransition().EventTransition(()=>{
                _VideoStart.transform.SetPositionAndRotation(_VideoStartFocusObject.transform.position,_VideoStartFocusObject.transform.rotation);
            }).JoinTransition()
                .positionTransition_xy(new(_VideoStartFocusObject.transform.position.x + 1.5f, _VideoStartFocusObject.transform.position.y + 1f), 0.5f, LeanEase.Smooth);
        // this.Delay(4f,() =>
        // {
        //     _BubbleTransition.Play();
        //     StartCoroutine(ChangeColor(new Color(0.08810966f, 0.1572813f, 0.4150943f, 1f),Color.black,7f,() => SceneManager.LoadScene(_QueuedSceneName)));
        // }
        // );

    }

    IEnumerator ChangeColor(Color startColor, Color endColor, float duration, System.Action callback)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            _MainCamera.backgroundColor = Color.Lerp(startColor, endColor, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure the final color is set
        _MainCamera.backgroundColor = endColor;
        callback();
    }
}
