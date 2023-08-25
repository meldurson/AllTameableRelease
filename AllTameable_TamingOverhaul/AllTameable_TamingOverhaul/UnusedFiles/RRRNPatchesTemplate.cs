using System.Linq;
using RRRCore;
using RRRCore.prefabs._0_2_0;
using UnityEngine;

public class RRRMobCustomization : MonoBehaviour
{
	public Humanoid humanoid;

	public MonsterAI monsterAI;


	public string MobDataNewPrefabName;

	public string FullPrefabName;

	public void SetupTameable()
	{
		if (!base.gameObject.TryGetComponent<Tameable>(out var tameable))
		{
			tameable = base.gameObject.AddComponent<Tameable>();
		}
		//Tameable tameable = base.gameObject.AddComponent<Tameable>();
		GameObject prefab = ZNetScene.instance.GetPrefab("Wolf");
		Tameable component = prefab.GetComponent<Tameable>();
		tameable.m_fedDuration = component.m_fedDuration;

	}

}