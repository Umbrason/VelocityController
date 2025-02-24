using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class VelocityController : MonoBehaviour
{
    private Rigidbody rb => cached_rb ??= GetComponent<Rigidbody>();
    private Rigidbody cached_rb;
    public Vector3 CurrentVelocity { get => rb.velocity; }
    private Coroutine PostFixedUpdateRoutine;
    public Vector3 PhysicsUpdateVelocityChange { get; private set; }
    private Vector3 lastDesiredVelocity;

    public struct MovementOverride
    {
        public VelocityBlendMode blendMode;
        public VelocityChannelMask channelMask;
        public Func<float, float> speedCurve;
        public Vector3 direction;
        public event Action OnCompleteActions;

        public Vector3 ChannelMaskVector { get => new(((int)channelMask & 0b001), ((int)channelMask & 0b010) >> 1, ((int)channelMask & 0b100) >> 2); }

        public MovementOverride(Vector3 direction,
                                Func<float, float> speedCurve,
                                VelocityBlendMode blendMode = VelocityBlendMode.Overwrite,
                                VelocityChannelMask channelMask = VelocityChannelMask.XYZ)
        {
            this.direction = direction;
            this.blendMode = blendMode;
            this.speedCurve = speedCurve;
            this.channelMask = channelMask;
            OnCompleteActions = () => { };
        }
        public MovementOverride(Vector3 direction,
                                float constantSpeed,
                                VelocityBlendMode blendMode = VelocityBlendMode.Overwrite,
                                VelocityChannelMask channelMask = VelocityChannelMask.XYZ)
        : this(direction, (t) => constantSpeed, blendMode, channelMask) { }

        public MovementOverride(Vector3 desiredVelocity,
                                VelocityBlendMode blendMode = VelocityBlendMode.Overwrite,
                                VelocityChannelMask channelMask = VelocityChannelMask.XYZ) : this(desiredVelocity.normalized, (t) => desiredVelocity.magnitude, blendMode, channelMask) { }

        public MovementOverride OnComplete(Action callback)
        {
            OnCompleteActions += callback;
            return this;
        }

        public void Complete()
        {
            OnCompleteActions?.Invoke();
        }
    }

    private struct MovementOverrideInstance
    {
        public MovementOverride movementOverride;
        public float startTime;
        public float duration;
        public MovementOverrideInstance(MovementOverride movementOverride, float startTime, float duration)
        {
            this.movementOverride = movementOverride;
            this.startTime = startTime;
            this.duration = duration;
        }
    }
    private Dictionary<int, List<MovementOverrideInstance>> movementOverrideInstances = new Dictionary<int, List<MovementOverrideInstance>>();    

    void OnEnable() => PostFixedUpdateRoutine = StartCoroutine(PostFixedUpdate());
    void OnDisable() => StopCoroutine(PostFixedUpdateRoutine);

    public void Clear() => movementOverrideInstances.Clear();

    public void AddOverwriteMovement(MovementOverride movementOverride, float duration, int priority = 0)
    {
        (   //ensure list
            movementOverrideInstances.ContainsKey(priority) ?
            movementOverrideInstances[priority] :
            movementOverrideInstances[priority] = new List<MovementOverrideInstance>()
        ).Add(new MovementOverrideInstance(movementOverride, Time.time, duration));
    }
    private void FixedUpdate()
    {
        //apply movement overrides onto current movement
        var desiredVelocity = CurrentVelocity;
        ProcessMovementOverrides(ref desiredVelocity);
        //remove expired entries (after applying their effects at least once)
        RemoveExpiredMovementOverrides();

        lastDesiredVelocity = desiredVelocity;

        //apply such an impulse to the rb, that it reaches the desired velocity
        rb.AddForce((desiredVelocity - CurrentVelocity), ForceMode.VelocityChange);
    }

    private IEnumerator PostFixedUpdate()
    {
        var WaitForFixedUpdate = new WaitForFixedUpdate();
        while (true)
        {
            PhysicsUpdateVelocityChange = CurrentVelocity - lastDesiredVelocity;
            yield return WaitForFixedUpdate;
        }
    }

    //proplems:
    //how should I handle multiple overwrite movements with the same priority?    
    private void ProcessMovementOverrides(ref Vector3 currentVelocity)
    {
        var startVelocity = currentVelocity; //memorize start movement in case a movementOverride has its blend mode set to overwrite
        //iterate over remaining entries
        foreach (var priority in movementOverrideInstances.Keys.OrderBy(x => x))
            foreach (var movementOverrideInstance in movementOverrideInstances[priority])
            {
                var t = Mathf.Clamp01((Time.time - movementOverrideInstance.startTime) / movementOverrideInstance.duration);
                var movementOverrideData = movementOverrideInstance.movementOverride;

                var speed = movementOverrideData.speedCurve(t);
                var newMovement = speed * movementOverrideData.direction.normalized;
                var maskedVelocity = Vector3.Scale(newMovement, movementOverrideData.ChannelMaskVector);

                switch (movementOverrideData.blendMode)
                {
                    case VelocityBlendMode.Additive:
                        currentVelocity += maskedVelocity;
                        break;
                    case VelocityBlendMode.Multiplicative:
                        currentVelocity = Vector3.Scale(currentVelocity, maskedVelocity + (Vector3.one - movementOverrideData.ChannelMaskVector));
                        break;
                    case VelocityBlendMode.MaximumMagnitude: //pick components so the resulting velocity has the maximum possible magnitude
                        currentVelocity = VectorMath.MaxMagnitude(currentVelocity, maskedVelocity);
                        break;
                    case VelocityBlendMode.Overwrite: //overwrite all lower priority movement for this physics step
                        currentVelocity = Vector3.Scale(newMovement, movementOverrideData.ChannelMaskVector) + Vector3.Scale(currentVelocity, Vector3.one - movementOverrideData.ChannelMaskVector);
                        break;
                }
            }
    }

    private void RemoveExpiredMovementOverrides()
    {
        //call ToList() to copy the keys to avoid collection modified exception
        foreach (var priority in movementOverrideInstances.Keys.ToList())
        {
            //filter out expired entries by checking normalized time
            var expired = movementOverrideInstances[priority].Where((movementOverrideInstance) =>
                    (Time.time - movementOverrideInstance.startTime) / movementOverrideInstance.duration >= 1f).ToList();
            foreach (var expiredOverride in expired)
                expiredOverride.movementOverride.Complete();
            movementOverrideInstances[priority] = movementOverrideInstances[priority].Except(expired).ToList();
        }
    }
}

public enum VelocityChannelMask
{
    X = 0b001,
    Y = 0b010,
    Z = 0b100,
    XY = 0b011,
    XZ = 0b101,
    YZ = 0b110,
    XYZ = 0b111
}

public enum VelocityBlendMode { Additive, Overwrite, MaximumMagnitude, Multiplicative }
