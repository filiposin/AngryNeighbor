using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class RandomTransformReplacer : MonoBehaviour
{
    public enum TransformMode
    {
        Position,
        Rotation,
        PositionAndRotation
    }

    [System.Serializable]
    public class TargetEntry
    {
        [Tooltip("Куда переместить (позиция и/или ротация берутся из этого Transform)")]
        public Transform target;

        [Tooltip("Сработает сразу после переключения на этот target")]
        public UnityEvent OnTransform;

        [Tooltip("Если > 0 — переопределяет общую длительность для этого элемента")]
        public float overrideDuration = -1f;

        [Tooltip("Опционально: плавный переход вместо резкой телепортации")]
        public bool smoothTransition = false;

        [Tooltip("Длительность плавного перехода (если smoothTransition == true)")]
        public float smoothTime = 0.5f;
    }

    [Header("Список целей")]
    public List<TargetEntry> targets = new List<TargetEntry>();

    [Header("Общие настройки")]
    [Tooltip("Стандартное время (в секундах) перед переключением на следующий target")]
    public float defaultDuration = 2f;

    [Tooltip("Режим: менять позицию,/ротацию/или оба")]
    public TransformMode transformMode = TransformMode.PositionAndRotation;

    [Tooltip("Использовать локальные (Transform.localPosition/localRotation) или мировые значения")]
    public bool useLocalSpace = false;

    [Tooltip("Если true — стартует автоматически в Start()")]
    public bool startOnAwake = true;

    [Tooltip("Если true — порядок будет случайным, иначе по очереди")]
    public bool randomOrder = false;

    private Coroutine _cycleCoroutine;

    void Start()
    {
        if (startOnAwake)
            StartCycle();
    }

    void OnDisable()
    {
        StopCycle();
    }

    public void StartCycle()
    {
        if (_cycleCoroutine != null) return;
        _cycleCoroutine = StartCoroutine(CycleRoutine());
    }

    public void StopCycle()
    {
        if (_cycleCoroutine != null)
        {
            StopCoroutine(_cycleCoroutine);
            _cycleCoroutine = null;
        }
    }

    private IEnumerator CycleRoutine()
    {
        if (targets == null || targets.Count == 0)
            yield break;

        List<int> order = new List<int>(targets.Count);
        for (int i = 0; i < targets.Count; i++) order.Add(i);

        if (randomOrder)
            Shuffle(order);

        int idx = 0;
        while (true)
        {
            int entryIndex = order[idx];
            var entry = targets[entryIndex];

            if (entry == null || entry.target == null)
            {
                // пропускаем пустой элемент
                yield return new WaitForSeconds(GetDuration(entry));
                idx = NextIndex(idx, order.Count, randomOrder);
                continue;
            }

            if (entry.smoothTransition && entry.smoothTime > 0f)
            {
                yield return StartCoroutine(DoSmoothTransition(entry));
            }
            else
            {
                ApplyTransformInstant(entry.target);
            }

            // вызвать эвент (после применения трансформа)
            entry.OnTransform?.Invoke();

            // ждать заданное время (или переопределение у элемента)
            float wait = GetDuration(entry);
            if (wait > 0f)
                yield return new WaitForSeconds(wait);
            else
                yield return null;

            idx = NextIndex(idx, order.Count, randomOrder);
        }
    }

    private float GetDuration(TargetEntry entry)
    {
        if (entry != null && entry.overrideDuration > 0f) return entry.overrideDuration;
        return Mathf.Max(0f, defaultDuration);
    }

    private int NextIndex(int current, int count, bool random)
    {
        if (random)
            return Random.Range(0, count);
        return (current + 1) % count;
    }

    private void ApplyTransformInstant(Transform target)
    {
        if (useLocalSpace)
        {
            if (transformMode == TransformMode.Position || transformMode == TransformMode.PositionAndRotation)
                transform.localPosition = target.localPosition;
            if (transformMode == TransformMode.Rotation || transformMode == TransformMode.PositionAndRotation)
                transform.localRotation = target.localRotation;
        }
        else
        {
            if (transformMode == TransformMode.Position || transformMode == TransformMode.PositionAndRotation)
                transform.position = target.position;
            if (transformMode == TransformMode.Rotation || transformMode == TransformMode.PositionAndRotation)
                transform.rotation = target.rotation;
        }
    }

    private IEnumerator DoSmoothTransition(TargetEntry entry)
    {
        Transform tgt = entry.target;
        float t = 0f;
        float dur = Mathf.Max(0.0001f, entry.smoothTime);
        Vector3 startPos = useLocalSpace ? transform.localPosition : transform.position;
        Quaternion startRot = useLocalSpace ? transform.localRotation : transform.rotation;
        Vector3 endPos = useLocalSpace ? tgt.localPosition : tgt.position;
        Quaternion endRot = useLocalSpace ? tgt.localRotation : tgt.rotation;

        while (t < dur)
        {
            t += Time.deltaTime;
            float frac = Mathf.Clamp01(t / dur);
            if (transformMode == TransformMode.Position || transformMode == TransformMode.PositionAndRotation)
            {
                Vector3 p = Vector3.Lerp(startPos, endPos, frac);
                if (useLocalSpace) transform.localPosition = p; else transform.position = p;
            }

            if (transformMode == TransformMode.Rotation || transformMode == TransformMode.PositionAndRotation)
            {
                Quaternion q = Quaternion.Slerp(startRot, endRot, frac);
                if (useLocalSpace) transform.localRotation = q; else transform.rotation = q;
            }

            yield return null;
        }

        // на всякий случай выставим точные значения в конце
        if (useLocalSpace)
        {
            if (transformMode == TransformMode.Position || transformMode == TransformMode.PositionAndRotation)
                transform.localPosition = endPos;
            if (transformMode == TransformMode.Rotation || transformMode == TransformMode.PositionAndRotation)
                transform.localRotation = endRot;
        }
        else
        {
            if (transformMode == TransformMode.Position || transformMode == TransformMode.PositionAndRotation)
                transform.position = endPos;
            if (transformMode == TransformMode.Rotation || transformMode == TransformMode.PositionAndRotation)
                transform.rotation = endRot;
        }
    }

    private void Shuffle(List<int> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int j = Random.Range(i, list.Count);
            int tmp = list[i];
            list[i] = list[j];
            list[j] = tmp;
        }
    }
}
