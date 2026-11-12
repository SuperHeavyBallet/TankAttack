using UnityEngine;
using UnityEngine.AI;
namespace TankAttack
{
    public sealed class EnemyTankAI : MonoBehaviour
    {
        public TankMotor motor;
        public TurretAim aim;
        public TankWeapon cannon, machineGun;
        public float firingDistance = 24;
        public float repathInterval = .75f;
        NavMeshPath path;
        Vector3[] corners = new Vector3[0];
        int corner;
        float nextPath;
        void Awake() => path = new NavMeshPath();
        void Update()
        {
            var target = BattleSession.Instance != null ? BattleSession.Instance.player : null;
            if (!BattleSession.IsRunning || target == null || target.IsDead)
            { motor.Command = Vector2.zero; cannon.TriggerHeld = machineGun.TriggerHeld = false; return; }
            Vector3 destination = target.transform.position;
            aim.Target = destination + Vector3.up * .6f;
            float distance = Vector3.Distance(transform.position, destination);
            bool clear = cannon.CanHit(aim.Target, target.transform);
            cannon.TriggerHeld = clear && distance <= firingDistance;
            machineGun.TriggerHeld = machineGun.CanHit(aim.Target, target.transform) && distance < 14;
            if (distance < firingDistance * .8f && clear) { motor.Command = Vector2.zero; return; }
            if (Time.time >= nextPath)
            {
                nextPath = Time.time + repathInterval;
                if (NavMesh.SamplePosition(transform.position, out var start, 4, NavMesh.AllAreas) &&
                    NavMesh.SamplePosition(destination, out var end, 4, NavMesh.AllAreas) &&
                    NavMesh.CalculatePath(start.position, end.position, NavMesh.AllAreas, path))
                { corners = path.corners; corner = corners.Length > 1 ? 1 : 0; }
                else corners = new Vector3[0];
            }
            if (corners.Length == 0) { motor.Command = Vector2.zero; return; }
            Vector3 delta = Vector3.ProjectOnPlane(corners[corner] - transform.position, Vector3.up);
            if (delta.magnitude < 1.5f && corner < corners.Length - 1) corner++;
            float angle = Vector3.SignedAngle(transform.forward, delta, Vector3.up);
            motor.Command = new Vector2(Mathf.Clamp(angle / 35, -1, 1), Mathf.Abs(angle) < 40 && delta.magnitude > 1 ? .8f : 0);
            GetComponent<VehicleAudio>()?.SetMotion(motor.Command.magnitude, aim.TurnAmount);
        }
    }
}
