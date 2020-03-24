using System.Collections.Generic ;
using UnityEditor ;
using UnityEngine;
using UnityEngine.AI ;

namespace Wrld.Streaming
{
    class GameObjectFactory
    {
        private Transform m_parentTransform;

        public GameObjectFactory(Transform parentTransform)
        {
            m_parentTransform = parentTransform;
        }

        private static string CreateGameObjectName(string baseName, int meshIndex)
        {
            return string.Format("{0}_INDEX{1}", baseName, meshIndex);
        }

        private GameObject CreateGameObject(Mesh mesh, Material material, string objectName, Transform parentTransform, CollisionStreamingType collisionType)
        {
            var gameObject = new GameObject(objectName);
            List<Vector3> spawnpos ;
            // for traffic simulation, set tag for include roads to navmesh bake calculation.
            /*if (objectName.Substring(0 , 6) == "Roads_")
            {
                GameObjectUtility.SetStaticEditorFlags(gameObject,StaticEditorFlags.NavigationStatic);
                gameObject.AddComponent<NavMeshSourceTag>() ;
            }
            */
            gameObject.SetActive(false);
            gameObject.transform.SetParent(parentTransform, false);

            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;

            if (objectName.ToLower().Contains("interior"))
            {
                // Making a copy of the indoor material at this point as each indoor renderable has a separate material
                // state.  This is updated during the render loop for non-unity platforms, but for unity we need our
                // materials to be immutable at render time.
                gameObject.AddComponent<MeshRenderer>().sharedMaterial = new Material(material);
            }
            else
            {
                gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;
            }

            switch (collisionType)
            {
                case CollisionStreamingType.SingleSidedCollision:
                {
                    gameObject.AddComponent<MeshCollider>().sharedMesh = mesh;
                    break;
                }
                case CollisionStreamingType.DoubleSidedCollision:
                {
                    gameObject.AddComponent<MeshCollider>().sharedMesh = CreateDoubleSidedCollisionMesh(mesh);
                    break;
                }
            }
            // UV to World Position translation and collecting positions
            if (objectName.Substring(0 , 6) == "Roads_")
            {
                spawnpos = UvTo3D(new Vector2(0.5f , 0.5f) , gameObject.GetComponent<MeshFilter>().mesh , gameObject.transform) ;
                SpawnPositionCollector.PushNewPositions(spawnpos);
            }
            return gameObject;
        }

        public GameObject[] CreateGameObjects(Mesh[] meshes, Material material, Transform parentTransform, CollisionStreamingType collisionType)
        {
            var gameObjects = new GameObject[meshes.Length];

            for (int meshIndex = 0; meshIndex < meshes.Length; ++meshIndex)
            {
                var name = CreateGameObjectName(meshes[meshIndex].name, meshIndex);
                gameObjects[meshIndex] = CreateGameObject(meshes[meshIndex], material, name, parentTransform, collisionType);
            }
            return gameObjects;
        }


        private static Mesh CreateDoubleSidedCollisionMesh(Mesh sourceMesh)
        {
            Mesh mesh = new Mesh();
            mesh.name = sourceMesh.name;
            mesh.vertices = sourceMesh.vertices;

            int[] sourceTriangles = sourceMesh.triangles;
            int triangleCount = sourceTriangles.Length;
            int[] triangles = new int[triangleCount * 2];

            for (int index=0; index<triangleCount; index += 3)
            {
                // Copy all source triangles into first half of array
                triangles[index+0] = sourceTriangles[index+0];
                triangles[index+1] = sourceTriangles[index+1];
                triangles[index+2] = sourceTriangles[index+2];

                // Insert flipped triangles into second half of array
                triangles[triangleCount + index + 0] = sourceTriangles[index+0];
                triangles[triangleCount + index + 1] = sourceTriangles[index+2];
                triangles[triangleCount + index + 2] = sourceTriangles[index+1];
            }

            mesh.triangles = triangles;
            return mesh;
        }
        
        private List<Vector3> UvTo3D(Vector2 UvPos, Mesh mesh , Transform transform)
        {
            int[] tris = mesh.triangles ;
            Vector2[] uvs = mesh.uv ;
            Vector3[] verts = mesh.vertices ;
            List<Vector3> positions = new List<Vector3>();
            for (int i = 0; i < tris.Length; i += 3)
            {
                Vector2 u1 = uvs[tris[i]]; // get the triangle UVs
                Vector2 u2 = uvs[tris[i+1]];
                Vector2 u3 = uvs[tris[i+2]];
                // calculate triangle area - if zero, skip it
                float a = Area(u1, u2, u3);
                if (a >= -0.00001f && a <= 0.00001f)
                {
                    continue;
                }
                // calculate barycentric coordinates of u1, u2 and u3
                // if anyone is negative, point is outside the triangle: skip it
                float a1 = Area(u2, u3, UvPos)/a; if (a1 < 0) continue;
                float a2 = Area(u3, u1, UvPos)/a; if (a2 < 0) continue;
                float a3 = Area(u1, u2, UvPos)/a; if (a3 < 0) continue;
                // point inside the triangle - find mesh position by interpolation...
                Vector3 p3D = a1*verts[tris[i]]+a2*verts[tris[i+1]]+a3*verts[tris[i+2]];
                // and push it to positions list in world coordinates:
                positions.Add(transform.TransformPoint(p3D)) ;
            }
            return positions ;
        }

        private float Area(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            Vector2 v1 = p1 - p3 ;
            Vector2 v2 = p2 - p3 ;
            return (v1.x * v2.y - v1.y * v2.x)/2 ;
        }
    }
}
