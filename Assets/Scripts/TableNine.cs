using System;
using QFramework;

public sealed class TableNine : Architecture<TableNine>
{
    public static TableNine Current => mArchitecture;

    public static bool IsInitialized => mArchitecture != null;

#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
    public static void ResetForTests()
    {
        if (mArchitecture != null)
        {
            mArchitecture.Deinit();
        }
    }
#endif

    protected override void Init()
    {
        RegisterUtility<IRandomUtility>(new UnityRandomUtility());
        RegisterUtility<ISaveUtility>(new MemorySaveUtility());
        RegisterUtility<IConfigUtility>(new ScriptableConfigUtility());
        RegisterUtility<ISequenceUtility>(new ImmediateSequenceUtility());
        RegisterUtility<ICommandTraceUtility>(new CommandTraceUtility());

        RegisterModel<IRunModel>(new RunModel());
        RegisterModel<IPlayerModel>(new PlayerModel());
        RegisterModel<IBoardModel>(new BoardModel());
        RegisterModel<IDeckModel>(new DeckModel());
        RegisterModel<ICollectionModel>(new CollectionModel());
        RegisterModel<IConfigModel>(new ConfigModel());
        RegisterModel<IFlowModel>(new FlowModel());
        RegisterModel<IRewardModel>(new RewardModel());

        RegisterSystem<IRunSystem>(new RunSystem());
        RegisterSystem<ILevelFlowSystem>(new LevelFlowSystem());
        RegisterSystem<IBoardSystem>(new BoardSystem());
        RegisterSystem<IDeckSystem>(new DeckSystem());
        RegisterSystem<ICombatSystem>(new CombatSystem());
        RegisterSystem<IStatSystem>(new StatSystem());
        RegisterSystem<IEffectSystem>(new EffectSystem());
        RegisterSystem<IInputLockSystem>(new InputLockSystem());
        RegisterSystem<IRewardSystem>(new RewardSystem());
        RegisterSystem<IRelicSystem>(new RelicSystem());
        RegisterSystem<IShopSystem>(new ShopSystem());
    }

    protected override void ExecuteCommand(ICommand command)
    {
        var trace = GetTraceUtilityOrNull();
        trace?.Before(command);

        try
        {
            base.ExecuteCommand(command);
            trace?.After(command);
        }
        catch (Exception exception)
        {
            trace?.OnException(command, exception);
            throw;
        }
    }

    protected override TResult ExecuteCommand<TResult>(ICommand<TResult> command)
    {
        var trace = GetTraceUtilityOrNull();
        trace?.Before(command);

        try
        {
            var result = base.ExecuteCommand(command);
            trace?.After(command, result);
            return result;
        }
        catch (Exception exception)
        {
            trace?.OnException(command, exception);
            throw;
        }
    }

    private ICommandTraceUtility GetTraceUtilityOrNull()
    {
        return GetUtility<ICommandTraceUtility>();
    }
}
