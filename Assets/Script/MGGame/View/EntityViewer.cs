#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using XN;

public class EntityViewer : MonoBehaviour
{
    [ShowInInspector]
    [HideReferenceObjectPicker]
    [ReadOnly]
    public Entity Entity { get; set; }

    [ShowInInspector]
    [LabelText("Components List")]
    [ListDrawerSettings(IsReadOnly = true, Expanded = true, ListElementLabelName = "@this.GetType().Name")]
    [Obsolete("Obsolete")]
    public List<IComponent> ComponentList
    {
        get
        {
            if (Entity == null || Entity.IsDispose) return null;
            var comps = Entity.GetAllComponents();
            if (comps == null) return null;
            return new List<IComponent>(comps.Values);
        }
    }

    private void Awake()
    {
        if (Entity == null || Entity.IsDispose) return;

        if (Entity.GetComponent<PlayerInfoComponent>(out var playerInfo))
        {
            name = $"Entity_{Entity.Tag}_{Entity.Id}_{playerInfo.Name}";
            return;
        }

        name = $"Entity_{Entity.Tag}_{Entity.Id}";
    }

    public void UpdateInfo()
    {
        if (Entity == null || Entity.IsDispose) return;

        // Update Parent if needed
        var logicalParent = Entity.GetParent();
        if (logicalParent != null)
        {
            var parentGo = EntityManager.Instance.GetEntityViewer(logicalParent.Id);
            if (parentGo != null && transform.parent != parentGo.transform)
            {
                transform.SetParent(parentGo.transform);
            }
        }
        else if (transform.parent != EntityManager.Instance.EntityRoot)
        {
            transform.SetParent(EntityManager.Instance.EntityRoot);
        }
    }
}

#endif