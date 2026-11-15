using UnityEngine;
namespace TankAttack
{
    // Run state survives scene reloads, but is reset on deployment and on entering Play mode.
    public static class LevelRun
    {
        public static int Level { get; private set; } = 1;
        public static int Seed { get; private set; }
        public static int EntryScore { get; private set; }
        static bool started;
        static float entryHealth;
        static TankWeapon.Loadout cannon, machineGun;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset()
        { Level = 1; Seed = 0; EntryScore = 0; started = false; cannon = machineGun = null; }
        public static void EnsureStarted(int fixedSeed)
        {
            if (started) return;
            started = true;
            Seed = fixedSeed != 0 ? fixedSeed : System.Guid.NewGuid().GetHashCode();
        }
        public static void Advance(BattleSession session)
        {
            EntryScore = session.Score;
            entryHealth = session.player.Health;
            var input = session.player.GetComponent<PlayerTankInput>();
            cannon = input.cannon.CaptureLoadout(); machineGun = input.machineGun.CaptureLoadout();
            Level++;
        }
        public static void Restore(Damageable player)
        {
            if (cannon == null) return;
            player.RestoreHealth(entryHealth);
            var input = player.GetComponent<PlayerTankInput>();
            input.cannon.RestoreLoadout(cannon); input.machineGun.RestoreLoadout(machineGun);
        }
    }
}
