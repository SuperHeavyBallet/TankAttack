using UnityEngine;
namespace TankAttack
{
    // Apply the selected tuning before health, weapons and audio initialize themselves.
    [DefaultExecutionOrder(-200)]
    public sealed class PlayerTankConfiguration : MonoBehaviour
    {
        public TankDefinition defaultTank;
        void Awake()
        {
            var tank = TankSelection.Selected != null ? TankSelection.Selected : defaultTank;
            if (tank == null || tank.vehicle == null || tank.cannon == null) return;
            GetComponent<Damageable>().settings = tank.vehicle;
            GetComponent<TankMotor>().settings = tank.vehicle;
            GetComponent<TurretAim>().settings = tank.vehicle;
            GetComponent<PlayerTankInput>().cannon.settings = tank.cannon;
            if (tank.engineAudio != null) GetComponent<VehicleAudio>().engineSettings = tank.engineAudio;
        }
    }
}
