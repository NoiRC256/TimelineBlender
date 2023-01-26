using System;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace NekoNeko
{
    /// <summary>
    /// Provides the ability to fade a playable director's animation output weight over time.
    /// <para>Each <see cref="TimelineBlender"/> instance handles one playable director.</para>
    /// </summary>
    public class TimelineBlender
    {
        private enum FadeMode
        {
            In,
            Out,
        }

        public AnimationPlayableOutput AnimationPlayableOutput => _output;

        #region Fields
        private AnimationPlayableOutput _output;
        private bool _isFading;
        private float _fadeSpeed;
        #endregion

        #region Events
        public event Action FadeOutCompleted;
        #endregion

        #region Control
        /// <summary>
        /// Setup the playable director for fading.
        /// </summary>
        /// <param name="playableDirector"></param>
        public void Setup(PlayableDirector playableDirector)
        {
            if (!playableDirector.playableGraph.IsValid())
            {
                playableDirector.RebuildGraph();
                ConnectDummyOutput(playableDirector);
            }
        }

        /// <summary>
        /// Update tick.
        /// </summary>
        /// <param name="deltaTime"></param>
        public void OnUpdate(float deltaTime)
        {
            UpdateFade(deltaTime);
        }

        /// <summary>
        /// Start to fade in for the specified duration.
        /// </summary>
        /// <param name="duration"></param>
        public void FadeIn(float duration)
        {
            StopFade();
            if (duration == 0f)
            {
                SetWeight(0f);
                StartFade(FadeMode.In, duration);
                return;
            }
            else SetWeight(1f);
        }

        /// <summary>
        /// Start to fade out for the specified duration.
        /// <para><see cref="FadeOutCompleted"/> will be invoked when the weight becomes zero.</para>
        /// </summary>
        /// <param name="duration"></param>
        public void FadeOut(float duration)
        {
            StopFade();
            if (duration == 0f)
            {
                SetWeight(0f);
                FadeOutCompleted?.Invoke();
                return;
            }
            else StartFade(FadeMode.Out, duration);
        }
        #endregion

        #region Fading
        private void StartFade(FadeMode fadeMode, float duration)
        {
            switch (fadeMode)
            {
                case FadeMode.In:
                    _fadeSpeed = 1f / duration;
                    break;
                case FadeMode.Out:
                    _fadeSpeed = -1f / duration;
                    break;
            }
            _isFading = true;
        }

        private void UpdateFade(float deltaTime)
        {
            if (!_isFading) return;

            float weight = GetWeight();
            weight += _fadeSpeed * deltaTime;
            if (weight >= 1f)
            {
                // Fade in complete.
                SetWeight(1f);
                StopFade();
                return;
            }
            else if (weight <= 0f)
            {
                // Fade out complete.
                SetWeight(0f);
                StopFade();
                UnityEngine.Debug.Log("Fade out complete.");
                FadeOutCompleted?.Invoke();
                return;
            }
            else SetWeight(weight);
        }

        private void StopFade()
        {
            _isFading = false;
            _fadeSpeed = 0f;
        }
        #endregion

        #region Helpers
        /// <summary>
        /// Redirect the playable director graph's output to a new dummy output so we can control its weight.
        /// </summary>
        /// <param name="playableDirector"></param>
        private void ConnectDummyOutput(PlayableDirector playableDirector)
        {
            if (!playableDirector.playableGraph.IsValid()) return;
            var oldOutput = (AnimationPlayableOutput)playableDirector.
                playableGraph.GetOutputByType<AnimationPlayableOutput>(0);

            if (oldOutput.IsOutputValid() && oldOutput.GetTarget() != null)
            {
                _output = AnimationPlayableOutput.Create(
                    playableDirector.playableGraph, "Dummy Output", oldOutput.GetTarget());
                var sourcePlayable = oldOutput.GetSourcePlayable();
                int outputPort = oldOutput.GetSourceOutputPort();
                oldOutput.SetSourcePlayable(Playable.Null, -1);
                _output.SetSourcePlayable(sourcePlayable, outputPort);
                _output.SetWeight(1f);
            }
        }

        public float GetWeight()
        {
            return _output.IsOutputValid() ? _output.GetWeight() : 0f;
        }

        public void SetWeight(float weight)
        {
            if (_output.IsOutputValid()) _output.SetWeight(weight);
        }
        #endregion
    }
}
