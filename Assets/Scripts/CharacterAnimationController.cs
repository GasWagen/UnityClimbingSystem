﻿using System;
using System.Runtime.InteropServices;

using Sirenix.OdinInspector.Editor.Drawers;

using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Animation controller of the player character
/// </summary>
[RequireComponent(typeof(Animator))]
public class CharacterAnimationController : MyMonoBehaviour{
    /// <summary>
    /// Animator responsible for the animations
    /// </summary>
    [HideInInspector] public Animator animator;

    /// <summary>
    /// List of triggers used to transition
    /// </summary>
    private static readonly string[] TRIGGERS = {
        "BracedHangTrigger",
        "BracedHangUntrigger",
        "BracedShimmyRightTrigger",
        "BracedShimmyLeftTrigger",
        "FreeHangTrigger",
        "FreeHangUntrigger",
        "FreeHangShimmyRight",
        "FreeHangShimmyLeft",
        "FallTrigger",
    };

    // Braced Hangs
    private const int TRIGGER_BRACED_HANG = 0;
    private const int TRIGGER_BRACED_UNHANG = 1;
    private const int TRIGGER_BRACED_SHIMMY_RIGHT = 2;

    private const int TRIGGER_BRACED_SHIMMY_LEFT = 3;

    // Free Hangs
    private const int TRIGGER_FREE_HANG_HANG = 4;
    private const int TRIGGER_FREE_HANG_UNHANG = 5;
    private const int TRIGGER_FREE_HANG_SHIMMY_RIGHT = 6;

    private const int TRIGGER_FREE_HANG_SHIMMY_LEFT = 7;

    // Fall
    private const int TRIGGER_FALLING_IDLE = 8;

    /// <summary>
    /// List of booleans used to tronsition
    /// </summary>
    private static readonly string[] BOOLS = {
        "isGrounded",
        "isCrouching",
        "isJumping",
        "isClimbing",
        "isStrafingRight",
        "isStrafingLeft"
    };

    private const int BOOL_IS_GROUNDED = 0;
    private const int BOOL_IS_CROUCHING = 1;
    private const int BOOL_IS_JUMPING = 2;
    private const int BOOL_IS_CLIMBING = 3;
    private const int BOOL_IS_STRAFING_RIGHT = 4;
    private const int BOOL_IS_STRAFING_LEFT = 5;

    /// <summary>
    /// List of floats used to transitiom
    /// </summary>
    private static readonly string[] FLOATS = {
        "distanceToGround",
        "yVelocity",
        "forwardSpeed",
        "IdleWalkBlend",
        "JumpBlend"
    };

    private const int FLOAT_DISTANCE_TO_GROUND = 0;
    private const int FLOAT_Y_VELOCITY = 1;
    private const int FLOAT_FORWARD_SPEED = 2;
    private const int FLOAT_IDLE_WALK_BLEND = 3;
    private const int FLOAT_JUMP_BLEND = 4;

    /// <summary>
    /// List of some states used from the code
    /// </summary>
    private static readonly string[] STATES = {
        "Standing Blend Tree",
        "Crouching Blend Tree",
        "Idle Braced Hang",
        "Idle Free Hang"
    };

    private const int STATE_STANDING = 0;
    private const int STATE_CROUCHING = 1;
    private const int STATE_BRACED_HANG = 2;
    private const int STATE_FREE_HANG = 3;

//  public event Action OnBracedShimmyAnimationEnd;

    /// <summary>
    /// The end position of the ray shooting from the hips
    /// </summary>
    private Vector3 hipsFrontRayEnd;

    /// <summary>
    ///  The start position of the ray shooting from the hips
    /// </summary>
    private Vector3 hipsFrontRayOrigin;

    /// <summary>
    /// Reference to the transform that hints where the right hand should be
    /// </summary>
    [HideInInspector] public Transform rightHandIK;

    /// <summary>
    /// Reference to the transform that hints where the left hand should be
    /// </summary>
    [HideInInspector] public Transform leftHandIK;

    /// <summary>
    /// Is the player hanging on a ledge?
    /// </summary>
    public bool isHanging;

