using System;
using UnityEngine.Events;

[Serializable]
public class FloatEvent : UnityEvent<float> { }

[Serializable]
public class TwoFloatEvent : UnityEvent<float, float> { }

[Serializable]
public class BoolEvent : UnityEvent<bool> { }