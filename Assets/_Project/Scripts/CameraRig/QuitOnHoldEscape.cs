using UnityEngine;
using UnityEngine.InputSystem;

namespace BossFight.CameraRig
{
    /// <summary>
    /// 按住 Esc 一小段时间退出游戏。短按 Esc 只是释放鼠标（CursorLocker），
    /// 用"按住"来区分，避免误触直接关掉游戏。只在打出来的包里起作用，编辑器里不会退出 Play 模式。
    /// </summary>
    public sealed class QuitOnHoldEscape : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float holdSeconds = 1f;

        private float held;

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.escapeKey.isPressed)
            {
                held = 0f;
                return;
            }

            // 用不受时间缩放影响的时间：以后加入慢动作时，退出所需的按住时长不变
            held += Time.unscaledDeltaTime;
            if (held >= holdSeconds)
            {
                Application.Quit();
            }
        }
    }
}
