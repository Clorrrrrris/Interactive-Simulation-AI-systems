using UnityEngine;
using System.Collections.Generic;

public class BallCollider : MonoBehaviour
{
    private Transform[] walls = new Transform[0];
    private Transform[] cylinders = new Transform[0];
    private PaddleController[] paddles = new PaddleController[0];
    private Transform[] prisms = new Transform[0];
    private static List<BallCollider> balls = new List<BallCollider>();

    public float ballRadius;
    public float detectionRadius;
    public Vector3 velocity;
    
    public float wallEnergyLoss; // 0.1f lose energy
    public float cylinderBoost; // 0.2f gain energy
    public float prismEnergyLoss; // 0.2f lose energy

    void Start()
    {
        ballRadius = 0.5f * transform.localScale.x;
        balls.Add(this);

        GameObject[] wallObjs = GameObject.FindGameObjectsWithTag("Wall");
        walls = new Transform[wallObjs.Length];
        for (int i = 0; i < wallObjs.Length; i++)
            walls[i] = wallObjs[i].transform;

        GameObject[] paddleObjs = GameObject.FindGameObjectsWithTag("Paddle");
        paddles = new PaddleController[paddleObjs.Length];
        for (int i = 0; i < paddleObjs.Length; i++)
            paddles[i] = paddleObjs[i].GetComponent<PaddleController>();

        GameObject[] cylObjs = GameObject.FindGameObjectsWithTag("Cylinder");
        cylinders = new Transform[cylObjs.Length];
        for (int i = 0; i < cylObjs.Length; i++)
            cylinders[i] = cylObjs[i].transform;

        GameObject[] prismObjs = GameObject.FindGameObjectsWithTag("Prism");
        prisms = new Transform[prismObjs.Length];
        for (int i = 0; i < prismObjs.Length; i++)
            prisms[i] = prismObjs[i].transform;
    }

