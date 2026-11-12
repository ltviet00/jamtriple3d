using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Shared helpers for the "touch this toy" tutorials.
/// Toys start as a pile in the center that collapses, so the tutorials must wait for it and pick a toy the user can see.
/// </summary>
public static class TutorialToyHelper {
	const float SETTLED_SPEED = 0.25f;
	const float MAX_WAIT_SECONDS = 4f;

	///<summary> Wait until no toy moves faster than SETTLED_SPEED (max MAX_WAIT_SECONDS) </summary>
	public static async UniTask WaitForToysToSettle(LevelToyStatistics toyStats) {
		var endTime = Time.time + MAX_WAIT_SECONDS;
		// physics needs a few frames to start moving the pile
		await UniTask.WaitForFixedUpdate();
		await UniTask.WaitForFixedUpdate();
		while(Time.time < endTime && toyStats.GetMaxToySpeed() > SETTLED_SPEED) {
			await UniTask.WaitForFixedUpdate();
		}
	}

	///<summary> Collider on so the user can touch it, but kinematic: the toys under it have no collider during the tutorial, it would fall through them </summary>
	public static void MakeTouchableButFrozen(ToyView toy) {
		toy.SetActivePhysics(true);
		toy.mainRigid.velocity = Vector3.zero;
		toy.mainRigid.angularVelocity = Vector3.zero;
		toy.mainRigid.isKinematic = true;
	}

	public static Vector3 HandPositionFor(ToyView toy, float offsetToCamera) {
		var pos = toy.mainCollider != null ? toy.mainCollider.bounds.center : toy.transform.position;
		pos.z -= offsetToCamera;
		return pos;
	}
}
