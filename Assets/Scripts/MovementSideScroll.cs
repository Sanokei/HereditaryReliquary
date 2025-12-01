using System;
using System.Collections;
using System.Collections.Generic;
using Monologue.Dialogue;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class MovementSideScroll : MonoBehaviour
{
    [SerializeField] float _WalkingSpeed = 4.5f;
    [SerializeField] float _SpinSpeed = 12f;
    [SerializeField] float _Gravity = 500.0f;
    [SerializeField] CharacterController _CharacterController;
    [SerializeField] Animator _PlayerAnimator;
    [SerializeField] Transform _Spin;
    Vector3 m_MoveDirection = Vector3.zero;
    [HideInInspector] public bool CanMove = true;
    void OnEnable()
    {
        StoryFunctions.OnSpeakerEvent += PlayerSpoken;
    }
    void OnDisable()
    {
        StoryFunctions.OnSpeakerEvent -= PlayerSpoken;
    }
    void PlayerSpoken(string speaker)
    {
        if(speaker.Equals("player"))
        {
            _PlayerAnimator.SetBool("talking",true);
            DialogueManager.OnDialogueTryingToContinueEvent += StopPlayerYappin;
        }
    }

    void StopPlayerYappin()
    {
        // calls this first then player spoken so should be okay that there is no check.
        _PlayerAnimator.SetBool("talking",false);
        DialogueManager.OnDialogueTryingToContinueEvent -= StopPlayerYappin;
    }
    void Update()
    {
        // We are grounded, so recalculate move direction based on axes
        // dont know if this is efficient
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);
        float curSpeedX = CanMove ? _WalkingSpeed * Input.GetAxis("Horizontal") : 0;
        float curSpeedY = CanMove ? _WalkingSpeed * -Input.GetAxis("Vertical") : 0;
        m_MoveDirection = (forward * curSpeedX) + (right * curSpeedY);
        if (!_CharacterController.isGrounded)
        {
            m_MoveDirection.y -= _Gravity * Time.deltaTime;
        }
        _CharacterController.Move(m_MoveDirection * Time.deltaTime);

        // calculate the angle of movement in radians then turn it to degrees
        float targetYRotation = (curSpeedY != 0 || curSpeedX != 0) ? Mathf.Atan2(curSpeedY,curSpeedX) * Mathf.Rad2Deg : _Spin.rotation.eulerAngles.y;
        if (targetYRotation != _Spin.rotation.eulerAngles.y)
            _PlayerAnimator.SetBool("walking", true);
        else
            _PlayerAnimator.SetBool("walking", false);
        Vector3 targetRotation = new(0, targetYRotation, 0);

        _Spin.rotation = Quaternion.Slerp(_Spin.rotation, Quaternion.Euler(targetRotation), Time.deltaTime * _SpinSpeed);
    }
}