    /// <summary>
    /// How is the player hanging on the ledge?
    /// </summary>
    public Point.HangType hangType;

//
//    private float _distanceToGround;
//    private float _yVelocity;
//    private float _forwardSpeed;
//    private bool _isGrounded;
//    private bool _isCrouching;
//    private bool _isJumping;
    /// <summary>
    /// Is the player falling?
    /// </summary>
    private bool _isFalling;
//    private bool _isClimbing;

    /// <summary>
    /// Velocity along the y world axis
    /// </summary>
    public float yVelocity{
        get { return GetFloat(FLOAT_Y_VELOCITY); }
        set { SetFloat(FLOAT_Y_VELOCITY, value); }
    }

    /// <summary>
    /// Speed of the player along the XZ world plane
    /// </summary>
    public float forwardSpeed{
        get { return GetFloat(FLOAT_FORWARD_SPEED); }
        set { SetFloat(FLOAT_FORWARD_SPEED, value); }
    }

    /// <summary>
    /// Is the player crouching?
    /// </summary>
    public bool isCrouching{
        get { return GetBool(BOOL_IS_CROUCHING); }
        set { SetBool(BOOL_IS_CROUCHING, value); }
    }

    /// <summary>
    /// Is the player jumping?
    /// </summary>
    public bool isJumping{
        get { return GetBool(BOOL_IS_JUMPING); }
        set { SetBool(BOOL_IS_JUMPING, value); }
    }

    /// <summary>
    /// Is the player touching/close enough to the ground?
    /// </summary>
    public bool isGrounded{
        get { return GetBool(BOOL_IS_GROUNDED); }
        set { SetBool(BOOL_IS_GROUNDED, value); }
    }

    /// <summary>
    /// Is the player falling?
    /// </summary>
    public bool isFalling{
        get { return _isFalling; }
        set
        {
            if (!_isFalling && value) {
                Trigger(TRIGGER_FALLING_IDLE);
            }
            else if (!value) {
                Trigger(TRIGGER_FALLING_IDLE, false);
            }

            _isFalling = value;
        }
    }

    /// <summary>
    /// Is the player climbing a ledge?
    /// </summary>
    public bool isClimbing{
        get { return GetBool(BOOL_IS_CLIMBING); }
        set
        {
            var _isClimbing = isClimbing;
            if (!_isClimbing && value) {
                animator.applyRootMotion = true;
                Debug.Log("Root motion TRUE");
            }
            else if (_isClimbing && !value) {
                animator.applyRootMotion = false;
                Debug.Log("Root motion FALSE");

            }

            SetBool(BOOL_IS_CLIMBING, value);
        }
    }

    /// <summary>
    /// Distance of the feet of the player from the ground
    /// </summary>
    public float distanceToGround{
        get { return GetFloat(FLOAT_DISTANCE_TO_GROUND); }
        set { SetFloat(FLOAT_DISTANCE_TO_GROUND, value); }
    }

    /// <summary>
    /// Was the player hanging in the last frame?
    /// </summary>
    private bool wasHanging;

    /// <summary>
    /// Was the player jumping in the last frame?
    /// </summary>
    private bool wasJumping;

    /// <summary>
    /// Current speed (walk speed, jog speed,...)
    /// </summary>
    [HideInInspector] public float currentSpeed;

    /// <summary>
    /// Speed of the player when jogging
    /// </summary>
    [HideInInspector] public float jogSpeed;

    /// <summary>
    /// Speed of the player when walking
    /// </summary>
    [HideInInspector] public float walkSpeed;

    /// <summary>
    /// Is the player shimmying right?
    /// </summary>
    private bool _isShimmyRight;

    /// <summary>
    /// Is the player shimmying right?
    /// </summary>
    public bool isShimmyRight{
        get { return _isShimmyRight; }
        set
        {
            animator.applyRootMotion = value;
            _isShimmyRight = value;
        }
    }

    /// <summary>
    /// Was the player shimmying right in the last frame?
    /// </summary>
    private bool wasShimmyRight;

    /// <summary>
    /// Was the player shimmying left in the last frame?
    /// </summary>
    private bool wasShimmyLeft;

    /// <summary>
    /// Is the player shimmying left?
    /// </summary>
    private bool _isShimmyLeft;

