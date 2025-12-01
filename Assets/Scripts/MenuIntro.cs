using System.Collections;
using System.Collections.Generic;
using Lean.Transition;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CannedScene : MonoBehaviour
{
    [SerializeField] Transform _CameraStart;
    [SerializeField] Transform _CameraZoomOut;

    [SerializeField] CinemachineSplineDolly _SplineAutoDolly;

    void ButtonClicked()
    {
        // _CanOfTuna.positionTransition_y(0.91f,1.5f, LeanEase.Elastic)
            
    }
}

// .JoinDelayTransition(10f)
//     .EventTransition(()=>{
//         _Credits.transform.localPositionTransition_y(1000f, 10f, LeanEase.Smooth)
//         .JoinDelayTransition(0.5f)
//         .EventTransition(()=>{
//             SceneManager.LoadScene("Menu");
//     });