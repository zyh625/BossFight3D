using UnityEngine;

namespace BossFight.Player
{
    /// <summary>
    /// 切换武器形态时，显示对应的武器外观、隐藏其余的。
    /// 它只监听形态变化事件——玩法层不知道"剑的模型是哪个物体"。换上正式模型后，把三个引用指向新的物体即可。
    /// </summary>
    public sealed class WeaponFormVisuals : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private GameObject sword;
        [SerializeField] private GameObject dart;
        [SerializeField] private GameObject fists;

        private void OnEnable()
        {
            player.FormChanged += Apply;
        }

        private void OnDisable()
        {
            player.FormChanged -= Apply;
        }

        private void Start()
        {
            Apply(player.Form);
        }

        private void Apply(WeaponForm form)
        {
            sword.SetActive(form == WeaponForm.Sword);
            dart.SetActive(form == WeaponForm.Dart);
            fists.SetActive(form == WeaponForm.Fist);
        }
    }
}
