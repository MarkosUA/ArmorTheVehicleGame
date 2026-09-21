using UnityEngine;
using VContainer;
using VContainer.Unity;
using ArmorTheVehicle.Config;
using ArmorTheVehicle.Car;
using ArmorTheVehicle.Turret;
using ArmorTheVehicle.Enemy;
using ArmorTheVehicle.UI;
using ArmorTheVehicle.CameraRig;
using ArmorTheVehicle.Audio;

namespace ArmorTheVehicle.Core
{
    /// The only DI composition root in the project. Registers GameState (the one plain
    /// class that genuinely needs to be shared across many MonoBehaviours) and injects it
    /// into the handful of components that read/write it. LevelConfig itself is just a
    /// ScriptableObject asset — every component that needs tuning values takes it via a
    /// plain [SerializeField] reference, not through the container.
    public sealed class GameLifetimeScope : LifetimeScope
    {
        [SerializeField] private LevelConfig _levelConfig;
        [SerializeField] private CarController _car;
        [SerializeField] private TurretShooter _turretShooter;
        [SerializeField] private TurretAimController _turretAimController;
        [SerializeField] private EnemySpawner _enemySpawner;
        [SerializeField] private ResultOverlayController _resultOverlay;
        [SerializeField] private PauseController _pauseController;
        [SerializeField] private ChaseCameraFollow _chaseCameraFollow;
        [SerializeField] private AudioManager _audioManager;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance(new GameState(_levelConfig));

            builder.RegisterComponent(_car);
            builder.RegisterComponent(_turretShooter);
            builder.RegisterComponent(_turretAimController);
            builder.RegisterComponent(_enemySpawner);
            builder.RegisterComponent(_resultOverlay);
            builder.RegisterComponent(_pauseController);
            builder.RegisterComponent(_chaseCameraFollow);
            builder.RegisterComponent(_audioManager);
        }
    }
}
