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

	///<summary> Lift the toy above every other toy (camera looks along +z, smaller z = closer to camera), so a buried toy is visible for the tutorial </summary>
	public static void BringToTopOfPile(ToyView toy, LevelToyStatistics toyStats) {
		const float GAP = 0.05f;
		var othersMinZ = float.MaxValue;
		var maps = new System.Collections.Generic.List<System.Collections.Generic.Dictionary<GameObject, System.Collections.Generic.List<ToyView>>>(toyStats.AllMaps) {
			toyStats.SupportMap, toyStats.EventMap
		};
		foreach(var map in maps) {
			if(map == null)
				continue;
			foreach(var item in map) {
				foreach(var other in item.Value) {
					if(other == null || other == toy)
						continue;
					foreach(var renderer in other.GetComponentsInChildren<Renderer>()) {
						if(IsMeshRenderer(renderer))
							othersMinZ = Mathf.Min(othersMinZ, renderer.bounds.min.z);
					}
				}
			}
		}
		var toyMaxZ = float.MinValue;
		foreach(var renderer in toy.GetComponentsInChildren<Renderer>()) {
			if(IsMeshRenderer(renderer))
				toyMaxZ = Mathf.Max(toyMaxZ, renderer.bounds.max.z);
		}
		if(othersMinZ == float.MaxValue || toyMaxZ == float.MinValue)
			return;
		var lift = toyMaxZ - othersMinZ + GAP;
		if(lift <= 0f)
			return;
		var pos = toy.mainRigid.position;
		pos.z -= lift;
		toy.mainRigid.position = pos;
		toy.transform.position = pos;
	}

	static bool IsMeshRenderer(Renderer renderer) {
		return renderer is MeshRenderer || renderer is SkinnedMeshRenderer;
	}

	public static Vector3 HandPositionFor(ToyView toy, float offsetToCamera) {
		var pos = toy.mainCollider != null ? toy.mainCollider.bounds.center : toy.transform.position;
		pos.z -= offsetToCamera;
		return pos;
	}
}
