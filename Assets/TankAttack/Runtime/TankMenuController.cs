using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace TankAttack
{
    public sealed class TankMenuController : MonoBehaviour
    {
        public TankRoster roster;
        public Button[] tankButtons;
        public Image[] cardBackgrounds,selectionBars;
        public Text[] cardNames,cardRoles,cardStats;
        public Text selectedName,selectedDescription,statLabels,statValues,status,confirmLabel;
        public Button confirmButton;
        public int SelectedIndex { get; private set; }
        public bool IsLoading { get; private set; }
        void Awake()
        {
            Cursor.visible=true; Cursor.lockState=CursorLockMode.None;
            CrossfadedEngineAudio.Existing?.StopEngine();
            for(int i=0;i<tankButtons.Length;i++)
            {
                int slot=i; tankButtons[i].onClick.AddListener(()=>Select(slot));
                var tank=roster.tanks[i];
                cardNames[i].text=tank.displayName; cardRoles[i].text=tank.description;
                cardStats[i].text=$"{tank.vehicle.health:0} HP     {tank.vehicle.forwardSpeed:0.#} m/s\n{tank.cannon.damage:0.#} cannon damage";
            }
            confirmButton.onClick.AddListener(Confirm);
            int selected=System.Array.IndexOf(roster.tanks,TankSelection.Selected);
            Select(selected>=0 ? selected : Mathf.Min(1,roster.tanks.Length-1));
        }
        void Start() { if(EventSystem.current!=null) EventSystem.current.SetSelectedGameObject(tankButtons[SelectedIndex].gameObject); }
        public void Select(int index)
        {
            if(IsLoading || index<0 || index>=roster.tanks.Length) return;
            SelectedIndex=index; var tank=roster.tanks[index];
            selectedName.text=tank.displayName; selectedName.color=tank.accent;
            selectedDescription.text=tank.description;
            statLabels.text="MAX HEALTH\nFORWARD / REVERSE\nHULL TURN RATE\nACCELERATION / BRAKING\nTURRET TURN RATE\nCANNON DAMAGE\nFIRING INTERVAL\nCANNON RANGE";
            var v=tank.vehicle; var w=tank.cannon;
            statValues.text=$"{v.health:0} HP\n{v.forwardSpeed:0.#} / {v.reverseSpeed:0.#} m/s\n{v.hullTurnSpeed:0.#} deg/s\n{v.acceleration:0.#} / {v.braking:0.#} m/s²\n{v.turretTurnSpeed:0.#} deg/s\n{w.damage:0.#}\n{w.interval:0.##} seconds\n{w.range:0.#} meters";
            for(int i=0;i<cardBackgrounds.Length;i++)
            {
                cardBackgrounds[i].color=i==index ? new Color(.16f,.22f,.28f) : new Color(.075f,.10f,.14f);
                selectionBars[i].color=i==index ? roster.tanks[i].accent : new Color(.17f,.21f,.26f);
            }
            status.text="Select your tank, then confirm deployment.";
            var navigation=confirmButton.navigation; navigation.selectOnUp=tankButtons[index]; confirmButton.navigation=navigation;
        }
        public void Confirm()
        {
            if(IsLoading) return;
            if(SceneUtility.GetBuildIndexByScenePath(roster.gameplayScene)<0)
            { status.text="Gameplay scene is missing from the build scene list."; return; }
            TankSelection.Select(roster.tanks[SelectedIndex]);
            IsLoading=true; confirmButton.interactable=false;
            foreach(var button in tankButtons) button.interactable=false;
            confirmLabel.text="DEPLOYING..."; status.text="Loading battlefield";
            StartCoroutine(Load());
        }
        IEnumerator Load()
        {
            yield return null;
            var operation=SceneManager.LoadSceneAsync(roster.gameplayScene);
            while(operation!=null && !operation.isDone) yield return null;
        }
    }
}
