using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kickflip
{
    public class Kickflip : MonoBehaviour
    {
        public static Kickflip Instance
        {
            get
            {
                if (instance == null)
                {
                    var gameObject = new GameObject();
                    instance = gameObject.AddComponent<Kickflip>();
                    DontDestroyOnLoad(gameObject);
                }

                return instance;
            }
        }

        private static Kickflip instance;

        private List<object> _activeTweens;

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _activeTweens = new List<object>();
        }

        public Coroutine StartTween<T>(Tween<T> tween)
        {
            var active = _activeTweens.ToArray();
            foreach (var o in active)
            {
                if (o is Tween<T> t)
                {
                    if (t.Conflicts(tween))
                    {
                        t.Kill();
                        _activeTweens.Remove(o);
                    }
                }
            }

            _activeTweens.Add(tween);
            tween.OnComplete += () => _activeTweens.Remove(tween);
            return StartCoroutine(PlayTween(tween));
        }

        IEnumerator PlayTween<T>(Tween<T> tween)
        {
            while (!tween.IsComplete)
            {
                tween.Tick(Time.deltaTime);
                yield return new WaitForEndOfFrame();
            }
        }
    }

    public class Tween<T>
    {
        private Action<T> setter;
        private Func<T, T, float, T> lerp;
        private T start;
        private T end;
        private AnimationCurve ease;
        private float duration;
        public Action OnComplete;

        private Coroutine routine;

        private float t = 0f;

        public bool IsComplete => t >= 1f;

        public Tween(Action<T> setter, T start, T end, float duration, Func<T, T, float, T> lerp,
            Action onComplete = null,
            AnimationCurve ease = null)
        {
            this.start = start;
            this.end = end;
            this.setter = setter;
            this.lerp = lerp;
            this.duration = duration;
            OnComplete = onComplete;
            this.ease = ease;
        }

        public void Tick(float deltaTime)
        {
            if (IsComplete) return;
            t += deltaTime / duration;
            t = Mathf.Min(t, 1f);
            setter.Invoke(lerp.Invoke(start, end, ease?.Evaluate(t) ?? t));
            if (IsComplete) OnComplete?.Invoke();
        }

        public void Play()
        {
            routine = Kickflip.Instance.StartTween(this);
        }

        public bool Conflicts(Tween<T> target) => target.CheckSetter(setter);

        public bool CheckSetter(Action<T> setter) => this.setter == setter;

        public void Kill()
        {
            if (routine == null) return;
            Kickflip.Instance.StopCoroutine(routine);
        }
    }
}