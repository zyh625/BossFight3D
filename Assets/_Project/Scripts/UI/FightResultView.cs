using BossFight.Game;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BossFight.UI
{
    [DefaultExecutionOrder(120)]
    public sealed class FightResultView : MonoBehaviour
    {
        [SerializeField] private FightDirector fight;
        [SerializeField] private GameObject panel;
        [SerializeField] private Text title;
        [SerializeField] private Text detail;
        [SerializeField] private Button restart;
        [SerializeField] private EventSystem eventSystem;

        private bool shown;

        private void OnEnable() => restart.onClick.AddListener(Restart);
        private void OnDisable() => restart.onClick.RemoveListener(Restart);

        private void LateUpdate()
        {
            if (shown || !fight.ResultReady)
            {
                return;
            }

            bool victory = fight.Result == FightResult.Victory;
            title.text = victory ? "胜利" : "战败";
            title.color = victory ? new Color(0.42f, 1f, 0.9f) : new Color(1f, 0.48f, 0.5f);
            detail.text = victory ? "暗影剑客已击败" : "你在交战中倒下";
            panel.SetActive(true);
            eventSystem.gameObject.SetActive(true);
            eventSystem.SetSelectedGameObject(restart.gameObject);
            shown = true;
        }

        private void Restart()
        {
            fight.Restart();
            restart.interactable = !fight.IsRestarting;
        }
    }
}
