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
using EasyTransition;

public class MenuCameraController : MonoBehaviour
{
    [SerializeField] CinemachineCamera _Start;
    [SerializeField] Volume _StartVolume;
    [SerializeField] CinemachineCamera _VideoStart;
    [SerializeField] Volume _VideoStartVolume;
    [SerializeField] GameObject _VideoStartFocusObject;
    [SerializeField] GameObject _OutsideOfTVObject;
    [SerializeField] GameObject _DogViewObject;
    [SerializeField] GameObject _CatViewObject;
    [SerializeField] Camera _MainCamera;
    [SerializeField] TransitionSettings transition;
    // [SerializeField] VideoPlayer _BubbleTransition;

    void Start()
    {
        CinemachineCameraManager.Instance.SetCam(_Start);
    }
    public void GoToScene(string sceneName)
    {
        _Start.Priority = 0;
        _VideoStart.Priority = 1;
        _VideoStartVolume.weight = 1;
        _StartVolume.weight = 0;
        
        SubtitleManager.Instance.SetSubtitles("The wave shrines were once a place of pilgrimage");
        _VideoStart.transform
            .positionTransition_y(-20.739f, 3.25f, LeanEase.Smooth).JoinTransition().EventTransition(()=>{
                SubtitleManager.Instance.SetSubtitles("Two centuries ago the most respected shrine by sailors and merchant marines");
                _VideoStart.transform.SetPositionAndRotation(_VideoStartFocusObject.transform.position,_VideoStartFocusObject.transform.rotation);
            }).JoinDelayTransition(2.75f).EventTransition(()=>{
                SubtitleManager.Instance.SetSubtitles("Legends say that those who visit the shrine will be blessed with safe travels");
                _VideoStart.transform.positionTransition_xy(new(_VideoStart.transform.position.x + 1.5f, _VideoStart.transform.position.y + 1f), 2.5f, LeanEase.Smooth)
                    .JoinTransition().EventTransition(()=>{
                        _VideoStart.transform.positionTransition(_OutsideOfTVObject.transform.position, 1.25f, LeanEase.Smooth);
                        _VideoStart.transform.rotationTransition(_OutsideOfTVObject.transform.rotation, 1.25f, LeanEase.Smooth);
                        SubtitleManager.Instance.SetSubtitles("But now, it can be yours for only $19.99!");
                        _StartVolume.weight = 1;
                        _VideoStartVolume.weight = 0;
                    }).JoinDelayTransition(1.75f).EventTransition(()=>{
                        SubtitleManager.Instance.SetSubtitles("That's right only 3 easy payments of $19.99!");
                        _VideoStart.transform.positionTransition_x(_VideoStart.transform.position.x + 10f, 2.25f, LeanEase.Smooth).JoinTransition()
                            .rotationTransition(Quaternion.Euler(_VideoStart.transform.rotation.eulerAngles + new Vector3(0f, 180f, 0f)), 0.75f, LeanEase.Smooth)
                            .JoinDelayTransition(0.75f).EventTransition(()=>{
                                SubtitleManager.Instance.SetSubtitles("Dog: What a scam, those wave shrines are way cool, and can't be sold to the highest bidder");
                                _VideoStart.transform.SetPositionAndRotation(_DogViewObject.transform.position, _DogViewObject.transform.rotation);
                            }).JoinDelayTransition(2.75f).EventTransition(()=>{
                                SubtitleManager.Instance.SetSubtitles("Cat: miao");
                                _VideoStart.transform.SetPositionAndRotation(_CatViewObject.transform.position, _CatViewObject.transform.rotation);
                            }).JoinDelayTransition(1.75f).EventTransition(()=>{
                                SubtitleManager.Instance.SetSubtitles("Being decerped and creepy now is not very in fashion");
                                _VideoStart.transform.SetPositionAndRotation(_OutsideOfTVObject.transform.position,_OutsideOfTVObject.transform.rotation);
                            }).JoinDelayTransition(2.0f).EventTransition(()=>{
                                SubtitleManager.Instance.SetSubtitles("Dog: Cat lets go on an adventure");
                                _VideoStart.transform.SetPositionAndRotation(_DogViewObject.transform.position, _DogViewObject.transform.rotation);
                            }).JoinDelayTransition(2.75f).EventTransition(()=>{
                                SubtitleManager.Instance.SetSubtitles("Cat: miao");
                                _VideoStart.transform.SetPositionAndRotation(_CatViewObject.transform.position, _CatViewObject.transform.rotation);
                                TransitionManager.Instance().Transition(sceneName, transition, 1.75f);
                            });
                    });
            });
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
