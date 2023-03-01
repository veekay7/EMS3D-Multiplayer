using System.Collections.Generic;
using UnityEngine;

namespace E3D
{
    [RequireComponent(typeof(SphereCollider))]
    public class APlacementArea : MonoBehaviour
    {
        public static List<APlacementArea> placementAreas = new List<APlacementArea>();

        public GameObject m_ViewModel;
        [HideInInspector]
        public SphereCollider m_Collider;

        public float m_Radius;


        private void Start()
        {
            SetVisible(false);

            if (!placementAreas.Contains(this))
                placementAreas.Add(this);
        }

        private void Reset()
        {
            m_Radius = 1.0f;
        }

        private void Update()
        {
            m_ViewModel.transform.localScale = new Vector3(1.0f + m_Radius, 1.0f + m_Radius, 1.0f + m_Radius);
        }

        public void SetVisible(bool value)
        {
            m_ViewModel.SetActive(value);
        }

        private void OnDrawGizmos()
        {
            Color oldColor = Gizmos.color;
            Gizmos.color = Color.cyan;

            Matrix4x4 originalMtx = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;

            Gizmos.DrawWireSphere(Vector3.zero, m_Radius);

            Gizmos.matrix = originalMtx;
            Gizmos.color = oldColor;

            // draw icon too
            Gizmos.DrawIcon(transform.position + Vector3.up, "build_zone.png", true);
        }

        private void OnDestroy()
        {
            if (placementAreas.Contains(this))
                placementAreas.Remove(this);
        }

        private void OnValidate()
        {
            m_Collider = gameObject.GetOrAddComponent<SphereCollider>();

            if (m_Radius <= 0.0f)
                m_Radius = 0.1f;

            if (m_Collider != null)
                m_Collider.radius = m_Radius;
        }
    }
}
