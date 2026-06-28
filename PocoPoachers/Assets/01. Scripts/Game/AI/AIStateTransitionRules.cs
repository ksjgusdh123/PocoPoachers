using System.Collections.Generic;

// AIState 끼리 허용된 전이만 정의. 여기 없는 (from, to) 조합은 ChangeAIState에서 거부됨
public static class AIStateTransitionRules
{
    private static readonly Dictionary<AIState, HashSet<AIState>> _allowedTransitions = new()
    {
        { AIState.Idle,    new HashSet<AIState> { AIState.Patrol, AIState.Chase, AIState.Retreat, AIState.Rolling } },
        { AIState.Patrol,  new HashSet<AIState> { AIState.Idle, AIState.Chase, AIState.Retreat, AIState.Rolling } },
        { AIState.Chase,   new HashSet<AIState> { AIState.Attack, AIState.Retreat, AIState.Patrol, AIState.Rolling } },
        { AIState.Attack,  new HashSet<AIState> { AIState.Reload, AIState.Retreat, AIState.Chase, AIState.Rolling} },
        { AIState.Reload,  new HashSet<AIState> { AIState.Attack, AIState.Retreat, AIState.Rolling } },
        { AIState.Retreat, new HashSet<AIState> { AIState.Idle, AIState.Rolling } },
        { AIState.Rolling, new HashSet<AIState> { AIState.Idle } },
    };

    public static bool CanTransition(AIState from, AIState to)
    {
        if (from == to) return true;
        return _allowedTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
    }
}
