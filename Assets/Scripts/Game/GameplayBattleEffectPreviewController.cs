using System.Collections;
using QFramework;
using UnityEngine;

public sealed class GameplayBattleEffectPreviewController : MonoBehaviour, IController
{
    private const int BattleEffectCinemachineChannel = 1;

    [Header("Battle Effect Preview")]
    [SerializeField, Range(0, 4)] private int mBattleEffectStyleIndex;
    [SerializeField, Range(0f, 3f)] private float mBattleEffectShakeAmplitudeMultiplier = 1f;
    [SerializeField, Range(0.2f, 3f)] private float mBattleEffectShakeFrequencyMultiplier = 1f;
    [SerializeField] private bool mUseFallbackCameraShakeWhenNoCinemachineListener = true;

    private bool mBattleEffectRunning;
    private Component mBattleImpulseSource;
    private Coroutine mFallbackCameraShakeCoroutine;
    private Transform mFallbackShakeCamera;
    private Vector3 mFallbackShakeCameraBaseLocalPosition;
    private static readonly BattleEffectStyle[] BattleEffectStyles =
    {
        new BattleEffectStyle(
            "2 Snap",
            0.18f,
            0.045f,
            0.20f,
            0.58f,
            1.12f,
            1.08f,
            10,
            1.8f,
            360f,
            0.18f,
            1.15f,
            1.15f,
            "Bump"),
        new BattleEffectStyle(
            "3 Heavy",
            0.24f,
            0.06f,
            0.28f,
            0.68f,
            1.22f,
            1.16f,
            18,
            2.8f,
            520f,
            0.26f,
            2.2f,
            0.9f,
            "Explosion"),
        new BattleEffectStyle(
            "4 Slice",
            0.16f,
            0.035f,
            0.22f,
            0.52f,
            1.08f,
            1.03f,
            14,
            2.4f,
            680f,
            0.16f,
            1.55f,
            1.45f,
            "Recoil"),
        new BattleEffectStyle(
            "5 Bounce",
            0.28f,
            0.08f,
            0.26f,
            0.62f,
            1.18f,
            1.24f,
            16,
            2.1f,
            420f,
            0.22f,
            1.6f,
            1.25f,
            "Bump"),
        new BattleEffectStyle(
            "6 Burst",
            0.20f,
            0.04f,
            0.34f,
            0.72f,
            1.28f,
            1.12f,
            22,
            3.2f,
            760f,
            0.30f,
            2.65f,
            1.1f,
            "Explosion")
    };

