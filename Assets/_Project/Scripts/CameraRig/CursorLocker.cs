using UnityEngine;
using UnityEngine.InputSystem;

namespace BossFight.CameraRig
{
    /// <summary>进入游戏时锁定并隐藏鼠标；Esc 释放，点击画面重新锁定。</summary>
    public sealed class CursorLocker : MonoBehaviour
    {
        private void OnEnable()
        {
            SetLocked(true);
        }

        private void OnDisable()
        {
            SetLocked(false);
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                SetLocked(false);
            }
            else if (Cursor.lockState != CursorLockMode.Locked
                     && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                SetLocked(true);
            }
        }

        private static void SetLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
