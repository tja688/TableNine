/// <summary>
/// EditMode 测试分类标签，供 Test Runner 筛选。
/// LegacyRuleTests：依赖旧规则语义，R2+ 需改期望。
/// NewRuleTests：新版规则测试。
/// RegressionTests：与规则语义无关的工程/流程回归。
/// </summary>
public static class TableNineTestCategories
{
    public const string LegacyRuleTests = "LegacyRuleTests";
    public const string NewRuleTests = "NewRuleTests";
    public const string RegressionTests = "RegressionTests";
}