    public Vector3 ResolveCollisions(Vector3 pos, Vector3 velocity)
    {
        Vector2 pos2D = new Vector2(pos.x, pos.z);

        float speed = new Vector2(velocity.x, velocity.z).magnitude;
        float dynamicBuffer = Mathf.Clamp(speed * 0.1f, 0.5f, 3.0f); // buffer larger if speed larger
        this.detectionRadius = ballRadius + dynamicBuffer;

        HashSet<Transform> processed = new HashSet<Transform>(); // avoid collide with the same obstacle

        //Debug.Log($"=== Resolve collisions ===");
        //Debug.Log($"Ball position: {pos2D}, Velocity: {velocity}, Radius: {ballRadius}");

        // ball collide with wall
        foreach (var wall in walls)
        {
            if (processed.Contains(wall)) continue;

            float dist = GetDistanceToRectangle(pos2D, wall);
            //Debug.Log($"Wall detected - Pos: {wall.position}, Dis: {dist:F4}, Radius: {ballRadius}, If collided: {dist < ballRadius}");
            if (dist < detectionRadius)
            {
                velocity = ReflectFromRectangle(ref pos2D, velocity, wall);
                
                Vector3 wallCenter = wall.position;
                Vector2 wallNormal = (pos2D - new Vector2(wallCenter.x, wallCenter.z)).normalized;
                pos2D += wallNormal * (detectionRadius - dist + 0.01f);

                processed.Add(wall);
            }
        }

        // ball collide with cylinder
        foreach (var cyl in cylinders)
        {
            if (processed.Contains(cyl)) continue;

            float r = cyl.localScale.x * 0.5f;
            if (CheckCircleCircle(pos2D, ballRadius, new Vector2(cyl.position.x, cyl.position.z), r))
            {
                float actualFactor = 1f + cylinderBoost; // collision with a cylinder should add energy to the bounce

                if (cyl.name.ToLower().Contains("pivot"))
                {
                    actualFactor = 1f - wallEnergyLoss; // do not add boost if the cylinder is pivot, instead, result in a bounce with a little energy loss
                }
                velocity = ReflectFromCylinder(pos2D, new Vector2(cyl.position.x, cyl.position.z), velocity, actualFactor);

                processed.Add(cyl);
            }
        }

        // ball collide with ball
        foreach (var other in balls)
        {
            if (other == this) continue; // skip ball itselves
            Vector2 otherPos = new Vector2(other.transform.position.x, other.transform.position.z);

            if (CheckCircleCircle(pos2D, detectionRadius, otherPos, ballRadius))
            {
                Vector3 velA = velocity;
                Vector3 velB = other.velocity;

                ReflectFromBall(pos2D, ref velA, otherPos, ref velB, 0.9f);

                velocity = velA;
                other.SetVelocity(velB);

                Vector2 collisionNormal = (pos2D - otherPos).normalized;
                float overlap = detectionRadius + ballRadius - Vector2.Distance(pos2D, otherPos);
                
                if (overlap > 0.01f)
                {
                    Vector2 separation = collisionNormal * (overlap * 0.5f + 0.01f);
                    pos2D += separation;
                    Vector2 otherNewPos = otherPos - separation;
                    other.transform.position = new Vector3(otherNewPos.x, other.transform.position.y, otherNewPos.y);
                }
            }
        }

        // ball collide with prism
        foreach (var prism in prisms)
        {
            if (processed.Contains(prism)) continue;

            MeshFilter mf = prism.GetComponent<MeshFilter>();
            if (mf == null) continue;
            Mesh mesh = mf.mesh;
            if (mesh.vertexCount < 3) continue;

            // transform to world axis
            Vector3 v1 = prism.TransformPoint(mesh.vertices[0]);
            Vector3 v2 = prism.TransformPoint(mesh.vertices[1]);
            Vector3 v3 = prism.TransformPoint(mesh.vertices[2]);
            Vector2 v1_2D = new Vector2(v1.x, v1.z);
            Vector2 v2_2D = new Vector2(v2.x, v2.z);
            Vector2 v3_2D = new Vector2(v3.x, v3.z);

            float dist = GetDistanceToTriangle(pos2D, v1_2D, v2_2D, v3_2D);
            //Debug.Log($"Prism detected - Point: [{v1_2D}, {v2_2D}, {v3_2D}], Distance: {dist:F4}, If collided: {dist < ballRadius}");
            if (dist < detectionRadius)
            {
                velocity = ReflectFromPrism(ref pos2D, velocity, v1_2D, v2_2D, v3_2D);
                Vector2 closestPoint = CalculateClosestPointOnTriangle(pos2D, v1_2D, v2_2D, v3_2D);
                
                Vector2 normal = (pos2D - closestPoint).normalized;
                normal = EnsureCorrectNormal(pos2D, closestPoint, normal);
                pos2D += normal * (detectionRadius - dist + 0.01f);

                processed.Add(prism);
            }
        }

        // ball collide with paddle
        foreach (var paddle in paddles)
        {
            if (processed.Contains(paddle.transform)) continue;

            MeshFilter mf = paddle.GetComponent<MeshFilter>();
            if (mf == null) continue;
            Mesh mesh = mf.mesh;
            if (mesh.vertexCount < 3) continue;

            Vector3 v1 = paddle.transform.TransformPoint(mesh.vertices[0]);
            Vector3 v2 = paddle.transform.TransformPoint(mesh.vertices[1]);
            Vector3 v3 = paddle.transform.TransformPoint(mesh.vertices[2]);
            Vector2 v1_2D = new Vector2(v1.x, v1.z);
            Vector2 v2_2D = new Vector2(v2.x, v2.z);
            Vector2 v3_2D = new Vector2(v3.x, v3.z);

            float dist = GetDistanceToTriangle(pos2D, v1_2D, v2_2D, v3_2D);
            //Debug.Log($"Paddle detected - Point: [{v1_2D}, {v2_2D}, {v3_2D}], Distance: {dist:F4}, If collided: {dist < ballRadius}");

            if (dist < detectionRadius)
            {
                velocity = ReflectFromPaddle(ref pos2D, velocity, v1_2D, v2_2D, v3_2D, paddle);
                Vector2 closestPoint = CalculateClosestPointOnTriangle(pos2D, v1_2D, v2_2D, v3_2D);
                Vector2 normal = (pos2D - closestPoint).normalized;
                normal = EnsureCorrectNormal(pos2D, closestPoint, normal);
                pos2D += normal * (detectionRadius - dist + 0.005f);

                processed.Add(paddle.transform);
            }
        }
        //Debug.Log($"=== Resolve collision finished - Final pos: {pos2D}, Final vel: {velocity} ===");

        pos.x = pos2D.x;
        pos.z = pos2D.y;
        return velocity;
    }

