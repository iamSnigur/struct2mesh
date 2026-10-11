using Integrations.Mojang;
using Logic;
using Logic.BuildInfo;
using Logic.PlayerPreview;
using UI;
using UI.Components;
using UI.Views;
using UnityEngine;
using UnityEngine.Assertions;
using VContainer;
using VContainer.Unity;

namespace Core.Bootstrap
{
    public class BootstrapInstaller : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(GetOnScene<CameraController>());
            builder.RegisterComponent(GetOnScene<StructureBuilder>());
            builder.RegisterComponent(GetOnScene<FileReceiver>());
            builder.RegisterComponent(GetOnScene<PlayerPreviewController>()).As<IPlayerPreview>();
            builder.RegisterComponent(GetOnScene<UIRoot>()).As<IUIRoot>();

            builder.Register<LoadingCurtainView>(Lifetime.Scoped).As<ILoadingCurtain>().As<IStartable>();
            builder.Register<MainView>(Lifetime.Scoped).AsSelf().As<IStartable>();
            builder.Register<OverlayPopupView>(Lifetime.Scoped).AsSelf().As<IStartable>();
            builder.Register<SidebarView>(Lifetime.Scoped).AsSelf().As<IStartable>();
            builder.Register<DisclaimerView>(Lifetime.Scoped).AsSelf().As<IStartable>();
            builder.Register<WelcomeHubView>(Lifetime.Scoped).AsSelf().As<IStartable>();
            builder.Register<KnowledgeBaseView>(Lifetime.Scoped).AsSelf().As<IStartable>();
            builder.Register<ToastComponent>(Lifetime.Scoped).AsSelf().As<IStartable>();

            builder.Register<MojangService>(Lifetime.Singleton);
            builder.Register<SkinLoader>(Lifetime.Singleton);
            builder.Register<BuildInfoService>(Lifetime.Singleton).As<IBuildInfoService>();

            builder.RegisterEntryPoint<Bootstrapper>();
        }

        private static T GetOnScene<T>() where T : MonoBehaviour
        {
            T monoBehaviour = FindFirstObjectByType<T>();
            Assert.IsNotNull(monoBehaviour, $"{typeof(T).Name} not found on boot scene");
            return monoBehaviour;
        }
    }
}