    public bool isStrafingLeft{
        get { return GetBool(BOOL_IS_STRAFING_LEFT); }
        set { SetBool(BOOL_IS_STRAFING_LEFT, value); }
    }

    public bool isStrafingRight{
        get { return GetBool(BOOL_IS_STRAFING_RIGHT); }
        set { SetBool(BOOL_IS_STRAFING_RIGHT, value); }
    }

    /// <summary>
    /// Is the player shimmying left?
    /// </summary>
    public bool isShimmyLeft{
        get { return _isShimmyLeft; }
        set
        {
            animator.applyRootMotion = value;
            _isShimmyLeft = value;
        }
    }

    /// <summary>
    /// Returns the index of the current animator state
    /// </summary>
    public int currentState{
        get
        {
            for (var stateIndex = 0; stateIndex < STATES.Length; stateIndex++) {
                var stateName = STATES[stateIndex];
                if (animator.GetCurrentAnimatorStateInfo(0).IsName(stateName)) {
                    return stateIndex;
                }
            }

            return -1;
        }
    }

    /// <summary>
    /// A blend float where 0 = idle, 1 = walking, 2 = jog speed
    /// </summary>
    private float idleWalkBlend{
        get { return GetFloat(FLOAT_IDLE_WALK_BLEND); }
        set { SetFloat(FLOAT_IDLE_WALK_BLEND, value); }
    }

    /// <summary>
    /// A blend float where 0 = jumping from stand, 1 = jumping forward from jog
    /// </summary>
    private float jumpBlend{
        get { return GetFloat(FLOAT_JUMP_BLEND); }
        set { SetFloat(FLOAT_JUMP_BLEND, value); }
    }

    /// <summary>
    /// Is the animator in the <paramref name="stateIndex"/>?
    /// </summary>
    /// <param name="stateIndex">Index of the state</param>
    private bool IsInState(int stateIndex) => animator.GetCurrentAnimatorStateInfo(0).IsName(STATES[stateIndex]);

    /// <summary>
    /// Triggers the trigger referred to by <paramref name="triggerIndex" /> by default, or resets it
    /// </summary>
    /// <param name="triggerIndex">Index of the trigger</param>
    /// <param name="setOrReset">True by default sets the trigger, false resets it</param>
    private void Trigger(int triggerIndex, bool setOrReset = true){
        if (setOrReset)
            animator.SetTrigger(TRIGGERS[triggerIndex]);
        else
            animator.ResetTrigger(TRIGGERS[triggerIndex]);
    }

    /// <summary>
    /// Get the boolean parameter referred to by <paramref name="boolIndex"/>
    /// </summary>
    /// <param name="boolIndex">Index of the boolean</param>
    /// <returns>The value of the boolean in the animator parameters</returns>
    private bool GetBool(int boolIndex) => animator.GetBool(BOOLS[boolIndex]);

    /// <summary>
    /// Get the float parameter referred to by <paramref name="floatIndex"/>
    /// </summary>
    /// <param name="floatIndex">Index of the float</param>
    /// <returns>The value of the float in the animator parameters</returns>
    private float GetFloat(int floatIndex) => animator.GetFloat(FLOATS[floatIndex]);

    /// <summary>
    /// Sets the boolean referred to by <paramref name="boolIndex"/>
    /// </summary>
    /// <param name="boolIndex">Index of the boolean</param>
    /// <param name="value">New value of the boolean</param>
    private void SetBool(int boolIndex, bool value) => animator.SetBool(BOOLS[boolIndex], value);

    /// <summary>
    /// Sets the float referred to by <paramref name="floatIndex"/>
    /// </summary>
    /// <param name="floatIndex">Index of the float</param>
    /// <param name="value">New value of the float</param>
    private void SetFloat(int floatIndex, float value) => animator.SetFloat(FLOATS[floatIndex], value);

    private void Awake(){
        animator = GetComponent<Animator>();
    }

    private void Start(){

//        var bracedHangShimmyRightBehaviour = mAnimator.GetBehaviour<BracedHangShimmyRightBehaviour>();
//        var bracedHangShimmyLeftBehaviour = mAnimator.GetBehaviour<BracedHangShimmyLeftBehaviours>();
//        bracedHangShimmyRightBehaviour.OnAnimationEnd += () =>
//        {
//            isShimmyRight = false;
//            OnBracedShimmyAnimationEnd?.Invoke();
//        };
//        bracedHangShimmyLeftBehaviour.OnAnimationEnd += () =>
//        {
//            isShimmyLeft = false;
//            OnBracedShimmyAnimationEnd?.Invoke();
//        };
    }

