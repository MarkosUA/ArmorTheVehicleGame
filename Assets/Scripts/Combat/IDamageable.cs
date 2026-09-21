namespace ArmorTheVehicle.Combat
{
    public interface IDamageable
    {
        bool IsAlive { get; }
        void TakeDamage(float amount);
    }
}
