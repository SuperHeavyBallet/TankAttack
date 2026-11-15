using UnityEngine;
using UnityEngine.EventSystems;
namespace TankAttack
{
    public sealed class TankCardFocus : MonoBehaviour, ISelectHandler
    {
        public TankMenuController menu;
        public int index;
        public void OnSelect(BaseEventData data) => menu.Select(index);
    }
}
