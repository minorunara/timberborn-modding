using Bindito.Core;

namespace ToriiGatesLanternMod
{
    // ゲーム内でのみ使う設定を登録するConfigurator
    [Context("Game")]  // "Game"のコンテキストでのみ使用する
    internal class WindSwingModConfigurator : IConfigurator
    {
        public void Configure(IContainerDefinition containerDefinition)
        {
            // WindSwingAnimatorSettings をシングルトンとして注入
            containerDefinition.Bind<WindSwingAnimatorSettings>().AsSingleton();
        }
    }

    // メインメニュー用のConfigurator
    [Context("MainMenu")]
    internal class MainMenuConfigurator : IConfigurator
    {
        public void Configure(IContainerDefinition containerDefinition)
        {
            containerDefinition.Bind<WindSwingAnimatorSettings>().AsSingleton();
        }
    }
}
