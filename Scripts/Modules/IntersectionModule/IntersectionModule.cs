#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Data;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Modules
{
    sealed class IntersectionModule : MonoBehaviour, IUsesGameObjectLocking
    {
        const int k_MaxTestsPerTester = 250;

        readonly Dictionary<IntersectionTester, Renderer> m_IntersectedObjects = new Dictionary<IntersectionTester, Renderer>();
        readonly List<IntersectionTester> m_Testers = new List<IntersectionTester>();
        readonly Dictionary<Transform, RayIntersection> m_RaycastGameObjects = new Dictionary<Transform, RayIntersection>(); // Stores which gameobject the proxies' ray origins are pointing at

        SpatialHash<Renderer> m_SpatialHash;
        MeshCollider m_CollisionTester;

        class RayIntersection
        {
            public GameObject go;
            public float distance;
        }

        public bool ready { get { return m_SpatialHash != null; } }

        public List<IntersectionTester> testers { get { return m_Testers; } }

        public List<Renderer> allObjects { get { return m_SpatialHash == null ? null : m_SpatialHash.allObjects; } }

        public int intersectedObjectCount { get { return m_IntersectedObjects.Count; } }

        // Local method use only -- created here to reduce garbage collection
        readonly List<Renderer> m_Intersections = new List<Renderer>();
        readonly List<SortableRenderer> m_SortedIntersections = new List<SortableRenderer>();

        struct SortableRenderer
        {
            public Renderer renderer;
            public float distance;
        }

        void Awake()
        {
            IntersectionUtils.BakedMesh = new Mesh(); // Create a new Mesh in each Awake because it is destroyed on scene load

            IRaycastMethods.raycast = Raycast;
        }

        internal void Setup(SpatialHash<Renderer> hash)
        {
            m_SpatialHash = hash;
            m_CollisionTester = ObjectUtils.CreateGameObjectWithComponent<MeshCollider>(transform);
        }

        void Update()
        {
            if (m_SpatialHash == null)
                return;

            if (m_Testers == null)
                return;

            for (int i = 0; i < m_Testers.Count; i++)
            {
                var tester = m_Testers[i];
                if (!tester.active)
                {
                    Renderer intersectedObject;
                    if (m_IntersectedObjects.TryGetValue(tester, out intersectedObject))
                        OnIntersectionExit(tester);

                    continue;
                }

                var testerTransform = tester.transform;
                if (testerTransform.hasChanged)
                {
                    var intersectionFound = false;
                    m_Intersections.Clear();
                    var testerCollider = tester.collider;
                    if (m_SpatialHash.GetIntersections(m_Intersections, testerCollider.bounds))
                    {
                        var testerBounds = testerCollider.bounds;
                        var testerBoundsCenter = testerBounds.center;

                        m_SortedIntersections.Clear();
                        for (int j = 0; j < m_Intersections.Count; j++)
                        {
                            var obj = m_Intersections[j];

                            // Ignore destroyed objects
                            if (!obj)
                                continue;

                            // Ignore inactive objects
                            if (!obj.gameObject.activeInHierarchy)
                                continue;

                            // Ignore locked objects
                            if (this.IsLocked(obj.gameObject))
                                continue;

                            // Bounds check
                            if (!obj.bounds.Intersects(testerBounds))
                                continue;

                            m_SortedIntersections.Add(new SortableRenderer
                            {
                                renderer = obj,
                                distance = (obj.bounds.center - testerBoundsCenter).magnitude
                            });
                        }

                        //Sort list to try and hit closer object first
                        m_SortedIntersections.Sort((a, b) => a.distance.CompareTo(b.distance));

                        if (m_SortedIntersections.Count > k_MaxTestsPerTester)
                            continue;

                        for (int j = 0; j < m_SortedIntersections.Count; j++)
                        {
                            var obj = m_SortedIntersections[j].renderer;
                            if (IntersectionUtils.TestObject(m_CollisionTester, obj, tester))
                            {
                                intersectionFound = true;
                                Renderer currentObject;
                                if (m_IntersectedObjects.TryGetValue(tester, out currentObject))
                                {
                                    if (currentObject == obj)
                                    {
                                        OnIntersectionStay(tester, obj);
                                    }
                                    else
                                    {
                                        OnIntersectionExit(tester);
                                        OnIntersectionEnter(tester, obj);
                                    }
                                }
                                else
                                {
                                    OnIntersectionEnter(tester, obj);
                                }
                            }

                            if (intersectionFound)
                                break;
                        }
                    }

                    if (!intersectionFound)
                    {
                        Renderer intersectedObject;
                        if (m_IntersectedObjects.TryGetValue(tester, out intersectedObject))
                            OnIntersectionExit(tester);
                    }

                    testerTransform.hasChanged = false;
                }
            }
        }

        internal void AddTester(IntersectionTester tester)
        {
            m_IntersectedObjects.Clear();
            m_Testers.Add(tester);
        }

        void OnIntersectionEnter(IntersectionTester tester, Renderer obj)
        {
            m_IntersectedObjects[tester] = obj;
        }

        void OnIntersectionStay(IntersectionTester tester, Renderer obj)
        {
            m_IntersectedObjects[tester] = obj;
        }

        void OnIntersectionExit(IntersectionTester tester)
        {
            m_IntersectedObjects.Remove(tester);
        }

        internal Renderer GetIntersectedObjectForTester(IntersectionTester tester)
        {
            Renderer obj = null;
            if (tester)
                m_IntersectedObjects.TryGetValue(tester, out obj);

            return obj;
        }

        internal GameObject GetFirstGameObject(Transform rayOrigin, out float distance)
        {
            RayIntersection intersection;
            if (m_RaycastGameObjects.TryGetValue(rayOrigin, out intersection))
            {
                distance = intersection.distance;
                return intersection.go;
            }

            distance = 0;
            return null;
        }

        internal void UpdateRaycast(Transform rayOrigin, float distance)
        {
            GameObject go;
            RaycastHit hit;
            Raycast(new Ray(rayOrigin.position, rayOrigin.forward), out hit, out go, distance);
            m_RaycastGameObjects[rayOrigin] = new RayIntersection { go = go, distance = hit.distance };
        }

        internal bool Raycast(Ray ray, out RaycastHit hit, out GameObject obj, float maxDistance = Mathf.Infinity, List<Renderer> ignoreList = null)
        {
            obj = null;
            hit = new RaycastHit();
            var result = false;
            var distance = Mathf.Infinity;
            m_Intersections.Clear();
            if (m_SpatialHash.GetIntersections(m_Intersections, ray, maxDistance))
            {
                for (int i = 0; i < m_Intersections.Count; i++)
                {
                    var renderer = m_Intersections[i];
                    if (ignoreList != null && ignoreList.Contains(renderer))
                        continue;

                    var transform = renderer.transform;

                    IntersectionUtils.SetupCollisionTester(m_CollisionTester, transform);

                    RaycastHit tmp;
                    if (IntersectionUtils.TestRay(m_CollisionTester, transform, ray, out tmp, maxDistance))
                    {
                        var point = transform.TransformPoint(tmp.point);
                        var dist = Vector3.Distance(point, ray.origin);
                        if (dist < distance)
                        {
                            result = true;
                            distance = dist;
                            hit.distance = dist;
                            hit.point = point;
                            hit.normal = transform.TransformDirection(tmp.normal);
                            obj = renderer.gameObject;
                        }
                    }
                }
            }

            return result;
        }
    }
}
#endif