    public IArchitecture GetArchitecture()
    {
        return TableNine.Interface;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha2)) SetBattleEffectStyle(0);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SetBattleEffectStyle(1);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SetBattleEffectStyle(2);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SetBattleEffectStyle(3);
        if (Input.GetKeyDown(KeyCode.Alpha6)) SetBattleEffectStyle(4);
    }

    public bool TryPlayRightClickBattleEffect(BoardSlotNo targetSlot)
    {
        if (!TableNine.IsInitialized || mBattleEffectRunning || !enabled)
        {
            return false;
        }

        var flowModel = this.GetModel<IFlowModel>();
        if (flowModel.IsInputLocked)
        {
            return false;
        }

        var boardModel = this.GetModel<IBoardModel>();
        var collectionModel = this.GetModel<ICollectionModel>();
        var targetUid = boardModel.GetCardAt(targetSlot);
        if (!targetUid.HasValue || targetUid.Value.Equals(this.GetModel<IPlayerModel>().PlayerCardUid))
        {
            return false;
        }

        if (!collectionModel.TryGetCard(targetUid.Value, out _))
        {
            return false;
        }

        var playerSlot = boardModel.PlayerSlot;
        if (!TryFindBoardCardView(playerSlot, out var playerView) ||
            !TryFindBoardCardView(targetSlot, out var targetView) ||
            playerView == null ||
            targetView == null ||
            !playerView.gameObject.activeInHierarchy ||
            !targetView.gameObject.activeInHierarchy)
        {
            return false;
        }

        StartCoroutine(PlayRightClickBattleEffectCoroutine(playerSlot, targetSlot, targetUid.Value, playerView, targetView));
        return true;
    }

    private static bool TryFindBoardCardView(BoardSlotNo slot, out CardView cardView)
    {
        var slotViews = FindObjectsOfType<BoardSlotView>();
        for (var i = 0; i < slotViews.Length; i++)
        {
            if (slotViews[i] != null &&
                !slotViews[i].IsItemSlot &&
                slotViews[i].BoardSlotNo == slot.Value &&
                slotViews[i].CardView != null)
            {
                cardView = slotViews[i].CardView;
                return true;
            }
        }

        cardView = null;
        return false;
    }

    private void SetBattleEffectStyle(int styleIndex)
    {
        mBattleEffectStyleIndex = Mathf.Clamp(styleIndex, 0, BattleEffectStyles.Length - 1);
        Debug.Log($"Battle FX {BattleEffectStyles[mBattleEffectStyleIndex].Name}");
    }

    private IEnumerator PlayRightClickBattleEffectCoroutine(
        BoardSlotNo playerSlot,
        BoardSlotNo targetSlot,
        CardUid expectedTargetUid,
        CardView playerView,
        CardView targetView)
    {
        var style = BattleEffectStyles[Mathf.Clamp(mBattleEffectStyleIndex, 0, BattleEffectStyles.Length - 1)];
        var inputLockSystem = this.GetSystem<IInputLockSystem>();
        var boardModel = this.GetModel<IBoardModel>();

        mBattleEffectRunning = true;
        inputLockSystem.Lock(InputLockReason.SequenceRunning);

        var playerTransform = playerView.transform;
        var targetTransform = targetView.transform;
        var playerStartPosition = playerTransform.position;
        var targetStartPosition = targetTransform.position;
        var playerStartScale = playerTransform.localScale;
        var targetStartScale = targetTransform.localScale;
        var direction = (targetStartPosition - playerStartPosition);
        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = Vector3.right;
        }

        direction.Normalize();
        var hitPosition = Vector3.Lerp(playerStartPosition, targetStartPosition, style.ContactRatio);
        var targetHitPosition = targetStartPosition + direction * 0.08f;

        yield return AnimateApproach(
            playerTransform,
            targetTransform,
            playerStartPosition,
            hitPosition,
            targetStartPosition,
            targetHitPosition,
            playerStartScale,
            targetStartScale,
            style);

        TriggerBattleCameraShake(targetStartPosition, direction, style);

        if (TryGetFaceRenderer(targetView, out var faceRenderer, out var backRenderer))
        {
            var impactPoint = CardFakeShatterEffect.ComputeImpactPoint(faceRenderer, targetStartPosition, direction);
            yield return CardFakeShatterEffect.Play(
                faceRenderer,
                backRenderer,
                impactPoint,
                direction,
                style.ImpactHold,
                style.ShardDuration,
                CardFakeShatterSettings.FromShardCount(style.ShardCount));
        }
        else
        {
            yield return new WaitForSeconds(style.ImpactHold + style.ShardDuration);
        }

        targetTransform.localScale = Vector3.zero;

        yield return AnimateRecover(playerTransform, playerStartPosition, playerStartScale, style.RecoverDuration);

        playerTransform.position = playerStartPosition;
        playerTransform.localScale = playerStartScale;
        targetTransform.position = targetStartPosition;
        targetTransform.localScale = targetStartScale;

        if (boardModel.GetCardAt(targetSlot).HasValue &&
            boardModel.GetCardAt(targetSlot).Value.Equals(expectedTargetUid) &&
            boardModel.GetCardAt(playerSlot).HasValue)
        {
            this.GetSystem<IBoardSystem>().RemoveCardAt(targetSlot, RemoveReason.Debug);
            this.SendCommand(new RequestRefillBoardCommand());
        }

        inputLockSystem.Unlock(InputLockReason.SequenceRunning);
        mBattleEffectRunning = false;
    }

    private static IEnumerator AnimateApproach(
        Transform playerTransform,
        Transform targetTransform,
        Vector3 playerStartPosition,
        Vector3 playerHitPosition,
        Vector3 targetStartPosition,
        Vector3 targetHitPosition,
        Vector3 playerStartScale,
        Vector3 targetStartScale,
        BattleEffectStyle style)
    {
        var elapsed = 0f;
        while (elapsed < style.ApproachDuration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / style.ApproachDuration);
            var eased = EaseOutBack(t);
            playerTransform.position = Vector3.LerpUnclamped(playerStartPosition, playerHitPosition, eased);
            targetTransform.position = Vector3.Lerp(targetStartPosition, targetHitPosition, EaseOutQuad(t));
            playerTransform.localScale = playerStartScale * Mathf.Lerp(1f, style.PlayerScale, EaseOutQuad(t));
            targetTransform.localScale = targetStartScale * Mathf.Lerp(1f, style.TargetScale, EaseOutQuad(t));
            yield return null;
        }
    }

    private static IEnumerator AnimateRecover(
        Transform playerTransform,
        Vector3 playerStartPosition,
        Vector3 playerStartScale,
        float duration)
    {
        var fromPosition = playerTransform.position;
        var fromScale = playerTransform.localScale;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            var eased = EaseOutQuad(t);
            playerTransform.position = Vector3.Lerp(fromPosition, playerStartPosition, eased);
            playerTransform.localScale = Vector3.Lerp(fromScale, playerStartScale, eased);
            yield return null;
        }
    }

    private static bool TryGetFaceRenderer(CardView cardView, out SpriteRenderer faceRenderer, out SpriteRenderer backRenderer)
    {
        return CardFakeShatterEffect.TryGetFaceRenderer(cardView, out faceRenderer, out backRenderer);
    }

    private void TriggerBattleCameraShake(Vector3 origin, Vector3 hitDirection, BattleEffectStyle style)
    {
        var amplitude = style.ShakeAmplitude * mBattleEffectShakeAmplitudeMultiplier;
        var frequency = style.ShakeFrequency * mBattleEffectShakeFrequencyMultiplier;
        var hasCinemachineListener = TryTriggerCinemachineImpulse(
            origin,
            hitDirection,
            style.ImpulseShapeName,
            style.ShakeDuration,
            amplitude,
            frequency);

        if (!hasCinemachineListener && mUseFallbackCameraShakeWhenNoCinemachineListener)
        {
            StartFallbackCameraShake(style.ShakeDuration, amplitude, frequency);
        }
    }

    public static bool TryTriggerCinemachineImpulse(
        Vector3 origin,
        Vector3 hitDirection,
        string impulseShapeName,
        float duration,
        float amplitude,
        float frequency)
    {
        var hasCinemachineListener = EnsureCinemachineListener();
        var source = GetOrCreateSharedImpulseSource();
        if (source == null)
        {
            return false;
        }

        source.transform.position = origin;
        ConfigureImpulseSource(source, impulseShapeName, duration, amplitude, frequency);
        var generateMethod = source.GetType().GetMethod("GenerateImpulseWithVelocity", new[] { typeof(Vector3) });
        generateMethod?.Invoke(source, new object[] { hitDirection.normalized * amplitude });
        return hasCinemachineListener;
    }

    private Component GetOrCreateImpulseSource()
    {
        if (mBattleImpulseSource != null)
        {
            return mBattleImpulseSource;
        }

        var impulseSourceType = FindType("Unity.Cinemachine.CinemachineImpulseSource") ??
                                FindType("Cinemachine.CinemachineImpulseSource");
        if (impulseSourceType == null)
        {
            return null;
        }

        var sourceObject = new GameObject("TableNine Battle Impulse Source");
        mBattleImpulseSource = sourceObject.AddComponent(impulseSourceType);
        return mBattleImpulseSource;
    }

    private static Component sSharedBattleImpulseSource;

    private static Component GetOrCreateSharedImpulseSource()
    {
        if (sSharedBattleImpulseSource != null)
        {
            return sSharedBattleImpulseSource;
        }

        var impulseSourceType = FindType("Unity.Cinemachine.CinemachineImpulseSource") ??
                                FindType("Cinemachine.CinemachineImpulseSource");
        if (impulseSourceType == null)
        {
            return null;
        }

        var sourceObject = new GameObject("TableNine Shared Battle Impulse Source");
        sSharedBattleImpulseSource = sourceObject.AddComponent(impulseSourceType);
        return sSharedBattleImpulseSource;
    }

    private static bool EnsureCinemachineListener()
    {
        var listenerType = FindType("Unity.Cinemachine.CinemachineImpulseListener") ??
                           FindType("Cinemachine.CinemachineImpulseListener");
        if (listenerType == null)
        {
            return false;
        }

        var existingListener = FindObjectOfType(listenerType) as Component;
        if (existingListener != null)
        {
            SetMember(existingListener, "ChannelMask", GetIntMember(existingListener, "ChannelMask") | BattleEffectCinemachineChannel);
            SetMember(existingListener, "Gain", Mathf.Max(GetFloatMember(existingListener, "Gain", 1f), 1f));
            SetMember(existingListener, "Use2DDistance", true);
            return true;
        }

        var cameraType = FindType("Unity.Cinemachine.CinemachineCamera") ??
                         FindType("Cinemachine.CinemachineVirtualCamera");
        if (cameraType == null)
        {
            return false;
        }

        var camera = FindObjectOfType(cameraType) as Component;
        if (camera == null)
        {
            return false;
        }

        var listener = camera.gameObject.AddComponent(listenerType);
        SetMember(listener, "ChannelMask", BattleEffectCinemachineChannel);
        SetMember(listener, "Gain", 1f);
        SetMember(listener, "Use2DDistance", true);
        return true;
    }

    private static void ConfigureImpulseSource(Component source, BattleEffectStyle style, float amplitude, float frequency)
    {
        ConfigureImpulseSource(source, style.ImpulseShapeName, style.ShakeDuration, amplitude, frequency);
    }

    private static void ConfigureImpulseSource(
        Component source,
        string impulseShapeName,
        float duration,
        float amplitude,
        float frequency)
    {
        if (source == null)
        {
            return;
        }

        SetMember(source, "DefaultVelocity", Vector3.down * amplitude);
        var definition = GetMember(source, "ImpulseDefinition");
        if (definition == null)
        {
            return;
        }

        SetMember(definition, "ImpulseChannel", BattleEffectCinemachineChannel);
        SetEnumMember(definition, "ImpulseShape", impulseShapeName);
        SetEnumMember(definition, "ImpulseType", "Uniform");
        SetMember(definition, "ImpulseDuration", duration);
        SetMember(definition, "AmplitudeGain", amplitude);
        SetMember(definition, "FrequencyGain", frequency);

        var envelope = GetMember(definition, "TimeEnvelope");
        if (envelope == null)
        {
            return;
        }

        SetMember(envelope, "AttackTime", 0f);
        SetMember(envelope, "SustainTime", duration * 0.18f);
        SetMember(envelope, "DecayTime", duration * 0.82f);
        SetMember(definition, "TimeEnvelope", envelope);
    }

    private void StartFallbackCameraShake(float duration, float amplitude, float frequency)
    {
        var mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        if (mFallbackCameraShakeCoroutine != null)
        {
            StopCoroutine(mFallbackCameraShakeCoroutine);
            if (mFallbackShakeCamera != null)
            {
                mFallbackShakeCamera.localPosition = mFallbackShakeCameraBaseLocalPosition;
            }
        }

        mFallbackCameraShakeCoroutine = StartCoroutine(FallbackCameraShake(
            mainCamera.transform,
            duration,
            amplitude * 0.055f,
            frequency * 24f));
    }

    private IEnumerator FallbackCameraShake(Transform cameraTransform, float duration, float amplitude, float frequency)
    {
        mFallbackShakeCamera = cameraTransform;
        mFallbackShakeCameraBaseLocalPosition = cameraTransform.localPosition;
        var elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            var fade = 1f - t;
            var x = (Mathf.PerlinNoise(Time.time * frequency, 0.1f) - 0.5f) * 2f;
            var y = (Mathf.PerlinNoise(0.2f, Time.time * frequency) - 0.5f) * 2f;
            cameraTransform.localPosition = mFallbackCameraShakeBasePosition() + new Vector3(x, y, 0f) * amplitude * fade;
            yield return null;
        }

        cameraTransform.localPosition = mFallbackCameraShakeBasePosition();
        mFallbackCameraShakeCoroutine = null;
    }

    private Vector3 mFallbackCameraShakeBasePosition()
    {
        return mFallbackShakeCameraBaseLocalPosition;
    }

    private static System.Type FindType(string typeName)
    {
        var direct = System.Type.GetType(typeName);
        if (direct != null)
        {
            return direct;
        }

        var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
        for (var i = 0; i < assemblies.Length; i++)
        {
            var type = assemblies[i].GetType(typeName);
            if (type != null)
            {
                return type;
            }
        }

        return null;
    }

    private static object GetMember(object target, string memberName)
    {
        if (target == null)
        {
            return null;
        }

        var type = target.GetType();
        var field = type.GetField(memberName);
        if (field != null)
        {
            return field.GetValue(target);
        }

        var property = type.GetProperty(memberName);
        return property != null ? property.GetValue(target, null) : null;
    }

    private static void SetMember(object target, string memberName, object value)
    {
        if (target == null)
        {
            return;
        }

        var type = target.GetType();
        var field = type.GetField(memberName);
        if (field != null)
        {
            field.SetValue(target, value);
            return;
        }

        var property = type.GetProperty(memberName);
        if (property != null && property.CanWrite)
        {
            property.SetValue(target, value, null);
        }
    }

    private static void SetEnumMember(object target, string memberName, string enumName)
    {
        if (target == null)
        {
            return;
        }

        var type = target.GetType();
        var field = type.GetField(memberName);
        var memberType = field != null ? field.FieldType : type.GetProperty(memberName)?.PropertyType;
        if (memberType == null || !memberType.IsEnum)
        {
            return;
        }

        SetMember(target, memberName, System.Enum.Parse(memberType, enumName));
    }

    private static int GetIntMember(object target, string memberName)
    {
        var value = GetMember(target, memberName);
        return value is int intValue ? intValue : 0;
    }

    private static float GetFloatMember(object target, string memberName, float fallback)
    {
        var value = GetMember(target, memberName);
        return value is float floatValue ? floatValue : fallback;
    }

    private static float EaseOutQuad(float t)
    {
        return 1f - (1f - t) * (1f - t);
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    private struct BattleEffectStyle
    {
        public BattleEffectStyle(
            string name,
            float approachDuration,
            float impactHold,
            float shardDuration,
            float contactRatio,
            float playerScale,
            float targetScale,
            int shardCount,
            float shardSpeed,
            float shardSpin,
            float shakeDuration,
            float shakeAmplitude,
            float shakeFrequency,
            string impulseShapeName)
        {
            Name = name;
            ApproachDuration = approachDuration;
            ImpactHold = impactHold;
            ShardDuration = shardDuration;
            ContactRatio = contactRatio;
            PlayerScale = playerScale;
            TargetScale = targetScale;
            ShardCount = shardCount;
            ShardSpeed = shardSpeed;
            ShardSpin = shardSpin;
            ShakeDuration = shakeDuration;
            ShakeAmplitude = shakeAmplitude;
            ShakeFrequency = shakeFrequency;
            ImpulseShapeName = impulseShapeName;
            RecoverDuration = 0.12f;
            ForwardShardBias = 0.55f;
            CrackAngleOffset = approachDuration * 8f;
        }

        public string Name;
        public float ApproachDuration;
        public float ImpactHold;
        public float ShardDuration;
        public float ContactRatio;
        public float PlayerScale;
        public float TargetScale;
        public int ShardCount;
        public float ShardSpeed;
        public float ShardSpin;
        public float ShakeDuration;
        public float ShakeAmplitude;
        public float ShakeFrequency;
        public float RecoverDuration;
        public float ForwardShardBias;
        public float CrackAngleOffset;
        public string ImpulseShapeName;
    }

}
