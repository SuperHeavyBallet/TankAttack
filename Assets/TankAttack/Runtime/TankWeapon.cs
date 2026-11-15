using System.Collections.Generic;
using UnityEngine;
namespace TankAttack
{
    public sealed class TankWeapon : MonoBehaviour
    {
        public WeaponSettings settings;
        public Transform muzzle, breech;
        public bool TriggerHeld { get; set; }
        public float ReadyFraction => settings == null ? 0 : 1 - Mathf.Clamp01((nextShot - Time.time) / settings.interval);
        public IReadOnlyList<WeaponSettings> CollectedWeapons => collected;
        public int RemainingAmmo => AmmoFor(settings);
        public bool HasUnlimitedAmmo => settings != null && settings.capacity == WeaponSettings.UnlimitedCapacity;
        public string AmmoLabel => HasUnlimitedAmmo ? "Unlimited" : RemainingAmmo.ToString();
        readonly List<WeaponSettings> collected = new List<WeaponSettings>();
        readonly Dictionary<WeaponSettings,int> ammunition = new Dictionary<WeaponSettings,int>();
        WeaponSettings defaultSettings;
        float nextShot;
        Damageable owner;
        VehicleAudio audioBus;
        public sealed class Loadout
        {
            public WeaponSettings[] weapons;
            public int[] ammo;
            public int selected;
            public float reloadRemaining;
        }
        public Loadout CaptureLoadout()
        {
            var state = new Loadout { weapons=collected.ToArray(), ammo=new int[collected.Count], selected=collected.IndexOf(settings), reloadRemaining=Mathf.Max(0,nextShot-Time.time) };
            for (int i=0;i<collected.Count;i++) state.ammo[i]=AmmoFor(collected[i]);
            return state;
        }
        public void RestoreLoadout(Loadout state)
        {
            if (state == null) return;
            collected.Clear(); ammunition.Clear();
            for (int i=0;i<state.weapons.Length;i++)
            {
                if (state.weapons[i] == null) continue;
                collected.Add(state.weapons[i]); ammunition[state.weapons[i]]=state.ammo[i];
            }
            settings = defaultSettings;
            if (state.selected >= 0 && state.selected < state.weapons.Length && Available(state.weapons[state.selected])) settings=state.weapons[state.selected];
            nextShot=Time.time+state.reloadRemaining;
        }
        void Awake()
        {
            owner = GetComponentInParent<Damageable>(); audioBus = GetComponentInParent<VehicleAudio>();
            defaultSettings = settings;
            if (settings != null) { collected.Add(settings); ammunition[settings] = Mathf.Max(0,settings.capacity); }
        }
        void Update() { if (TriggerHeld && BattleSession.IsRunning) TryFire(); }
        int AmmoFor(WeaponSettings weapon) => weapon != null && ammunition.TryGetValue(weapon,out int amount) ? amount : 0;
        bool Available(WeaponSettings weapon) => weapon != null && (weapon.capacity == WeaponSettings.UnlimitedCapacity || AmmoFor(weapon) > 0);
        public bool Collect(WeaponSettings weapon)
        {
            if (weapon == null || weapon.capacity <= 0 || owner.IsDead) return false;
            if (!collected.Contains(weapon)) collected.Add(weapon);
            ammunition[weapon] = weapon.capacity;
            settings = weapon; // Preserve cooldown when collecting or switching, so switching cannot bypass reload.
            return true;
        }
        public bool Select(int index)
        {
            if (index < 0 || index >= collected.Count || !Available(collected[index])) return false;
            settings = collected[index]; return true;
        }
        public void Cycle(int direction)
        {
            int current = collected.IndexOf(settings);
            for (int step=1; step<=collected.Count; step++)
                if (Select((current + (direction >= 0 ? step : -step) + collected.Count) % collected.Count)) return;
        }
        void SpendRound(WeaponSettings fired)
        {
            if (fired.capacity == WeaponSettings.UnlimitedCapacity) return;
            ammunition[fired] = Mathf.Max(0,AmmoFor(fired) - 1);
            if (ammunition[fired] == 0 && Available(defaultSettings)) settings = defaultSettings;
        }
        public bool CanHit(Vector3 point, Transform targetRoot = null)
        {
            if (settings == null || owner == null || owner.IsDead || BarrelBlocked(out _)) return false;
            if (targetRoot != null)
            {
                // A target occupies a volume. Any first contact with that target is a valid shot,
                // even when the camera sees its roof and the barrel will strike its front armour.
                return CombatPhysics.Cast(muzzle.position,muzzle.forward,settings.range,settings.radius,owner.transform,out var targetHit)
                    && targetHit.transform.IsChildOf(targetRoot);
            }
            // Ground cursor positions represent a horizontal location at barrel height.
            point.y = muzzle.position.y;
            Vector3 delta = point - muzzle.position;
            float along = Vector3.Dot(delta, muzzle.forward);
            if (along <= 0 || along > settings.range || Vector3.Cross(delta, muzzle.forward).magnitude > settings.aimTolerance) return false;
            if (!CombatPhysics.Cast(muzzle.position, muzzle.forward, Mathf.Min(settings.range,along + settings.aimTolerance), settings.radius, owner.transform, out var hit)) return true;
            return Vector3.Distance(hit.point, point) <= settings.aimTolerance;
        }
        bool BarrelBlocked(out RaycastHit hit)
        {
            var delta = muzzle.position - breech.position;
            return CombatPhysics.Cast(breech.position, delta, delta.magnitude, settings.radius, owner.transform, out hit);
        }
        public bool TryFire()
        {
            if (!BattleSession.IsRunning || owner.IsDead || Time.time < nextShot || !Available(settings)) return false;
            WeaponSettings fired = settings;
            nextShot = Time.time + fired.interval;
            audioBus?.Play(fired.fireClip, fired.volume);
            if (BarrelBlocked(out var barrelHit)) CombatPhysics.Hit(barrelHit, fired, owner.team, owner.transform);
            else if (fired.projectileSpeed > 0)
            {
                var go = new GameObject("Cannon shell"); go.transform.position = muzzle.position;
                go.AddComponent<ShellProjectile>().Launch(fired, owner.team, owner.transform, muzzle.forward);
            }
            else
            {
                bool blocked = CombatPhysics.Cast(muzzle.position, muzzle.forward, fired.range, fired.radius, owner.transform, out var hit);
                ShotVisual.Trace(muzzle.position, blocked ? hit.point : muzzle.position + muzzle.forward * fired.range, fired, .035f);
                if (blocked) CombatPhysics.Hit(hit, fired, owner.team, owner.transform);
            }
            SpendRound(fired);
            return true;
        }
    }
}
