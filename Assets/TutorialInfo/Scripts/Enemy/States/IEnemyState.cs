/// <summary>
/// Interface State Pattern cho AI kẻ địch.
/// Ba trạng thái: Patrol → Investigate → Alert
/// </summary>
public interface IEnemyState
{
    void EnterState(EnemyAI enemy);
    void UpdateState(EnemyAI enemy);
    void ExitState(EnemyAI enemy);
}