    // GET DISTANCES
    float GetDistanceToRectangle(Vector2 c, Transform rect)
    {
        Vector3 center = rect.position;
        Vector3 right = rect.right * rect.localScale.x * 0.5f;
        Vector3 forward = rect.forward * rect.localScale.z * 0.5f;

        Vector3 v1 = center - right - forward;
        Vector3 v2 = center + right - forward;
        Vector3 v3 = center + right + forward;
        Vector3 v4 = center - right + forward;

        Vector2[] verts = {
            new Vector2(v1.x, v1.z),
            new Vector2(v2.x, v2.z),
            new Vector2(v3.x, v3.z),
            new Vector2(v4.x, v4.z)
        };

        float minDist = float.MaxValue;
        for (int i = 0; i < 4; i++)
        {
            Vector2 a = verts[i];
            Vector2 b = verts[(i + 1) % 4];
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(c - a, ab) / ab.sqrMagnitude);
            Vector2 closest = a + t * ab;
            float dist = (c - closest).magnitude;
            if (dist < minDist) minDist = dist;
        }
        return minDist;
    }

    float GetDistanceToTriangle(Vector2 c, Vector2 v1, Vector2 v2, Vector2 v3)
    {
        Vector2[] verts = { v1, v2, v3 };
        float minDist = float.MaxValue;
        for (int i = 0; i < 3; i++)
        {
            Vector2 a = verts[i];
            Vector2 b = verts[(i + 1) % 3];
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(c - a, ab) / ab.sqrMagnitude);
            Vector2 closest = a + t * ab;
            float dist = (c - closest).magnitude;
            if (dist < minDist) minDist = dist;
        }
        return minDist;
    }



    // DETECT COLLISION
    bool CheckCircleCircle(Vector2 c1, float r1, Vector2 c2, float r2)
    {
        return (c1 - c2).sqrMagnitude <= (r1 + r2) * (r1 + r2);
    }

    // REFLECT COLLISION
    Vector3 ReflectFromPolygonEdge(ref Vector2 pos2D, Vector3 vel, Vector2 a, Vector2 b, float energyLoss)
    {
        // center to closest point on edge
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(pos2D - a, ab) / ab.sqrMagnitude);
        Vector2 closest = a + t * ab;

        Vector2 diff = pos2D - closest;
        Vector2 normal = diff.sqrMagnitude < 1e-8f ? 
            new Vector2(-(b.y - a.y), b.x - a.x).normalized : 
            diff.normalized;
        normal = EnsureCorrectNormal(pos2D, closest, normal);
        //Debug.Log($"RefelctFromPolygonEdge - Closest: {closest}, Normal: {normal}, Distance: {diff.magnitude}");

        float penetration = ballRadius - diff.magnitude;
        if (penetration > 0)
        {
            pos2D = closest + normal * (ballRadius + 0.01f);
        }
        // reflect
        Vector2 vel2D = new Vector2(vel.x, vel.z);
        vel2D = vel2D - 2 * Vector2.Dot(vel2D, normal) * normal;
        vel2D *= (1f - energyLoss);

        vel.x = vel2D.x;
        vel.z = vel2D.y;
        return vel;
    }

    Vector3 ReflectFromRectangle(ref Vector2 pos2D, Vector3 vel, Transform rect)
    {
        Vector3 center = rect.position;
        Vector3 right = rect.right * rect.localScale.x * 0.5f;
        Vector3 forward = rect.forward * rect.localScale.z * 0.5f;

        Vector3 v1 = center - right - forward;
        Vector3 v2 = center + right - forward;
        Vector3 v3 = center + right + forward;
        Vector3 v4 = center - right + forward;

        Vector2[] verts = {
            new Vector2(v1.x, v1.z),
            new Vector2(v2.x, v2.z),
            new Vector2(v3.x, v3.z),
            new Vector2(v4.x, v4.z)
        };

        float minDist = float.MaxValue;

        Vector2 bestA = Vector2.zero, bestB = Vector2.zero;
        Vector2 closest = pos2D;

        for (int i = 0; i < 4; i++)
        {
            Vector2 a = verts[i];
            Vector2 b = verts[(i + 1) % 4];

            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(pos2D - a, ab) / ab.sqrMagnitude);
            Vector2 cand = a + t * ab;
            float dist = (pos2D - cand).sqrMagnitude;

            if (dist < minDist)
            {
                minDist = dist;
                bestA = a;
                bestB = b;
                closest = cand;
            }
        }

        if (minDist < detectionRadius * detectionRadius)
        {
            Vector2 normal = (closest - pos2D).normalized;
            normal = EnsureCorrectNormal(pos2D, closest, normal);
            return ReflectFromPolygonEdge(ref pos2D, vel, bestA, bestB, wallEnergyLoss);
        }

        return vel;
    }

    Vector3 ReflectFromCylinder(Vector2 c, Vector2 target, Vector3 vel, float factor)
    {
        Vector2 normal = (c - target).normalized;
        Vector2 vel2D = new Vector2(vel.x, vel.z);
        vel2D = vel2D - 2 * Vector2.Dot(vel2D, normal) * normal;
        vel2D *= factor;

        vel.x = vel2D.x;
        vel.z = vel2D.y;

        return vel;
    }

    void ReflectFromBall(Vector2 posA, ref Vector3 velA, Vector2 posB, ref Vector3 velB, float elasticity = 1f)
    {
        Vector2 normal = (posA - posB).normalized;

        Vector2 velA2D = new Vector2(velA.x, velA.z);
        Vector2 velB2D = new Vector2(velB.x, velB.z);

        float va = Vector2.Dot(velA2D, normal);
        float vb = Vector2.Dot(velB2D, normal);

        float vaAfter = vb * elasticity;
        float vbAfter = va * elasticity;

        velA2D += (vaAfter - va) * normal;
        velB2D += (vbAfter - vb) * normal;

        velA.x = velA2D.x;
        velA.z = velA2D.y;
        velB.x = velB2D.x;
        velB.z = velB2D.y;
    }

    Vector3 ReflectFromPrism(ref Vector2 pos2D, Vector3 vel, Vector2 v1, Vector2 v2, Vector2 v3)
    {
        // find closest edge
        Vector2[] verts = { v1, v2, v3 };
        float minDist = float.MaxValue;
        Vector2 bestA = Vector2.zero, bestB = Vector2.zero;
        Vector2 closest = pos2D;
        bool isNearVertex = false;
        Vector2 nearestVertex = Vector2.zero;

        for (int i = 0; i < 3; i++)
        {
            Vector2 a = verts[i];
            Vector2 b = verts[(i + 1) % 3];

            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(pos2D - a, ab) / ab.sqrMagnitude);
            Vector2 cand = a + t * ab;
            float dist = (pos2D - cand).sqrMagnitude;

            if (dist < minDist)
            {
                minDist = dist;
                bestA = a;
                bestB = b;
                closest = cand;
            }
        }

        // check if close to vertex
        float vertexThreshold = ballRadius * 0.8f; 
        for (int i = 0; i < 3; i++)
        {
            float vertexDist = Vector2.Distance(pos2D, verts[i]);
            if (vertexDist < vertexThreshold)
            {
                isNearVertex = true;
                nearestVertex = verts[i];
                minDist = vertexDist;
                break;
            }
        }

        if (minDist < detectionRadius * detectionRadius)
        {
            Vector2 normal;
            if (isNearVertex)
            {
                normal = CalculateVertexNormal(pos2D, nearestVertex, v1, v2, v3);
            }
            else
            {
                normal = (closest - pos2D).normalized;
            }
            normal = EnsureCorrectNormal(pos2D, isNearVertex ? nearestVertex : closest, normal);
            return ReflectFromPolygonEdge(ref pos2D, vel, bestA, bestB, prismEnergyLoss);
        }

        return vel;
    }


    Vector3 ReflectFromPaddle(ref Vector2 pos2D, Vector3 vel, Vector2 v1, Vector2 v2, Vector2 v3, PaddleController paddle)
    {
        Vector2[] verts = { v1, v2, v3 };
        float minDist = float.MaxValue;
        Vector2 bestA = Vector2.zero, bestB = Vector2.zero;
        Vector2 closest = pos2D;
        bool isNearVertex = false;
        Vector2 nearestVertex = Vector2.zero;

        for (int i = 0; i < 3; i++)
        {
            Vector2 a = verts[i];
            Vector2 b = verts[(i + 1) % 3];

            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(pos2D - a, ab) / ab.sqrMagnitude);
            Vector2 cand = a + t * ab;
            float dist = (pos2D - cand).sqrMagnitude;

            if (dist < minDist)
            {
                minDist = dist;
                bestA = a;
                bestB = b;
                closest = cand;
            }
        }

        // check if close to vertex
        float vertexThreshold = ballRadius * 0.8f;
        for (int i = 0; i < 3; i++)
        {
            float vertexDist = Vector2.Distance(pos2D, verts[i]);
            if (vertexDist < vertexThreshold)
            {
                isNearVertex = true;
                nearestVertex = verts[i];
                minDist = vertexDist;
                break;
            }
        }

        if (minDist < detectionRadius * detectionRadius)
        {
            Vector3 result = ReflectFromPolygonEdge(ref pos2D, vel, bestA, bestB, prismEnergyLoss);

            // if paddle is in motion
            if (Mathf.Abs(paddle.GetSpeed()) > 0.01f) 
            {
                Vector2 normal;
                if (isNearVertex)
                {
                    normal = CalculateVertexNormal(pos2D, nearestVertex, v1, v2, v3);
                }
                else
                {
                    normal = (pos2D - closest).normalized;
                }

                normal = EnsureCorrectNormal(pos2D, isNearVertex ? nearestVertex : closest, normal);
                Vector2 accelerationNormal = paddle.GetSpeed() > 0 ? normal : -normal;

                Vector2 vel2D = new Vector2(result.x, result.z);
                if (vel2D.y < 0)
                {
                    vel2D.y = -Mathf.Abs(vel2D.y); 
                }

                float accelerationAmount = Mathf.Abs(paddle.GetSpeed()) * 0.3f;
                vel2D -= accelerationNormal * accelerationAmount;
                //Debug.Log($"Paddle moving - Normal: {accelerationNormal}, Acceleration: {accelerationAmount}, Velocity change: {new Vector2(result.x, result.z)} -> {vel2D}");

                result.x = vel2D.x;
                result.z = vel2D.y;
            }
            return result;
        }

        return vel;
    }

    Vector2 CalculateClosestPointOnTriangle(Vector2 point, Vector2 v1, Vector2 v2, Vector2 v3)
    {
        Vector2[] verts = { v1, v2, v3 };
        float minDist = float.MaxValue;
        Vector2 closestPoint = point;

        for (int i = 0; i < 3; i++)
        {
            Vector2 a = verts[i];
            Vector2 b = verts[(i + 1) % 3];
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / ab.sqrMagnitude);
            Vector2 cand = a + t * ab;
            float dist = (point - cand).sqrMagnitude;

            if (dist < minDist)
            {
                minDist = dist;
                closestPoint = cand;
            }
        }
        return closestPoint;
    }

    // NORMAL RELATED
    Vector2 EnsureCorrectNormal(Vector2 ballPos, Vector2 collisionPoint, Vector2 proposedNormal)
    {
        Vector2 toBall = ballPos - collisionPoint;
        float dot = Vector2.Dot(proposedNormal, toBall);
        
        if (dot < 0)
        {
            return -proposedNormal;
        }
        return proposedNormal;
    }

    Vector2 CalculateVertexNormal(Vector2 ballPos, Vector2 vertex, Vector2 v1, Vector2 v2, Vector2 v3)
    {
        Vector2 triangleCenter = (v1 + v2 + v3) / 3f;
        
        int vertexIndex = -1;
        Vector2[] verts = { v1, v2, v3 };
        for (int i = 0; i < 3; i++)
        {
            if (verts[i] == vertex)
            {
                vertexIndex = i;
                break;
            }
        }
        
        Vector2 prevVertex = verts[(vertexIndex + 2) % 3];
        Vector2 nextVertex = verts[(vertexIndex + 1) % 3];

        Vector2 edge1 = vertex - prevVertex;
        Vector2 edge2 = nextVertex - vertex;
        
        Vector2 normal1 = new Vector2(-edge1.y, edge1.x).normalized;
        Vector2 normal2 = new Vector2(-edge2.y, edge2.x).normalized;
        
        // make sure normal point out of triangle
        Vector2 edge1Center = (vertex + prevVertex) / 2f;
        Vector2 edge2Center = (vertex + nextVertex) / 2f;
        
        if (Vector2.Dot(normal1, triangleCenter - edge1Center) > 0)
            normal1 = -normal1;
        if (Vector2.Dot(normal2, triangleCenter - edge2Center) > 0)
            normal2 = -normal2;
        
        return (normal1 + normal2).normalized;
    }


    void OnDestroy()
    {
        balls.Remove(this);
    }
    
    public void SetVelocity(Vector3 v)
    {
        velocity = v;
    }
}