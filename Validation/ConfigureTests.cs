using System;
using UnityEditor;
[InitializeOnLoad]
static class ConfigureTests {
    static ConfigureTests() {
        EditorSettings.enterPlayModeOptionsEnabled = Environment.GetEnvironmentVariable("GEKKO_TEST_DISABLE_RELOAD") == "1";
        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
    }
}
