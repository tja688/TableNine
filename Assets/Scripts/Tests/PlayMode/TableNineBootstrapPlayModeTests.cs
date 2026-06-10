using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class TableNineBootstrapPlayModeTests
{
    [UnitySetUp]
    public IEnumerator UnitySetUp()
    {
        TableNine.ResetForTests();
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator UnityTearDown()
    {
        TableNine.ResetForTests();
        yield return null;
    }

    [UnityTest]
    public IEnumerator BootstrapScene_Starts_Without_ConsoleErrors()
    {
        LogAssert.ignoreFailingMessages = false;
        yield return SceneManager.LoadSceneAsync("TableNineBootstrap", LoadSceneMode.Single);
        yield return null;

        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator GameplayBootstrap_Initializes_TableNine()
    {
        yield return SceneManager.LoadSceneAsync("TableNineBootstrap", LoadSceneMode.Single);
        yield return null;

        Assert.That(TableNine.IsInitialized, Is.True);
        Assert.That(TableNine.Current, Is.Not.Null);
    }
}
