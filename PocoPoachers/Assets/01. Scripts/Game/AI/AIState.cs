using System;
using Unity.Behavior;

[BlackboardEnum]
public enum AIState
{
	Idle,
	Patrol,
	Chase,
	Reload,
	Attack,
	Retreat,
}

[BlackboardEnum]
public enum AIAnimState
{
	Idle,
	Walk
}
