using Bindito.Core;

namespace ToriiGatesLanternMod
{
    // �Q�[�����ł̂ݎg���ݒ��o�^����Configurator
    [Context("Game")]  // "Game"�̃R���e�L�X�g�ł̂ݎg�p����
    internal class WindSwingModConfigurator : IConfigurator
    {
        public void Configure(IContainerDefinition containerDefinition)
        {
            // WindSwingAnimatorSettings ���V���O���g���Ƃ��Ē���
            containerDefinition.Bind<WindSwingAnimatorSettings>().AsSingleton();
        }
    }

    // ���C�����j���[�p��Configurator
    [Context("MainMenu")]
    internal class MainMenuConfigurator : IConfigurator
    {
        public void Configure(IContainerDefinition containerDefinition)
        {
            containerDefinition.Bind<WindSwingAnimatorSettings>().AsSingleton();
        }
    }
}
