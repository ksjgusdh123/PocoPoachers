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
	Rolling
}

[BlackboardEnum]
public enum AIAnimState
{
	Idle,
	Walk,
	Rolling

}