    private void Update(){
        SetBlends();
        ProcessStateCases();
        SetLastFrameStates();
    }

    /// <summary>
    /// Processes state transitions case by case
    /// </summary>
    private void ProcessStateCases(){
        switch (currentState) {
            case STATE_STANDING:
            {
                if (!wasHanging && isHanging) {
                    switch (hangType) {
                        case Point.HangType.BracedHang:
                            Trigger(TRIGGER_BRACED_HANG);
                            break;
                        case Point.HangType.FreeHang:
                            Trigger(TRIGGER_FREE_HANG_HANG);
                            break;
                    }
                }

                break;
            }
            case STATE_BRACED_HANG:
            {
                if (!wasShimmyRight && isShimmyRight) {
                    switch (hangType) {
                        case Point.HangType.BracedHang:
                            Debug.Log("Trigger RIGHT!");
                            Trigger(TRIGGER_BRACED_SHIMMY_RIGHT);
                            break;
                        case Point.HangType.FreeHang:
//                            Trigger(TRIGGER_FREE_HANG_SHIMMY_RIGHT);
                            Trigger(TRIGGER_FREE_HANG_HANG);
                            break;
                    }
                }

                if (!wasShimmyLeft && isShimmyLeft) {
                    switch (hangType) {
                        case Point.HangType.BracedHang:
                            Trigger(TRIGGER_BRACED_SHIMMY_LEFT);
                            break;
                        case Point.HangType.FreeHang:
//                            Trigger(TRIGGER_FREE_HANG_SHIMMY_LEFT);
                            Trigger(TRIGGER_FREE_HANG_HANG);
                            break;
                    }
                }

                break;
            }
            case STATE_FREE_HANG:
            {
                if (wasHanging && !isHanging) {
                    Trigger(TRIGGER_FALLING_IDLE);
                }

                if (!wasShimmyRight && isShimmyRight) {
                    switch (hangType) {
                        case Point.HangType.BracedHang:
//                            Trigger(TRIGGER_BRACED_SHIMMY_RIGHT);
                            Trigger(TRIGGER_BRACED_HANG);
                            break;
                        case Point.HangType.FreeHang:
                            Debug.Log("right");
                            Trigger(TRIGGER_FREE_HANG_SHIMMY_RIGHT);
                            break;
                    }
                }

                if (!wasShimmyLeft && isShimmyLeft) {
                    switch (hangType) {
                        case Point.HangType.BracedHang:
                            Trigger(TRIGGER_BRACED_HANG);
//                            Trigger(TRIGGER_BRACED_SHIMMY_LEFT);
                            break;
                        case Point.HangType.FreeHang:
                            Debug.Log("left");
                            Trigger(TRIGGER_FREE_HANG_SHIMMY_LEFT);
                            break;
                    }
                }
            }
                break;
        }
    }

    /// <summary>
    /// Sets the blends based on multiple factors (e.g. sets locomotion blend tree based on player's current speed)
    /// </summary>
    private void SetBlends(){
        if (currentSpeed >= jogSpeed) {
            idleWalkBlend = 2;
            jumpBlend = 1;
        }
        else if (currentSpeed >= walkSpeed) {
            idleWalkBlend = 1;
            jumpBlend = 0;
        }
        else {
            jumpBlend = 0;
            idleWalkBlend = 0;
        }
    }

    /// <summary>
    /// Keeps in memory this frame's state variables for the next frame
    /// </summary>
    private void SetLastFrameStates(){
//        wasCrouching = isCrouching;
        wasJumping = isJumping;
        wasHanging = isHanging;
        wasShimmyRight = isShimmyRight;
        wasShimmyLeft = isShimmyLeft;
    }

    /// <summary>
    /// Gets called during IK pass
    /// </summary>
    /// <param name="layerIndex"></param>
    private void OnAnimatorIK(int layerIndex){
        if (isHanging) { }
    }
}