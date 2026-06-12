using System;
using QFramework;

public sealed class TableNine : Architecture<TableNine>
{
    public static TableNine Current => mArchitecture;

    public static bool IsInitialized => mArchitecture != null;

    private int mCommandDepth;

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
        RegisterUtility<ICommandReplayUtility>(new CommandReplayUtility());
        RegisterUtility<IDebugEventLogUtility>(new DebugEventLogUtility());

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
        RegisterSystem<ISkillSystem>(new SkillSystem());
        RegisterSystem<IInputLockSystem>(new InputLockSystem());
        RegisterSystem<IRewardSystem>(new RewardSystem());
        RegisterSystem<IRelicSystem>(new RelicSystem());
        RegisterSystem<IShopSystem>(new ShopSystem());
        RegisterSystem<ISaveSystem>(new SaveSystem());
        RegisterSystem<IDebugEventSystem>(new DebugEventSystem());
    }

    protected override void ExecuteCommand(ICommand command)
    {
        var isRoot = mCommandDepth == 0;
        mCommandDepth++;
        if (isRoot)
        {
            GetSystem<ISkillSystem>()?.BeginRootCommand();
        }

        var trace = GetTraceUtilityOrNull();
        var replay = GetReplayUtilityOrNull();
        trace?.Before(command);
        replay?.Record(command);

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
        finally
        {
            mCommandDepth--;
        }
    }

    protected override TResult ExecuteCommand<TResult>(ICommand<TResult> command)
    {
        var isRoot = mCommandDepth == 0;
        mCommandDepth++;
        if (isRoot)
        {
            GetSystem<ISkillSystem>()?.BeginRootCommand();
        }

        var trace = GetTraceUtilityOrNull();
        var replay = GetReplayUtilityOrNull();
        trace?.Before(command);
        replay?.Record(command);

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
        finally
        {
            mCommandDepth--;
        }
    }

    private ICommandTraceUtility GetTraceUtilityOrNull()
    {
        return GetUtility<ICommandTraceUtility>();
    }

    private ICommandReplayUtility GetReplayUtilityOrNull()
    {
        return GetUtility<ICommandReplayUtility>();
    }
}
