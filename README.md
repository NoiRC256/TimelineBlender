# TimelineBlender


# Usage
A `TimelineBlender` instance provides the ability to fade in and fade out a playable director's animation output.

### Setup
Create a timeline blender instance.
```csharp
timelineBlender = new TimelineBlender(playableDirector);
```
Tick the timeline blender.
```csharp
private void Update()
{
    timelineBlender.OnUpdate(Time.deltaTime);
}
```

### Fade in
Set the desired `_playableDirector.time` to start at. The timeline automatically starts or resumes when it begins to fade in.
```csharp
timelineBlender.FadeIn(float duration)
```
When fading in a timeline for the first time, the timeline blender redirects the playable graph's output to a dummy output, so that it can control its weight. This will only happen once, as long as the playable graph is not destroyed (e.g. by `playableDirector.Stop()` or `playableDirector.Play()`)

### Fade out
```csharp
timelineBlender.FadeOut(float duration)
```
When the weight reaches zero, the timeline will be paused. Then, a `timelineBlender.FadeOutCompleted` event will be invoked. You can listen to this event to trigger your own methods.

### Note
When fading out and back into the same timeline, having animation clips with root motion may cause the corresponding gameobject to incorrectly reset its position. In this case, it is recommended to set the fade duration to zero.

<details>
    <summary>Usage Example</summary>

`MonoTimelineSkill` is a component attached to a skill prefab that has a playable director. A high-level skill controller maintains a collection of skill instances in the scene. 

When a skill is played, the corresponding `MonoTimelineSkill` will activate its gameobject and fade in its timeline over time. 

When a skill is stopped, the corresponding `MonoTimelineSkill` will fade out its timeline over time, and deactive its gameobject when the fade out completes.

```csharp
    /// <summary>
    /// MonoBehaviour component that controls a timeline skill.
    /// </summary>
    [RequireComponent(typeof(PlayableDirector))]
    public class MonoTimelineSkill : MonoSkill
    {
        [SerializeField] private PlayableDirector _playableDirector;

        private TimelineBlender _timelineBlender;

        #region MonoBehaviour
        private void OnValidate()
        {
            if (_playableDirector == null) TryGetComponent(out _playableDirector);
        }

        private void Awake()
        {
            OnValidate();
            _timelineBlender = new TimelineBlender(_playableDirector);
        }

        private void OnEnable()
        {
            _timelineBlender.FadeOutCompleted += OnFadeOutCompleted;
        }

        private void OnDisable()
        {
            _timelineBlender.FadeOutCompleted -= OnFadeOutCompleted;
        }

        private void Update()
        {
            _timelineBlender.OnUpdate(Time.deltaTime);
        }
        #endregion

        public override void Play(float fadeInDuration = 0f)
        {
            this.gameObject.SetActive(true);
            _playableDirector.time = 0f;
            _timelineBlender.FadeIn(fadeInDuration);
            _playableDirector.stopped += OnPlayableDirectorStopped;
        }

        public override void Stop(float fadeOutDuration = 0f)
        {
            _timelineBlender.FadeOut(fadeOutDuration);
        }

        private void OnFadeOutCompleted()
        {
            _playableDirector.stopped -= OnPlayableDirectorStopped;
            this.gameObject.SetActive(false);
        }

        private void OnPlayableDirectorStopped(PlayableDirector playableDirector)
        {
            Complete();
        }

        /// <summary>
        /// Setup timeline track bindings using the specified track name dictionary.
        /// </summary>
        /// <param name="bindingDict"></param>
        public void SetupBindings(Dictionary<string, UnityEngine.Object> bindingDict)
        {
            TimelineAsset timelineAsset = (TimelineAsset)_playableDirector.playableAsset;
            var outputTracks = timelineAsset.GetOutputTracks();
            foreach (var track in outputTracks)
            {
                if (bindingDict.TryGetValue(track.name, out UnityEngine.Object obj))
                {
                    Debug.Log("[Mono Timeline Skill] Binding timeline track: " + track.name);
                    _playableDirector.SetGenericBinding(track, obj);
                }
            }
        }
    }
```
</details>


