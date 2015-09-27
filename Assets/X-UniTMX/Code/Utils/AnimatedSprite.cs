/*! 
 * X-UniTMX: A tiled map editor file importer for Unity3d
 * https://bitbucket.org/Chaoseiro/x-unitmx
 * 
 * Copyright 2013-2014 Guilherme "Chaoseiro" Maia
 *           2014 Mario Madureira Fontes
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace X_UniTMX.Utils
{
	/// <summary>
	/// Animation Loop mode
	/// </summary>
	public enum SpriteAnimationMode
	{
		/// <summary>
		/// One-time only forward animation
		/// </summary>
		FORWARD,
		/// <summary>
		/// One-time only backward (inverted) animation
		/// </summary>
		BACKWARD,
		/// <summary>
		/// Loops this animation in a ping-pong loop
		/// </summary>
		PING_PONG,
		/// <summary>
		/// Simple animation Loop
		/// </summary>
		LOOP,
		/// <summary>
		/// Simple animation Loop in reversed SpriteFrame order
		/// </summary>
		REVERSE_LOOP
	}

	/// <summary>
	/// A simple Sprite Frame, with reference to a Sprite and it's duration
	/// </summary>
	[System.Serializable]
	public class SpriteFrame
	{
		/// <summary>
		/// Sprite reference
		/// </summary>
		public Sprite Sprite;
		/// <summary>
		/// Frame's duration in milliseconds
		/// </summary>
		public float Duration;

		/// <summary>
		/// Creates a Sprite Frame
		/// </summary>
		/// <param name="sprite">SpriteFrames' Sprite reference</param>
		/// <param name="duration">Duration of this frame in milliseconds</param>
		public SpriteFrame(Sprite sprite, float duration)
		{
			Sprite = sprite;
			Duration = duration;
		}

		/// <summary>
		/// Gets this frame duration in seconds
		/// </summary>
		/// <returns>Frame duration in seconds</returns>
		public float GetDurationInSeconds()
		{
			return Duration / 1000.0f;
		}
	}

	/// <summary>
	/// A simple Animated Sprite utility you can use to animate sprite frames.
	/// </summary>
	[RequireComponent(typeof(SpriteRenderer))]
	public class AnimatedSprite : MonoBehaviour
	{
		/// <summary>
		/// This Animation's SpriteAnimationMode
		/// </summary>
		[Tooltip("Animation Loop Type")]
		public SpriteAnimationMode AnimationMode = SpriteAnimationMode.LOOP;
		/// <summary>
		/// This Animation's speed scale. 2 = 2 times faster, 0.5 = half-speed
		/// </summary>
		[Tooltip("Animation speed scale")]
		public float AnimationSpeedScale = 1;
		/// <summary>
		/// true to automatically play this AnimatedSprite when it enter scene
		/// </summary>
		[Tooltip("Automatically starts animation when this GameObject enters the scene?")]
		public bool PlayAutomatically = true;

		[Tooltip("Automatically disables this GameObject when the animation finishes if set to true")]
		/// <summary>
		/// Automatically disables this GameObject when the animation finishes if set to true
		/// </summary>
		public bool AutoRemoveOnFinish = true;

		/// <summary>
		/// If set to True, and AutoRemoveOnFinish is also true, the object will be destroyed, instead of only deactivated;
		/// </summary>
		public bool DestroyOnAutoRemove = false;


		bool _canAnimate = true;
		/// <summary>
		/// If false, this animation has ended or has been stopped, else it is still running.
		/// </summary>
		public bool CanAnimate
		{
			get { return _canAnimate; }
			protected set { _canAnimate = value; }
		}

		/// <summary>
		/// List of this AnimatedSprite's SpriteFrame
		/// </summary>
		public List<SpriteFrame> _spriteFrames;

		int _currentFrame = 0;
		/// <summary>
		/// Current Animation Frame
		/// </summary>
		public int CurrentFrame
		{
			get { return _currentFrame; }
			protected set { _currentFrame = value; }
		}
		float _timer = 0;

		SpriteRenderer _thisRenderer;

		int _pingPongDirection = 1;
		/// <summary>
		/// PingPong loop current direction
		/// 1 = Forward
		/// 2 = Backward
		/// </summary>
		public int PingPongDirection
		{
			get { return _pingPongDirection; }
			protected set { _pingPongDirection = value; }
		}

		int _loopCount = 0;
		/// <summary>
		/// Current loop count of a looping animation
		/// </summary>
		public int LoopCount
		{
			get { return _loopCount; }
			protected set { _loopCount = value; }
		}

		#region Delegates you can hook to
		/// <summary>
		/// Called when Animation Starts/Finishes
		/// </summary>
		/// <param name="animatedSprite">The animation that fired this callback</param>
		public delegate void StartEndDelegate(AnimatedSprite animatedSprite);
		/// <summary>
		/// Called when animation starts
		/// </summary>
		public event StartEndDelegate Init;
		/// <summary>
		/// Called when animation ends
		/// </summary>
		public event StartEndDelegate End;

		/// <summary>
		/// Called when an animation loop cycle completes
		/// </summary>
		/// <param name="animatedSprite">The animation that fired this callback</param>
		/// <param name="loopCount">Current loop count</param>
		public delegate void CompleteDelegate(AnimatedSprite animatedSprite, int loopCount);
		/// <summary>
		/// Called when a looping animation completes a cycle
		/// </summary>
		public event CompleteDelegate Complete;
		#endregion

		/// <summary>
		/// Adds a SpriteFrame to this animation list
		/// </summary>
		/// <param name="frame">The sprite reference</param>
		/// <param name="duration">Duration of this frame in milliseconds</param>
		public void AddSpriteFrame(Sprite frame, float duration)
		{
			if (_spriteFrames == null)
				_spriteFrames = new List<SpriteFrame>();
			_spriteFrames.Add(new SpriteFrame(frame, duration));
		}

		// Use this for initialization
		void OnEnable()
		{
			_thisRenderer = GetComponent<Renderer>() as SpriteRenderer;
			_canAnimate = false;

			if (AnimationMode == SpriteAnimationMode.BACKWARD || AnimationMode == SpriteAnimationMode.REVERSE_LOOP)
			{
				_currentFrame = _spriteFrames.Count - 1;
				_thisRenderer.sprite = _spriteFrames[_currentFrame].Sprite;
			}

			if (PlayAutomatically)
				Play();
		}

		// Update is called once per frame
		void Update()
		{
			if (_canAnimate)
			{
				_timer += Time.deltaTime * AnimationSpeedScale;
				if (_timer >= _spriteFrames[_currentFrame].GetDurationInSeconds())
				{
					_timer = 0;
					switch (AnimationMode)
					{
						case SpriteAnimationMode.FORWARD:
							_currentFrame++;
							if (_currentFrame > _spriteFrames.Count - 1)
								Stop();
							break;
						case SpriteAnimationMode.BACKWARD:
							_currentFrame--;
							if (_currentFrame < 0)
								Stop();
							break;
						case SpriteAnimationMode.LOOP:
							_currentFrame++;
							if (_currentFrame > _spriteFrames.Count - 1)
							{
								_currentFrame = 0;
								if (Complete != null) Complete(this, _loopCount);
							}
							break;
						case SpriteAnimationMode.REVERSE_LOOP:
							_currentFrame--;
							if (_currentFrame < 0)
							{
								_currentFrame = _spriteFrames.Count - 1;
								if (Complete != null) Complete(this, _loopCount);
							}
							break;
						case SpriteAnimationMode.PING_PONG:
							_currentFrame += _pingPongDirection;
							if (_currentFrame >= _spriteFrames.Count - 1 || _currentFrame < 1)
							{
								_pingPongDirection = -_pingPongDirection;
								if (Complete != null) Complete(this, _loopCount);
							}
							break;
					}
					if (_currentFrame < _spriteFrames.Count && _currentFrame > -1)
						_thisRenderer.sprite = _spriteFrames[_currentFrame].Sprite;
				}
			}
		}

		/// <summary>
		/// Resets this animation, preparing it for a new play.
		/// </summary>
		public void Reset()
		{
			_canAnimate = true;
			_timer = 0;
			_pingPongDirection = 1;
			if (AnimationMode == SpriteAnimationMode.BACKWARD || AnimationMode == SpriteAnimationMode.REVERSE_LOOP)
				_currentFrame = _spriteFrames.Count - 1;
			else
				_currentFrame = 0;
			if (_thisRenderer && _spriteFrames != null)
				_thisRenderer.sprite = _spriteFrames[_currentFrame].Sprite;
		}

		/// <summary>
		/// Starts this animation, calling Init event
		/// </summary>
		public void Play()
		{
			Reset();
			if (Init != null) Init(this);
		}

		/// <summary>
		/// Completely stops this animation, calling End event. If AutoRemoveOnFinish is enabled this GameObject will also be disabled.
		/// </summary>
		public void Stop()
		{
			_canAnimate = false;
			if (End != null) End(this);
			if (AutoRemoveOnFinish)
			{
				if (DestroyOnAutoRemove)
					Destroy(gameObject);
				else
					gameObject.SetActive(false);
			}
		}

		/// <summary>
		/// Pause this animation
		/// </summary>
		public void Pause()
		{
			_canAnimate = false;
		}

		/// <summary>
		/// Resume this animation
		/// </summary>
		public void Resume()
		{
			// Can we resume?
			switch (AnimationMode)
			{
				case SpriteAnimationMode.FORWARD:
					if (_currentFrame >= _spriteFrames.Count - 1)
						return;
					break;
				case SpriteAnimationMode.BACKWARD:
					if (_currentFrame < 1)
						return;
					break;
				default:
					break;
			}
			_canAnimate = true;
		}
	}
}
