using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Detects screen size changes and invokes <see cref="OnWindowSizeChange"/> event.
/// </summary>
public class ScreenSizeManager : MonoBehaviourSingleton<ScreenSizeManager>
{
    /// <summary>
    /// Invoked on screen related event. (One <see href="https://docs.unity3d.com/ScriptReference/Vector2.html">Vector2</see> argument version of <see href="https://docs.unity3d.com/ScriptReference/Events.UnityEvent.html">UnityEvent</see>.)
    /// </summary>
    /// <typeparam name="Vector2">Window size.</typeparam>
    [System.Serializable] public class ScreenEvent : UnityEvent<Vector2> { }

    /// <summary>
    /// Invoked when the game screen size changed.
    /// </summary>
    public ScreenEvent OnWindowSizeChange;

    private Vector2 windowSize = new Vector2(0, 0);

    // Update is called once per frame
    void Update()
    {
        if (windowSize.x != Screen.width || windowSize.y != Screen.height)
        {
            windowSize = new Vector2(Screen.width, Screen.height);
            OnWindowSizeChange.Invoke(windowSize);
        }
    }
}
