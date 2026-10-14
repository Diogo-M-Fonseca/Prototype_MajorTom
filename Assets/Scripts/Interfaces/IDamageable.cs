public interface IDamageable
{
    event System.Action Damaged;
    void TakeDamage(float damage);
}
